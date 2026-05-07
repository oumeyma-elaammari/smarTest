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
            ArgumentNullException.ThrowIfNull(chemin);

            if (!File.Exists(chemin))
                throw new FileNotFoundException("Fichier d’import introuvable.", chemin);

            string ext = Path.GetExtension(chemin).ToLowerInvariant();
            try
            {
                return ext switch
                {
                    ".csv" => ImporterCsvDetaille(chemin),
                    ".xlsx" => ImporterExcelDetaille(chemin),
                    _ => throw new NotSupportedException($"Format non supporté : {ext}")
                };
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException($"Accès refusé au fichier : {chemin}", ex);
            }
            catch (Exception ex) when (ex is OpenXmlPackageException or InvalidDataException)
            {
                throw new InvalidOperationException(
                    "Le fichier Excel semble corrompu ou n’est pas un classeur .xlsx valide.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Import des étudiants impossible : {ex.Message}", ex);
            }
        }

        private static ImportEtudiantsResult ImporterCsvDetaille(string chemin)
        {
            var result = new ImportEtudiantsResult();
            string[] lignes;
            try
            {
                lignes = File.ReadAllLines(chemin, Encoding.UTF8);
            }
            catch (DecoderFallbackException ex)
            {
                throw new InvalidOperationException(
                    "Encodage du CSV illisible. Enregistrez le fichier en UTF-8.", ex);
            }

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
