
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emaily.DAL.Interfaces
{
    public interface ISubmissionRepository
    {
        Task<bool> IsOwnerAsync(Guid userId, Guid submissionId);
    }
}