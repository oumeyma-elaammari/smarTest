using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using smartest_desktop.Models;
using smartest_desktop.Services;
using App = smartest_desktop.App;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public enum TypeEvalStat
    {
        Quiz,
        Examen,
    }

    public sealed class StatistiqueEvalItem
    {
        public StatistiqueEvalItem(TypeEvalStat type, long backendId, string titre)
        {
            Type = type;
            BackendId = backendId;
            Titre = titre.Trim();
        }

        public TypeEvalStat Type { get; }
        public long BackendId { get; }
        public string Titre { get; }

        public string LibelleListe =>
            Type == TypeEvalStat.Quiz ? $"Quiz — {Titre}" : $"Examen — {Titre}";
    }

    public sealed class HistogrammeBarVm
    {
        public HistogrammeBarVm(string labelTranche, int effectif, double hauteurPx)
        {
            LabelTranche = labelTranche;
            Effectif = effectif;
            HauteurBarrePx = hauteurPx;
        }

        public string LabelTranche { get; }
        public int Effectif { get; }
        public double HauteurBarrePx { get; }
    }

    public sealed class QuestionReussiteBarVm
    {
        /// <summary>Largeur fixe de la piste 0–100 % (alignée sur le XAML).</summary>
        public const double PisteBarrePx = 360;

        public QuestionReussiteBarVm(string libelleQ, double pourcentReussite, double largeurBarrePx, bool alerte)
        {
            LibelleQuestion = libelleQ;
            PourcentageReussite = pourcentReussite;
            LargeurBarrePx = largeurBarrePx;
            AfficherAlerte = alerte;
            CouleurBarreReussite = CreerBrushCouleurPourPourcent(pourcentReussite);
        }

        public string LibelleQuestion { get; }
        public double PourcentageReussite { get; }
        public double LargeurBarrePx { get; }
        public bool AfficherAlerte { get; }

        /// <summary>Rouge (#C62828) → vert (#2E7D32) selon le taux de réussite 0–100 %.</summary>
        public SolidColorBrush CouleurBarreReussite { get; }

        private static SolidColorBrush CreerBrushCouleurPourPourcent(double pourcent)
        {
            double t = Math.Clamp(pourcent, 0, 100) / 100.0;
            // #C62828 → #2E7D32
            byte r = (byte)Math.Round(0xC6 + (0x2E - 0xC6) * t);
            byte g = (byte)Math.Round(0x28 + (0x7D - 0x28) * t);
            byte b = (byte)Math.Round(0x28 + (0x32 - 0x28) * t);
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }

    /// <summary>Contenu principal : données API + export CSV/PDF.</summary>
    public sealed class StatistiquesQuizProfViewModel : BaseViewModel
    {
        public static readonly string[] LabelsTranches =
        {
            "0–5", "5–8", "8–10", "10–12", "12–15", "15–18", "18–20",
        };

        /// <summary>Seuil d’échec pour la bannière d’alerte et l’icône par question.</summary>
        public const double SeuilPourcentageEchecAlerte = 40.0;

        private const double HistogrammeMaxPx = 250;

        private readonly LocalQuizService _quizLocal = new(App.LocalDb);
        private readonly LocalExamenService _examenLocal = new(App.LocalDb);

        private bool _chargement;
        public bool Chargement
        {
            get => _chargement;
            private set => SetProperty(ref _chargement, value);
        }

        private string? _erreur;
        public string? Erreur
        {
            get => _erreur;
            private set
            {
                if (!SetProperty(ref _erreur, value))
                    return;
                OnPropertyChanged(nameof(AErreur));
            }
        }

        public bool AErreur => !string.IsNullOrWhiteSpace(Erreur);

        private bool _donneesChargees;
        public bool DonneesChargees
        {
            get => _donneesChargees;
            private set
            {
                if (!SetProperty(ref _donneesChargees, value))
                    return;
                NotifierPeutExporter();
            }
        }

        public ObservableCollection<StatistiqueEvalItem> EvaluationsItems { get; } = new();

        private StatistiqueEvalItem? _evaluationChoisi;
        public StatistiqueEvalItem? EvaluationChoisi
        {
            get => _evaluationChoisi;
            set
            {
                if (!SetProperty(ref _evaluationChoisi, value))
                    return;
                if (value != null)
                    _ = ChargerDonneesAsync(value);
            }
        }

        public ICommand ExporterCsvCommand { get; }
        public ICommand ExporterPdfCommand { get; }

        private StatistiquesQuizResponseDto? _dtoPourExport;

        /// <summary>Dernières stats chargées — utilisées pour les exports.</summary>
        public bool PeutExporter => DonneesChargees && _dtoPourExport != null;

        private bool _afficherAlerteQuestionsRisque;
        public bool AfficherAlerteQuestionsRisque
        {
            get => _afficherAlerteQuestionsRisque;
            private set => SetProperty(ref _afficherAlerteQuestionsRisque, value);
        }

        private string _alerteQuestionsRisqueDetail = "";
        public string AlerteQuestionsRisqueDetail
        {
            get => _alerteQuestionsRisqueDetail;
            private set => SetProperty(ref _alerteQuestionsRisqueDetail, value);
        }

        private string _titreCarte = "—";
        public string TitreCarteQuiz
        {
            get => _titreCarte;
            private set => SetProperty(ref _titreCarte, value);
        }

        private string _moyenneSur20 = "—/20";
        public string MoyenneGeneraleSur20
        {
            get => _moyenneSur20;
            private set => SetProperty(ref _moyenneSur20, value);
        }

        private string _tauxGlobal = "— %";
        public string TauxReussiteGlobalLibelle
        {
            get => _tauxGlobal;
            private set => SetProperty(ref _tauxGlobal, value);
        }

        private int _nbEtudiants;
        public int NombreEtudiants
        {
            get => _nbEtudiants;
            private set => SetProperty(ref _nbEtudiants, value);
        }

        public ObservableCollection<HistogrammeBarVm> BarresHistogramme { get; } = new();
        public ObservableCollection<QuestionReussiteBarVm> BarresQuestionReussite { get; } = new();

        public StatistiquesQuizProfViewModel(long? preferBackendQuizId = null, long? preferBackendExamenId = null)
        {
            ExporterCsvCommand = new RelayCommand(_ => ExporterCsv());
            ExporterPdfCommand = new RelayCommand(_ => ExporterPdf());
            _ = InitialiserFenetreAsync(preferBackendQuizId, preferBackendExamenId);
        }

        private void NotifierPeutExporter()
        {
            OnPropertyChanged(nameof(PeutExporter));
        }

        private static string NomFichierExport(string titreQuiz)
        {
            var t = string.IsNullOrWhiteSpace(titreQuiz) ? "statistiques" : titreQuiz;
            foreach (var c in Path.GetInvalidFileNameChars())
                t = t.Replace(c, '_');
            return t.Trim();
        }

        private void ExporterCsv()
        {
            if (_dtoPourExport == null || !DonneesChargees)
            {
                MessageBox.Show(
                    "Aucune donnée à exporter. Sélectionnez un quiz publié dont les statistiques sont chargées.",
                    "Export CSV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                Title = "Exporter en CSV",
                FileName = $"{NomFichierExport(TitreCarteQuiz)}_statistiques.csv",
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                var d = _dtoPourExport;
                var fr = CultureInfo.GetCultureInfo("fr-FR");
                var sb = new StringBuilder();
                sb.AppendLine($"Titre;{ChampCsv(d.QuizTitre ?? "")}");
                sb.AppendLine($"Identifiant quiz;{d.QuizId}");
                sb.AppendLine($"Nombre de participants;{d.NombreParticipants}");
                sb.AppendLine($"Moyenne sur 20;{(d.MoyenneNoteSur20 ?? 0).ToString("F2", fr)}");
                sb.AppendLine($"Taux de réussite global (%);{d.TauxReussiteGlobal.ToString("F2", fr)}");

                var rep = d.RepartitionParTranche;
                if (rep != null && rep.Count > 0)
                {
                    sb.AppendLine("Répartition par tranche (notes /20);");
                    for (int i = 0; i < LabelsTranches.Length && i < rep.Count; i++)
                        sb.AppendLine($"{LabelsTranches[i]};{rep[i]}");
                }

                sb.AppendLine("Question;Taux réussite (%);Taux échec (%);Nombre réponses");
                int n = 1;
                foreach (var sq in d.StatistiquesParQuestion ?? new List<StatistiqueQuestionResponseDto>())
                {
                    sb.AppendLine($"Q{n};{sq.PourcentageReussite.ToString("F2", fr)};{sq.PourcentageEchec.ToString("F2", fr)};{sq.NombreReponses}");
                    n++;
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                MessageBox.Show(
                    $"Le fichier a été enregistré :\n{dlg.FileName}",
                    "Export CSV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible d’enregistrer le fichier :\n{ex.Message}",
                    "Export CSV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static string ChampCsv(string s)
        {
            if (s.Contains(';') || s.Contains('"') || s.Contains('\n'))
                return $"\"{s.Replace("\"", "\"\"")}\"";
            return s;
        }

        private void ExporterPdf()
        {
            if (_dtoPourExport == null || !DonneesChargees)
            {
                MessageBox.Show(
                    "Aucune donnée à exporter. Sélectionnez un quiz publié dont les statistiques sont chargées.",
                    "Export PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                Title = "Exporter en PDF",
                FileName = $"{NomFichierExport(TitreCarteQuiz)}_statistiques.pdf",
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                var d = _dtoPourExport;
                var fr = CultureInfo.GetCultureInfo("fr-FR");
                var lignes = new List<string>
                {
                    $"Statistiques — {d.QuizTitre ?? "Quiz"}",
                    "",
                    $"Participants : {d.NombreParticipants}",
                    $"Moyenne sur 20 : {(d.MoyenneNoteSur20 ?? 0).ToString("F2", fr)}",
                    $"Taux de réussite global : {d.TauxReussiteGlobal.ToString("F1", fr)} %",
                    "",
                    "Répartition des notes",
                };

                var rep = d.RepartitionParTranche;
                if (rep != null)
                {
                    for (int i = 0; i < LabelsTranches.Length && i < rep.Count; i++)
                        lignes.Add($"  {LabelsTranches[i]} : {rep[i]}");
                }

                lignes.Add("");
                lignes.Add("Détail par question");

                int n = 1;
                foreach (var sq in d.StatistiquesParQuestion ?? new List<StatistiqueQuestionResponseDto>())
                {
                    lignes.Add(
                        $"Q{n} — Réussite {sq.PourcentageReussite.ToString("F0", fr)} %, " +
                        $"échec {sq.PourcentageEchec.ToString("F0", fr)} % ({sq.NombreReponses} réponses)");
                    n++;
                }

                using var doc = new PdfDocument();
                doc.Info.Title = $"Statistiques — {d.QuizTitre ?? "Quiz"}";

                PdfPage page = doc.AddPage();
                page.Size = PageSize.A4;
                double margin = 40;
                double lineHeight = 16;
                double y = margin;
                double pageBottom = page.Height.Point - margin;

                // Arial : résolu via GlobalFontSettings.UseWindowsFontsUnderWindows (PDFsharp 6, sans GDI/WPF pour les polices).
                var fontTitle = new XFont("Arial", 15, XFontStyleEx.Bold);
                var fontBody = new XFont("Arial", 11, XFontStyleEx.Regular);
                XGraphics gfx = XGraphics.FromPdfPage(page);

                try
                {
                    foreach (var texte in lignes)
                    {
                        if (y > pageBottom)
                        {
                            gfx.Dispose();
                            page = doc.AddPage();
                            page.Size = PageSize.A4;
                            pageBottom = page.Height.Point - margin;
                            y = margin;
                            gfx = XGraphics.FromPdfPage(page);
                        }

                        bool titrePrincipal = texte.StartsWith("Statistiques —", StringComparison.Ordinal);
                        var font = titrePrincipal ? fontTitle : fontBody;
                        gfx.DrawString(texte, font, XBrushes.Black, margin, y);
                        y += titrePrincipal ? lineHeight * 1.8 : lineHeight * 1.15;
                    }
                }
                finally
                {
                    gfx.Dispose();
                }

                doc.Save(dlg.FileName);

                MessageBox.Show(
                    $"Le fichier PDF a été enregistré :\n{dlg.FileName}",
                    "Export PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible d’enregistrer le PDF :\n{ex.Message}",
                    "Export PDF",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static bool EstQuizPublie(string? statut) =>
            string.Equals(statut?.Trim(), "Publié", StringComparison.OrdinalIgnoreCase);

        private static bool EstExamenPublie(string? statut) =>
            string.Equals(statut?.Trim(), "PUBLIE", StringComparison.OrdinalIgnoreCase);

        private async Task InitialiserFenetreAsync(long? preferQuizId, long? preferExamenId)
        {
            Chargement = true;
            Erreur = null;
            try
            {
                var quizzes = await _quizLocal.GetAllAsync();
                var qRows = quizzes
                    .Where(q => EstQuizPublie(q.Statut) && q.BackendQuizId is long id && id > 0)
                    .Select(q => new StatistiqueEvalItem(TypeEvalStat.Quiz, q.BackendQuizId!.Value, q.Titre));

                var examens = await _examenLocal.GetAllAsync();
                var eRows = examens
                    .Where(e => EstExamenPublie(e.Statut) && e.BackendId is long bid && bid > 0)
                    .Select(e => new StatistiqueEvalItem(TypeEvalStat.Examen, e.BackendId!.Value, e.Titre));

                var rows = qRows.Concat(eRows).OrderBy(x => x.LibelleListe, StringComparer.OrdinalIgnoreCase).ToList();

                EvaluationsItems.Clear();
                foreach (var r in rows)
                    EvaluationsItems.Add(r);

                if (rows.Count == 0)
                {
                    Erreur =
                        "Aucun quiz ou examen publié avec synchronisation serveur. Publiez un contenu pour afficher les statistiques.";
                    Chargement = false;
                    return;
                }

                StatistiqueEvalItem? sel =
                    (preferQuizId is long pq ? rows.FirstOrDefault(x => x.Type == TypeEvalStat.Quiz && x.BackendId == pq) : null)
                    ?? (preferExamenId is long pe ? rows.FirstOrDefault(x => x.Type == TypeEvalStat.Examen && x.BackendId == pe) : null)
                    ?? rows[0];

                _evaluationChoisi = sel;
                OnPropertyChanged(nameof(EvaluationChoisi));
                await ChargerDonneesAsync(sel);
            }
            catch (Exception ex)
            {
                Erreur = ex.Message;
            }
            finally
            {
                Chargement = false;
            }
        }

        private async Task ChargerDonneesAsync(StatistiqueEvalItem item)
        {
            var app = WpfApp.Current;
            if (app == null)
                return;

            if (item.Type == TypeEvalStat.Examen)
            {
                await app.Dispatcher.InvokeAsync(() =>
                {
                    ViderTableauBord(item.Titre);
                    Erreur =
                        "Les statistiques détaillées des examens (sessions) ne sont pas encore disponibles via le serveur. " +
                        "Sélectionnez un quiz publié pour afficher le tableau de bord.";
                    DonneesChargees = false;
                    Chargement = false;
                });
                return;
            }

            string? token = app.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                await app.Dispatcher.InvokeAsync(() =>
                {
                    Erreur = "Connectez-vous au serveur pour afficher les statistiques.";
                    DonneesChargees = false;
                    ViderTableauBord(item.Titre);
                });
                return;
            }

            Chargement = true;
            Erreur = null;
            DonneesChargees = false;

            var api = new QuizStatistiquesApiService();
            var (data, err) = await api.GetStatistiquesQuizAsync(token, item.BackendId);

            await app.Dispatcher.InvokeAsync(() =>
            {
                Chargement = false;
                if (err != null)
                {
                    Erreur = err;
                    ViderTableauBord(item.Titre);
                    DonneesChargees = false;
                    return;
                }

                if (data == null)
                {
                    Erreur = "Réponse vide.";
                    ViderTableauBord(item.Titre);
                    DonneesChargees = false;
                    return;
                }

                AppliquerDto(data);
                DonneesChargees = true;
            });
        }

        private void ViderTableauBord(string titreFallback)
        {
            _dtoPourExport = null;
            AfficherAlerteQuestionsRisque = false;
            AlerteQuestionsRisqueDetail = "";
            NotifierPeutExporter();

            TitreCarteQuiz = titreFallback;
            MoyenneGeneraleSur20 = "—/20";
            TauxReussiteGlobalLibelle = "— %";
            NombreEtudiants = 0;
            BarresHistogramme.Clear();
            foreach (var lb in LabelsTranches)
                BarresHistogramme.Add(new HistogrammeBarVm(lb, 0, 0));
            BarresQuestionReussite.Clear();
        }

        private void AppliquerDto(StatistiquesQuizResponseDto d)
        {
            _dtoPourExport = d;
            TitreCarteQuiz = string.IsNullOrWhiteSpace(d.QuizTitre) ? "Quiz" : d.QuizTitre!;

            var fr = CultureInfo.GetCultureInfo("fr-FR");
            double note20 = d.MoyenneNoteSur20 ?? (d.TauxReussiteGlobal / 100.0 * 20.0);
            MoyenneGeneraleSur20 = $"{note20.ToString("F1", fr)}/20";

            TauxReussiteGlobalLibelle = $"{d.TauxReussiteGlobal.ToString("F0", fr)} %";
            NombreEtudiants = d.NombreParticipants;

            var risques = new List<string>();
            int idx = 1;
            foreach (var sq in d.StatistiquesParQuestion ?? new List<StatistiqueQuestionResponseDto>())
            {
                if (sq.PourcentageEchec > SeuilPourcentageEchecAlerte)
                    risques.Add($"Q{idx}");
                idx++;
            }

            AfficherAlerteQuestionsRisque = risques.Count > 0;
            AlerteQuestionsRisqueDetail = AfficherAlerteQuestionsRisque
                ? $"Les questions suivantes ont un taux d'échec supérieur à {SeuilPourcentageEchecAlerte:F0} % : {string.Join(", ", risques)}."
                : "";

            BarresHistogramme.Clear();
            var rep = d.RepartitionParTranche;
            int max = 1;
            if (rep != null && rep.Count > 0)
                max = Math.Max(1, rep.Max());

            if (rep != null && rep.Count >= LabelsTranches.Length)
            {
                for (int i = 0; i < LabelsTranches.Length; i++)
                {
                    int c = i < rep.Count ? rep[i] : 0;
                    double h = max <= 0 ? 0 : HistogrammeMaxPx * c / max;
                    BarresHistogramme.Add(new HistogrammeBarVm(LabelsTranches[i], c, h));
                }
            }
            else
            {
                foreach (var lb in LabelsTranches)
                    BarresHistogramme.Add(new HistogrammeBarVm(lb, 0, 0));
            }

            BarresQuestionReussite.Clear();
            int n = 1;
            foreach (var sq in d.StatistiquesParQuestion ?? new List<StatistiqueQuestionResponseDto>())
            {
                double pr = sq.PourcentageReussite;
                bool alerte = sq.PourcentageEchec > SeuilPourcentageEchecAlerte;
                double w = QuestionReussiteBarVm.PisteBarrePx * Math.Clamp(pr, 0, 100) / 100.0;
                BarresQuestionReussite.Add(new QuestionReussiteBarVm($"Q{n}", pr, w, alerte));
                n++;
            }
        }
    }
}
