using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Univi.Core.Interfaces;
public interface IProcessService
{
    /// <summary>
    ///     Runs a process
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="args"></param>
    /// <param name="noWindow"></param>
    /// <param name="waitForExit"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<int> RunProgramAsync(string filename, string? args = null, bool noWindow = false, bool waitForExit = true, CancellationToken token = default);

    /// <summary>
    ///     Tells if the current program is running as administrator
    /// </summary>
    bool IsRunningAsAdmin { get; set; }

    /// <summary>
    ///     Parses a multi-spaced argument string (e.g: program uninstall string)
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    List<string> ParseMultiSpacedArguments(string commandLine);

    /// <summary>
    ///     Restarts current process as admin
    /// </summary>
    /// <param name="v"></param>
    void RestartAsAdministrator(string? args = null);
}
