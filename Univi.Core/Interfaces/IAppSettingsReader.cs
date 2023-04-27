using System.Collections.Generic;
using Univi.Core.Models.Base;

namespace Univi.Core.Interfaces;
public interface IAppSettingsReader
{
    List<SoftwareBase> GetSoftwareFromConfiguration();
}
