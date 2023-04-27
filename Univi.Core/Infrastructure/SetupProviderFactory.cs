using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using Univi.Core.Interfaces;
using Univi.Core.Models.SetupProviders;

namespace Univi.Core.Infrastructure;
public class SetupProviderFactory : ISetupProviderFactory
{
    private readonly Dictionary<string, ISetupProvider> _providerMappings;

    public SetupProviderFactory(IContainerProvider containerProvider)
    {
        _providerMappings = new(StringComparer.OrdinalIgnoreCase)
            {
            { "github:", containerProvider.Resolve<GithubSetupProvider>() },
            { "ftp://", containerProvider.Resolve<FtpSetupProvider>() },
            { "\\\\", containerProvider.Resolve<SmbSetupProvider>() },
            { "http", containerProvider.Resolve<HttpxSetupProvider>() }
        };
    }

    /// <summary>
    ///     Creates a setup provider from the software configuration's installer location
    /// </summary>
    /// <param name="installerLocation"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ISetupProvider Create(string installerLocation)
    {

        if (string.IsNullOrEmpty(installerLocation))
            throw new NotImplementedException($"Le fichier d'installation n'est pas spécifié");

        var mappingKey = _providerMappings.Keys.FirstOrDefault(key => installerLocation.StartsWith(key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(mappingKey) && _providerMappings.TryGetValue(mappingKey, out var providerType))
        {
            return providerType;
        }

        throw new NotImplementedException($"Il n'existe pas de provider capable de récupérer le fichier d'installation à partir de : {installerLocation}");
    }
}

