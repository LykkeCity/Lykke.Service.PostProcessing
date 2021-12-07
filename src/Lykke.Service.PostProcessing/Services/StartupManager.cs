using System.Threading.Tasks;
using Antares.Sdk.Services;
using Lykke.Cqrs;

namespace Lykke.Service.PostProcessing.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ICqrsEngine _cqrsEngine;

        public StartupManager(ICqrsEngine cqrsEngine)
        {
            _cqrsEngine = cqrsEngine;
        }

        public Task StartAsync()
        {
            _cqrsEngine.StartSubscribers();
            _cqrsEngine.StartProcesses();

            return Task.CompletedTask;
        }
    }
}
