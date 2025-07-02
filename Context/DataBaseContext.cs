using Microsoft.EntityFrameworkCore;
using websocket.Entities;

namespace websocket.Context
{
    public class DataBaseContext : DbContext, IDataBaseContext
    {
        public DataBaseContext(DbContextOptions option) : base(option)
        {

        }

        public DbSet<Users> Users { get; set; }
        public DbSet<UserSessions> UserSessions { get; set; }
        public DbSet<Friends> Friends { get; set; }
        public DbSet<Messages> Messages { get; set; }
        public DbSet<Conversations> Conversations { get; set; }
        public DbSet<FriendHidden> FriendHidden { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
        public override int SaveChanges()
        {
            var modifiedEntries = ChangeTracker.Entries()
           .Where(e =>
               e.State == EntityState.Modified ||
               e.State == EntityState.Added ||
               e.State == EntityState.Deleted
               ).ToList();
            foreach (var entry in modifiedEntries)
            {
                var entityType = entry.Context.Model.FindEntityType(entry.Entity.GetType());
                var inserted = entityType.FindProperty("InsertDate");
                var updated = entityType.FindProperty("UpdateDate");
                var deleted = entityType.FindProperty("DeleteDate");
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (inserted != null) entry.Property("InsertDate").CurrentValue = DateTime.Now;
                        break;
                    case EntityState.Modified:
                        if (updated != null) entry.Property("UpdateDate").CurrentValue = DateTime.Now;
                        break;
                    case EntityState.Deleted:
                        if (deleted != null)
                        {
                            entry.Property("DeleteDate").CurrentValue = DateTime.Now;
                            entry.State = EntityState.Modified;
                        }
                        break;
                }
            }
            return base.SaveChanges();
        }
    }
}
