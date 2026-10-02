using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Template
{
    public class TemplateAttachmentDto
    {
        public string Id { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public long FileSizeInBytes { get; set; }
    }
}
