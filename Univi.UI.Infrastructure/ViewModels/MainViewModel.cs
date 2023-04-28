using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Univi.Core.Interfaces;

namespace Univi.UI.Infrastructure.ViewModels;

public class MainViewModel : BindableBase
{
    #region Private readonly fields
    private readonly ILogger<MainViewModel> _logger;
    private readonly ILocalWindowsSessionService _localWindowsSessionService;
    private readonly IProcessService _processService;
    private readonly IAppSettingsReader _appSettingsReader;
    private readonly IContainerProvider _containerProvider;
    private readonly ISoftwareManagerService _softwareManagerService;
    private readonly ISoftwareUninstallService _softwareUninstallService;
    private readonly ISetupProviderFactory _setupProviderFactory;
    #endregion

    #region Properties

    private List<SoftwareViewModel> _selecteSoftwares;
    public List<SoftwareViewModel> SelectedSoftwares
    {
        get { return Softwares.Where(s => s.IsSelected).ToList(); }
        set { SetProperty(ref _selecteSoftwares, value); }
    }

    private string? _title;
    public string? Title
    {
        get { return _title; }
        set { SetProperty(ref _title, value); }
    }

    private bool _isStartedAsAdmin;
    public bool IsStartedAsAdmin
    {
        get { return _isStartedAsAdmin; }
        set { SetProperty(ref _isStartedAsAdmin, value); }
    }

    public string? CurrentConsoleUsername { get; set; }


    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            FilterSoftwares();
        }
    }

    public ICollectionView FilteredSoftwares { get; }

    public ObservableCollection<SoftwareViewModel> Softwares { get; private set; } = new ObservableCollection<SoftwareViewModel>();


    #endregion


    public MainViewModel(
        ILogger<MainViewModel> logger,
        ILocalWindowsSessionService localWindowsSessionService,
        IProcessService processService,
        IAppSettingsReader appSettingsReader,
        IContainerProvider containerProvider,
        ISoftwareManagerService softwareManagerService,
        ISoftwareUninstallService softwareUninstallService,
        ISetupProviderFactory setupProviderFactory)
    {
        _logger = logger;
        _localWindowsSessionService = localWindowsSessionService;
        _processService = processService;
        _appSettingsReader = appSettingsReader;
        _containerProvider = containerProvider;
        _softwareManagerService = softwareManagerService;
        _softwareUninstallService = softwareUninstallService;
        _setupProviderFactory = setupProviderFactory;
        CurrentConsoleUsername = _localWindowsSessionService.GetLoggedInUsername();
        IsStartedAsAdmin = _processService.IsRunningAsAdmin;
        Title = "Univi" + (IsStartedAsAdmin ? " (Admin)" : string.Empty) + $" | Utilisateur détecté : {CurrentConsoleUsername}";



        FilteredSoftwares = CollectionViewSource.GetDefaultView(Softwares);


        LoadSoftwares();
    }

    public DelegateCommand InstallSelectedSoftwaresCommand => new(async () => await InstallSelectedSoftwares());

    private async Task InstallSelectedSoftwares()
    {
        foreach (SoftwareViewModel software in SelectedSoftwares)
        {
            await software.Install();
        }
    }


    private async void LoadSoftwares()
    {
        var appSettingsSoftwares = _appSettingsReader.GetSoftwareFromConfiguration();
        var dynamicSoftwares = _softwareManagerService.GetSoftwares();

        // Combine the lists of softwares
        var allSoftwares = appSettingsSoftwares.Concat(dynamicSoftwares).ToList();

        // Add the softwares to the ObservableCollection
        foreach (var software in allSoftwares)
        {
            var softwareViewModel = _containerProvider.Resolve<SoftwareViewModel>((typeof(ISoftware), software));
            Softwares.Add(softwareViewModel);
            await softwareViewModel.CheckIfInstalled();
        }
    }


    private void FilterSoftwares()
    {
        FilteredSoftwares.Filter = item =>
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                return true;
            }

            if (item is SoftwareViewModel softwareViewModel)
            {
                return softwareViewModel.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        };
    }
}
