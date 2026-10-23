using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YelG.DAL.Access.Data;
using YelG.Model.Audit;
using YelG.Model.Auth;
using YelG.Model.Evaluations;
using YelG.Model.QuestionBank;
using YelG.Model.Scope;

namespace YelG.DAL.Data
{
    public class YelgDbContext :
        IdentityDbContext<User, Role, long, UserClaim, UserRole, UserLogin, RoleClaim, userToken>
    {
        public YelgDbContext(DbContextOptions<YelgDbContext> options) : base(options)
        {
        }

        // Accounts
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        // Question bank & weights
        public DbSet<QuestionBankVersion> QuestionBankVersions => Set<QuestionBankVersion>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
        public DbSet<QuestionScoreBand> QuestionScoreBands => Set<QuestionScoreBand>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<ActivityFactor> ActivityFactors => Set<ActivityFactor>();

        // Companies & evaluations
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Evaluation> Evaluations => Set<Evaluation>();
        public DbSet<Answer> Answers => Set<Answer>();

        // Scope of work
        public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();
        public DbSet<AnswerRule> AnswerRules => Set<AnswerRule>();
        public DbSet<ScopeItem> ScopeItems => Set<ScopeItem>();
        public DbSet<PricingSettings> PricingSettings => Set<PricingSettings>();

        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Every table is mapped by an IMap class in Maps/Main (accounts and YELG alike).
            foreach (var mapping in MappingsHelper.GetMainMappings())
                mapping.Visit(builder);

            // AddedBy / ModifiedBy (MainBaseEntity) and the other links to a user must never delete
            // rows when a user is removed, and SQL Server rejects that many cascade paths anyway.
            foreach (var fk in builder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            {
                if (fk.PrincipalEntityType.ClrType == typeof(User) && !IsIdentityTable(fk.DeclaringEntityType.ClrType))
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
            configurationBuilder.Properties<decimal?>().HavePrecision(18, 4);
        }

        private static bool IsIdentityTable(Type type) =>
            type == typeof(UserRole) || type == typeof(UserClaim) || type == typeof(UserLogin)
            || type == typeof(userToken) || type == typeof(RefreshToken);
    }
}
