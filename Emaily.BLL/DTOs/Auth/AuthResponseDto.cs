using System;

namespace Emaily.BLL.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public int ExpiresIn { get; set; } // بالثواني
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Roles { get; set; } = null!;
    }
}