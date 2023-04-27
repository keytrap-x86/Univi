using System;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;

namespace Univi.Core.Models.Base;
public abstract class SoftwareBase : ISoftware
{
    private readonly IProcessService _processService;
    private readonly IRegistryService _registryService;
    private readonly ISoftwareUninstallService _softwareUninstallService;
    private readonly ISetupProviderFactory _setupProviderFactory;

    public virtual string? SoftwareDisplayNameRegex { get; set; }
    public virtual string Name { get; set; }
    public virtual bool InstallationRequiresPrivileges { get; set; }
    public virtual ISoftwareUninstallInfo? UninstallInfo { get; set; }
    public virtual string? InstallerArguments { get; set; }
    public virtual string? InstallerVersion { get; set; }
    public virtual string? UninstallerArguments { get; set; }
    public virtual string? InstallerLocation { get; set; }
    public virtual string? InstallerFileNameRegex { get; set; }
    public bool IsDynamic { get; set; }

    public double SetupDownloadProgress { get; set; }

    protected SoftwareBase(
        IProcessService processService,
        IRegistryService registryService,
        ISoftwareUninstallService softwareUninstallService,
        ISetupProviderFactory setupProviderFactory)
    {
        _processService = processService;
        _registryService = registryService;
        _softwareUninstallService = softwareUninstallService;
        _setupProviderFactory = setupProviderFactory;
    }

    public virtual async Task<bool> CheckIfInstalled(CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(SoftwareDisplayNameRegex))
            throw new ArgumentNullException(nameof(SoftwareDisplayNameRegex), $"La propriété {nameof(SoftwareDisplayNameRegex)} ne peut être vide");

        UninstallInfo = await _registryService.GetSoftwareUninstallInfo(SoftwareDisplayNameRegex, token);
        return UninstallInfo.FoundInRegistry;
    }


    public virtual async Task<int> Install(CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(InstallerLocation))
            throw new ArgumentNullException(nameof(InstallerLocation), $"La propriété {nameof(InstallerLocation)} ne peut être vide");

        var setupProvider = _setupProviderFactory.Create(InstallerLocation);
        InstallerLocation = await setupProvider.GetInstaller(this, (i) => i = SetupDownloadProgress, token);


        if (InstallationRequiresPrivileges && _processService.IsRunningAsAdmin == false)
        {
            _processService.RestartAsAdministrator();
            return await Task.FromResult(0);
        }

        return await _processService.RunProgramAsync(InstallerLocation, InstallerArguments, true, true, token);
    }


    public virtual async Task<int> Uninstall(CancellationToken token = default)
    {

        if (string.IsNullOrEmpty(UninstallInfo?.UninstallString))
            return await Task.FromResult(0);

        return await _softwareUninstallService.UninstallSoftware(UninstallInfo, token);
    }

}
