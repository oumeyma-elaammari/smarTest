using smartest_desktop.Data;
using System;
using System.IO;
using System.Windows;

namespace smartest_desktop
{
    public partial class App : Application
    {
        public static LocalDbContext LocalDb { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Gestion des exceptions globales
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
                LocalDb = InitialiserBase();

                ShutdownMode = ShutdownMode.OnLastWindowClose;

                var welcome = new MainWindow();
                MainWindow = welcome;
                welcome.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur au démarrage :\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

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
            const string dbPath = "smartest_local.db";

            var ctx = new LocalDbContext();
            try
            {
                ctx.Database.EnsureCreated();

                // Vérification rapide : on lit une ligne de chaque table critique
                _ = ctx.Examens.Count();
                _ = ctx.Questions.Count();
            }
            catch
            {
                // Schéma obsolète — on recrée la base
                ctx.Dispose();
                if (File.Exists(dbPath))
                    File.Delete(dbPath);

                ctx = new LocalDbContext();
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
