
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emaily.DAL.Interfaces
{
    public interface IProjectRepository
    {
        Task<bool> IsOwnerAsync(Guid userId, string projectId);
    }
}