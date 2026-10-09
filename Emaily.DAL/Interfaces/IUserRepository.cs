
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emaily.DAL.Interfaces
{
    public interface IUserRepository
    {
        Task<int> DecreaseQuotaAsync(Guid userId);
        Task IncreaseQuotaAsync(Guid userId);
        Task IncreaseOverageEmailsAsync(Guid userId);
    }
}