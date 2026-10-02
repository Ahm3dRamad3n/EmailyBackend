namespace Emaily.BLL.DTOs.Template
{
    public class TemplateDto
    {
        public string Id { get; set; } = null!;
        public string ProjectId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}