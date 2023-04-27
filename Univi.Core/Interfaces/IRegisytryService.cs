using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Univi.Core.Interfaces;
public interface IRegistryService
{
    void CreateRegistryEntries(RegistryHive registryHive, string keyPath, List<IRegistryEntry> registryEntries, bool createKeyIfNotExist = false);
    (bool Success, string? Error) CreateRegistryEntry(RegistryHive registryHive, string keyPath, string entryName, object entryValue, RegistryValueKind registryValueKind = RegistryValueKind.DWord, bool createKeyIfNotExist = false);
    bool CreateRegistryKey(RegistryHive registryHive, string keyPath);
    (bool Success, string? Error) CreateUACRegKeys();
    void DeleteKey(RegistryHive registryHive, string keyPath, string keyName);
    void DeleteValue(RegistryHive registryHive, string keyPath, string valueName);
    string? GetRegistryEntry(RegistryHive registryHive, string keyPath, string valueName, bool expandEnvironmentVariables = true);
    bool KeyExist(RegistryHive registryHive, string keyPath);
    bool ValueExist(RegistryHive registryHive, string keyPath, string valueName);

    /// <summary>
    ///     Check if a program is installed by looking the uninstall string in registry
    /// </summary>
    /// <param name="softwareDisplayNameRegex"></param>
    /// <returns></returns>
    Task<ISoftwareUninstallInfo> GetSoftwareUninstallInfo(string softwareDisplayNameRegex, CancellationToken token = default);
    Task<bool> WaitForRegistryEntryToExist(RegistryHive registryHive, string keyPath, string entryName, int timeout = 20000);
}
