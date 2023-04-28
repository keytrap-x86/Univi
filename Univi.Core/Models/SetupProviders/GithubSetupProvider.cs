using Downloader;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;
using System.Text.RegularExpressions;
using Univi.Core.Services;

namespace Univi.Core.Models.SetupProviders;
public class GithubSetupProvider : ISetupProvider
{
    private readonly ILogger<GithubSetupProvider> _logger;
    private readonly FileDownloadService _fileDownloadService;

    public GithubSetupProvider(ILogger<GithubSetupProvider> logger, FileDownloadService fileDownloadService)
    {
        _logger = logger;
        _fileDownloadService = fileDownloadService;

        // Configurez Flurl.Http pour utiliser le nouvel objet SystemTextJsonSerializer avec les options personnalisées
        FlurlHttp.Configure(settings =>
        {
            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            settings.JsonSerializer = new NewtonsoftJsonSerializer(jsonSettings);
        });
    }

    public async Task<string?> GetInstaller(SoftwareBase software, Func<double, double> downloadProgress = null, CancellationToken token = default)
    {
        try
        {
            _logger.LogInformation("Téléchargement de l'installation {n} depuis {url}...", software.Name, software.InstallerLocation);

            var githubRepoApi = "https://api.github.com/repos";

            // Parse installer location
            var installerLocation = software.InstallerLocation?.ToLower().Replace("github:", null);
            installerLocation = $"{githubRepoApi}/{installerLocation}/releases";

            // Get latest releases by calling the api

            var githubRepoApiResults = await installerLocation
                .WithAutoRedirect(true)
                .WithHeader("User-Agent", $"{nameof(Univi)}/{Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3)}") // Needed otherwise error 403
                .GetJsonAsync<List<GithubApiRepo>>(token);

            // Get latest release
            var latestRelease = githubRepoApiResults.FirstOrDefault()?.Assets
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault(x => string.IsNullOrEmpty(software.InstallerFileNameRegex) || Regex.IsMatch(x.Name, software.InstallerFileNameRegex, RegexOptions.IgnoreCase))?
                .BrowserDownloadUrl;

            if (latestRelease?.OriginalString is not string releaseUrl)
            {
                _logger.LogError("Impossible de trouver la dernière version de {n} sur {url}", software.Name, installerLocation);
                return null;
            }

            var tempPath = Path.GetTempPath();
            var filePath = await _fileDownloadService.DownloadFileWithProgressAsync(releaseUrl, tempPath, downloadProgress, token);

            _logger.LogInformation("Fichier d'installation téléchargé : {x}", filePath);

            return filePath;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erreur lors du téléchargement de l'installation {n} depuis {url}", software.Name, software.InstallerLocation);
            return null;
        }
    }
}
