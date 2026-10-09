using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.DAL.Repositories
{
    public class UserRepository(EmailyDbContext context) : IUserRepository
    {
        private readonly EmailyDbContext _context = context;
        public async Task<int> DecreaseQuotaAsync(Guid userId)
        {
            var rowsAffected = await _context.Users
             .Where(s => s.Id == userId && s.RemainingQuota > 0)
             .ExecuteUpdateAsync(s => s.SetProperty(p => p.RemainingQuota, p => p.RemainingQuota - 1));

            return rowsAffected;
        }

        public async Task IncreaseQuotaAsync(Guid userId)
        {
            await _context.Users
                .Where(s => s.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.RemainingQuota, p => p.RemainingQuota + 1));
        }
        public async Task IncreaseOverageEmailsAsync(Guid userId)
        {
            await _context.Users
                .Where(s => s.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.OverageEmails, p => p.OverageEmails + 1));
        }
    }
}
