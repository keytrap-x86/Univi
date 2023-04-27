using DryIoc;
using DryIoc.Microsoft.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Windows;
using Univi.Core.Infrastructure;
using Univi.Core.Interfaces;
using Univi.Core.Models.SetupProviders;
using Univi.Core.Services;
using Univi.UI.Infrastructure.ViewModels;

namespace Univi;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    /// <summary>
    ///     Create the main Window
    /// </summary>
    /// <returns></returns>
    protected override Window CreateShell()
    {
        // Configure Serilog and the sinks at the startup of the app
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Debug(LogEventLevel.Verbose)
#if RELEASE
            .WriteTo.File("C:\\Temp\\Universal.Installer.log", LogEventLevel.Debug)
#endif
            .CreateLogger();

        return Container.Resolve<MainView>();
    }

    /// <summary>
    ///     Register types with the container that will be used by your application.
    /// </summary>
    /// <param name="container"></param>
    protected override void RegisterTypes(IContainerRegistry container)
    {
        container.RegisterInstance<IConfiguration>(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());
        container.Register<ILocalWindowsSessionService, LocalWindowsSessionService>();
        container.Register<IRegistryService, RegistryService>();
        container.Register<IProcessService, ProcessService>();
        container.Register<IAppSettingsReader, AppSettingsReader>();
        container.Register<ISoftwareManagerService, SoftwareManagerService>();
        container.Register<ISoftwareUninstallService, SoftwareUninstallService>();
        container.Register<ISetupProviderFactory, SetupProviderFactory>();
        container.Register<GithubSetupProvider>();
        container.Register<FtpSetupProvider>();
        container.Register<SmbSetupProvider>();
        container.Register<HttpxSetupProvider>();
        container.Register<SoftwareViewModel>();

    }

    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();
        ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver((viewType) =>
        {
            var viewName = viewType.FullName;
            var viewAssemblyName = viewType.GetTypeInfo().Assembly.GetName().Name;
            var viewModelName = $"Univi.UI.Infrastructure.ViewModels.{viewName[(viewName.LastIndexOf('.') + 1)..]}ViewModel".Replace("ViewView", "View");

            var infrastructureAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Univi.UI.Infrastructure");

            if (infrastructureAssembly == null)
            {
                throw new InvalidOperationException("L'assemblage Univi.UI.Infrastructure n'a pas été trouvé.");
            }

            var output = infrastructureAssembly.GetType(viewModelName);
            return output;
        });
    }

    /// <summary>
    ///     Add serilog extensions to DryIoc
    /// </summary>
    /// <returns></returns>
    protected override IContainerExtension CreateContainerExtension()
    {
        ServiceCollection serviceCollection = new();
        serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        return new DryIocContainerExtension(new Container(CreateContainerRules())
                .WithDependencyInjectionAdapter(serviceCollection));
    }
}
