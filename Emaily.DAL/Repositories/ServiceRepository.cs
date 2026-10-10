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
    public class ServiceRepository(EmailyDbContext context) : IServiceRepository
    {
        private readonly EmailyDbContext _context = context;

        public async Task<bool> IsOwnerAsync(Guid userId, string serviceId)
        {
            return await _context.Services
                .AnyAsync(s => s.Id == serviceId && s.UserId == userId && !s.IsDeleted);
        }
    }
}
