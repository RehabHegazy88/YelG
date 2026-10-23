using Microsoft.Extensions.DependencyInjection;
using YelG.BLL.IRepository;
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
using YelG.BLL.Services.Users;

namespace YelG.BLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddYelgBusiness(this IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            services.AddScoped<IEvaluationRepository, EvaluationRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IScopeRepository, ScopeRepository>();
            services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

            // Services
            services.AddScoped<IEvaluationScoringService, EvaluationScoringService>();
            services.AddScoped<IEvaluationService, EvaluationService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IScopeService, ScopeService>();
            services.AddScoped<IQuestionBankService, QuestionBankService>();
            services.AddScoped<IQuestionBankAdminService, QuestionBankAdminService>();
            services.AddScoped<ITaskLibraryAdminService, TaskLibraryAdminService>();
            services.AddScoped<IUserAdminService, UserAdminService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IExcelExportService, ExcelExportService>();
            services.AddScoped<IActivityLogService, ActivityLogService>();

            return services;
        }
    }
}
