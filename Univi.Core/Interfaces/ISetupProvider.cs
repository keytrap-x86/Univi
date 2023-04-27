using System;
using System.Threading;
using System.Threading.Tasks;
using Univi.Core.Models.Base;

namespace Univi.Core.Interfaces;

public interface ISetupProvider
{
    /// <summary>
    ///     
    /// </summary>
    /// <param name="software"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<string?> GetInstaller(SoftwareBase software, Func<double, double> downloadProgress = null, CancellationToken token = default);
}
