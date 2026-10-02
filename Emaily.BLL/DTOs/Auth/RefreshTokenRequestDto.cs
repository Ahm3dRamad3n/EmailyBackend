using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Auth
{
    public class RefreshTokenRequestDto
    {
        [RequiredText]
        public string Token { get; set; } = null!;
    }
}