using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Submission;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface ISubmissionService
    {
        Task<Result<bool>> SubmitAsync(string publicApiKey, SubmitDto dto, string originDomain);
        Task<Result<bool>> SubmitSupportAsync(SupportDto dto, string originDomain);
        Task<Result<PagedResultDto<SubmissionHistoryDto>>> GetProjectSubmissionsAsync(string projectId, int page);
        Task<Result<SubmissionDetailsDto>> GetSubmissionDetailsAsync(string submissionId);
        Task<Result<SubmissionHistoryDto>> RetrySubmissionAsync(Guid userId, string submissionId);
        Task<Result<string>> GetSubmissionStatusAsync(string submissionId);
    }
}