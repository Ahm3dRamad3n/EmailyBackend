using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.User;
using System;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserProfileDto>> GetProfileAsync(Guid userId);
        Task<Result<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<Result<UserQuotaDto>> GetQuotaAsync(Guid userId);
        Task<bool> DeleteAccountAsync(Guid userId);
    }
}