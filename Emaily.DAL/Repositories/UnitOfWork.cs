using Emaily.DAL.Interfaces;
using Emaily.DAL.Entities;
using System.Threading.Tasks;

namespace Emaily.DAL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EmailyDbContext _context;

        public IGenericRepository<User> Users { get; private set; }
        public IGenericRepository<Project> Projects { get; private set; }
        public IGenericRepository<Service> Services { get; private set; }
        public IGenericRepository<ServiceAppPassword> ServiceAppPasswords { get; private set; }
        public IGenericRepository<ServiceApiKey> ServiceApiKeys { get; private set; }
        public IGenericRepository<ServiceOauth> ServiceOauths { get; private set; }
        public IGenericRepository<ProjectService> ProjectServices { get; private set; }
        public IGenericRepository<Template> Templates { get; private set; }
        public IGenericRepository<Submission> Submissions { get; private set; }
        public IGenericRepository<RefreshToken> RefreshTokens { get; private set; }
        public IGenericRepository<Subscription> Subscriptions { get; private set; }
        public IGenericRepository<Plan> Plans { get; private set; }
        public IGenericRepository<TemplateAttachment> TemplateAttachments { get; private set; }
        public IGenericRepository<Banned> Banned { get; private set; }
        public IGenericRepository<BanDetail> BanDetails { get; private set; }
        public IGenericRepository<Integration> Integrations { get; private set; }
        public IGenericRepository<Invoice> Invoices { get; private set; }
        public IGenericRepository<SystemLog> SystemLogs { get; private set; }

        public UnitOfWork(EmailyDbContext context)
        {
            _context = context;
            Users = new GenericRepository<User>(_context);
            Projects = new GenericRepository<Project>(_context);
            Services = new GenericRepository<Service>(_context);
            ServiceAppPasswords = new GenericRepository<ServiceAppPassword>(_context);
            ServiceApiKeys = new GenericRepository<ServiceApiKey>(_context);
            ServiceOauths = new GenericRepository<ServiceOauth>(_context);
            ProjectServices = new GenericRepository<ProjectService>(_context);
            Templates = new GenericRepository<Template>(_context);
            Submissions = new GenericRepository<Submission>(_context);
            RefreshTokens = new GenericRepository<RefreshToken>(_context);
            Subscriptions = new GenericRepository<Subscription>(_context);
            Plans = new GenericRepository<Plan>(_context);
            TemplateAttachments = new GenericRepository<TemplateAttachment>(_context);
            Banned = new GenericRepository<Banned>(_context);
            BanDetails = new GenericRepository<BanDetail>(_context);
            Integrations = new GenericRepository<Integration>(_context);
            Invoices = new GenericRepository<Invoice>(_context);
            SystemLogs = new GenericRepository<SystemLog>(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}