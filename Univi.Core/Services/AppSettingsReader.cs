using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;

namespace Univi.Core.Services;

public class AppSettingsReader : IAppSettingsReader
{
    private readonly ILogger<AppSettingsReader> _logger;
    private readonly IConfiguration _config;
    private readonly IProcessService _processService;
    private readonly IRegistryService _registryService;
    private readonly ISoftwareUninstallService _softwareUninstallService;
    private readonly ISetupProviderFactory _setupProviderFactory;

    public AppSettingsReader(
        ILogger<AppSettingsReader> logger,
        IConfiguration config,
        IProcessService processService,
        IRegistryService registryService,
        ISoftwareUninstallService softwareUninstallService,
        ISetupProviderFactory setupProviderFactory)
    {
        _logger = logger;
        _config = config;
        _processService = processService;
        _registryService = registryService;
        _softwareUninstallService = softwareUninstallService;
        _setupProviderFactory = setupProviderFactory;
    }


    /// <summary>
    ///     Reads the appsettings.json file and returns the list of softwares while formatting the installer location if needed
    /// </summary>
    /// <returns></returns>
    public List<SoftwareBase> GetSoftwareFromConfiguration()
    {
        _logger.LogDebug("Lecture du fichier appsettings.json...");

        List<SoftwareBase> output = new();

        var softwaresConfigurationsSection = _config.GetSection("Softwares");
        var softwaresConfigurations = softwaresConfigurationsSection.Get<List<SoftwareConfig>>() ?? new List<SoftwareConfig>();
        var variables = _config.GetSection("Variables").GetChildren().ToDictionary(x => x.Key, x => x.Value);

        _logger.LogInformation("{x} configurations logicielles lues dans le fichier appsettings.json", softwaresConfigurations.Count);

        for (int i = 0; i < softwaresConfigurations.Count; i++)
        {
            var softwareConfig = softwaresConfigurations[i];
            ApplyVariables(ref softwareConfig, variables);

            // Create a dynamic type and instantiate it
            var dynamicType = CreateDynamicSoftwareType(softwareConfig.Name);
            var dynamicSoftwareInstance = Activator.CreateInstance(dynamicType,
                _processService, _registryService, _softwareUninstallService, _setupProviderFactory) as SoftwareBase;

            // Set the properties of the dynamic software instance
            foreach (var property in typeof(SoftwareConfig).GetProperties())
            {
                typeof(SoftwareBase).GetProperty(property.Name).SetValue(dynamicSoftwareInstance, property.GetValue(softwareConfig));
                dynamicSoftwareInstance.IsDynamic = true;
            }

            output.Add(dynamicSoftwareInstance);
        }

        return output;
    }

    private static Type CreateDynamicSoftwareType(string name)
    {
        var assemblyName = new AssemblyName(name + "DynamicSoftware");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

        var typeBuilder = moduleBuilder.DefineType(name + "DynamicSoftware", TypeAttributes.Public, typeof(SoftwareBase));

        // Define the constructor
        var baseConstructor = typeof(SoftwareBase).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new Type[] { typeof(IProcessService), typeof(IRegistryService), typeof(ISoftwareUninstallService), typeof(ISetupProviderFactory) },
            null);

