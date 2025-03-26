using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text;
using System.Net.Sockets;
using System.Net;

Log.Logger = new LoggerConfiguration()
      .MinimumLevel.Debug() // Set the minimum log level to Debug
      .WriteTo.Console()
      .CreateLogger();

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(configure => configure.AddSerilog());
 string lineTerminator  = "\r\n";
// Define the endpoint for the server
int port = 12345;
TcpListener server = new TcpListener(IPAddress.Any, port);

try
{
    // Start listening for incoming connections
    server.Start();
    Console.WriteLine("Server started. Waiting for a connection...");

    // Accept incoming client connection
    TcpClient client = server.AcceptTcpClient();
    Console.WriteLine("Client connected.");

    Console.WriteLine("Server started on {0}:{1}", 12345, port);

    // Start listening for client connections in a new thread
    ListenForClients(client); 
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}

void ListenForClients(TcpClient client)
{
    Task.Factory.StartNew(() =>
    { 
        while (true)
        {
            try
            {
                // Start a new thread to handle the communication with this client
                ProcessCurrentClientStream(client);
            }
            catch (Exception ex)
            {
                // Log any error that occurs while accepting clients
                Console.WriteLine("Error while accepting client: " + ex.Message);
                client = server.AcceptTcpClient();
                Console.WriteLine("Waiting for a client.");
            }
        }
    });
}


void ProcessCurrentClientStream(TcpClient tcpClient)
{ 
    var stream = tcpClient.GetStream();
    var buffer = new byte[255];
    var bytesRead = stream.Read(buffer, 0, buffer.Length);
    var pendingString = Encoding.ASCII.GetString(buffer, 0, bytesRead);

    // Gather the terminated strings of interest
    var lineEndIndex = pendingString.IndexOf(lineTerminator, StringComparison.Ordinal);
    while (lineEndIndex >= 0)
    {
        var message = pendingString[..(lineEndIndex + lineTerminator.Length)];
        pendingString = pendingString.Remove(0, lineEndIndex + 1);
        message = message.Replace(lineTerminator.ToString(), string.Empty);

        if (message.Contains("PWR?"))
        {
            string response = "PWR=1";

            var sendStream = tcpClient.GetStream();
            var data = Encoding.ASCII.GetBytes(response);
            sendStream.Write(data, 0, data.Length); 
        }
        lineEndIndex = pendingString.IndexOf(lineTerminator, StringComparison.Ordinal);
    }
}
