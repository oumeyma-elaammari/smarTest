using smartest_desktop.Data;
using smartest_desktop.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using smartest_desktop.Services;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Fonts;

namespace smartest_desktop
{
    public partial class App : Application
    {
        public static event Action<string, string>? ProfilMisAJour;

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

        /// <summary>Réglages de profil (nom affiché) par compte. Anciennement un seul fichier global : source du bug au changement d'email.</summary>
        private static string FichierPreferencesProfilLegacy =>
            Path.Combine(DossierApp, "profile_prefs.json");

        private sealed class LocalProfilePreferences
        {
            public string Nom { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        private static string DossierDonneesPourEmail(string email)
        {
            string dossier = CheminDossierDonneesPourEmail(email);
            Directory.CreateDirectory(dossier);
            return dossier;
        }

        /// <summary>
        /// Racine du dossier SQLite + préférences pour cet email (sans le créer).
        /// </summary>
        private static string CheminDossierDonneesPourEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;
            return Path.Combine(DossierApp,
                email.Trim().ToLowerInvariant()
                     .Replace("@", "_at_")
                     .Replace(" ", "_"));
        }

        private static string CheminDbPourEmail(string email) =>
            Path.Combine(DossierDonneesPourEmail(email), "smartest_local.db");

        private static string CheminPreferenceProfilPourEmail(string email) =>
            Path.Combine(DossierDonneesPourEmail(email), "profile_prefs.json");

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

