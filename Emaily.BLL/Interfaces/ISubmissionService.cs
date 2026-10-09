using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Submission;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface ISubmissionService
    {
        Task<Result<bool>> SubmitAsync(string publicApiKey, SubmitDto dto, string originDomain);
        Task<Result<bool>> SubmitSupportAsync(SupportDto dto, string originDomain);
        Task<Result<PagedResultDto<SubmissionHistoryDto>>> GetProjectSubmissionsAsync(Guid userId, string projectId, int page);
        Task<Result<SubmissionDetailsDto>> GetSubmissionDetailsAsync(Guid userId, string submissionId);
        Task<Result<SubmissionHistoryDto>> RetrySubmissionAsync(Guid userId, string submissionId);
        Task<Result<string>> GetSubmissionStatusAsync(Guid userId, string submissionId);
    }
}