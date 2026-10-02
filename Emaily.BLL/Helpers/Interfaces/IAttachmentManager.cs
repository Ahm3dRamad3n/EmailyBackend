using Emaily.BLL.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Emaily.BLL.Helpers.Services.AttachmentManager;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface IAttachmentManager
    {
        string Add(IFormFile file, AttachmentSource attachmentSource);
        bool Delete(string fileUrl, AttachmentSource attachmentSource);
        IFormFile? ConvertToIFormFile(string fileUrl);
        Task<FileDto?> ConvertToFileDtoAsync(IFormFile? file);
        IFormFile? ConvertToIFormFile(FileDto? fileDto);
    }
}
