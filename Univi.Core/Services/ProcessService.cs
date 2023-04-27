using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;

namespace Univi.Core.Services;
public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> _logger;

    public ProcessService(ILogger<ProcessService> logger)
    {
        _logger = logger;
    }

    public async Task<int> RunProgramAsync(string filename, string? args = null, bool noWindow = false, bool waitForExit = true, CancellationToken token = default)
    {
        var exitCode = -1;

        var isMsiFile = filename.Contains(".msi", StringComparison.InvariantCultureIgnoreCase);
        var pi = new ProcessStartInfo(filename)
        {
            CreateNoWindow = noWindow,
            RedirectStandardOutput = true
        };

        if (string.IsNullOrEmpty(args) == false)
            pi.Arguments = args.Replace("\"", null);

        if (pi.CreateNoWindow || pi.RedirectStandardOutput)
            pi.UseShellExecute = false;

        if (isMsiFile)
        {
            pi.UseShellExecute = true;
        }
        else
        {
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardError = true;
            pi.RedirectStandardInput = true;
            pi.StandardOutputEncoding = CodePagesEncodingProvider.Instance.GetEncoding(437);
        }

        var proc = new Process
        {
            StartInfo = pi,
            EnableRaisingEvents = true
        };

        proc.OutputDataReceived += (e, o) =>
        {
            if (string.IsNullOrEmpty(o.Data))
                return;
            _logger.LogDebug("Output > {p}", o.Data);
        };
        proc.ErrorDataReceived += (e, o) =>
        {
            if (string.IsNullOrEmpty(o.Data))
                return;
            _logger.LogDebug("Error > {p}", o.Data);
        };

        proc.Exited += (s, e) =>
        {
            _logger.LogDebug("Program exited with code {c}", ((Process)s).ExitCode);
            exitCode = proc.ExitCode;
        };

        await Task.Run(() =>
        {
            try
            {
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                if (waitForExit)
                    proc.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error while running process : {err}", ex.Message);
            }
        }, token);

        return exitCode;
    }
    public bool IsRunningAsAdmin { get; set; } =
           new WindowsPrincipal(WindowsIdentity.GetCurrent())
        .IsInRole(WindowsBuiltInRole.Administrator);

    public List<string> ParseMultiSpacedArguments(string commandLine)
    {
        if (string.IsNullOrEmpty(commandLine))
            return new List<string>();

        if (!commandLine.Contains('"') && commandLine.Contains(' '))
        {
            commandLine = $"\"{commandLine}\"";
        }

        var isLastCharSpace = false;
        char[] parmChars = commandLine.ToCharArray();
        bool inQuote = false;
        for (int index = 0; index < parmChars.Length; index++)
        {
            if (parmChars[index] == '"')
                inQuote = !inQuote;
            if (!inQuote && parmChars[index] == ' ' && !isLastCharSpace)
                parmChars[index] = '\n';

            isLastCharSpace = parmChars[index] == '\n' || parmChars[index] == ' ';
        }

        return new string(parmChars).Split('\n').ToList();
    }

    public void RestartAsAdministrator(string? args = null)
    {
        try
        {
            var info = new ProcessStartInfo(Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe"))
            {
                UseShellExecute = true,
                Arguments = args,
                Verb = "runas"
            };

            var process = new Process
            {
                EnableRaisingEvents = true, // enable WaitForExit()
                StartInfo = info
            };

            process.Start();
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Erreur pendant le redémarrage en tant qu'administrateur : {err}", ex.Message);
        }
    }
}
