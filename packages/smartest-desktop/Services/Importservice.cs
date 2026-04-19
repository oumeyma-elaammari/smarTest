using DocumentFormat.OpenXml.Packaging;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace smartest_desktop.Services
{
    public class ImportService
    {
        private readonly LocalCoursService _coursService;

        // ══════════════════════════════════════════════════════════
        //  Formats acceptés
        // ══════════════════════════════════════════════════════════
        private static readonly string[] FormatsPDF = { ".pdf" };
        private static readonly string[] FormatsWord = { ".docx", ".doc" };
        private static readonly string[] FormatsTexte = { ".txt", ".md" };

        public ImportService()
        {
            _coursService = new LocalCoursService();
        }

        // ══════════════════════════════════════════════════════════
        //  MÉTHODE PRINCIPALE — détecte le format automatiquement
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Importe un fichier (PDF, Word ou Texte) et crée un CoursLocal.
        /// Détecte automatiquement le format selon l'extension.
        /// </summary>
        /// <param name="cheminFichier">Chemin complet du fichier</param>
        /// <param name="titrePersonnalise">Titre optionnel — sinon nom du fichier utilisé</param>
        /// <returns>Le cours créé dans SQLite</returns>
        public CoursLocal ImporterFichier(string cheminFichier, string? titrePersonnalise = null)
        {
            // 1. Vérifier que le fichier existe
            if (!File.Exists(cheminFichier))
                throw new FileNotFoundException($"Fichier introuvable : {cheminFichier}");

            // 2. Détecter le format
            string ext = Path.GetExtension(cheminFichier).ToLower();
            string contenu;

            if (FormatsPDF.Contains(ext))
                contenu = ExtraireTextePdf(cheminFichier);

            else if (FormatsWord.Contains(ext))
                contenu = ExtraireTexteWord(cheminFichier);

            else if (FormatsTexte.Contains(ext))
                contenu = ExtraireTexteSimple(cheminFichier);

            else
                throw new NotSupportedException(
                    $"Format '{ext}' non supporté.\n" +
                    $"Formats acceptés : PDF, DOCX, DOC, TXT, MD");

            // 3. Vérifier que le contenu n'est pas vide
            if (string.IsNullOrWhiteSpace(contenu))
                throw new InvalidOperationException(
                    "Le fichier est vide ou le contenu n'a pas pu être extrait.\n" +
                    "Vérifiez que le fichier n'est pas protégé ou corrompu.");

            // 4. Définir le titre
            string titre = string.IsNullOrWhiteSpace(titrePersonnalise)
                ? Path.GetFileNameWithoutExtension(cheminFichier)
                : titrePersonnalise.Trim();

            // 5. Sauvegarder dans SQLite
            return _coursService.Ajouter(titre, contenu, cheminFichier);
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRACTION PDF
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Extrait le texte d'un fichier PDF page par page.
        /// Utilise la bibliothèque PdfPig.
        /// </summary>
        private string ExtraireTextePdf(string cheminFichier)
        {
            try
            {
                var sb = new StringBuilder();

                using PdfDocument pdf = PdfDocument.Open(cheminFichier);

                foreach (Page page in pdf.GetPages())
                {
                    // Extraire le texte de la page
                    string textePage = page.Text;

                    if (!string.IsNullOrWhiteSpace(textePage))
                    {
                        sb.AppendLine(textePage);
                        sb.AppendLine(); // ligne vide entre pages
                    }
                }

                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Erreur lors de la lecture du PDF.\n" +
                    $"Le fichier est peut-être protégé par mot de passe.\n" +
                    $"Détail : {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRACTION WORD (.docx)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Extrait le texte d'un fichier Word (.docx).
        /// Utilise DocumentFormat.OpenXml.
        /// </summary>
        private string ExtraireTexteWord(string cheminFichier)
        {
            string ext = Path.GetExtension(cheminFichier).ToLower();

            // .doc (ancien format) — non supporté directement
            if (ext == ".doc")
                throw new NotSupportedException(
                    "Le format .doc (Word ancien) n'est pas supporté.\n" +
                    "Convertissez votre fichier en .docx dans Word : Fichier → Enregistrer sous → .docx");

            try
            {
                var sb = new StringBuilder();

                using WordprocessingDocument doc =
                    WordprocessingDocument.Open(cheminFichier, isEditable: false);

                var body = doc.MainDocumentPart?.Document?.Body;

                if (body == null)
                    throw new InvalidOperationException("Document Word vide ou corrompu.");

                // Parcourir paragraphe par paragraphe
                foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    string texteParagraphe = paragraph.InnerText;

                    if (!string.IsNullOrWhiteSpace(texteParagraphe))
                        sb.AppendLine(texteParagraphe);
                }

                return sb.ToString().Trim();
            }
            catch (NotSupportedException)
            {
                throw; // propager l'exception .doc
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Erreur lors de la lecture du fichier Word.\n" +
                    $"Détail : {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRACTION TEXTE SIMPLE (.txt, .md)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Lit un fichier texte simple (.txt ou .md).
        /// </summary>
        private string ExtraireTexteSimple(string cheminFichier)
        {
            try
            {
                return File.ReadAllText(cheminFichier, Encoding.UTF8).Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Erreur lors de la lecture du fichier texte.\n" +
                    $"Détail : {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  UTILITAIRES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aperçu du contenu extrait (premiers 500 caractères).
        /// Utilisé dans CoursWindow pour afficher un aperçu avant sauvegarde.
        /// </summary>
        public static string Apercu(string contenu, int nbCaracteres = 500)
        {
            if (string.IsNullOrWhiteSpace(contenu))
                return "(contenu vide)";

            return contenu.Length <= nbCaracteres
                ? contenu
                : contenu[..nbCaracteres] + "\n\n... (tronqué)";
        }

        /// <summary>
        /// Vérifie si un fichier est supporté avant import.
        /// </summary>
        public static bool EstFormatSupporte(string cheminFichier)
        {
            string ext = Path.GetExtension(cheminFichier).ToLower();
            return FormatsPDF.Contains(ext)
                || FormatsWord.Contains(ext)
                || FormatsTexte.Contains(ext);
        }

        /// <summary>
        /// Retourne le filtre pour OpenFileDialog.
        /// </summary>
        public static string FiltreOpenFileDialog =>
            "Tous les formats supportés|*.pdf;*.docx;*.doc;*.txt;*.md" +
            "|PDF|*.pdf" +
            "|Word|*.docx;*.doc" +
            "|Texte|*.txt;*.md";

        /// <summary>
        /// Retourne des statistiques sur le contenu extrait.
        /// </summary>
        public static (int NbMots, int NbLignes, int NbCaracteres) Statistiques(string contenu)
        {
            if (string.IsNullOrWhiteSpace(contenu))
                return (0, 0, 0);

            int nbMots = contenu.Split(new[] { ' ', '\n', '\r', '\t' },
                                StringSplitOptions.RemoveEmptyEntries).Length;
            int nbLignes = contenu.Split('\n').Length;
            int nbCaracteres = contenu.Length;

            return (nbMots, nbLignes, nbCaracteres);
        }
    }
}