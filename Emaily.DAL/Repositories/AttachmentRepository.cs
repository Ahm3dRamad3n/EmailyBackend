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
    public class AttachmentRepository(EmailyDbContext context) : IAttachmentRepository
    {
        private readonly EmailyDbContext _context = context;

        public async Task<bool> IsOwnerAsync(Guid userId, Guid attachmentId)
        {
            return await _context.TemplateAttachments
                .AnyAsync(a => a.Id == attachmentId && a.Template.Project.UserId == userId);
        }
    }
}
