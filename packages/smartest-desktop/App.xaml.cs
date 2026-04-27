using smartest_desktop.Data;
using System;
using System.IO;
using System.Windows;
using smartest_desktop.Services;

namespace smartest_desktop
{
    public partial class App : Application
    {
        public static LocalDbContext LocalDb { get; private set; } = null!;

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
                LocalDb = new LocalDbContext();
                LocalDb.Database.EnsureCreated();

                ShutdownMode = ShutdownMode.OnLastWindowClose;

                // Vérifier session existante
                var sessionService = new SessionService(LocalDb);
                var session = sessionService.ChargerSession();

                if (session != null)
                {
                    // Session valide: restaurer Properties et aller au Dashboard
                    Current.Properties["Token"] = session.TokenChiffre; 
                    Current.Properties["Nom"] = session.Nom;
                    Current.Properties["Email"] = session.Email;

                    var dashboard = new Views.DashboardWindow();
                    MainWindow = dashboard;
                    dashboard.Show();
                }
                else
                {
                    // Pas de session : LoginWindow
                    var login = new MainWindow();
                    MainWindow = login;
                    login.Show();
                }
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

        protected override void OnExit(ExitEventArgs e)
        {
            LocalDb?.Dispose();
            base.OnExit(e);
        }
    }
}
