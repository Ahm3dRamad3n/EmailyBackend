
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emaily.DAL.Interfaces
{
    public interface IIntegrationRepository
    {
        Task<bool> IsOwnerAsync(Guid userId, Guid integrationId);
    }
}