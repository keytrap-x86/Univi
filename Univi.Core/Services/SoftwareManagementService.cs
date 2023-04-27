using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;

namespace Univi.Core.Services;
public class SoftwareManagerService : ISoftwareManagerService
{
    private readonly ILogger<SoftwareManagerService> _logger;
    private readonly IProcessService _processService;
    private readonly IRegistryService _registryService;
    private readonly ISoftwareUninstallService _softwareUninstallService;
    private readonly ISetupProviderFactory _setupProviderFactory;

    public SoftwareManagerService(
        ILogger<SoftwareManagerService> logger,
        IProcessService processService,
        IRegistryService registryService,
        ISoftwareUninstallService softwareUninstallService,
        ISetupProviderFactory setupProviderFactory)
    {
        _logger = logger;
        _processService = processService;
        _registryService = registryService;
        _softwareUninstallService = softwareUninstallService;
        _setupProviderFactory = setupProviderFactory;
    }

    /// <summary>
    ///     Gets all the softwares declared in assembly and instanciates them
    /// </summary>
    /// <returns></returns>
    public List<ISoftware> GetSoftwares()
    {
        _logger.LogDebug("Création des logiciels dynamiques...");

        var output = new List<ISoftware>();

        // Reflection to get all the softwares that inherit from SoftwareBase
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(SoftwareBase))))
        {
            var constructor = type.GetConstructor(new[] { typeof(IProcessService), typeof(IRegistryService), typeof(ISoftwareUninstallService), typeof(ISetupProviderFactory) });
            if (constructor == null)
            {
                throw new InvalidOperationException($"The type {type} does not have a constructor with the required parameters.");
            }

            var processServiceParam = Expression.Parameter(typeof(IProcessService));
            var registryServiceParam = Expression.Parameter(typeof(IRegistryService));
            var softwareUninstallServiceParam = Expression.Parameter(typeof(ISoftwareUninstallService));
            var setupProviderFactoryParam = Expression.Parameter(typeof(ISetupProviderFactory));

            var exp = Expression.New(constructor, processServiceParam, registryServiceParam, softwareUninstallServiceParam, setupProviderFactoryParam);
            var creator = Expression.Lambda<Func<IProcessService, IRegistryService, ISoftwareUninstallService, ISetupProviderFactory, ISoftware>>(exp, processServiceParam, registryServiceParam, softwareUninstallServiceParam, setupProviderFactoryParam).Compile();

            // Create an instance of the current type using the creator and add it to the output list
            output.Add(creator(_processService, _registryService, _softwareUninstallService, _setupProviderFactory));

        }

        _logger.LogInformation("{x} logiciels à installation pré-programmé découvert(s) : [{softs}]", output.Count, string.Join(",", output.Select(s => s.Name)));

        return output;
    }
}
