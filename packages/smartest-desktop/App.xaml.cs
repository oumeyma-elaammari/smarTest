using smartest_desktop.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using smartest_desktop.Services;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Fonts;

namespace smartest_desktop
{
    public partial class App : Application
    {
        /// <summary>
        /// PDFsharp 6 (build Core) : résolution des polices système Windows (Arial, etc.).
        /// À exécuter avant toute création de <see cref="PdfSharp.Drawing.XFont"/>.
        /// </summary>
        static App()
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }

        public static LocalDbContext LocalDb { get; private set; } = null!;

        // ── Chemins de stockage par email ─────────────────────────────────────

        private static readonly string DossierApp = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmarTest");

        private static string FichierDernierEmail =>
            Path.Combine(DossierApp, "last_user.txt");

        private static string CheminDbPourEmail(string email)
        {
            string dossier = Path.Combine(DossierApp,
                email.Trim().ToLowerInvariant()
                     .Replace("@", "_at_")
                     .Replace(" ", "_"));
            Directory.CreateDirectory(dossier);
            return Path.Combine(dossier, "smartest_local.db");
        }

        private static string? LireDernierEmail()
        {
            try
            {
                if (File.Exists(FichierDernierEmail))
                    return File.ReadAllText(FichierDernierEmail, Encoding.UTF8).Trim();
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Initialise (ou réinitialise) la base de données locale pour un email donné.
        /// Appelé au login et au démarrage si un email est connu.
        /// </summary>
        public static void InitialiserPourEmail(string email)
        {
            LocalDb?.Dispose();
            LocalDbContext.CheminBase = CheminDbPourEmail(email);
            LocalDb = InitialiserBase();
            Directory.CreateDirectory(DossierApp);
            File.WriteAllText(FichierDernierEmail, email.Trim().ToLower(), Encoding.UTF8);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                var ex = (Exception)ev.ExceptionObject;
                MessageBox.Show($"{ex.Message}\n\n{ex.StackTrace}", "Erreur critique");
            };

            DispatcherUnhandledException += (s, ev) =>
            {
                MessageBox.Show($"{ev.Exception.Message}\n\n{ev.Exception.StackTrace}", "Erreur WPF");
                ev.Handled = true;
            };

            try
            {
                ShutdownMode = ShutdownMode.OnLastWindowClose;

                // Charger la DB du dernier utilisateur connu (si existant)
                string? dernierEmail = LireDernierEmail();
                if (!string.IsNullOrWhiteSpace(dernierEmail))
                {
                    LocalDbContext.CheminBase = CheminDbPourEmail(dernierEmail);
                    LocalDb = InitialiserBase();

                    var session = new SessionService(LocalDb).ChargerSession();
                    if (session != null)
                    {
                        // Session valide : restaurer Properties et aller au Dashboard
                        Current.Properties["Token"] = session.TokenChiffre;
                        Current.Properties["Nom"]   = session.Nom;
                        Current.Properties["Email"] = session.Email;

                        var dashboard = new Views.DashboardWindow();
                        MainWindow = dashboard;
                        dashboard.Show();
                        base.OnStartup(e);
                        return;
                    }
                }

                // Aucun utilisateur connu ou session expirée → LoginWindow
                // LocalDb sera initialisé lors du login via InitialiserPourEmail()
                var login = new MainWindow();
                MainWindow = login;
                login.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur au démarrage :\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// Initialise la base SQLite. Si le schéma est incompatible avec le modèle actuel
        /// (par exemple après une migration de colonnes), la base est supprimée et recréée.
        /// </summary>
        private static LocalDbContext InitialiserBase()
        {
            string dbPath = LocalDbContext.CheminBase;

            var ctx = new LocalDbContext();
            try
            {
                ctx.Database.EnsureCreated();
                ApplyQuizLocalColumnPatches(ctx);

                // Vérification rapide : on lit une ligne de chaque table critique
                _ = ctx.SessionsLocales.Count();
                _ = ctx.Cours.Count();
                _ = ctx.Quiz.Count();
                _ = ctx.Examens.Count();
                _ = ctx.Questions.Count();
            }
            catch
            {
                // Schéma obsolète — supprimer via EF Core
                try
                {
                    ctx.Database.EnsureDeleted();
                }
                catch
                {
                    ctx.Dispose();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    foreach (var f in new[] { dbPath, dbPath + "-shm", dbPath + "-wal" })
                        if (File.Exists(f)) File.Delete(f);
                }
                finally
                {
                    ctx.Dispose();
                }

                ctx = new LocalDbContext();
                ctx.Database.EnsureCreated();
                ApplyQuizLocalColumnPatches(ctx);
            }

            return ctx;
        }

        /// <summary>
        /// SQLite : <see cref="DbContext.Database.EnsureCreated"/> ne migre pas les colonnes ajoutées au modèle.
        /// </summary>
        private static void ApplyQuizLocalColumnPatches(LocalDbContext ctx)
        {
            var conn = ctx.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) conn.Open();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(quiz_local);";
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        existing.Add(r.GetString(1));
                }

                void AddColumnIfMissing(string columnName, string alterSql)
                {
                    if (existing.Contains(columnName)) return;
                    using var c = conn.CreateCommand();
                    c.CommandText = alterSql;
                    c.ExecuteNonQuery();
                }

                AddColumnIfMissing("BackendQuizId",
                    "ALTER TABLE quiz_local ADD COLUMN BackendQuizId INTEGER NULL;");
                AddColumnIfMissing("EmailsPublicationWebJson",
                    "ALTER TABLE quiz_local ADD COLUMN EmailsPublicationWebJson TEXT NULL;");
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        /// <summary>
        /// Déconnecte l'utilisateur depuis n'importe quelle fenêtre :
        /// supprime la session persistante, efface les Properties, ouvre LoginWindow
        /// et ferme toutes les autres fenêtres.
        /// </summary>
        public static void Deconnecter()
        {
            try { new SessionService(LocalDb).SupprimerSession(); } catch { }

            Current.Properties["Token"] = null;
            Current.Properties["Nom"]   = null;
            Current.Properties["Email"] = null;

            var login = new Views.LoginWindow();
            login.Show();

            foreach (Window w in Current.Windows.Cast<Window>().ToList())
            {
                if (w is not Views.LoginWindow)
                    w.Close();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LocalDb?.Dispose();
            base.OnExit(e);
        }
    }
}
