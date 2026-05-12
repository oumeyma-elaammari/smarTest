using System;
using System.Collections.Generic;
using System.Linq;

namespace smartest_desktop.ViewModels
{
    /// <summary>
    /// Répartit les durées indicatives (secondes) des questions pour que leur somme
    /// corresponde à la durée totale de l'épreuve, avec bornes 5–7200 s par question.
    /// </summary>
    internal static class ExamenDureeQuestionsHelper
    {
        public const int MinSecondesParQuestion = 5;
        public const int MaxSecondesParQuestion = 7200;

        public static int SecondesTotalesCible(int dureeMinutes) =>
            Math.Clamp(dureeMinutes, 1, 600) * 60;

        /// <summary>
        /// Vrai si on peut assigner au moins <see cref="MinSecondesParQuestion"/> s à chaque question.
        /// </summary>
        public static bool RepartitionAuMinimumRealisable(int nombreQuestions, int totalSecondes) =>
            nombreQuestions == 0 || totalSecondes >= nombreQuestions * MinSecondesParQuestion;

        /// <summary>
        /// Répartition équitable : même base + reste sur les premières, puis ajustements si plafond 7200 s.
        /// </summary>
        public static void RepartirEquitable(IReadOnlyList<QuestionExamen> questions, int dureeMinutes)
        {
            int total = SecondesTotalesCible(dureeMinutes);
            RepartirEquitableSurListe(questions, total);
        }

        public static void RepartirEquitableSurListe(IReadOnlyList<QuestionExamen> questions, int totalSecondes)
        {
            int n = questions.Count;
            if (n == 0)
                return;

            if (totalSecondes < n * MinSecondesParQuestion)
            {
                for (int i = 0; i < n; i++)
                    questions[i].DureeSecondesIndicative = MinSecondesParQuestion;
                return;
            }

            var arr = new int[n];
            int slack = totalSecondes - n * MinSecondesParQuestion;
            int baseSlack = slack / n;
            int rem = slack % n;
            for (int i = 0; i < n; i++)
            {
                int v = MinSecondesParQuestion + baseSlack + (i < rem ? 1 : 0);
                arr[i] = Math.Min(MaxSecondesParQuestion, v);
            }

            AjusterSommeVersCibleRoundRobin(arr, totalSecondes);
            for (int i = 0; i < n; i++)
                questions[i].DureeSecondesIndicative = arr[i];
        }

        /// <summary>
        /// Conserve <paramref name="conservee"/> ; les autres questions partagent le reliquat de secondes.
        /// </summary>
        public static void RepartirEnConservantUneQuestion(
            IReadOnlyList<QuestionExamen> toutes,
            QuestionExamen conservee,
            int dureeMinutes)
        {
            int target = SecondesTotalesCible(dureeMinutes);
            var autres = toutes.Where(q => !ReferenceEquals(q, conservee)).ToList();
            if (autres.Count == 0)
            {
                conservee.DureeSecondesIndicative = Math.Clamp(
                    target,
                    MinSecondesParQuestion,
                    MaxSecondesParQuestion);
                return;
            }

            int v = conservee.DureeSecondesIndicative;
            int sumAutres = target - v;

            if (sumAutres < autres.Count * MinSecondesParQuestion)
            {
                int maxCons = target - autres.Count * MinSecondesParQuestion;
                conservee.DureeSecondesIndicative = Math.Max(MinSecondesParQuestion, maxCons);
                v = conservee.DureeSecondesIndicative;
                sumAutres = target - v;
            }

            if (sumAutres > autres.Count * MaxSecondesParQuestion)
            {
                int minCons = target - autres.Count * MaxSecondesParQuestion;
                conservee.DureeSecondesIndicative = Math.Max(MinSecondesParQuestion, minCons);
                v = conservee.DureeSecondesIndicative;
                sumAutres = target - v;
            }

            RepartirEquitableSurListe(autres, sumAutres);
        }

        /// <summary>
        /// Ajuste seconde par seconde en round-robin pour atteindre <paramref name="cible"/>.
        /// </summary>
        private static void AjusterSommeVersCibleRoundRobin(int[] arr, int cible)
        {
            int n = arr.Length;
            int rr = 0;
            for (int guard = 0; guard < 100_000; guard++)
            {
                int sum = 0;
                for (int i = 0; i < n; i++)
                    sum += arr[i];
                int diff = cible - sum;
                if (diff == 0)
                    return;

                if (diff > 0)
                {
                    bool moved = false;
                    for (int t = 0; t < n; t++)
                    {
                        int i = (rr + t) % n;
                        if (arr[i] < MaxSecondesParQuestion)
                        {
                            arr[i]++;
                            moved = true;
                            rr = (i + 1) % n;
                            break;
                        }
                    }

                    if (!moved)
                        return;
                }
                else
                {
                    bool moved = false;
                    for (int t = 0; t < n; t++)
                    {
                        int i = (rr + t) % n;
                        if (arr[i] > MinSecondesParQuestion)
                        {
                            arr[i]--;
                            moved = true;
                            rr = (i + 1) % n;
                            break;
                        }
                    }

                    if (!moved)
                        return;
                }
            }
        }
    }
}
