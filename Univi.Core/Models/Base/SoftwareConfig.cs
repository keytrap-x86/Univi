namespace Univi.Core.Models.Base;
public class SoftwareConfig
{
    public string Name { get; set; }
    public string InstallerLocation { get; set; }
    public string InstallerArguments { get; set; }
    public string SoftwareDisplayNameRegex { get; set; }
    public bool InstallationRequiresPrivileges { get; set; }
    public string InstallerVersion { get; set; }
    public string InstallerFileNameRegex { get; set; }
    public string UninstallerArguments { get; set; }
}
