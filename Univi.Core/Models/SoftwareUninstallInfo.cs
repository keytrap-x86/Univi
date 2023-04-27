using Univi.Core.Interfaces;

namespace Univi.Core.Models;

public class SoftwareUninstallInfo : ISoftwareUninstallInfo
{
    public string? UninstallString { get; set; }
    public bool FoundInRegistry { get; set; }
}
