using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [ValidEmail]
        public string Email { get; set; } = null!;
    }
}