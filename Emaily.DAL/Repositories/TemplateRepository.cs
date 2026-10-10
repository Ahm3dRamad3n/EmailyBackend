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
    public class TemplateRepository(EmailyDbContext context) : ITemplateRepository
    {
        private readonly EmailyDbContext _context = context;

        public async Task<bool> IsOwnerAsync(Guid userId, string templateId)
        {
            return await _context.Templates
              .AnyAsync(t => t.Id == templateId && t.Project.UserId == userId && !t.IsDeleted);
        }
    }
}
