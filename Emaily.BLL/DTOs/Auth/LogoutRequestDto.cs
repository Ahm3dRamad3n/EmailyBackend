using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Auth
{
    public class LogoutRequestDto
    {
        [RequiredText]
        public string RefreshToken { get; set; } = null!;
    }
}