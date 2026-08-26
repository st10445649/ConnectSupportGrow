using ConnectGrowAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectGrowAPI.Data;

public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options) { }

            public DbSet<Webinar> Webinars => Set<Webinar>();
            public DbSet<Booking> Bookings => Set<Booking>();
            public DbSet<Transaction> Transactions => Set<Transaction>();
            public DbSet<Recording> Recordings => Set<Recording>();
            public DbSet<RecordingAccess> RecordingAccesses => Set<RecordingAccess>();
            public DbSet<Evaluation> Evaluations => Set<Evaluation>();
            public DbSet<Faq> Faqs => Set<Faq>();
            public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
            public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<ApplicationRole>().ToTable("roles");

        ConfigureWebinar(builder);
        ConfigureBooking(builder);
        ConfigureTransaction(builder);
        ConfigureRecording(builder);
        ConfigureRecordingAccess(builder);
        ConfigureEvaluation(builder);
        ConfigureContent(builder);
        ConfigureRefreshToken(builder);

    }

    private static void ConfigureWebinar(ModelBuilder builder)
    {
        builder.Entity<Webinar>(e =>
        {
            e.ToTable("webinars", t =>
            {
                t.HasCheckConstraint("ck_webinars_capacity_positive", "capacity > 0");
                t.HasCheckConstraint("ck_webinars_price_non_negative", "price >= 0");
                t.HasCheckConstraint("ck_webinars_end_after_start", "end_date_time > start_date_time");
            });
            e.Property(w => w.Title).HasMaxLength(500).IsRequired();
            e.Property(w => w.Description).HasMaxLength(4000);
            e.Property(w => w.Category).HasMaxLength(100).IsRequired();
            e.Property(w => w.Price).HasPrecision(10, 2);
            e.Property(w => w.Status).HasMaxLength(20).HasConversion<string>();
            e.Property(w => w.TeamsJoinUrl).HasMaxLength(2000);
            e.Property(w => w.PresenterName).HasMaxLength(200);
            e.Property(w => w.PresenterBio).HasMaxLength(4000);

            e.Property(w => w.Version).IsRowVersion();

            e.HasIndex(w => new { w.Status, w.StartDateTime });
            e.HasIndex(w => w.Category);
        });
    }

    private static void ConfigureBooking(ModelBuilder builder)
    {
        builder.Entity<Booking>(e =>
        {
            e.ToTable("bookings");
            e.Property(b => b.BookingReference).HasMaxLength(50).IsRequired();
            e.Property(b => b.Amount).HasPrecision(10, 2);
            e.Property(b => b.Status).HasMaxLength(20).HasConversion<string>();
            e.Property(b => b.PaymentReference).HasMaxLength(100);

            e.HasOne(b => b.User)
             .WithMany(u => u.Bookings)
             .HasForeignKey(b => b.UserId)
             .OnDelete(DeleteBehavior.Restrict); // keep financial history on user deletion

            e.HasOne(b => b.Webinar)
             .WithMany(w => w.Bookings)
             .HasForeignKey(b => b.WebinarId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(b => b.BookingReference).IsUnique();

            e.HasIndex(b => b.PaymentReference)
             .IsUnique()
             .HasFilter("payment_reference IS NOT NULL");

            e.HasIndex(b => new { b.UserId, b.WebinarId })
             .IsUnique()
             .HasFilter("status <> 'Cancelled'")
             .HasDatabaseName("ux_bookings_user_webinar_active");

            e.HasIndex(b => new { b.WebinarId, b.Status });
        });
    }

    private static void ConfigureTransaction(ModelBuilder builder)
    {
        builder.Entity<Transaction>(e =>
        {
            e.ToTable("transactions");
            e.Property(t => t.Amount).HasPrecision(10, 2);
            e.Property(t => t.PaymentMethod).HasMaxLength(50).IsRequired();
            e.Property(t => t.TransactionReference).HasMaxLength(100).IsRequired();
            e.Property(t => t.Status).HasMaxLength(20).HasConversion<string>();
            e.Property(t => t.ErrorMessage).HasMaxLength(2000);
            e.Property(t => t.ResponseData).HasColumnType("jsonb");

            e.HasOne(t => t.Booking)
             .WithOne(b => b.Transaction)
             .HasForeignKey<Transaction>(t => t.BookingId)
             .OnDelete(DeleteBehavior.Restrict);

        
            e.HasIndex(t => t.TransactionReference).IsUnique();
            e.HasIndex(t => t.TransactionDate);
        });
    }

    private static void ConfigureRecording(ModelBuilder builder)
    {
        builder.Entity<Recording>(e =>
        {
            e.ToTable("recordings");
            e.Property(r => r.Title).HasMaxLength(500).IsRequired();
            e.Property(r => r.ApiVideoId).HasMaxLength(200).IsRequired();

            e.HasOne(r => r.Webinar)
             .WithMany(w => w.Recordings)
             .HasForeignKey(r => r.WebinarId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.WebinarId);
        });
    }

    private static void ConfigureRecordingAccess(ModelBuilder builder)
    {
        builder.Entity<RecordingAccess>(e =>
        {
            e.ToTable("recording_access");

            e.HasOne(ra => ra.User)
             .WithMany(u => u.RecordingAccesses)
             .HasForeignKey(ra => ra.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ra => ra.Recording)
             .WithMany(r => r.AccessGrants)
             .HasForeignKey(ra => ra.RecordingId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(ra => new { ra.UserId, ra.RecordingId }).IsUnique();

            e.HasIndex(ra => new { ra.UserId, ra.IsActive, ra.ExpiresAt });
        });
    }

    private static void ConfigureEvaluation(ModelBuilder builder)
    {
        builder.Entity<Evaluation>(e =>
        {
            e.ToTable("evaluations", t =>
                t.HasCheckConstraint("ck_evaluations_rating_range", "rating >= 1 AND rating <= 5"));
            e.Property(ev => ev.Feedback).HasMaxLength(2000);

            e.HasOne(ev => ev.Booking)
             .WithOne(b => b.Evaluation)
             .HasForeignKey<Evaluation>(ev => ev.BookingId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ev => ev.User)
             .WithMany(u => u.Evaluations)
             .HasForeignKey(ev => ev.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(ev => ev.BookingId).IsUnique();
        });
    }

    private static void ConfigureContent(ModelBuilder builder)
    {
        builder.Entity<Faq>(e =>
        {
            e.ToTable("faqs");
            e.Property(f => f.Question).HasMaxLength(500).IsRequired();
            e.Property(f => f.Answer).HasMaxLength(4000).IsRequired();
            e.HasIndex(f => new { f.IsActive, f.SortOrder });
        });

        builder.Entity<BlogPost>(e =>
        {
            e.ToTable("blog_posts");
            e.Property(b => b.Title).HasMaxLength(500).IsRequired();
            e.Property(b => b.Slug).HasMaxLength(250).IsRequired();
            e.Property(b => b.Summary).HasMaxLength(1000);
            e.Property(b => b.Status).HasMaxLength(20).HasConversion<string>();
            e.HasIndex(b => b.Slug).IsUnique();
            e.HasIndex(b => new { b.Status, b.PublishedAt });
        });
    }

    private static void ConfigureRefreshToken(ModelBuilder builder)
    {
        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            e.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
            e.Property(t => t.CreatedByIp).HasMaxLength(64);

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => new { t.UserId, t.RevokedAt });
        });
    }

      public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified) continue;

            var updatedAt = entry.Metadata.FindProperty("UpdatedAt");
            if (updatedAt is not null && updatedAt.ClrType == typeof(DateTime))
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
    }

