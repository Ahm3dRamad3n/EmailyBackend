using Emaily.DAL.Entities;
using System;
using System.Threading.Tasks;

namespace Emaily.DAL.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Generic repositories for each entity
        IGenericRepository<User> Users { get; }
        IGenericRepository<Project> Projects { get; }
        IGenericRepository<Service> Services { get; }
        IGenericRepository<ServiceApiKey> ServiceApiKeys { get; }
        IGenericRepository<ServiceAppPassword> ServiceAppPasswords { get; }
        IGenericRepository<ServiceOauth> ServiceOauths { get; }
        IGenericRepository<ProjectService> ProjectServices { get; }
        IGenericRepository<Template> Templates { get; }
        IGenericRepository<Submission> Submissions { get; }
        IGenericRepository<RefreshToken> RefreshTokens { get; }
        IGenericRepository<Subscription> Subscriptions { get; }
        IGenericRepository<Plan> Plans { get; }
        IGenericRepository<TemplateAttachment> TemplateAttachments { get; }
        IGenericRepository<Banned> Banned { get; }
        IGenericRepository<BanDetail> BanDetails { get; }
        IGenericRepository<Integration> Integrations { get; }
        IGenericRepository<Invoice> Invoices { get; }
        IGenericRepository<SystemLog> SystemLogs { get; }

        // Specific repositories for entities that require custom methods
        IUserRepository UserRepository { get; }
        //IProjectRepository ProjectRepository { get; }
        //IServiceRepository ServiceRepository { get; }
        //ITemplateRepository TemplateRepository { get; }

        Task<int> CompleteAsync(); // هذه الدالة الوحيدة التي ستستدعي SaveChangesAsync
    }
}