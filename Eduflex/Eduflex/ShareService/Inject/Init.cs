using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShareService.Common;
using ShareService.DataAccess;
using ShareService.DataAccess.Interface;
using ShareService.Models.Application;
using ShareService.Models.Auth;
using ShareService.Models.BusinessPartner;
using ShareService.Models.Course;
using ShareService.Models.CoursePromotion;
using ShareService.Models.Department;
using ShareService.Models.DynamicForm;
using ShareService.Models.EducationPartner;
using ShareService.Models.Enquiry;
using ShareService.Models.Enrolment;
using ShareService.Models.Feedback;
using ShareService.Models.Invoice;
using ShareService.Models.Role;
using ShareService.Models.Student;
using ShareService.Models.MigrationCase;
using ShareService.Models.Task;
using ShareService.Models.VisaProcess;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using ShareService.Services.Service;
using ShareService.Services.Service.Integration;
using ShareService.Validations.Application;
using ShareService.Validations.Auth;
using ShareService.Validations.BusinessPartner;
using ShareService.Validations.Course;
using ShareService.Validations.CoursePromotion;
using ShareService.Validations.Department;
using ShareService.Validations.DynamicForm;
using ShareService.Validations.EducationPartner;
using ShareService.Validations.Enquiry;
using ShareService.Validations.Enrolment;
using ShareService.Validations.Feedback;
using ShareService.Validations.Invoice;
using ShareService.Validations.Role;
using ShareService.Validations.Student;
using ShareService.Validations.Task;
using ShareService.Validations.VisaProcess;

namespace ShareService.Inject
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            // Add your shared DataAccess here
            #region DataAccess
            //services.AddScoped<IMongoDbContext, MongoDbContext>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAuthentication, Authentication>();
            services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
            services.AddScoped<IRole, Role>();
            services.AddScoped<IPermissionCatalog, PermissionCatalog>();
            services.AddScoped<IModuleCatalog, ModuleCatalog>();
            services.AddScoped<IApplication, Application>();
            services.AddScoped<IUserDB, UserDB>();
            services.AddScoped<IStudentDB, StudentDB>();
            services.AddScoped<IEnquiry, Enquiry>();
            services.AddScoped<IFeedback, Feedback>();
            services.AddScoped<ICoursePromotion, CoursePromotion>();
            services.AddScoped<IEducationPartner, EducationPartner>();
            services.AddScoped<IBusinessPartner, BusinessPartner>();
            services.AddScoped<ICourse, Course>();
            services.AddScoped<IEnrolment, Enrolment>();
            services.AddScoped<IEmailTemplate, EmailTemplate>();
            services.AddScoped<ISettings, Settings>();
            services.AddScoped<IFinancialRecord, FinancialRecord>();
            services.AddScoped<INotification, Notification>();
            services.AddScoped<IDepartment, Department>();
            services.AddScoped<IDynamicFormTemplate, DynamicFormTemplate>();
            services.AddScoped<IInvoiceTemplate, InvoiceTemplate>();
            services.AddScoped<IInvoice, Invoice>();
            services.AddScoped<IStudentPaymentPlanEntry, StudentPaymentPlanEntry>();
            services.AddScoped<IChatQuestion, ChatQuestion>();
            services.AddScoped<ITaskItem, TaskItem>();
            services.AddScoped<IVisaProcessTemplate, VisaProcessTemplate>();
            services.AddScoped<IPractitionerTag, PractitionerTag>();
            services.AddScoped<IMigrationCase, MigrationCase>();



            #endregion


            // Register all your services here
            #region Services
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEnquiryService, EnquiryService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<ICoursePromotionService, CoursePromotionService>();
            services.AddScoped<IEducationPartnerService, EducationPartnerService>();
            services.AddScoped<IBusinessPartnerService, BusinessPartnerService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IEnrolmentService, EnrolmentService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IFinancialRecordService, FinancialRecordService>();
            services.AddScoped<INotificationPublisher, NotificationPublisher>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IDynamicFormTemplateService, DynamicFormTemplateService>();
            services.AddScoped<IInvoiceTemplateService, InvoiceTemplateService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IStudentPaymentPlanService, StudentPaymentPlanService>();
            services.AddScoped<IAccountsService, AccountsService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ITaskItemService, TaskItemService>();
            services.AddScoped<IVisaProcessTemplateService, VisaProcessTemplateService>();
            services.AddScoped<IPractitionerTagService, PractitionerTagService>();
            services.AddScoped<IMigrationCaseService, MigrationCaseService>();


            #endregion


            // Register all validators here
            #region Validators
            services.AddScoped<IValidator<LoginModel>, LoginModelValidator>();
            services.AddScoped<IValidator<ApplicationModel>, ApplicationModelValidator>();
            services.AddScoped<IValidator<UpdateUserProfileModel>, UpdateUserProfileValidator>();
            services.AddScoped<IValidator<ChangePasswordModel>, ChangePasswordValidator>();
            services.AddScoped<IValidator<EnquiryModel>, EnquiryModelValidator>();
            services.AddScoped<IValidator<FeedbackModel>, FeedbackModelValidator>();
            services.AddScoped<IValidator<CoursePromotionModel>, CoursePromotionModelValidator>();
            services.AddScoped<IValidator<EducationPartnerModel>, EducationPartnerModelValidator>();
            services.AddScoped<IValidator<BusinessPartnerModel>, BusinessPartnerModelValidator>();
            services.AddScoped<IValidator<CourseModel>, CourseModelValidator>();
            services.AddScoped<IValidator<RoleModel>, RoleModelValidator>();
            services.AddScoped<IValidator<UserModel>, CreateUserModelValidator>();
            services.AddScoped<IValidator<StudentModel>, StudentModelValidator>();
            services.AddScoped<IValidator<EnrolmentModel>, EnrolmentModelValidator>();
            services.AddScoped<IValidator<EmailTemplateModel>, EmailTemplateModelValidator>();
            services.AddScoped<IValidator<DepartmentModel>, DepartmentModelValidator>();
            services.AddScoped<IValidator<DynamicFormTemplateModel>, DynamicFormTemplateModelValidator>();
            services.AddScoped<IValidator<InvoiceTemplateModel>, InvoiceTemplateModelValidator>();
            services.AddScoped<IValidator<TaskItemModel>, TaskItemModelValidator>();
            services.AddScoped<IValidator<VisaProcessTemplateModel>, VisaProcessTemplateModelValidator>();
            services.AddScoped<IValidator<PractitionerTagModel>, PractitionerTagModelValidator>();

            #endregion

            // Register all your integration services here
            #region Integration Services
            services.AddScoped<IAzureBlobDocStorageService, AzureBlobDocStorageService>();
            services.AddHttpClient<IRecaptchaService, RecaptchaService>();
            services.AddScoped<IAzureEmailService, AzureEmailService>();
            services.AddScoped<IInvoicePdfService, InvoicePdfService>();

            #endregion

            return services;
        }
    }
}