        /// <remarks>
        /// Préférences stockées sous le dossier utilisateur (même emplacement que la DB locale pour cet email).
        /// L'email retourné est toujours <paramref name="emailDefaut"/> tant que la connexion/serveur fait foi.
        /// </remarks>
        public static (string Nom, string Email) ChargerPreferencesProfilLocal(string nomDefaut, string emailDefaut)
        {
            if (string.IsNullOrWhiteSpace(emailDefaut))
                return (nomDefaut, emailDefaut);

            string emailSession = emailDefaut.Trim();

            LocalProfilePreferences? ChargerDepuisChemin(string path)
            {
                try
                {
                    if (!File.Exists(path)) return null;
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    if (string.IsNullOrWhiteSpace(json)) return null;
                    return JsonSerializer.Deserialize<LocalProfilePreferences>(json);
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                var fichierCompte = CheminPreferenceProfilPourEmail(emailSession);
                var prefs = ChargerDepuisChemin(fichierCompte);

                if (prefs == null && File.Exists(FichierPreferencesProfilLegacy))
                {
                    var legacyPrefs = ChargerDepuisChemin(FichierPreferencesProfilLegacy);
                    if (legacyPrefs != null &&
                        string.Equals((legacyPrefs.Email ?? string.Empty).Trim(), emailSession,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        prefs = legacyPrefs;
                        try
                        {
                            File.Copy(FichierPreferencesProfilLegacy, fichierCompte, overwrite: false);
                        }
                        catch { }
                    }
                }

                if (prefs == null)
                    return (nomDefaut, emailSession);

                var nom = string.IsNullOrWhiteSpace(prefs.Nom) ? nomDefaut : prefs.Nom.Trim();
                return (nom, emailSession);
            }
            catch
            {
                return (nomDefaut, emailSession);
            }
        }

        public static bool EnregistrerPreferencesProfilLocal(string nom, string email, out string erreur)
        {
            erreur = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    erreur = "Email requis.";
                    return false;
                }

                var payload = new LocalProfilePreferences
                {
                    Nom = nom.Trim(),
                    Email = email.Trim()
                };
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Directory.CreateDirectory(DossierApp);
                var fichier = CheminPreferenceProfilPourEmail(email);
                File.WriteAllText(fichier, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                erreur = ex.Message;
                return false;
            }
        }

        public static void AppliquerProfilCourant(string nom, string email)
        {
            Current.Properties["Nom"] = nom;
            Current.Properties["Email"] = email;
            ProfilMisAJour?.Invoke(nom, email);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                var ex = (Exception)ev.ExceptionObject;
                MessageBox.Show(
                    Helpers.UserErrorMessage.FromException(ex, "Une erreur critique est survenue. Redemarrez l'application."),
                    "Erreur critique",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (s, ev) =>
            {
                MessageBox.Show(
                    Helpers.UserErrorMessage.FromException(ev.Exception, "Une erreur inattendue est survenue."),
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

                        var shell = new Views.MainShellWindow(ViewModels.MainShellSection.Home);
                        MainWindow = shell;
                        shell.Show();

                        if (string.Equals(session.Role, "PROFESSEUR", StringComparison.OrdinalIgnoreCase))
                        {
                            string t = session.TokenChiffre;
                            string n = session.Nom;
                            string em = session.Email;
                            string r = session.Role;
                            _ = Dispatcher.InvokeAsync(async () =>
                            {
                                try
                                {
                                    var auth = new AuthResponse
                                    {
                                        Token = t,
                                        Nom = n,
                                        Email = em,
                                        Role = r
                                    };
                                    await LocalProfesseurIdentityService.ApresConnexionProfesseurAsync(auth);
                                    new SessionService(LocalDb).SauvegarderSession(auth);
                                }
                                catch
                                {
                                    // Réseau : ne pas empêcher l’usage de l’app.
                                }
                            });
                        }

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
                    Helpers.UserErrorMessage.FromException(ex, "Impossible de demarrer l'application."),
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
                AddColumnIfMissing("ServeurOuQrToucheUtc",
                    "ALTER TABLE quiz_local ADD COLUMN ServeurOuQrToucheUtc TEXT NULL;");
                AddColumnIfMissing("BackendQuizIdPublicationWeb",
                    "ALTER TABLE quiz_local ADD COLUMN BackendQuizIdPublicationWeb INTEGER NULL;");
                AddColumnIfMissing("BackendQuizIdQr",
                    "ALTER TABLE quiz_local ADD COLUMN BackendQuizIdQr INTEGER NULL;");
                AddColumnIfMissing("QrLiveSessionToken",
                    "ALTER TABLE quiz_local ADD COLUMN QrLiveSessionToken TEXT NULL;");

                using (var mig = conn.CreateCommand())
                {
                    // Une seule colonne historique : on la rattache à la publication web. La copie QR serveur
                    // restera vide jusqu’à la première ouverture « Code QR » (nouvelle ligne MySQL dédiée).
                    mig.CommandText =
                        "UPDATE quiz_local SET BackendQuizIdPublicationWeb = COALESCE(BackendQuizIdPublicationWeb, BackendQuizId) " +
                        "WHERE BackendQuizId IS NOT NULL AND BackendQuizId > 0;";
                    mig.ExecuteNonQuery();
                }

                cmd.CommandText = "PRAGMA table_info(question_locale);";
                var questionColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        questionColumns.Add(r.GetString(1));
                }

                if (!questionColumns.Contains("BaremePoints"))
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = "ALTER TABLE question_locale ADD COLUMN BaremePoints REAL NOT NULL DEFAULT 0;";
                    c.ExecuteNonQuery();
                }
            }
            finally
            {
                if (!wasOpen && conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        /// <summary>
        /// Recrée un fichier SQLite vide au chemin <see cref="LocalDbContext.CheminBase"/> actuel
        /// (même dossier utilisateur). Utilisé lorsque le compte serveur ne correspond plus aux données locales.
        /// </summary>
        public static void RecréerBaseSqliteAuCheminActuel()
        {
            string dbPath = LocalDbContext.CheminBase;
            try { LocalDb?.Dispose(); }
            catch { }

            try
            {
                foreach (var f in new[] { dbPath, dbPath + "-shm", dbPath + "-wal" })
                {
                    try
                    {
                        if (File.Exists(f))
                            File.Delete(f);
                    }
                    catch { }
                }
            }
            catch { }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            LocalDbContext.CheminBase = dbPath;
            LocalDb = InitialiserBase();
        }

        /// <summary>
        /// Supprime la base SQLite locale, la session, les préférences de profil et la clé Groq
        /// stockées pour cet email (dossier sous LocalApplicationData/SmarTest).
        /// À utiliser lorsque le compte est supprimé côté serveur, avant <see cref="Deconnecter"/>.
        /// </summary>
        public static void SupprimerDonneesLocalesPourEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            string emailNorm = email.Trim().ToLowerInvariant();
            try { LocalDb?.Dispose(); }
            catch { /* libérer le fichier SQLite */ }

            string dossierCompte = CheminDossierDonneesPourEmail(emailNorm);
            try
            {
                string fichiersDb = Path.Combine(dossierCompte, "smartest_local.db");
                if (File.Exists(fichiersDb))
                {
                    try
                    {
                        LocalDbContext.CheminBase = fichiersDb;
                        using (var ctx = new LocalDbContext())
                        {
                            ctx.Database.EnsureDeleted();
                        }
                    }
                    catch { }
                    foreach (var suf in new[] { string.Empty, "-shm", "-wal" })
                    {
                        try
                        {
                            var p = fichiersDb + suf;
                            if (File.Exists(p))
                                File.Delete(p);
                        }
                        catch { }
                    }
                }

                if (!string.IsNullOrEmpty(dossierCompte) && Directory.Exists(dossierCompte))
                    Directory.Delete(dossierCompte, recursive: true);
            }
            catch { /* dossier verrouillé ou autre — la déconnexion reste prioritaire */ }

            try
            {
                if (File.Exists(FichierDernierEmail))
                {
                    string dernier = File.ReadAllText(FichierDernierEmail, Encoding.UTF8).Trim().ToLowerInvariant();
                    if (dernier == emailNorm)
                        File.Delete(FichierDernierEmail);
                }
            }
            catch { }

            try
            {
                if (File.Exists(FichierPreferencesProfilLegacy))
                {
                    var json = File.ReadAllText(FichierPreferencesProfilLegacy, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var prefs = JsonSerializer.Deserialize<LocalProfilePreferences>(json);
                        if (prefs != null &&
                            string.Equals((prefs.Email ?? string.Empty).Trim(), emailNorm, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(FichierPreferencesProfilLegacy);
                        }
                    }
                }
            }
            catch { }

            string placeholderDir = Path.Combine(DossierApp, "_placeholder");
            Directory.CreateDirectory(placeholderDir);
            LocalDbContext.CheminBase = Path.Combine(placeholderDir, "disconnected.db");
            LocalDb = InitialiserBase();
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

        public static void OuvrirShell(ViewModels.MainShellSection section = ViewModels.MainShellSection.Home)
        {
            var shell = new Views.MainShellWindow(section);
            shell.Show();
            Current.MainWindow = shell;

            foreach (Window w in Current.Windows.Cast<Window>().ToList())
            {
                if (!ReferenceEquals(w, shell))
                    w.Close();
            }
        }

        /// <summary>
        /// Définit <paramref name="fenetre"/> comme fenêtre principale et ferme toutes les autres.
        /// Utilisé quand Quiz / Examen résultat doit remplacer le shell au lieu de s'afficher en parallèle.
        /// </summary>
        public static void GarderUneSeuleFenetreOuverte(Window fenetre)
        {
            if (fenetre == null)
                return;

            Current.MainWindow = fenetre;

            foreach (Window w in Current.Windows.Cast<Window>().ToList())
            {
                if (!ReferenceEquals(w, fenetre))
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
