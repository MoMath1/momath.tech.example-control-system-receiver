using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

namespace ExampleControlsSystemReceiver.ConsoleUi
{
    public class Program
    {
        private static AsyncUdpLink _asyncUdpLink;

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // Set the minimum log level to Debug
                .WriteTo.Console()
                .WriteTo.File(
                    new JsonFormatter(),
                    "logs/log.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7) // Retain log files for 7 days
                .CreateLogger();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(configure => configure.AddSerilog());
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var version = VersionService.GetAppVersion();
            Log.Information("ExampleControlsSystemReceiver.ConsoleUi version {version}", version);

            var logger = serviceProvider.GetService<ILogger<AsyncUdpLink>>();
            _asyncUdpLink = new AsyncUdpLink(logger, "127.0.0.1", 12345, 500);

            // Subscribe to the DataReceived event
            _asyncUdpLink.DataReceived += OnDataReceived;

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void OnDataReceived(object sender, EventArgs e)
        {
            Log.Information("Data received!");
            var message = _asyncUdpLink.GetMessage();

            {
                var msg = Encoding.ASCII.GetString(message);
                Log.Debug("Received message {msg}", msg);

                if (msg == "GetContent") GetContent();
                if (msg == "Stop") Stop();
                if (msg == "Start") Start();
                if (msg == "SoftReset") SoftReset();
                if (msg == "DebugOn") DebugOn();
                if (msg == "DebugOff") DebugOff();

                if (msg.Contains("ShowScene"))
                {
                    var parts = msg.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[1], out var scene))
                    {
                        ShowScene(scene);
                    }

                    else
                    {
                        Log.Warning("Invalid ShowScene command format: {msg}", msg);
                    }
                }
            }
        }

        private static void GetContent()
        {
            Log.Information("Getting Content Somehow");
        }

        private static void Stop()
        {
            Log.Information("Stopped");
        }

        private static void Start()
        {
            Log.Information("Started");
        }

        private static void SoftReset()
        {
            Log.Information("Soft Resetting");
            Thread.Sleep(500);
            Log.Information("Soft Reset Complete");
        }

        private static void HardReset()
        {
            Log.Information("Hard Resetting");
            Thread.Sleep(500);
            Log.Information("Hard Reset Complete");
        }

        private static void DebugOn()
        {
            Log.Information("Debug Mode On");
        }

        private static void DebugOff()
        {
            Log.Information("Debug Mode Off");
        }

        private static void ShowScene(int scene)
        {
            Log.Information("Scene Command Received, Switching to {scene}", scene);
            Thread.Sleep(500);
            Log.Information("Scene Switched to {scene}", scene);
        }
    }
}