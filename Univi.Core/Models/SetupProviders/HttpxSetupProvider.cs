using Downloader;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;
using Univi.Core.Services;

namespace Univi.Core.Models.SetupProviders;
public class HttpxSetupProvider : ISetupProvider
{
    private readonly ILogger<HttpxSetupProvider> _logger;
    private readonly FileDownloadService _fileDownloadService;

    public HttpxSetupProvider(ILogger<HttpxSetupProvider> logger, FileDownloadService fileDownloadService)
    {
        _logger = logger;
        _fileDownloadService = fileDownloadService;
    }

    public async Task<string?> GetInstaller(SoftwareBase software, Func<double, double> downloadProgress = null, CancellationToken token = default)
    {
        _logger.LogInformation("Téléchargement de l'installation {n} depuis {url}...", software.Name, software.InstallerLocation);

        try
        {
            var tempPath = Path.GetTempPath();
            var filePath = await _fileDownloadService.DownloadFileWithProgressAsync(software.InstallerLocation, tempPath, downloadProgress, token);

            _logger.LogInformation("Fichier d'installation téléchargé : {x}", filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Une erreur s'est produite lors du téléchargement : {err}", ex.Message);
        }

        return null;
    }
}
