using smartest_desktop.Data;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using smartest_desktop.Services;

namespace smartest_desktop
{
    public partial class App : Application
    {
        public static LocalDbContext LocalDb { get; private set; } = null!;

        // ── Chemins ─────────────────────────────────────────────────
        private static string UsersFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users");

        private static string CurrentUserFile =>
            Path.Combine(UsersFolder, "current_user.txt");

        private static string GetDbPath(string email)
        {
            var safe = Regex.Replace(email.ToLower().Trim(), @"[^a-z0-9]", "_");
            return Path.Combine(UsersFolder, $"{safe}.db");
        }

        // ── API publique ─────────────────────────────────────────────
        /// <summary>
        /// Appelé après un login réussi pour initialiser la base de l'utilisateur.
        /// </summary>
        public static void ReinitialiserPourUtilisateur(string email)
        {
            LocalDb?.Dispose();
            Directory.CreateDirectory(UsersFolder);
            File.WriteAllText(CurrentUserFile, email.Trim().ToLower());
            LocalDb = InitialiserBase(GetDbPath(email));
        }

        /// <summary>
        /// Appelé lors d'un logout pour effacer le pointeur de session.
        /// </summary>
        public static void SupprimerFichierSession()
        {
            if (File.Exists(CurrentUserFile))
                File.Delete(CurrentUserFile);
        }

        /// <summary>
        /// Déconnecte l'utilisateur depuis n'importe quelle fenêtre :
        /// supprime la session, ferme toutes les fenêtres, ouvre LoginWindow.
        /// </summary>
        public static void Deconnecter()
        {
            if (LocalDb != null)
                new SessionService(LocalDb).SupprimerSession();
            else
                SupprimerFichierSession();

            Current.Properties["Token"] = null;
            Current.Properties["Nom"]   = null;
            Current.Properties["Email"] = null;

            Current.Dispatcher.Invoke(() =>
            {
                var login = new Views.LoginWindow();
                login.Show();

                foreach (var w in Current.Windows.OfType<Window>().Where(w => w is not Views.LoginWindow).ToList())
                    w.Close();
            });
        }

        // ── Démarrage ────────────────────────────────────────────────
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

                // Vérifier si un utilisateur était connecté
                if (File.Exists(CurrentUserFile))
                {
                    var email = File.ReadAllText(CurrentUserFile).Trim();
                    if (!string.IsNullOrEmpty(email))
                    {
                        LocalDb = InitialiserBase(GetDbPath(email));
                        var session = new SessionService(LocalDb).ChargerSession();

                        if (session != null)
                        {
                            Current.Properties["Token"] = session.TokenChiffre;
                            Current.Properties["Nom"] = session.Nom;
                            Current.Properties["Email"] = session.Email;

                            var dashboard = new Views.DashboardWindow();
                            MainWindow = dashboard;
                            dashboard.Show();
                            base.OnStartup(e);
                            return;
                        }

                        // Session expirée/corrompue : nettoyer
                        SupprimerFichierSession();
                        LocalDb?.Dispose();
                        LocalDb = null!;
                    }
                }

                // Pas de session valide : LoginWindow
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

        private static LocalDbContext InitialiserBase(string dbPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            var ctx = new LocalDbContext(dbPath);
            try
            {
                ctx.Database.EnsureCreated();

                _ = ctx.SessionsLocales.Count();
                _ = ctx.Cours.Count();
                _ = ctx.Quiz.Count();
                _ = ctx.Examens.Count();
                _ = ctx.Questions.Count();
            }
            catch
            {
                try { ctx.Database.EnsureDeleted(); }
                catch
                {
                    ctx.Dispose();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    foreach (var f in new[] { dbPath, dbPath + "-shm", dbPath + "-wal" })
                        if (File.Exists(f)) File.Delete(f);
                }
                finally { ctx.Dispose(); }

                ctx = new LocalDbContext(dbPath);
                ctx.Database.EnsureCreated();
            }

            return ctx;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LocalDb?.Dispose();
            base.OnExit(e);
        }
    }
}
