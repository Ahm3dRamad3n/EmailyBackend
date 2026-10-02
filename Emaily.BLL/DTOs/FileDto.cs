using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs
{
    public class FileDto
    {
        public byte[] Content { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string Name { get; set; } = null!; // اسم حقل الإدخال في الـ Form
    }
}
