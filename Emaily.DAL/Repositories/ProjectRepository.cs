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
    public class ProjectRepository(EmailyDbContext context) : IProjectRepository
    {
        private readonly EmailyDbContext _context = context;
      
        public async Task<bool> IsOwnerAsync(Guid userId, string projectId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.UserId == userId && !p.IsDeleted);
        }
    }
}
