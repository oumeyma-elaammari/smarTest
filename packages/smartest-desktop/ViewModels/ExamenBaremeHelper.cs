using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace smartest_desktop.ViewModels
{
    internal static class ExamenBaremeHelper
    {
        public const double TotalCible = 20.0;
        public const double Pas = 0.25;

        public static void AppliquerBaremeParDefaut(IReadOnlyList<QuestionExamen> questions)
        {
            if (questions == null || questions.Count == 0)
                return;

            var totalQuestions = questions.Count;
            var scores = questions.Select(q => CalculerScore(q, totalQuestions)).ToList();
            var sommeScores = scores.Sum();
            if (sommeScores <= 0)
            {
                RepartirEquitablement(questions);
                return;
            }

            var bruts = scores.Select(s => (s / sommeScores) * TotalCible).ToList();
            var arrondis = bruts.Select(ArrondirAuPas).ToList();
            CorrigerEcart(bruts, arrondis);

            for (int i = 0; i < questions.Count; i++)
                questions[i].BaremePoints = arrondis[i];
        }

        public static bool EstAuPas(double valeur)
        {
            var quarters = valeur / Pas;
            return Math.Abs(quarters - Math.Round(quarters)) < 1e-9;
        }

        public static string FormatPoints(double points) =>
            points.ToString("0.##", CultureInfo.GetCultureInfo("fr-FR"));

        private static void RepartirEquitablement(IReadOnlyList<QuestionExamen> questions)
        {
            var brut = TotalCible / questions.Count;
            var arrondi = ArrondirAuPas(brut);
            foreach (var q in questions)
                q.BaremePoints = arrondi;

            var arrondis = questions.Select(q => q.BaremePoints).ToList();
            CorrigerEcart(arrondis, arrondis);
            for (int i = 0; i < questions.Count; i++)
                questions[i].BaremePoints = arrondis[i];
        }

        private static double CalculerScore(QuestionExamen q, int totalQuestions)
        {
            var type = (q.Type ?? string.Empty).Trim().ToUpperInvariant();
            var difficulte = (q.Difficulte ?? string.Empty).Trim().ToLowerInvariant();

            var typeWeight = type switch
            {
                "REDACTION" => 1.40,
                "CHECKBOX" => 1.22,
                "IMAGE" => 1.12,
                "QCM" => 1.00,
                "VF" => 0.86,
                _ => 1.00
            };

            var diffWeight = difficulte switch
            {
                "difficile" => 1.20,
                "facile" => 0.88,
                _ => 1.00
            };

            // Plus il y a de questions, plus on réduit l'écart entre poids.
            var spreadFactor = totalQuestions switch
            {
                <= 6 => 1.12,
                <= 10 => 1.00,
                <= 16 => 0.93,
                _ => 0.88
            };

            return 1.0 + ((typeWeight * diffWeight) - 1.0) * spreadFactor;
        }

        private static double ArrondirAuPas(double value) =>
            Math.Round(value / Pas, MidpointRounding.AwayFromZero) * Pas;

        private static void CorrigerEcart(IReadOnlyList<double> bruts, IList<double> arrondis)
        {
            var ecart = Math.Round(TotalCible - arrondis.Sum(), 8);
            if (Math.Abs(ecart) < 1e-9)
                return;

            int steps = (int)Math.Round(Math.Abs(ecart) / Pas);
            var augmenter = ecart > 0;

            var ordre = Enumerable.Range(0, arrondis.Count)
                .OrderByDescending(i => augmenter ? (bruts[i] - arrondis[i]) : (arrondis[i] - bruts[i]))
                .ToList();

            int guard = 0;
            while (steps > 0 && guard < 10_000)
            {
                foreach (var i in ordre)
                {
                    if (steps == 0)
                        break;
                    var candidate = augmenter ? arrondis[i] + Pas : arrondis[i] - Pas;
                    if (candidate < 0)
                        continue;
                    arrondis[i] = candidate;
                    steps--;
                }
                guard++;
            }
        }
    }
}
