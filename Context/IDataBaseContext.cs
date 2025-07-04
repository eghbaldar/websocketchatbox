using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using websocket.Entities;

namespace websocket.Context
{
    public interface IDataBaseContext : IDisposable
    {
        DatabaseFacade Database { get; }
        DbSet<Users> Users { get; set; }
        DbSet<UserSessions> UserSessions { get; set; }
        DbSet<Friends> Friends { get; set; }
        DbSet<Messages> Messages { get; set; }
        DbSet<FriendHidden> FriendHidden { get; set; }
        //SaveChanges
        int SaveChanges(bool acceptAllChangesOnSuccess);
        int SaveChanges();
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken());
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    }
}
