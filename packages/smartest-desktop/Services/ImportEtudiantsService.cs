using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace smartest_desktop.Services
{
    /// <summary>Résultat d'analyse d'un fichier d'import (emails valides + indication pour messages utilisateur).</summary>
    public sealed class ImportEtudiantsResult
    {
        public List<string> EmailsValides { get; } = new();
        /// <summary>Lignes non vides (CSV) ou lignes Excel avec au moins une cellule non vide.</summary>
        public int LignesAvecDuContenu { get; internal set; }
    }

    public static class ImportEtudiantsService
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<string> ImporterDepuisFichier(string chemin) =>
            ImporterDepuisFichierDetaille(chemin).EmailsValides;

        public static ImportEtudiantsResult ImporterDepuisFichierDetaille(string chemin)
        {
            string ext = Path.GetExtension(chemin).ToLowerInvariant();
            return ext switch
            {
                ".csv" => ImporterCsvDetaille(chemin),
                ".xlsx" => ImporterExcelDetaille(chemin),
                _ => throw new NotSupportedException($"Format non supporté : {ext}")
            };
        }

        private static ImportEtudiantsResult ImporterCsvDetaille(string chemin)
        {
            var result = new ImportEtudiantsResult();
            var lignes = File.ReadAllLines(chemin, Encoding.UTF8);
            bool premiereIgnoree = false;

            foreach (var ligne in lignes)
            {
                if (string.IsNullOrWhiteSpace(ligne)) continue;

                result.LignesAvecDuContenu++;

                var colonnes = ligne.Split(new[] { ',', ';', '\t' });

                foreach (var colonne in colonnes)
                {
                    string valeur = colonne.Trim().Trim('"').ToLowerInvariant();

                    if (!premiereIgnoree && valeur == "email")
                    {
                        premiereIgnoree = true;
                        break;
                    }

                    if (EstEmail(valeur))
                    {
                        result.EmailsValides.Add(valeur);
                        break;
                    }
                }
            }

            Dedoublonner(result);
            return result;
        }

        private static ImportEtudiantsResult ImporterExcelDetaille(string chemin)
        {
            var result = new ImportEtudiantsResult();

            using var doc = SpreadsheetDocument.Open(chemin, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return result;

            var sheet = workbookPart.WorksheetParts.FirstOrDefault();
            if (sheet == null) return result;

            var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
            var rows = sheet.Worksheet.Descendants<Row>().ToList();

            bool premiereIgnoree = false;

            foreach (var row in rows)
            {
                var cells = row.Elements<Cell>().ToList();
                if (cells.Count == 0) continue;

                bool ligneVide = cells.All(c =>
                    string.IsNullOrWhiteSpace(ObtenirValeurCellule(c, sharedStrings)));
                if (ligneVide) continue;

                result.LignesAvecDuContenu++;

                foreach (var cell in cells)
                {
                    string valeur = ObtenirValeurCellule(cell, sharedStrings)
                        .Trim().ToLowerInvariant();

                    if (!premiereIgnoree && valeur == "email")
                    {
                        premiereIgnoree = true;
                        break;
                    }

                    if (EstEmail(valeur))
                    {
                        result.EmailsValides.Add(valeur);
                        break;
                    }
                }
            }

            Dedoublonner(result);
            return result;
        }

        private static void Dedoublonner(ImportEtudiantsResult result)
        {
            var vus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var liste = new List<string>();
            foreach (var e in result.EmailsValides)
            {
                if (vus.Add(e))
                    liste.Add(e);
            }
            result.EmailsValides.Clear();
            result.EmailsValides.AddRange(liste);
        }

        private static string ObtenirValeurCellule(Cell cell, SharedStringTable? sharedStrings)
        {
            if (cell.CellValue == null) return string.Empty;
            string valeur = cell.CellValue.InnerText;

            if (cell.DataType?.Value == CellValues.SharedString && sharedStrings != null)
            {
                if (int.TryParse(valeur, out int index))
                    valeur = sharedStrings.ElementAt(index).InnerText;
            }

            return valeur;
        }

        public static bool EstEmail(string valeur) =>
            !string.IsNullOrWhiteSpace(valeur) && EmailRegex.IsMatch(valeur);
    }
}
