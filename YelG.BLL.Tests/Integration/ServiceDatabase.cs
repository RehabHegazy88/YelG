using Microsoft.EntityFrameworkCore;
using YelG.BLL.Repository;
using YelG.BLL.Repository.Audit;
using YelG.BLL.Repository.Companies;
using YelG.BLL.Repository.Evaluations;
using YelG.BLL.Repository.QuestionBank;
using YelG.BLL.Repository.Scope;
using YelG.BLL.Repository.Users;
using YelG.BLL.Services.Audit;
using YelG.BLL.Services.Companies;
using YelG.BLL.Services.Evaluations;
using YelG.BLL.Services.Exports;
using YelG.BLL.Services.QuestionBank;
using YelG.BLL.Services.Scope;
using YelG.DAL.Abstractions;
using YelG.DAL.Data;
using YelG.DAL.Interceptors;
using YelG.DAL.Seeding;
using YelG.Model.Auth;
using YelG.Model.Scope;

namespace YelG.BLL.Tests.Integration
{
    /// <summary>Runs on Windows with LocalDB; set YELG_SKIP_LOCALDB to skip.</summary>
    public sealed class LocalDbFactAttribute : FactAttribute
    {
        public LocalDbFactAttribute()
        {
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB")))
                Skip = "Needs SQL Server LocalDB.";
        }
    }

    [CollectionDefinition(Name)]
    public sealed class ServiceDatabaseCollection : ICollectionFixture<ServiceDatabase>
    {
        public const string Name = "Service database";
    }

    /// <summary>
    /// A throw-away database (YelgLens_ServiceTests on LocalDB) migrated and seeded once for all
    /// service tests, then dropped. Never points at the shared server.
    /// </summary>
    public sealed class ServiceDatabase : IAsyncLifetime
    {
        private const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=YelgLens_ServiceTests;Trusted_Connection=True;TrustServerCertificate=True";

        // Assigned by the database when the fixture creates the accounts.
        public long AdminId { get; private set; }
        public long ReviewerId { get; private set; }
        public long Reviewer2Id { get; private set; }
        public long InactiveId { get; private set; }

        public async Task InitializeAsync()
        {
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB"))) return;

            await using var context = NewContext(new TestUser(null, false));
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            context.QuestionBankVersions.Add(SeedData.LoadQuestionBank());
            var (templates, rules) = SeedData.LoadTaskLibrary();
            templates.Add(new TaskTemplate
            {
                Code = "T-TEST", Title = "مهمة اختبار", DefaultIncluded = true,
                RoleHours = new RoleHours { TeamLead = 2 }
            });
            context.TaskTemplates.AddRange(templates);
            context.AnswerRules.AddRange(rules);
            context.PricingSettings.Add(new PricingSettings());

            User admin = User("admin", "مدير النظام", true), reviewer = User("reviewer", "مقيم أول", true),
                 reviewer2 = User("reviewer2", "مقيم ثان", true), inactive = User("inactive", "حساب موقوف", false);
            context.Users.AddRange(admin, reviewer, reviewer2, inactive);
            await context.SaveChangesAsync();
            (AdminId, ReviewerId, Reviewer2Id, InactiveId) = (admin.Id, reviewer.Id, reviewer2.Id, inactive.Id);
        }

        public async Task DisposeAsync()
        {
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB"))) return;
            await using var context = NewContext(new TestUser(null, false));
            await context.Database.EnsureDeletedAsync();
        }

        public Session AsAdmin() => new(this, new TestUser(AdminId, true));

        public Session AsReviewer() => new(this, new TestUser(ReviewerId, false));

        public Session AsReviewer2() => new(this, new TestUser(Reviewer2Id, false));

        internal YelgDbContext NewContext(ICurrentUser user) =>
            new(new DbContextOptionsBuilder<YelgDbContext>()
                .UseSqlServer(ConnectionString)
                .AddInterceptors(new AuditableEntityInterceptor(user, TimeProvider.System))
                .Options);

        private static User User(string userName, string displayName, bool active) => new()
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            DisplayName = displayName,
            IsActive = active,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    public sealed record TestUser(long? UserId, bool IsAdmin) : ICurrentUser
    {
        public string? UserName => UserId?.ToString();
    }

    /// <summary>One request: its own DbContext and services, acting as one user.</summary>
    public sealed class Session : IAsyncDisposable
    {
        public Session(ServiceDatabase database, TestUser user)
        {
            Context = database.NewContext(user);
            var clock = TimeProvider.System;
            var unitOfWork = new UnitOfWork(Context);
            var evaluations = new EvaluationRepository(Context);
            var companies = new CompanyRepository(Context);
            var questionBank = new QuestionBankRepository(Context);
            var log = new ActivityLogRepository(Context, user, clock);

            Companies = new CompanyService(companies, evaluations, questionBank, new UserRepository(Context), log, unitOfWork, user, clock);
            Evaluations = new EvaluationService(evaluations, companies, questionBank, log, unitOfWork, user, clock);
            Scope = new ScopeService(evaluations, questionBank, new ScopeRepository(Context), log, unitOfWork, user);
            QuestionBank = new QuestionBankAdminService(questionBank, log, unitOfWork, user, clock);
            TaskLibrary = new TaskLibraryAdminService(new ScopeRepository(Context), questionBank, log, unitOfWork, user, clock);
            Exports = new ExcelExportService(evaluations, questionBank, new ScopeRepository(Context), log, new UserRepository(Context), unitOfWork, user, clock);
            ActivityLog = new ActivityLogService(log, new UserRepository(Context), user, clock);
        }

        public ITaskLibraryAdminService TaskLibrary { get; }

        public IExcelExportService Exports { get; }

        public IActivityLogService ActivityLog { get; }

        public IQuestionBankAdminService QuestionBank { get; }

        public YelgDbContext Context { get; }
        public ICompanyService Companies { get; }
        public IEvaluationService Evaluations { get; }
        public IScopeService Scope { get; }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
