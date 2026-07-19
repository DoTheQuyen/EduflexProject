using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShareService.DataAccess;
using ShareService.DataAccess.Interface;
using ShareService.Models.Application;
using ShareService.Models.Auth;
using ShareService.Models.CoursePromotion;
using ShareService.Models.Enquiry;
using ShareService.Models.Feedback;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Service;
using ShareService.Validations.Application;
using ShareService.Validations.Auth;
using ShareService.Validations.CoursePromotion;
using ShareService.Validations.Enquiry;
using ShareService.Validations.Feedback;

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
            services.AddScoped<IEnquiry, Enquiry>();
            services.AddScoped<IFeedback, Feedback>();
            services.AddScoped<ICoursePromotion, CoursePromotion>();

            #endregion


            // Register all your services here
            #region Services
            services.AddScoped<MongoDBService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEnquiryService, EnquiryService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<ICoursePromotionService, CoursePromotionService>();
            services.AddHttpClient<IRecaptchaService, RecaptchaService>();


            #endregion


            // Register all validators here
            #region Validators
            services.AddScoped<IValidator<LoginModel>, LoginModelValidator>();
            services.AddScoped<IValidator<CreateApplicationModel>, CreateApplicationModelValidator>();
            services.AddScoped<IValidator<UpdateUserProfileModel>, UpdateUserProfileValidator>();
            services.AddScoped<IValidator<ChangePasswordModel>, ChangePasswordValidator>();
            services.AddScoped<IValidator<CreateEnquiryModel>, CreateEnquiryModelValidator>();
            services.AddScoped<IValidator<CreateFeedbackModel>, CreateFeedbackModelValidator>();
            services.AddScoped<IValidator<CreateCoursePromotionModel>, CreateCoursePromotionModelValidator>();

            #endregion



            return services;
        }
    }
}