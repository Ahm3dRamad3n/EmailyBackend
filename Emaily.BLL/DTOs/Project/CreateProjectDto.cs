using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Project
{
    public class CreateProjectDto
    {
        [RequiredText, StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = null!;
        [ValidDomainsList]
        public string? RestrictedDomains { get; set; }
    }
}