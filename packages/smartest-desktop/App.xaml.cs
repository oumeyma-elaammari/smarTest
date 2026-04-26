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
                // Initialisation de la base locale
                LocalDb = new LocalDbContext();
                LocalDb.Database.EnsureCreated();

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

        protected override void OnExit(ExitEventArgs e)
        {
            LocalDb?.Dispose();
            base.OnExit(e);
        }
    }
}
