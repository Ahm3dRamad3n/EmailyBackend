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
    public class SubmissionRepository(EmailyDbContext context) : ISubmissionRepository
    {
        private readonly EmailyDbContext _context = context;

        public async Task<bool> IsOwnerAsync(Guid userId, Guid submissionId)
        {
            return await _context.Submissions
                .AnyAsync(s => s.Id == submissionId && s.Project.UserId == userId);
        }
    }
}
