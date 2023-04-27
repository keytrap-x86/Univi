using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;

namespace Univi.Core.Models.SetupProviders;
public partial class FtpSetupProvider : ISetupProvider
{
    private readonly ILogger<FtpSetupProvider> _logger;

    public FtpSetupProvider(ILogger<FtpSetupProvider> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetInstaller(SoftwareBase software, Func<double, double> downloadProgress = null, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(software.InstallerLocation))
        {
            _logger.LogWarning("Le paramètre InstallerLocation sur {n} est vide. Impossible de continuer l'installation.", software.Name);
            return null;
        }

        try
        {
            // Try to extract host, user & password from ftp connection string

            var ftpRegexMatch = FtpConnectionStringRegex().Match(software.InstallerLocation);
            if (ftpRegexMatch.Success == false)
            {
                _logger.LogWarning("Le paramètre InstallerLocation sur {n} n'est pas une chaîne de connexion FTP valide. Impossible de continuer l'installation.", software.Name);
                return null;
            }

            // Get the host, user, password and path from the connection string
            var host = ftpRegexMatch.Groups["host"].Value;
            var user = ftpRegexMatch.Groups["user"].Value;
            var password = ftpRegexMatch.Groups["passwd"].Value;
            var path = ftpRegexMatch.Groups["path"].Value;

            // Connect to ftp
            var ftp = new AsyncFtpClient(host)
            {
                Credentials = new NetworkCredential(user, password)
            };

            await ftp.Connect(token);

            // Get the list of files
            var files = await ftp.GetListing(path, token);

            FtpListItem? latestVersion;

            if (string.IsNullOrEmpty(software.InstallerFileNameRegex) == false)
            {
                latestVersion = files.OrderByDescending(x => x.Modified).FirstOrDefault(x => Regex.IsMatch(x.Name, software.InstallerFileNameRegex, RegexOptions.IgnoreCase));
            }
            else
            {
                latestVersion = files.OrderByDescending(x => x.Modified).FirstOrDefault();
            }


            if (latestVersion == null)
            {
                _logger.LogWarning("Impossible de trouver la dernière version de {x} en ligne. L'installation ne peut continuer.", software.Name);
                return null;
            }

            _logger.LogDebug("Fichier trouvé : {f}", latestVersion.Name);

            var tmpPath = Path.GetTempPath();
            var tmpFilePath = Path.Combine(tmpPath, latestVersion.Name);

            await ftp.DownloadFile(tmpFilePath, latestVersion.FullName, token: token);

            return tmpFilePath;
        }
        catch (Exception e)
        {
            _logger.LogError("Une erreur s'est produite lors de la récupération de l'installateur en ligne de {n} : {err}", software.Name, e.Message);
        }

        return null;
    }

    [GeneratedRegex("^(?<scheme>ftp):\\/\\/(?<user>[^:]+):(?<passwd>[^@]+)@(?<host>[a-zA-Z0-9.-]+)(?<path>\\/(?:[^/\\r\\n]+\\/?)*)", RegexOptions.IgnoreCase, "fr-FR")]
    private static partial Regex FtpConnectionStringRegex();
}
