using System;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Interfaces;
using Univi.Core.Models.Base;

namespace Univi.Core.Models.SetupProviders;
public class GithubSetupProvider : ISetupProvider
{
    public Task<string?> GetInstaller(SoftwareBase software, Func<double, double> downloadProgress = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
