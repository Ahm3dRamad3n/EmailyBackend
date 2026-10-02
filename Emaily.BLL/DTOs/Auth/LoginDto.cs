using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Auth
{
    public class LoginDto
    {
        [ValidEmail]
        public string Email { get; set; } = null!;

        [RequiredText]
        public string Password { get; set; } = null!;
    }
}