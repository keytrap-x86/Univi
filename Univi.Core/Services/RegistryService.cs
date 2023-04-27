using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;
using Univi.Core.Models;

namespace Univi.Core.Services;
public class RegistryService : IRegistryService
{
    private readonly ILogger<RegistryService> _logger;
    private readonly ILocalWindowsSessionService _windowsSessionService;

    public RegistryService(
        ILogger<RegistryService> logger, ILocalWindowsSessionService windowsSessionService)
    {
        _logger = logger;
        _windowsSessionService = windowsSessionService;
    }

    public async Task<bool> WaitForRegistryEntryToExist(RegistryHive registryHive, string keyPath, string entryName, int timeout = 20_000)
    {
        bool result = false;

        if (registryHive == RegistryHive.CurrentUser)
        {
            registryHive = RegistryHive.Users;
            keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}\\";
        }

        int count = 0;
        while (count < timeout / 1000)
        {
            if (ValueExist(registryHive, keyPath, entryName))
            {
                result = true;
                break;
            }

            await Task.Delay(1000);
        }

        return result;
    }
    public (bool Success, string? Error) CreateUACRegKeys()
    {
        try
        {
            var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
            var key = Registry.LocalMachine.OpenSubKey(path, true);
            if (key == null)
                return (false, $"Couldn't find the key {path}");

            key.SetValue("ConsentPromptBehaviorAdmin", "5", RegistryValueKind.DWord);
            key.SetValue("ConsentPromptBehaviorUser", "3", RegistryValueKind.DWord);
            key.SetValue("EnableLUA", "1", RegistryValueKind.DWord);
            key.SetValue("PromptOnSecureDesktop", "0", RegistryValueKind.DWord);
            key.Close();
            return (true, null);

        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error while creating UAC registry keys : {err}", ex.Message);
            return (false, ex.Message);
        }

    }
    public (bool Success, string? Error) CreateRegistryEntry(RegistryHive registryHive, string keyPath, string entryName, object entryValue, RegistryValueKind registryValueKind = RegistryValueKind.DWord, bool createKeyIfNotExist = false)
    {
        try
        {
            if (registryHive == RegistryHive.CurrentUser)
            {
                registryHive = RegistryHive.Users;
                keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}\\";
            }

            var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (key == null)
            {
                if (!createKeyIfNotExist)
                {
                    var msg = $"CreateRegistryEntry : La clé registre {keyPath} n'existe pas";
                    _logger.LogWarning(msg);
                    return (false, msg);
                }
                else
                {
                    var initialKeyPath = keyPath.TrimEnd('\\');
                    var keyPathParts = keyPath.TrimEnd('\\').Split('\\');
                    Array.Reverse(keyPathParts);
                    for (int i = 0; i < keyPathParts.Length - 1; i++)
                    {
                        var currentKeyPath = initialKeyPath[..initialKeyPath.LastIndexOf(keyPathParts[i])].TrimEnd('\\');
                        var tmpKey = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(currentKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                        if (tmpKey != null)
                        {
                            Array.Reverse(keyPathParts);
                            for (int j = keyPathParts.Length - 1 - i; j < keyPathParts.Length; j++)
                            {
                                var newKeyToCreate = currentKeyPath += $"\\{keyPathParts[j]}";
                                Console.WriteLine("Next : " + newKeyToCreate + " Created : " + CreateRegistryKey(registryHive, newKeyToCreate));
                            }
                            break;
                        }
                    }
                }

            }
            else
            {
                key.SetValue(entryName, entryValue, registryValueKind);
                key.Close();
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while creating registry entry key: {key} : {err}", keyPath, ex.Message);
            return (false, ex.Message);
        }
    }
    public bool CreateRegistryKey(RegistryHive registryHive, string keyPath)
    {
        try
        {
            keyPath = keyPath.TrimEnd('\\');
            if (registryHive == RegistryHive.CurrentUser)
            {
                registryHive = RegistryHive.Users;
                keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}\\";
            }
            var pathKeys = keyPath.Split('\\');
            var keyToCreate = pathKeys[^1];
            var parentPath = keyPath.Substring(0, keyPath.LastIndexOf(keyToCreate));

            var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(parentPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (key == null)
                return false;
            key.CreateSubKey(keyToCreate, RegistryKeyPermissionCheck.ReadWriteSubTree);

            key.Close();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while creating registry key {path} : {err}", keyPath, ex.Message);
            return false;
        }
    }
    public string? GetRegistryEntry(RegistryHive registryHive, string keyPath, string valueName, bool expandEnvironmentVariables = true)
    {
        try
        {
            if (registryHive == RegistryHive.CurrentUser)
            {
                registryHive = RegistryHive.Users;
                keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}\\";
            }

            var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.Default);
            if (key == null)
            {
                var msg = $"{nameof(GetRegistryEntry)} : La clé registre {keyPath} n'existe pas";
                _logger.LogWarning(msg);
                return null;
            }

            var val = key.GetValue(valueName, null, expandEnvironmentVariables ? RegistryValueOptions.None : RegistryValueOptions.DoNotExpandEnvironmentNames);
            key?.Close();
            return val?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while getting registry entry {path} {name} : {err}", keyPath, valueName, ex.Message);
            return null;
        }

    }
    public async Task<ISoftwareUninstallInfo> GetSoftwareUninstallInfo(string softwareDisplayNameRegex, CancellationToken token = default)
    {
        return await Task.Run(() =>
        {

            ISoftwareUninstallInfo output = new SoftwareUninstallInfo();

            const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";


            try
            {
                //Current User Registry
                var registryKeyCU = $"{_windowsSessionService.GetConsoleUserSid()}\\{registryKey}\\";

                var baseKeyCU = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);

                SearchUninstallStrings(ref output, registryKey, baseKeyCU, softwareDisplayNameRegex);
                if (output.FoundInRegistry)
                    return output;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error while looking at registry key HKCU:\\{key} for app {app} : {err}", registryKey, softwareDisplayNameRegex, ex.Message);
            }


            try
            {
                //32 bits LocalMachine Registry
                var baseKey32Bits = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

                SearchUninstallStrings(ref output, registryKey, baseKey32Bits, softwareDisplayNameRegex);
                if (output.FoundInRegistry)
                    return output;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error while looking at registry key HKLM:\\{key} ({x}) for app {app} : {err}", registryKey, "32 bits", softwareDisplayNameRegex, ex.Message);
            }


            try
            {
                //64 bits LocalMachine Registry
                var baseKey64Bits = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                SearchUninstallStrings(ref output, registryKey, baseKey64Bits, softwareDisplayNameRegex);
                if (output.FoundInRegistry)
                    return output;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error while looking at registry key HKLM:\\{key} ({x}) for app {app} : {err}", registryKey, "64 bits", softwareDisplayNameRegex, ex.Message);
            }

            // Return default
            return output;

        }, token);
    }
    public void CreateRegistryEntries(RegistryHive registryHive, string keyPath, List<IRegistryEntry> registryEntries, bool createKeyIfNotExist = false)
    {
        try
        {

            if (registryHive == RegistryHive.CurrentUser)
            {
                registryHive = RegistryHive.Users;
                keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}\\";
            }

            var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (key == null)
            {
                if (!createKeyIfNotExist)
                {
                    var msg = $"{nameof(CreateRegistryEntries)} : La clé registre {keyPath} n'existe pas";
                    _logger.LogWarning(msg);
                    return;
                }
                else
                {
                    var initialKeyPath = keyPath.TrimEnd('\\');
                    var keyPathParts = keyPath.TrimEnd('\\').Split('\\');
                    Array.Reverse(keyPathParts);
                    for (int i = 0; i < keyPathParts.Length - 1; i++)
                    {
                        var currentKeyPath = initialKeyPath.Substring(0, initialKeyPath.LastIndexOf(keyPathParts[i])).TrimEnd('\\');
                        var tmpKey = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(currentKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                        if (tmpKey != null)
                        {
                            //Debugger.Break();
                            Array.Reverse(keyPathParts);
                            for (int j = keyPathParts.Length - 1 - i; j < keyPathParts.Length; j++)
                            {
                                var newKeyToCreate = currentKeyPath += $"\\{keyPathParts[j]}";
                                Console.WriteLine("Next : " + newKeyToCreate + " Created : " + CreateRegistryKey(registryHive, newKeyToCreate));
                            }
                            break;
                        }
                    }

                    keyPath = keyPath.TrimEnd('\\');
                    key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                    if (key == null)
                    {
                        var msg = $"{nameof(CreateRegistryEntries)} : La clé registre {keyPath} n'existe pas";
                        _logger.LogWarning(msg);
                        return;
                    }
                }
            }

            for (int i = 0; i < registryEntries.Count; i++)
            {
                try
                {
                    object value = null;
                    if (registryEntries[i].Value.ToString() == uint.MaxValue.ToString())
                        value = unchecked((int)0xffffffff);
                    key.SetValue(registryEntries[i].Name, value ?? registryEntries[i].Value, registryEntries[i].ValueKind);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur lors de l'écriture de la clé : " +
                        registryEntries[i].Name + " : " + registryEntries[i].Value + $" [{registryEntries[i].ValueKind}]" + "\n" + ex.Message);
                }

            }

            key.Close();

        }
        catch (Exception ex)
        {
            _logger.LogError("Error while creating mutliple registry entries : {err}", ex.Message);
        }
    }
    public void DeleteKey(RegistryHive registryHive, string keyPath, string keyName)
    {
        try
        {

            if (registryHive == RegistryHive.CurrentUser)
            {
                registryHive = RegistryHive.Users;
                keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}\\";
            }

            if (!KeyExist(registryHive, keyPath + "\\" + keyName))
                return;

            var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            key?.DeleteSubKeyTree(keyName);
        }
        catch (Exception)
        {

            throw;
        }
    }
    public void DeleteValue(RegistryHive registryHive, string keyPath, string valueName)
    {
        try
        {

            if (registryHive == RegistryHive.CurrentUser)
            {
                registryHive = RegistryHive.Users;
                keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}";
            }

            if (!ValueExist(registryHive, keyPath, valueName))
                return;

            var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            key?.DeleteValue(valueName, false);
        }
        catch (Exception)
        {

            throw;
        }
    }
    public bool ValueExist(RegistryHive registryHive, string keyPath, string valueName)
    {
        if (registryHive == RegistryHive.CurrentUser)
        {
            registryHive = RegistryHive.Users;
            keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}";
        }

        var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadSubTree);
        var valueExist = key?.GetValue(valueName, null);
        key?.Close();
        return valueExist != null;
    }
    public bool KeyExist(RegistryHive registryHive, string keyPath)
    {
        if (registryHive == RegistryHive.CurrentUser)
        {
            registryHive = RegistryHive.Users;
            keyPath = $"{_windowsSessionService.GetConsoleUserSid()}\\{keyPath}";
        }

        var key = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default).OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadSubTree);
        key?.Close();
        return key != null;
    }

    private static void SearchUninstallStrings(ref ISoftwareUninstallInfo output, string registryKey, RegistryKey baseKey, string softwareDisplayNameRegex)
    {
        var subkeys = baseKey.OpenSubKey(registryKey);
        if (subkeys != null)
        {
            foreach (var subkey in subkeys.GetSubKeyNames().Select(subkeys.OpenSubKey))
            {
                if (subkey?.GetValue("DisplayName") is string displayName && Regex.IsMatch(displayName, softwareDisplayNameRegex, RegexOptions.IgnoreCase))
                {
                    string? uninstallString = subkey?.GetValue("UninstallString") as string;
                    if (string.IsNullOrEmpty(uninstallString) == false)
                    {
                        output.FoundInRegistry = true;
                        output.UninstallString = uninstallString;
                    }

                    string? quietUninstallString = subkey?.GetValue("QuietUninstallString") as string;

                    if (string.IsNullOrEmpty(quietUninstallString) == false)
                    {
                        output.FoundInRegistry = true;
                        output.UninstallString = quietUninstallString;
                    }

                    break;
                }
                subkey?.Close();
            }
            subkeys.Close();
        }
        baseKey.Close();
    }
}
