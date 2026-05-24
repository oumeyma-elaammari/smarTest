using System;
using System.Collections.Generic;
using System.Linq;

namespace smartest_desktop.ViewModels
{
    /// <summary>Répartition et libellés pour la sélection multiple Facile / Moyen / Difficile.</summary>
    internal static class DifficulteMultiSelectHelper
    {
        public static readonly string[] Ordre = { "Facile", "Moyen", "Difficile" };

        public const string Separateur = " + ";

        public static string FormaterLibelle(IEnumerable<string> niveauxSelectionnes)
        {
            var set = niveauxSelectionnes as HashSet<string>
                      ?? niveauxSelectionnes.ToHashSet(StringComparer.Ordinal);
            var parts = Ordre.Where(set.Contains).ToList();
            return parts.Count == 0 ? "Moyen" : string.Join(Separateur, parts);
        }

        public static List<string> ParserLibelle(string? libelle)
        {
            if (string.IsNullOrWhiteSpace(libelle))
                return new List<string> { "Moyen" };

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in libelle.Split(
                         new[] { Separateur, ",", "/", ";" },
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var normalized = NormaliserNiveau(part);
                if (normalized != null)
                    set.Add(normalized);
            }

            if (set.Count == 0)
                set.Add("Moyen");

            return Ordre.Where(set.Contains).ToList();
        }

        /// <summary>Bascule un niveau ; retourne false si désélection du dernier niveau restant.</summary>
        public static bool Toggle(HashSet<string> selection, string niveau)
        {
            if (!Ordre.Contains(niveau, StringComparer.Ordinal))
                return false;

            if (selection.Contains(niveau))
            {
                if (selection.Count <= 1)
                    return false;
                selection.Remove(niveau);
            }
            else
            {
                selection.Add(niveau);
            }

            return true;
        }

        /// <summary>Répartit <paramref name="total"/> entre les niveaux (reste au début de l'ordre canonique).</summary>
        public static Dictionary<string, int> Repartir(int total, IReadOnlyList<string> niveaux)
        {
            if (niveaux == null || niveaux.Count == 0)
                throw new ArgumentException("Au moins un niveau requis.", nameof(niveaux));
            if (total < 0)
                throw new ArgumentOutOfRangeException(nameof(total));

            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            int baseCount = total / niveaux.Count;
            int remainder = total % niveaux.Count;
            for (int i = 0; i < niveaux.Count; i++)
                result[niveaux[i]] = baseCount + (i < remainder ? 1 : 0);
            return result;
        }

        private static string? NormaliserNiveau(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (Ordre.Contains(raw.Trim(), StringComparer.Ordinal))
                return raw.Trim();

            return raw.Trim().ToLowerInvariant() switch
            {
                "facile" => "Facile",
                "moyen" => "Moyen",
                "difficile" => "Difficile",
                _ => null
            };
        }
    }
}
