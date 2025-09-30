using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DBMigration.Services.Interface
{
    public interface IMigrationRegistrationService
    {
        void RegisterMigrations(IServiceCollection services);
        List<Type> GetMigrationTypes();
    }
}