using System.Threading;
using System.Threading.Tasks;

namespace Univi.Core.Interfaces;
public interface ISoftwareUninstallService
{
    Task<int> UninstallSoftware(ISoftwareUninstallInfo softwareUninstallInfo, CancellationToken token = default);
}
