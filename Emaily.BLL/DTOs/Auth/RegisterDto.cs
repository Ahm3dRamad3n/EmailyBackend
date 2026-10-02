using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Auth
{
    public class RegisterDto
    {
        [RequiredText, StringLength(150, MinimumLength = 3)]
        public string FullName { get; set; } = null!;

        [RequiredText]
        public string Token { get; set; } = null!;

        [StrongPassword]
        public string Password { get; set; } = null!;
    }
}