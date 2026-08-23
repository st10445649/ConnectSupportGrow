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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("public");
        }
    }