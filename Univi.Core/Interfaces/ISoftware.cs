using System.Threading;
using System.Threading.Tasks;

namespace Univi.Core.Interfaces;
public interface ISoftware
{
    /// <summary>
    ///     Tells if the software was found in appsettings.json or is preinstalled
    /// </summary>
    bool IsDynamic { get; set; }

    /// <summary>
    ///     Regex to find the software in registry's Uninstall key
    ///     by checking all the 'SoftwareDisplayName' values.
    /// </summary>
    string SoftwareDisplayNameRegex { get; set; }

    /// <summary>
    ///     Regex to find the installer
    /// </summary>
    string? InstallerFileNameRegex { get; set; }

    /// <summary>
    ///     Software's uninstall string
    /// </summary>

    ISoftwareUninstallInfo? UninstallInfo { get; set; }

    /// <summary>
    ///     Title which will be displayed to user
    /// </summary>
    string Name { get; set; }

    /// <summary>
    ///     Tells if the software needs admin rights to be installed/uninstalled
    /// </summary>
    bool InstallationRequiresPrivileges { get; set; }

    /// <summary>
    ///     Arguments that will be user for the installer.
    ///     This is where you can put silent arguments.
    /// </summary>
    string? InstallerArguments { get; set; }

    /// <summary>
    ///     Path to the installer if there is one.
    /// </summary>
    string? InstallerLocation { get; set; }


    /// <summary>
    ///     Arguments that will be passed to the uninstaller
    ///     (in addition of UninstallString's arguments)
    /// </summary>
    string? UninstallerArguments { get; set; }

    /// <summary>
    ///     Checks if the software is installed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<bool> CheckIfInstalled(CancellationToken token = default);

    /// <summary>
    ///     Installs the software
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<int> Install(CancellationToken token = default);

    /// <summary>
    ///     Uninstalls the software
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<int> Uninstall(CancellationToken token = default);
}
