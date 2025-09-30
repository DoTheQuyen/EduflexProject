using DBMigration.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DBMigration.Services.Services
{
    public class MigrationRegistrationService : IMigrationRegistrationService
    {
        public void RegisterMigrations(IServiceCollection services)
        {
            var migrationTypes = GetMigrationTypes();

            foreach (var type in migrationTypes)
            {
                services.AddTransient(type);
                Console.WriteLine($"✅ Auto-registered migration: {type.Name}");
            }

            Console.WriteLine($"📊 Total migrations registered: {migrationTypes.Count}");
        }

        public List<Type> GetMigrationTypes()
        {
            var migrationTypes = new List<Type>();

            // Get all assemblies in the current domain
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => typeof(IMigration).IsAssignableFrom(t)
                                 && !t.IsInterface
                                 && !t.IsAbstract
                                 && t.Name.StartsWith("_")) // Only classes starting with underscore
                        .OrderBy(t => t.Name); // Order by migration ID

                    migrationTypes.AddRange(types);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Could not scan assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return migrationTypes.OrderBy(t => t.Name).ToList();
        }
    }
}