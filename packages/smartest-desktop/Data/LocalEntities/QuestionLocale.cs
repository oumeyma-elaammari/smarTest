using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Windows.Media.Imaging;

namespace smartest_desktop.Data.LocalEntities
{
    [Table("question_locale")]
    public class QuestionLocale
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Enonce { get; set; } = string.Empty;

        /// <summary>QCM | VF | REDACTION | DEVELOPPEMENT</summary>
        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "QCM";

        /// <summary>FACILE | MOYEN | DIFFICILE</summary>
        [MaxLength(20)]
        public string Difficulte { get; set; } = "MOYEN";

        public string Explication { get; set; } = string.Empty;

        /// <summary>
        /// Réponse modèle pour REDACTION/DEVELOPPEMENT.
        /// Stockée UNIQUEMENT en local — jamais envoyée au backend.
        /// Utilisée par Ollama pour corriger les copies.
        /// </summary>
        public string ReponseModele { get; set; } = string.Empty;

        // ══════════════════════════════════════════════════════════
        //  IMAGE (optionnelle)
        //  Stockée en Base64 directement dans SQLite
        //  → Portable, pas de risque de fichier perdu
        //  → Envoyée via WebSocket pendant l'examen
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Image encodée en Base64.
        /// null = pas d'image pour cette question.
        /// Tous formats acceptés : PNG, JPEG, BMP, GIF, TIFF, WEBP...
        /// </summary>
        public string? ImageBase64 { get; set; }

        /// <summary>
        /// Type MIME de l'image.
        /// ex: "image/png", "image/jpeg", "image/bmp", "image/gif"
        /// </summary>
        [MaxLength(50)]
        public string? ImageType { get; set; }

        /// <summary>
        /// Nom original du fichier image (pour affichage info).
        /// ex: "schema_reseau.png"
        /// </summary>
        [MaxLength(255)]
        public string? ImageNom { get; set; }

        // ══════════════════════════════════════════════════════════
        //  PROPRIÉTÉS CALCULÉES (non stockées en base)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// True si cette question a une image associée.
        /// </summary>
        [NotMapped]
        public bool HasImage => !string.IsNullOrEmpty(ImageBase64);

        /// <summary>
        /// Convertit le Base64 en BitmapImage pour l'affichage WPF.
        /// Retourne null si pas d'image.
        /// </summary>
        [NotMapped]
        public BitmapImage? ImageBitmap
        {
            get
            {
                if (!HasImage) return null;

                try
                {
                    byte[] bytes = Convert.FromBase64String(ImageBase64!);
                    using var stream = new MemoryStream(bytes);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // ← thread-safe pour WPF

                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  MÉTHODES UTILITAIRES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Charge une image depuis un fichier et la convertit en Base64.
        /// Accepte tous les formats images.
        /// </summary>
        public void ChargerImage(string cheminFichier)
        {
            if (!File.Exists(cheminFichier))
                throw new FileNotFoundException($"Fichier image introuvable : {cheminFichier}");

            byte[] bytes = File.ReadAllBytes(cheminFichier);
            ImageBase64 = Convert.ToBase64String(bytes);
            ImageNom = Path.GetFileName(cheminFichier);
            ImageType = DetecterTypeMime(cheminFichier);
        }

        /// <summary>
        /// Supprime l'image associée à cette question.
        /// </summary>
        public void SupprimerImage()
        {
            ImageBase64 = null;
            ImageType = null;
            ImageNom = null;
        }

        /// <summary>
        /// Retourne la taille de l'image en KB.
        /// </summary>
        [NotMapped]
        public double TailleImageKB
        {
            get
            {
                if (!HasImage) return 0;
                // Base64 → taille réelle ≈ Base64.Length * 3/4
                return Math.Round(ImageBase64!.Length * 0.75 / 1024, 1);
            }
        }

        // ── Helpers privés ────────────────────────────────────────

        private static string DetecterTypeMime(string cheminFichier)
        {
            string ext = Path.GetExtension(cheminFichier).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                ".tiff" or ".tif" => "image/tiff",
                ".webp" => "image/webp",
                ".ico" => "image/x-icon",
                ".svg" => "image/svg+xml",
                _ => "image/jpeg" // défaut
            };
        }

        // ── Clé étrangère ─────────────────────────────────────────
        public int CoursId { get; set; }

        [ForeignKey(nameof(CoursId))]
        public CoursLocal? Cours { get; set; }

        // ── Navigation ────────────────────────────────────────────
        public List<ReponseLocale> Reponses { get; set; } = new();

        public override string ToString() => Enonce;
    }
}