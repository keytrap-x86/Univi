# Univi
![](https://github.com/keytrap-x86/Univi/blob/master/Univi/Resources/Images/logo.png?raw=true)
Univi is a WPF application designed to simplify automated software installation on Windows machines. The application reads an appsettings.json file containing information about the software to be installed, then downloads and installs the software using the specified settings.

## Features
- Automatically installs software from various sources (GitHub, FTP, HTTP, etc.).
- Supports custom installation arguments for each software.
- Handles installations that require administrative privileges.
- Easy to configure and extend.
- Configuration
- The software to be installed and their settings are defined in an appsettings.json file. 


## Configuration
Here's an example configuration file:

```json
{
  "Software": [
	{
      "Name": "VLC",
      "InstallerLocation": "ftp://anonymous:anonymous@ftp.free.org/mirrors/videolan/vlc/last/win64/",
      "InstallerFileNameRegex": "vlc-(?<version>[0-9]+\\.[0-9]+\\.[0-9]+)-win64\\.exe$",
      "InstallerArguments": "/S",
      "SoftwareDisplayNameRegex": "VLC",
      "InstallationRequiresPrivileges": true
    },
    {
      "Name": "ScreenToGif",
      "InstallerLocation": "GitHub:NickeManarin/ScreenToGif",
      "InstallerVersion": "latest",
      "InstallerArguments": "/quiet /norestart INSTALLDESKTOPSHORTCUT=yes INSTALLSHORTCUT=yes",
      "SoftwareDisplayNameRegex": "ScreenToGif",
      "InstallationRequiresPrivileges": true
    }
  ]
}
```