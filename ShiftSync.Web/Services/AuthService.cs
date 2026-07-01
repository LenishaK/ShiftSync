using Microsoft.EntityFrameworkCore;
using ShiftSync.Web.Data;

namespace ShiftSync.Web.Services
{
    public class AuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public AuthService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<UserData?> LoginAsync(string username, string password)
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Users
                .FirstOrDefaultAsync(u => u.Username == username
                                       && u.Password == password);
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            using var db = _dbFactory.CreateDbContext();
            var exists = await db.Users
                .AnyAsync(u => u.Username == username);
            if (exists) return false;

            db.Users.Add(new UserData
            {
                Username = username,
                Password = password
            });
            await db.SaveChangesAsync();
            return true;
        }
    }
}