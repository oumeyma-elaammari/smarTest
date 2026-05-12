using System;
using System.Collections.Generic;
using System.Linq;

namespace smartest_desktop.ViewModels
{
    public sealed class PedagogicalQualityReport
    {
        public int Score { get; init; }
        public string Label { get; init; } = "A revoir";
        public string Color { get; init; } = "#DC2626";
        public List<string> Issues { get; init; } = new();
    }

    public static class PedagogicalQualityEvaluator
    {
        public static PedagogicalQualityReport EvaluateQuizQuestion(QuestionQCM q)
        {
            int score = 100;
            var issues = new List<string>();

            var enonce = q.Enonce?.Trim() ?? string.Empty;
            var explication = q.Explication?.Trim() ?? string.Empty;
            var options = new[] { q.OptionA, q.OptionB, q.OptionC, q.OptionD }
                .Select(o => (o ?? string.Empty).Trim())
                .ToArray();

            if (enonce.Length < 20)
            {
                score -= 25;
                issues.Add("Énoncé trop court");
            }
            if (!enonce.EndsWith("?", StringComparison.Ordinal))
            {
                score -= 8;
                issues.Add("Énoncé sans formulation interrogative");
            }

            if (options.Any(string.IsNullOrWhiteSpace))
            {
                score -= 20;
                issues.Add("Options incomplètes");
            }

            if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() < 4)
            {
                score -= 20;
                issues.Add("Distracteurs trop proches ou répétés");
            }

            if (!"ABCD".Contains((q.ReponseCorrecte ?? string.Empty).Trim(), StringComparison.Ordinal))
            {
                score -= 20;
                issues.Add("Réponse correcte invalide");
            }

            if (explication.Length < 25)
            {
                score -= 15;
                issues.Add("Explication insuffisante pour l'apprentissage");
            }

            score = Math.Clamp(score, 0, 100);
            return BuildReport(score, issues);
        }

        public static PedagogicalQualityReport EvaluateExamQuestion(QuestionExamen q)
        {
            int score = 100;
            var issues = new List<string>();
            var enonce = q.Enonce?.Trim() ?? string.Empty;
            var explication = q.Explication?.Trim() ?? string.Empty;

            if (enonce.Length < 20)
            {
                score -= 20;
                issues.Add("Énoncé trop court");
            }

            if (q.HasOptions)
            {
                var options = new[] { q.OptionA, q.OptionB, q.OptionC, q.OptionD }
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .Select(o => o!.Trim())
                    .ToList();
                if (options.Count < 2)
                {
                    score -= 30;
                    issues.Add("Propositions insuffisantes");
                }
                if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
                {
                    score -= 15;
                    issues.Add("Propositions répétées");
                }
            }

            if (q.IsRedaction && (q.ReponseModele?.Trim().Length ?? 0) < 30)
            {
                score -= 20;
                issues.Add("Réponse modèle trop courte");
            }

            if (q.IsQcmOuVf && !"ABCD".Contains((q.ReponseCorrecte ?? string.Empty).Trim(), StringComparison.Ordinal))
            {
                score -= 20;
                issues.Add("Réponse correcte invalide");
            }

            if (explication.Length < 20)
            {
                score -= 10;
                issues.Add("Explication à renforcer");
            }

            score = Math.Clamp(score, 0, 100);
            return BuildReport(score, issues);
        }

        private static PedagogicalQualityReport BuildReport(int score, List<string> issues)
        {
            if (score >= 80)
                return new PedagogicalQualityReport { Score = score, Label = "Bonne", Color = "#16A34A", Issues = issues };
            if (score >= 60)
                return new PedagogicalQualityReport { Score = score, Label = "Moyenne", Color = "#D97706", Issues = issues };
            return new PedagogicalQualityReport { Score = score, Label = "A revoir", Color = "#DC2626", Issues = issues };
        }
    }
}
