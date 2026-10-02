using System;
using System.Collections.Generic;
using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Emaily.BLL.DTOs.Submission
{
    public class SupportDto
    {
        [RequiredText, StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = null!;

        [ValidEmail]
        public string Email { get; set; } = null!;

        public ComplaintDto? Complaint { get; set; }
        public SuggestionDto? Suggestion { get; set; }
    }

    public class ComplaintDto
    {
        [RequiredPrefix("p_", AllowNull: true)]
        public string? ProjectID { get; set; } = null!;

        [RequiredText]
        public string Category { get; set; } = null!;

        [RequiredText]
        public string Details { get; set; } = null!;
        public IFormFile? Attachment { get; set; }
    }

    public class SuggestionDto
    {
        [RequiredText]
        public string Type { get; set; } = null!;

        [RequiredText]
        public string Details { get; set; } = null!;
        public string? ExpectedImpact { get; set; }
    }
}
