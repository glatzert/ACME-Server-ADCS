using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Configuration
{
    public static class ConfigurationExtensions
    {
        extension(IServiceCollection services)
        {
            public void AddExternalAccountBindingIfConfigured(IConfiguration configuration, string sectionName, ILogger logger)
            {
                var eabSection = configuration.GetSection(sectionName);
                if (eabSection.Exists())
                {
                    logger.ExternalAccountBindingEnabled(sectionName);
                    services.AddScoped<IExternalAccountBindingValidator, DefaultExternalAccountBindingValidator>();
                    services.AddHttpClient<IExternalAccountBindingClient, DefaultExternalAccountBindingClient>();
                }

                else
                {
                    logger.ExternalAccountBindingNotEnabled(sectionName);
                    services.AddSingleton<IExternalAccountBindingValidator, NullExternalAccountBindingValidator>();
                }
            }
        }

        extension(IConfiguration configuration)
        {
            public ProfileNamesCollection GetProfileNames(string sectionName, ILogger logger)
            {
                var profilesSection = configuration.GetSection(sectionName);
                var profileNames = new ProfileNamesCollection();

                foreach (var profile in profilesSection.GetChildren())
                {
                    if (string.IsNullOrWhiteSpace(profile.Key))
                    {
                        logger.InvalidProfileName(profile.Path);
                        continue;
                    }

                    if (!profileNames.Add(new(profile.Key)))
                    {
                        logger.ProfileExistsMultipleTimes(profile.Key);
                        continue;
                    }
                }

                return profileNames;
            }
        }
    }
}
