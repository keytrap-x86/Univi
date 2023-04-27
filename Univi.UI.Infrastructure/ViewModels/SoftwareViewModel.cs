using Prism.Commands;
using Prism.Mvvm;
using System.Threading.Tasks;
using System.Threading;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;

namespace Univi.UI.Infrastructure.ViewModels;

public class SoftwareViewModel : BindableBase
{
    #region Private fields

    private readonly IRegistryService _registryService;
    private string title;
    private bool installationRequiresPrivileges;
    private string? installerPath;

    #endregion

    public SoftwareViewModel(ISoftware software, IRegistryService registryService)
    {
        Software = software;
        _registryService = registryService;
        title = software.Name;
        installationRequiresPrivileges = software.InstallationRequiresPrivileges;
        installerPath = software.InstallerLocation;
        isDynamic = software.IsDynamic;
    }

    #region Properties

    private ISoftware software;
    public ISoftware Software
    {
        get { return software; }
        set { SetProperty(ref software, value); }
    }

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value, () => Software.Name = value);
    }

    public bool InstallationRequiresPrivileges
    {
        get => installationRequiresPrivileges;
        set => SetProperty(ref installationRequiresPrivileges, value, () => Software.InstallationRequiresPrivileges = value);
    }

    public string? InstallerPath
    {
        get => installerPath;
        set => SetProperty(ref installerPath, value, () => Software.InstallerLocation = value);
    }

    private bool isInstalled;
    public bool IsInstalled
    {
        get => isInstalled;
        set => SetProperty(ref isInstalled, value);
    }

    private bool isDynamic;
    public bool IsDynamic
    {
        get => isDynamic;
        set => SetProperty(ref isDynamic, value);
    }

    private double downloadPercentage;
    public double DownloadPercentage
    {
        get { return ((SoftwareBase)Software).SetupDownloadProgress; }
        set { SetProperty(ref downloadPercentage, value); }
    }

    private bool isInstalling;
    public bool IsInstalling
    {
        get { return isInstalling; }
        set
        {
            if (SetProperty(ref isInstalling, value))
            {
                InstallCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private ISoftwareUninstallInfo _uninstallInfo;
    public ISoftwareUninstallInfo UninstallInfo
    {
        get { return _uninstallInfo; }
        set { SetProperty(ref _uninstallInfo, value); }
    }

    #endregion

    #region Commands

    public DelegateCommand InstallCommand => new(async () => await Install(), () => !IsInstalling);
    public DelegateCommand UninstallCommand => new(async () => await Software.Uninstall());

    #endregion

    public async Task Install()
    {
        IsInstalling = true;
        await Software.Install();
        UninstallInfo = await _registryService.GetSoftwareUninstallInfo(Software.SoftwareDisplayNameRegex);
        IsInstalled = UninstallInfo.FoundInRegistry;
        IsInstalling = false;
    }

    public async Task CheckIfInstalled()
    {
        UninstallInfo = await _registryService.GetSoftwareUninstallInfo(Software.SoftwareDisplayNameRegex);
        IsInstalled = UninstallInfo.FoundInRegistry;
    }
}
