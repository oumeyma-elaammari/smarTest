using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public sealed class LigneCorrectionEditable : BaseViewModel
    {
        private double _note;
        private readonly Action? _onNoteChanged;

        public long QuestionId { get; init; }
        public string Enonce { get; init; } = "";
        public string TypeQuestion { get; init; } = "";
        public string ReponseLibelle { get; init; } = "";
        public double? ScorePartiel { get; init; }
        public double PointsMax { get; init; }
        public bool CorrigeParIa { get; init; }
        public string? GroqStatut { get; init; }

        public LigneCorrectionEditable(Action? onNoteChanged = null) => _onNoteChanged = onNoteChanged;

        public double Note
        {
            get => _note;
            set
            {
                double max = PointsMax > 0 ? PointsMax : 20;
                double clamped = Math.Round(Math.Clamp(value, 0, max), 2, MidpointRounding.AwayFromZero);
                if (!SetProperty(ref _note, clamped)) return;
                _onNoteChanged?.Invoke();
            }
        }

        public string SousTitre =>
            $"{TypeQuestion}{(CorrigeParIa ? " · Groq" : "")}{(GroqStatut != null ? " · " + GroqStatut : "")} · auto {(ScorePartiel?.ToString("0.##", CultureInfo.InvariantCulture) ?? "…")}";
    }

    public sealed class ExamenCorrectionEtudiantViewModel : BaseViewModel
    {
        private readonly ExamenWebPublicationApiService _api = new();
        private readonly long _examenId;
        private readonly long _etudiantId;
        private readonly string _token;
        private double _bareme = 20;
        private bool _busy;
        private string? _erreur;

        public string TitreFenetre { get; }
        public ObservableCollection<LigneCorrectionEditable> Lignes { get; } = new();

        public double NoteTotale =>
            Math.Round(Lignes.Sum(l => l.Note), 2, MidpointRounding.AwayFromZero);

        public double Bareme
        {
            get => _bareme;
            private set => SetProperty(ref _bareme, value);
        }

        public bool Busy
        {
            get => _busy;
            private set
            {
                if (!SetProperty(ref _busy, value)) return;
                if (ValiderCommand is RelayCommand c) c.RaiseCanExecuteChanged();
            }
        }

        public string? Erreur
        {
            get => _erreur;
            private set
            {
                if (!SetProperty(ref _erreur, value)) return;
                OnPropertyChanged(nameof(AfficherErreur));
            }
        }

        public bool AfficherErreur => !string.IsNullOrWhiteSpace(Erreur);

        public ICommand ValiderCommand { get; }

        public event Action? DemandeFermetureSucces;

        public ExamenCorrectionEtudiantViewModel(long examenId, string titreExamen, long etudiantId, string bearerToken)
        {
            _examenId = examenId;
            _etudiantId = etudiantId;
            _token = bearerToken;
            TitreFenetre = $"{titreExamen?.Trim() ?? "Examen"} — étudiant #{etudiantId}";
            ValiderCommand = new RelayCommand(_ => _ = ValiderAsync(), _ => !Busy && Lignes.Count > 0 && CorrectionNotesValides);
        }

        private void NotifierNotes()
        {
            OnPropertyChanged(nameof(NoteTotale));
            OnPropertyChanged(nameof(CorrectionNotesValides));
            if (ValiderCommand is RelayCommand c) c.RaiseCanExecuteChanged();
        }

        public bool CorrectionNotesValides =>
            Lignes.Count > 0
            && Lignes.All(l => l.Note >= 0 && l.Note <= l.PointsMax + 1e-6)
            && NoteTotale <= Bareme + 1e-6;

        public async Task ChargerAsync()
        {
            Busy = true;
            Erreur = null;
            Lignes.Clear();
            try
            {
                var root = await _api.GetCorrectionsEtudiantAsync(_token, _examenId, _etudiantId);
                var baremeTok = root["baremeReference"];
                if (baremeTok != null && (baremeTok.Type == JTokenType.Float || baremeTok.Type == JTokenType.Integer))
                    Bareme = baremeTok.Value<double>();

                var arr = root["lignes"] as JArray;
                if (arr == null)
                {
                    Erreur = "Aucune ligne de correction.";
                    return;
                }

                foreach (var item in arr)
                {
                    if (item is not JObject row) continue;
                    long qid = row["questionId"]?.Value<long>() ?? 0;
                    if (qid <= 0) continue;
                    var spTok = row["scorePartiel"];
                    double? sp = spTok == null || spTok.Type == JTokenType.Null ? null : spTok.Value<double>();
                    var nflTok = row["noteFinaleLigne"];
                    double? nfl = nflTok == null || nflTok.Type == JTokenType.Null ? null : nflTok.Value<double>();
                    double pts = row["pointsQuestionMax"]?.Value<double>() ?? 0;
                    double init = nfl ?? sp ?? 0;
                    Lignes.Add(new LigneCorrectionEditable(NotifierNotes)
                    {
                        QuestionId = qid,
                        Enonce = row["enonce"]?.Value<string>() ?? "",
                        TypeQuestion = row["typeQuestion"]?.Value<string>() ?? "",
                        ReponseLibelle = row["reponseEtudiantLibelle"]?.Value<string>() ?? "",
                        ScorePartiel = sp,
                        PointsMax = pts,
                        CorrigeParIa = row["corrigeParIa"]?.Value<bool>() ?? false,
                        GroqStatut = row["groqStatut"]?.Value<string>(),
                        Note = init,
                    });
                }

                NotifierNotes();
            }
            catch (SmartestApiException ex)
            {
                Erreur = ex.Message;
            }
            catch (Exception ex)
            {
                Erreur = ex.Message;
            }
            finally
            {
                Busy = false;
            }
        }

        private async Task ValiderAsync()
        {
            if (!CorrectionNotesValides)
            {
                Erreur = $"Notes invalides : chaque question est plafonnée à son barème et le total ne peut pas dépasser {Bareme:0.##}.";
                return;
            }
            Busy = true;
            Erreur = null;
            try
            {
                var notes = new Dictionary<string, double>();
                foreach (var l in Lignes)
                    notes[l.QuestionId.ToString(CultureInfo.InvariantCulture)] = l.Note;

                var body = new
                {
                    notesFinales = notes,
                    noteTotale = NoteTotale,
                    remarque = (string?)null,
                };
                string json = JsonConvert.SerializeObject(body);
                await _api.ValiderCorrectionsDetailAsync(_token, _examenId, _etudiantId, json);
                try
                {
                    await _api.SynchroniserNoteWorkbenchAsync(_token, _examenId, _etudiantId);
                }
                catch (SmartestApiException)
                {
                    /* Publication déjà effectuée par le serveur selon la configuration. */
                }

                DemandeFermetureSucces?.Invoke();
            }
            catch (SmartestApiException ex)
            {
                Erreur = ex.Message;
            }
            catch (Exception ex)
            {
                Erreur = ex.Message;
            }
            finally
            {
                Busy = false;
            }
        }
    }
}
