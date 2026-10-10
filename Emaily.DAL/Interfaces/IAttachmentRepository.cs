
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emaily.DAL.Interfaces
{
    public interface IAttachmentRepository
    {
        Task<bool> IsOwnerAsync(Guid userId, Guid attachmentId);
    }
}