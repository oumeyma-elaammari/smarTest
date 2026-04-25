using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data.LocalEntities;

namespace smartest_desktop.Data
{
    public class LocalDbContext : DbContext
    {
        // ══════════════════════════════════════════════════════════
        //  TABLES SQLite
        // ══════════════════════════════════════════════════════════

        public DbSet<CoursLocal> Cours { get; set; }
        public DbSet<QuestionLocale> Questions { get; set; }
        public DbSet<ReponseLocale> Reponses { get; set; }
        public DbSet<QuizLocal> Quiz { get; set; }
        public DbSet<ExamenLocal> Examens { get; set; }

        // ══════════════════════════════════════════════════════════
        //  CONFIGURATION — fichier smartest_local.db
        // ══════════════════════════════════════════════════════════

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=smartest_local.db");
        }

        // ══════════════════════════════════════════════════════════
        //  RELATIONS ET COLONNES
        // ══════════════════════════════════════════════════════════

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── CoursLocal ────────────────────────────────────────
            modelBuilder.Entity<CoursLocal>(entity =>
            {
                entity.ToTable("cours_local");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Titre).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Contenu).HasColumnType("TEXT");
                entity.Property(c => c.CheminFichier).HasMaxLength(500);
                entity.Property(c => c.Categorie).HasMaxLength(100);
                entity.Property(c => c.TypeFichier).HasMaxLength(50);
                entity.Property(c => c.TailleFichier).HasDefaultValue(0L);
                entity.Property(c => c.NomFichier).HasMaxLength(500);
                entity.Property(c => c.Statut).HasMaxLength(50).HasDefaultValue("Actif");
            });

            // ── QuestionLocale ────────────────────────────────────
            modelBuilder.Entity<QuestionLocale>(entity =>
            {
                entity.ToTable("question_locale");
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Enonce).IsRequired().HasColumnType("TEXT");
                entity.Property(q => q.Type).IsRequired().HasMaxLength(20);
                entity.Property(q => q.Difficulte).HasMaxLength(20);
                entity.Property(q => q.Explication).HasColumnType("TEXT");

                // Réponse modèle — locale uniquement
                entity.Property(q => q.ReponseModele).HasColumnType("TEXT");

                // ── Image Base64 ──────────────────────────────────
                // Stockée directement dans SQLite
                // TEXT car Base64 peut être très long
                entity.Property(q => q.ImageBase64).HasColumnType("TEXT");
                entity.Property(q => q.ImageType).HasMaxLength(50);
                entity.Property(q => q.ImageNom).HasMaxLength(255);

                // Relation : Question → Cours (Many-to-One)
                entity.HasOne(q => q.Cours)
                      .WithMany(c => c.Questions)
                      .HasForeignKey(q => q.CoursId)
                      .OnDelete(DeleteBehavior.Cascade);
                // ↑ Supprimer cours → supprime ses questions
            });

            // ── ReponseLocale ─────────────────────────────────────
            modelBuilder.Entity<ReponseLocale>(entity =>
            {
                entity.ToTable("reponse_locale");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Contenu).IsRequired().HasColumnType("TEXT");

                // Relation : Reponse → Question (Many-to-One)
                entity.HasOne(r => r.Question)
                      .WithMany(q => q.Reponses)
                      .HasForeignKey(r => r.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade);
                // ↑ Supprimer question → supprime ses réponses
            });

            // ── QuizLocal ─────────────────────────────────────────
            modelBuilder.Entity<QuizLocal>(entity =>
            {
                entity.ToTable("quiz_local");
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Titre).IsRequired().HasMaxLength(200);
                entity.Property(q => q.Description).HasColumnType("TEXT");
                entity.Property(q => q.Statut)
                      .HasMaxLength(20)
                      .HasDefaultValue("BROUILLON");

                // Relation : Quiz ↔ Cours (Many-to-Many)
                // Table liaison : quiz_local_cours
                entity.HasMany(q => q.Cours)
                      .WithMany()
                      .UsingEntity(j => j.ToTable("quiz_local_cours"));
            });

            // ── ExamenLocal ───────────────────────────────────────
            modelBuilder.Entity<ExamenLocal>(entity =>
            {
                entity.ToTable("examen_local");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasColumnType("TEXT");
                entity.Property(e => e.Statut)
                      .HasMaxLength(20)
                      .HasDefaultValue("BROUILLON");

                // Relation : Examen ↔ Cours (Many-to-Many)
                // Table liaison : examen_local_cours
                entity.HasMany(e => e.Cours)
                      .WithMany()
                      .UsingEntity(j => j.ToTable("examen_local_cours"));
            });
        }
    }
}