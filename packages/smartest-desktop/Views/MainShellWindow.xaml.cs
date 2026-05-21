using smartest_desktop.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace smartest_desktop.Views
{
    public partial class MainShellWindow : Window
    {
        private readonly Dictionary<MainShellSection, UIElement> _contentCache = new();

        public MainShellWindow()
        {
            InitializeComponent();
            var vm = new MainShellViewModel();
            vm.PropertyChanged += OnShellPropertyChanged;
            DataContext = vm;
            Loaded += (_, __) =>
            {
                UpdateCentralContent(vm.SectionActive);
                LierQuizAuxSessionsActives(vm);
            };
        }

        public MainShellWindow(MainShellSection initialSection)
            : this()
        {
            if (DataContext is MainShellViewModel vm)
            {
                vm.SectionActive = initialSection;
            }
        }

        private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainShellViewModel.SectionActive))
                return;

            if (sender is MainShellViewModel vm)
            {
                UpdateCentralContent(vm.SectionActive);
            }
        }

        private void UpdateCentralContent(MainShellSection section)
        {
            SidebarColumn.Width = section is MainShellSection.QuizGeneration or MainShellSection.ExamenGeneration
                ? new GridLength(0)
                : new GridLength(230);

            if (!_contentCache.TryGetValue(section, out var view))
            {
                view = section switch
                {
                    MainShellSection.Home => ExtractDashboardMainContent(),
                    MainShellSection.QuizExamens => ExtractQuizExamenMainContent(),
                    MainShellSection.QuizGeneration => ExtractGenerationContent(new QuizGenerationWindow()),
                    MainShellSection.ExamenGeneration => ExtractGenerationContent(new ExamenGenerationWindow()),
                    MainShellSection.Statistiques => ExtractStatistiquesMainContent(),
                    _ => new Grid(),
                };

                _contentCache[section] = view;
                if (section == MainShellSection.QuizExamens
                    && view is FrameworkElement feInit
                    && feInit.DataContext is QuizExamenViewModel qvmInit
                    && DataContext is MainShellViewModel shellInit)
                {
                    qvmInit.LierSessionsActives(shellInit.SessionsActives);
                }
            }

            CentralHostedContent.Content = view;

            if (DataContext is not MainShellViewModel shellVm)
                return;

            if (section == MainShellSection.QuizExamens && view is FrameworkElement fe && fe.DataContext is QuizExamenViewModel qvm)
            {
                qvm.LierSessionsActives(shellVm.SessionsActives);
                _ = qvm.ChargerDonneesAsync();
            }

            if (section == MainShellSection.Sessions)
                _ = shellVm.SessionsActives.RafraichirExamensAsync();
        }

        private static UIElement ExtractDashboardMainContent()
        {
            var source = new DashboardWindow();
            return ExtractWholeWindowContent(source);
        }

        private void LierQuizAuxSessionsActives(MainShellViewModel shellVm)
        {
            if (_contentCache.TryGetValue(MainShellSection.QuizExamens, out var view)
                && view is FrameworkElement fe
                && fe.DataContext is QuizExamenViewModel qvm)
            {
                qvm.LierSessionsActives(shellVm.SessionsActives);
            }
        }

        private static UIElement ExtractQuizExamenMainContent()
        {
            var source = new QuizExamenWindow();
            return ExtractWholeWindowContent(source);
        }

        private static UIElement ExtractStatistiquesMainContent()
        {
            var source = new StatistiquesProfWindow();
            return ExtractWholeWindowContent(source);
        }

        private static UIElement ExtractGenerationContent(Window source)
        {
            if (source.Content is not Grid root)
                return BuildError("Impossible de charger l'interface demandée.");

            var host = new Grid();
            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = root.Children.OfType<UIElement>().FirstOrDefault(c => Grid.GetRow(c) == 0);
            var body = root.Children.OfType<UIElement>().FirstOrDefault(c => Grid.GetRow(c) == 1);
            if (header == null || body == null)
                return BuildError("Contenu principal introuvable.");

            root.Children.Remove(header);
            root.Children.Remove(body);
            source.Content = null;

            if (header is FrameworkElement headerElement && headerElement.DataContext == null)
                headerElement.DataContext = source.DataContext;
            if (body is FrameworkElement bodyElement && bodyElement.DataContext == null)
                bodyElement.DataContext = source.DataContext;

            Grid.SetRow(header, 0);
            Grid.SetRow(body, 1);
            host.Children.Add(header);
            host.Children.Add(body);
            return host;
        }

        private static UIElement ExtractWindowColumnContent(Window source, int column)
        {
            if (source.Content is not Grid root)
                return BuildError("Impossible de charger l'interface demandée.");

            var target = root.Children.OfType<UIElement>().FirstOrDefault(c => Grid.GetColumn(c) == column);
            if (target == null)
                return BuildError("Contenu principal introuvable.");

            root.Children.Remove(target);
            source.Content = null;

            if (target is FrameworkElement fe && fe.DataContext == null)
                fe.DataContext = source.DataContext;

            return target;
        }

        private static UIElement ExtractWholeWindowContent(Window source)
        {
            if (source.Content is not UIElement content)
                return BuildError("Impossible de charger l'interface demandée.");

            source.Content = null;

            if (content is FrameworkElement fe && source.DataContext != null)
                fe.DataContext = source.DataContext;

            return content;
        }

        private static UIElement BuildError(string message)
        {
            return new TextBlock
            {
                Text = message,
                Margin = new Thickness(24),
                Foreground = System.Windows.Media.Brushes.Red
            };
        }
    }
}
