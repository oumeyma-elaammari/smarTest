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

                entity.Property(q => q.ReponseModele).HasColumnType("TEXT");
                entity.Property(q => q.ReponsesCorrectesJson).HasColumnType("TEXT");

                entity.Property(q => q.ImageBase64).HasColumnType("TEXT");
                entity.Property(q => q.ImageType).HasMaxLength(50);
                entity.Property(q => q.ImageNom).HasMaxLength(255);

                // Relation : Question → Cours (Many-to-One, optional)
                entity.HasOne(q => q.Cours)
                      .WithMany(c => c.Questions)
                      .HasForeignKey(q => q.CoursId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                // Relation : Question → QuizLocal (Many-to-One, optional)
                entity.HasOne(q => q.Quiz)
                      .WithMany(q => q.Questions)
                      .HasForeignKey(q => q.QuizLocalId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relation : Question → ExamenLocal (Many-to-One, optional)
                entity.HasOne(q => q.Examen)
                      .WithMany(e => e.Questions)
                      .HasForeignKey(q => q.ExamenLocalId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Cascade);
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
                // Relation : Quiz ↔ Cours (Many-to-Many)
                entity.HasMany(q => q.Cours)
                      .WithMany(c => c.Quiz)   // Correction : ajouter navigation côté CoursLocal
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