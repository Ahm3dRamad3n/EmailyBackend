using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Emaily.DAL.Entities;

public partial class EmailyDbContext : DbContext
{
    public EmailyDbContext()
    {
    }

    public EmailyDbContext(DbContextOptions<EmailyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Banned> Banneds { get; set; }

    public virtual DbSet<BanDetail> BanDetails { get; set; }

    public virtual DbSet<Integration> Integrations { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Plan> Plans { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectService> ProjectServices { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceApiKey> ServiceApiKeys { get; set; }

    public virtual DbSet<ServiceAppPassword> ServiceAppPasswords { get; set; }

    public virtual DbSet<ServiceOauth> ServiceOauths { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<Template> Templates { get; set; }

    public virtual DbSet<TemplateAttachment> TemplateAttachments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Banned>(entity =>
        {
            entity.HasKey(e => e.IpAddress)
                .HasName("PK__Banned__30C707A3546AA28A")
                .IsClustered(false);

            entity.ToTable("Banned");

            entity.HasIndex(e => e.ClusterKey, "UQ__Banned__A6C6B912B9B8C902")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.BannedUntil).HasColumnName("bannedUntil");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.BanLevel).HasColumnName("banLevel");
        });

        modelBuilder.Entity<BanDetail>(entity =>
        {
            entity.HasKey(e => e.ClusterKey)
                  .HasName("PK_BanDetails");

            entity.ToTable("BanDetails");

            entity.HasIndex(e => e.IpAddress, "IX_BanDetails_IpAddress");

            entity.Property(e => e.IpAddress)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.ActionType)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.UserAgent)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Endpoint)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.ViolationWeight)
                .IsRequired();

            entity.Property(e => e.AdminId)
                .IsRequired(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Banned)
                .WithMany(p => p.BanDetails)
                .HasForeignKey(d => d.IpAddress)
                .HasPrincipalKey(p => p.IpAddress)
                .HasConstraintName("FK_BanDetails_Banned_IpAddress")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Integrat__3214EC06D1051BDB")
                .IsClustered(false);

            entity.HasIndex(e => e.ClusterKey, "UQ__Integrat__A6C6B912E1979B3A")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.IntegrationType).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ProjectId).HasMaxLength(100);

            entity.HasOne(d => d.Project).WithMany(p => p.Integrations)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Integrations_Projects");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Invoices__3214EC06C6E46F01")
                .IsClustered(false);

            entity.HasIndex(e => e.ClusterKey, "UQ__Invoices__A6C6B91259598FC7")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.InvoiceDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.InvoicePdfUrl).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Subscription).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.SubscriptionId)
                .HasConstraintName("FK_Invoices_Subscriptions");
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plans__3214EC078C696148");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CanUseAi).HasColumnName("CanUseAI");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MonthlyPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Projects__3214EC060D001A9F")
                .IsClustered(false);

            entity.HasIndex(e => e.PublicApiKey, "UQ__Projects__202399D71229D10A").IsUnique();

            entity.HasIndex(e => e.PrivateApiKey, "UQ__Projects__8AECC551563DAFB7").IsUnique();

            entity.HasIndex(e => e.ClusterKey, "UQ__Projects__A6C6B912873B317C")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id)
                .HasMaxLength(100)
                .HasDefaultValueSql("('p_'+CONVERT([varchar](36),newid()))");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PrivateApiKey).HasMaxLength(100);
            entity.Property(e => e.ProjectAccessMode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("FrontendOnly");
            entity.Property(e => e.PublicApiKey).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Projects)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Projects_Users");
        });

        modelBuilder.Entity<ProjectService>(entity =>
        {
            entity.HasKey(e => new { e.ProjectId, e.ServiceId })
                .HasName("PK__Project___CA4B05F14438FFCA")
                .IsClustered(false);

            entity.ToTable("Project_Services");

            entity.HasIndex(e => e.ClusterKey, "UQ__Project___A6C6B912D25B54D1")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.ProjectId).HasMaxLength(100);
            entity.Property(e => e.ServiceId).HasMaxLength(100);
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectServices)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_ProjectServices_Projects");

            entity.HasOne(d => d.Service).WithMany(p => p.ProjectServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectServices_Services");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__RefreshT__3214EC0630B42CA5")
                .IsClustered(false);

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F817BBE97F22").IsUnique();

            entity.HasIndex(e => e.ClusterKey, "UQ__RefreshT__A6C6B9128290196A")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.JwtId).HasMaxLength(100);
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Services__3214EC063A7599D8")
                .IsClustered(false);

            entity.HasIndex(e => e.ClusterKey, "UQ__Services__A6C6B91240708F42")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id)
                .HasMaxLength(100)
                .HasDefaultValueSql("('s_'+CONVERT([varchar](36),newid()))");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.FromEmail).HasMaxLength(255);
            entity.Property(e => e.FromName).HasMaxLength(150);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ProviderType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Services)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Services_Users");
        });

        modelBuilder.Entity<ServiceApiKey>(entity =>
        {
            entity.HasKey(e => e.ServiceId)
                .HasName("PK__Service___C51BB00B7E29D980")
                .IsClustered(false);

            entity.ToTable("Service_ApiKeys");

            entity.HasIndex(e => e.ClusterKey, "UQ__Service___A6C6B9121CE4F295")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.ServiceId).HasMaxLength(100);
            entity.Property(e => e.ProviderName).HasMaxLength(50);
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();

            entity.HasOne(d => d.Service).WithOne(p => p.ServiceApiKey)
                .HasForeignKey<ServiceApiKey>(d => d.ServiceId)
                .HasConstraintName("FK_Service_ApiKeys_Services");
        });

        modelBuilder.Entity<ServiceAppPassword>(entity =>
        {
            entity.HasKey(e => e.ServiceId)
                .HasName("PK__Service___C51BB00BFD099E40")
                .IsClustered(false);

            entity.ToTable("Service_AppPasswords");

            entity.HasIndex(e => e.ClusterKey, "UQ__Service___A6C6B912B787918D")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.ServiceId).HasMaxLength(100);
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.SmtpHost).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(255);

            entity.HasOne(d => d.Service).WithOne(p => p.ServiceAppPassword)
                .HasForeignKey<ServiceAppPassword>(d => d.ServiceId)
                .HasConstraintName("FK_Service_AppPasswords_Services");
        });

        modelBuilder.Entity<ServiceOauth>(entity =>
        {
            entity.HasKey(e => e.ServiceId)
                .HasName("PK__Service___C51BB00B4EAFDB9D")
                .IsClustered(false);

            entity.ToTable("Service_OAuth");

            entity.HasIndex(e => e.ClusterKey, "UQ__Service___A6C6B912D70134B2")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.ServiceId).HasMaxLength(100);
            entity.Property(e => e.OauthProvider).HasMaxLength(50);
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.TokenExpiry).HasColumnType("datetime");

            entity.HasOne(d => d.Service).WithOne(p => p.ServiceOauth)
                .HasForeignKey<ServiceOauth>(d => d.ServiceId)
                .HasConstraintName("FK_Service_OAuth_Services");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Submissi__3214EC061A27A809")
                .IsClustered(false);

            entity.HasIndex(e => new { e.ProjectId, e.Status }, "IX_Submissions_ProjectId_Status");

            entity.HasIndex(e => e.SentAt, "IX_Submissions_SentAt");

            entity.HasIndex(e => e.ClusterKey, "UQ__Submissi__A6C6B912E712B03C")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.ProjectId).HasMaxLength(100);
            entity.Property(e => e.ReceivedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.RecipientEmail).HasMaxLength(255);
            entity.Property(e => e.RecipientName).HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.TemplateId).HasMaxLength(100);
            entity.Property(e => e.IsPrivateData).HasDefaultValue(false);

            entity.HasOne(d => d.Project).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Submissions_Projects");

            entity.HasOne(d => d.Template).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.TemplateId)
                .HasConstraintName("FK_Submissions_Templates");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Subscrip__3214EC0668B3DD96")
                .IsClustered(false);

            entity.HasIndex(e => e.ClusterKey, "UQ__Subscrip__A6C6B91288ACD1BC")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Plan).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_Plans");

            entity.HasOne(d => d.User).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Subscriptions_Users");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SystemLo__3214EC07C1B5BA65");

            entity.HasIndex(e => e.CreatedAt, "IX_SystemLogs_CreatedAt");

            entity.HasIndex(e => e.LogLevel, "IX_SystemLogs_LogLevel");

            entity.HasIndex(e => e.ProjectId, "IX_SystemLogs_ProjectId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ExceptionDetails).HasMaxLength(4000);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LogLevel).HasMaxLength(20);
            entity.Property(e => e.ProjectId).HasMaxLength(100);
            entity.Property(e => e.ExecutionTrace).HasMaxLength(500);
            entity.Property(e => e.Message).HasMaxLength(500);
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Template__3214EC06EADEE22B")
                .IsClustered(false);

            entity.HasIndex(e => e.ClusterKey, "UQ__Template__A6C6B9125BD20ED3")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id)
                .HasMaxLength(100)
                .HasDefaultValueSql("('t_'+CONVERT([varchar](36),newid()))");
            entity.Property(e => e.AppCheckSecret).HasMaxLength(500);
            entity.Property(e => e.AutoReplyTemplateId).HasMaxLength(100);
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DoSaveInHistory).HasDefaultValue(true);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Issuer)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ProjectId).HasMaxLength(100);
            entity.Property(e => e.RecaptchaSecretKey).HasMaxLength(500);
            entity.Property(e => e.ReplyTo).HasMaxLength(255);
            entity.Property(e => e.ServiceId).HasMaxLength(100);
            entity.Property(e => e.Subject).HasMaxLength(255);
            entity.Property(e => e.ToEmail).HasMaxLength(255);
            entity.Property(e => e.ToName).HasMaxLength(150);

            entity.HasOne(d => d.AutoReplyTemplate).WithMany(p => p.InverseAutoReplyTemplate)
                .HasForeignKey(d => d.AutoReplyTemplateId)
                .HasConstraintName("FK_Templates_AutoReply");

            entity.HasOne(d => d.Project).WithMany(p => p.Templates)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Templates_Projects");

            entity.HasOne(d => d.Service).WithMany(p => p.Templates)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_Templates_Services");
        });

        modelBuilder.Entity<TemplateAttachment>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Template__3214EC06AC04D560")
                .IsClustered(false);

            entity.HasIndex(e => e.ClusterKey, "UQ__Template__A6C6B912D38C65B2")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileUrl).HasMaxLength(1000);
            entity.Property(e => e.TemplateId).HasMaxLength(100);

            entity.HasOne(d => d.Template).WithMany(p => p.TemplateAttachments)
                .HasForeignKey(d => d.TemplateId)
                .HasConstraintName("FK_Attachments_Templates");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK__Users__3214EC06C1AF4003")
                .IsClustered(false);

            entity.HasIndex(e => e.Email, "IX_Users_Email_Active")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.ClusterKey, "UQ__Users__A6C6B9128FF1B8BF")
                .IsUnique()
                .IsClustered();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClusterKey).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DevNotificationEmail).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Roles).HasMaxLength(20);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clusterKeyProperty = entityType.FindProperty("ClusterKey");
            if (clusterKeyProperty != null)
            {
                // 1. يخبر إطار العمل أن القيمة يتم توليدها في قاعدة البيانات (ينطبق على الجميع)
                clusterKeyProperty.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;

                // 2. تطبيق قوانين التجاهل فقط إذا لم يكن الحقل هو المفتاح الأساسي (Primary Key)
                if (!clusterKeyProperty.IsPrimaryKey())
                {
                    // يمنع إرسال القيمة في جملة الـ UPDATE
                    clusterKeyProperty.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

                    // يمنع إرسال القيمة في جملة الـ INSERT
                    clusterKeyProperty.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
                }
            }
        }

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
