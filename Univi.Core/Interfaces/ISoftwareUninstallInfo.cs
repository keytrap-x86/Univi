namespace Univi.Core.Interfaces;
public interface ISoftwareUninstallInfo
{
    public string? UninstallString { get; set; }
    bool FoundInRegistry { get; set; }
}
