using Downloader;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;

namespace Univi.Core.Models.SetupProviders;
public class HttpxSetupProvider : ISetupProvider
{
    private readonly ILogger<HttpxSetupProvider> _logger;

    public HttpxSetupProvider(ILogger<HttpxSetupProvider> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetInstaller(SoftwareBase software, Func<double, double> downloadProgress = null, CancellationToken token = default)
    {
        _logger.LogInformation("Téléchargement de l'installation {n} depuis {url}...", software.Name, software.InstallerLocation);

        try
        {
            // Download file
            var config = new DownloadConfiguration()
            {
                ParallelDownload = true, // download parts of file as parallel or not
                BufferBlockSize = 10240, // usually, hosts support max to 8000 bytes
                ChunkCount = 8, // file parts to download
                MaxTryAgainOnFailover = int.MaxValue, // the maximum number of times to fail.
                Timeout = 1000, // timeout (millisecond) per stream block reader
                MaximumBytesPerSecond = 0, //1024 * 1024, // speed limited to 1MB/s
                RequestConfiguration = // config and customize request headers
                {
                    Accept = "*/*",
                    UserAgent = $"{nameof(Univi)}/{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}"
                }
            };

            var tempPath = Path.GetTempPath();

            IDownload download = DownloadBuilder.New()
                .WithUrl(software.InstallerLocation)
                .WithDirectory(tempPath)
                .WithConfiguration(config)
                .Build();

            download.DownloadProgressChanged += (e, s) => downloadProgress((int)Math.Round(s.ProgressPercentage));

            await download.StartAsync(token);

            var output = Path.Combine(tempPath, download.Package.FileName);
            _logger.LogInformation("Fichier d'installation téléchargé : {x}", output);

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Une erreur s'est produite lors du téléchargement : {err}", ex.Message);
        }

        return null;
    }
}
