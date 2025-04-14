using EmbedIO;
using U2NetSharp.Server.BGRemoval;

namespace U2NetSharp.Server
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private WebServer _webServer;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            // Configuração do WebSocket
            _webServer = new WebServer("http://localhost:8075")
                .WithModule(new BackgroundRemovalServer("/background-removal"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WebSocket server running at ws://localhost:8075/background-removal");

            // Inicia o servidor WebSocket
            await _webServer.RunAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_webServer != null)
            {
                _logger.LogInformation("Stopping WebSocket server...");
                _webServer.Dispose();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
