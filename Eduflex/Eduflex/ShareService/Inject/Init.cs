using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShareService.DataAccess;
using ShareService.DataAccess.Interface;
using ShareService.DataAccess.Service;
using ShareService.Models;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Service;
using ShareService.Validators;

namespace ShareService.Inject
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            // Add your shared DataAccess here
            #region DataAccess
            //services.AddScoped<IMongoDbContext, MongoDbContext>();
            services.AddScoped<IAuthentication, Authentication>();
            services.AddScoped<IApplication, Application>();
            services.AddScoped<IUserDB, UserDB>();

            #endregion


            // Register all your services here
            #region Services
            services.AddScoped<MongoDBService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();


            #endregion


            // Register all validators here
            #region Validators
            services.AddScoped<IValidator<LoginModel>, LoginModelValidator>();
            services.AddScoped<IValidator<CreateApplicationModel>, CreateApplicationModelValidator>();
            services.AddScoped<IValidator<UpdateUserProfileDto>, UpdateUserProfileValidator>();
            services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordValidator>();

            #endregion



            return services;
        }
    }
}