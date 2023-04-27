using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;

namespace Univi.Core.Services;
public class SoftwareUninstallService : ISoftwareUninstallService
{
    private readonly ILogger<SoftwareUninstallService> _logger;

    public SoftwareUninstallService(ILogger<SoftwareUninstallService> logger)
    {
        _logger = logger;
    }

    public async Task<int> UninstallSoftware(ISoftwareUninstallInfo softwareUninstallInfo, CancellationToken token = default)
    {
        var exitCode = -1;

        if (softwareUninstallInfo.FoundInRegistry == false)
        {
            _logger.LogDebug("No software found in registry, skipping uninstallation.");
            return exitCode;
        }

        var uninstallString = softwareUninstallInfo.UninstallString;


        if (string.IsNullOrEmpty(uninstallString))
        {
            _logger.LogDebug("Uninstall string and QuietUninstallString are empty.");
            return exitCode;
        }



        var isMsiFile = uninstallString.Contains(".msi", StringComparison.InvariantCultureIgnoreCase);
        var pi = new ProcessStartInfo(uninstallString)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };

        if (string.IsNullOrEmpty(uninstallString) == false)
            pi.Arguments = uninstallString.Replace("\"", null);

        if (pi.CreateNoWindow || pi.RedirectStandardOutput)
            pi.UseShellExecute = false;

        if (isMsiFile == false)
        {
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardError = true;
            pi.RedirectStandardInput = true;
            pi.StandardOutputEncoding = CodePagesEncodingProvider.Instance.GetEncoding(437); // important otherwise we get weird characters instead of accents etc..
        }
        else
        {
            pi.UseShellExecute = true;
        }

        var proc = new Process
        {
            StartInfo = pi,
            EnableRaisingEvents = true
        };

        proc.OutputDataReceived += (e, o) =>
        {
            _logger.LogDebug("Process '{x}' > {0}", proc.ProcessName, o.Data);
        };

        proc.ErrorDataReceived += (e, o) =>
        {
            _logger.LogDebug("Process '{x}' > {0}", proc.ProcessName, o.Data);
        };

        proc.Exited += (e, o) =>
        {
            exitCode = proc.ExitCode;
        };

        _logger.LogDebug("Starting uninstallation of {software}", uninstallString);

        try
        {
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error while uninstalling : {err}", ex.Message);
        }


        return exitCode;
    }
}
