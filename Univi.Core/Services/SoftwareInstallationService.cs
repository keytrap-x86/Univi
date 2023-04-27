using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Univi.Core.Services
{
    public class SoftwareInstallationService
    {
        private readonly ILogger<SoftwareInstallationService> _logger;

        public SoftwareInstallationService(ILogger<SoftwareInstallationService> logger)
        {
            _logger = logger;
        }
    }
}
