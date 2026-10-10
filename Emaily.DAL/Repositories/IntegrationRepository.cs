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
    public class IntegrationRepository(EmailyDbContext context) : IIntegrationRepository
    {
        private readonly EmailyDbContext _context = context;
      
        public async Task<bool> IsOwnerAsync(Guid userId, Guid integrationId)
        {
            return await _context.Integrations
                .AnyAsync(i => i.Id == integrationId && i.Project.UserId == userId && !i.IsDeleted);
        }
    }
}
