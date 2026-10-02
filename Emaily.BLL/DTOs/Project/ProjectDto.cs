using System;

namespace Emaily.BLL.DTOs.Project
{
    public class ProjectDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}