        var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(IProcessService), typeof(IRegistryService), typeof(ISoftwareUninstallService), typeof(ISetupProviderFactory) });
        var constructorIL = constructorBuilder.GetILGenerator();

        constructorIL.Emit(OpCodes.Ldarg_0); // Load 'this' onto the evaluation stack
        constructorIL.Emit(OpCodes.Ldarg_1); // Load the first constructor argument
        constructorIL.Emit(OpCodes.Ldarg_2); // Load the second constructor argument
        constructorIL.Emit(OpCodes.Ldarg_3); // Load the third constructor argument
        constructorIL.Emit(OpCodes.Ldarg, 4); // Load the fourth constructor argument
        constructorIL.Emit(OpCodes.Call, baseConstructor); // Call the base constructor
        constructorIL.Emit(OpCodes.Ret); // Return

        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.SoftwareDisplayNameRegex), typeof(string));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.Name), typeof(string));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.InstallationRequiresPrivileges), typeof(bool));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.UninstallInfo), typeof(ISoftwareUninstallInfo));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.InstallerArguments), typeof(string));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.UninstallerArguments), typeof(string));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.InstallerLocation), typeof(string));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.InstallerFileNameRegex), typeof(string));
        OverrideVirtualProperty(typeBuilder, nameof(SoftwareBase.InstallerVersion), typeof(string));

        return typeBuilder.CreateType();
    }

    private static void OverrideVirtualProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
    {
        // Define the backing field
        var backingField = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

        // Define the property
        var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);

        // Define the getter method
        var getterMethodBuilder = typeBuilder.DefineMethod("get_" + propertyName,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            propertyType, Type.EmptyTypes);

        var getterIlGenerator = getterMethodBuilder.GetILGenerator();
        getterIlGenerator.Emit(OpCodes.Ldarg_0); // Load 'this' onto the stack
        getterIlGenerator.Emit(OpCodes.Ldfld, backingField); // Load the value of the backing field onto the stack
        getterIlGenerator.Emit(OpCodes.Ret); // Return the value on the stack

        // Define the setter method
        var setterMethodBuilder = typeBuilder.DefineMethod("set_" + propertyName,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            null, new[] { propertyType });

        var setterIlGenerator = setterMethodBuilder.GetILGenerator();
        setterIlGenerator.Emit(OpCodes.Ldarg_0); // Load 'this' onto the stack
        setterIlGenerator.Emit(OpCodes.Ldarg_1); // Load the value passed to the setter onto the stack
        setterIlGenerator.Emit(OpCodes.Stfld, backingField); // Store the value in the backing field
        setterIlGenerator.Emit(OpCodes.Ret); // Return

        // Assign the getter and setter methods to the property
        propertyBuilder.SetGetMethod(getterMethodBuilder);
        propertyBuilder.SetSetMethod(setterMethodBuilder);
    }


    private static void ImplementAbstractProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
    {
        var backingField = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
        var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

        var getMethodBuilder = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
        var getIL = getMethodBuilder.GetILGenerator();

        getIL.Emit(OpCodes.Ldarg_0);
        getIL.Emit(OpCodes.Ldfld, backingField);
        getIL.Emit(OpCodes.Ret);

        var setMethodBuilder = typeBuilder.DefineMethod("set_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { propertyType });
        var setIL = setMethodBuilder.GetILGenerator();

        setIL.Emit(OpCodes.Ldarg_0);
        setIL.Emit(OpCodes.Ldarg_1);
        setIL.Emit(OpCodes.Stfld, backingField);
        setIL.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getMethodBuilder);
        propertyBuilder.SetSetMethod(setMethodBuilder);
    }




    private static void ApplyVariables(ref SoftwareConfig software, Dictionary<string, string> variables)
    {
        software.InstallerLocation = ReplaceVariables(software.InstallerLocation, variables);
        software.InstallerVersion = ReplaceVariables(software.InstallerVersion, variables);
        software.InstallerFileNameRegex = ReplaceVariables(software.InstallerFileNameRegex, variables);
        software.InstallerArguments = ReplaceVariables(software.InstallerArguments, variables);
        software.UninstallerArguments = ReplaceVariables(software.UninstallerArguments, variables);
        software.SoftwareDisplayNameRegex = ReplaceVariables(software.SoftwareDisplayNameRegex, variables);
    }

    private static string? ReplaceVariables(string? input, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        foreach (var variable in variables)
        {
            string variableName = $"{{{variable.Key}}}";
            if (input.Contains(variableName))
            {
                input = input.Replace(variableName, variable.Value);
            }
        }

        return input;
    }

}
