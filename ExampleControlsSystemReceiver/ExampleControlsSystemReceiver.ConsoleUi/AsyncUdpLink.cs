using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace ExampleControlsSystemReceiver.ConsoleUi
{
    public class AsyncUdpLink : IDisposable, INotifyPropertyChanged
    {
        private const int MaxDataSize = 100;
        private readonly object _clientLock = new();

        private readonly List<byte[]> _incomingData;
        private readonly ILogger<AsyncUdpLink> _logger;

        private bool _disposed;
        private bool _enabled;

        private Exception _error;

        private IAsyncResult? _receiveResult;
        private IAsyncResult? _sendResult;

        private UdpClient? _udpClient;

        //Assume local and remote port should be the same
        public AsyncUdpLink(ILogger<AsyncUdpLink> logger, string address, int remotePort, int localPort = 0,
            bool enabled = true)
        {
            Address = address;
            Port = remotePort;

            _logger = logger;
            _logger.LogInformation("AsyncUdp has started");
            _logger.LogInformation("Address is {address} and remote port num is {remotePort}", address, remotePort);

            _incomingData = new List<byte[]>();
            _udpClient = new UdpClient(localPort); //Typically don't bind to the same port that you send to

            Enabled = enabled; //Default is true

            ReceiveData(); //Start the listening process
        }

        public string Address { get; }
        public int Port { get; }

        public bool HasData => _incomingData.Count > 0;

        /// <summary>
        ///     Gets or sets a value indicating whether messages should be propagated to the network or not
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled)
                    //initiate receive data
                    lock (_clientLock)
                    {
                        if (_receiveResult == null) ReceiveData(); //Initiate Receive data because no result is pending
                    }

                NotifyPropertyChanged("Enabled");
            }
        }

        public Exception Error
        {
            get => _error;
            set
            {
                var oldError = _error;
                _error = value;
                if (oldError != _error) NotifyPropertyChanged("Error");
            }
        }

        /// <summary>
        ///     Implementation of IDisposable interface.  Cancels the thread and releases resources.
        ///     Clients of this class are responsible for calling it.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return; //Dispose has already been called
            _disposed = true;
            _logger.LogInformation("Cleaning up network resources");

            SafeClose();
        }

        //Observable Interface
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public event EventHandler DataReceived;

        /// <summary>
        ///     Very carefully checks and shuts down the tcpClient and sets it to null
        /// </summary>
        private void SafeClose()
        {
            _logger.LogDebug("Safe Close");
            lock (_clientLock)
            {
                if (_receiveResult != null)
                    //End the read process
                    _receiveResult = null;
                if (_sendResult != null)
                    //End the write process
                    _sendResult = null;

                if (_udpClient != null)
                {
                    if (_udpClient.Client != null) _udpClient.Client.Close();
                    _udpClient.Close();
                }

                _udpClient = null;

                lock (_incomingData)
                {
                    _incomingData.Clear();
                }
            }
        }

        /// <summary>
        ///     Asynchronously sends the udp message
        /// </summary>
        /// <param name="message">binary message to be sent</param>
        public void SendMessage(byte[] message)
        {
            if (Enabled)
                lock (_clientLock)
                {
                    try
                    {
                        _sendResult = _udpClient.BeginSend(message, message.Length, Address, Port, SendCallback, null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Cannot Send", ex);
                        Error = ex;
                    }
                }
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            lock (_clientLock)
            {
                try
                {
                    _udpClient.EndSend(asyncResult);
                    Error = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error sending message", ex);
                    Error = ex;
                }

                if (_sendResult == asyncResult)
                    //log.Debug("Clearing send Result");
                    _sendResult = null;
            }
        }

        private void ReceiveData()
        {
            if (Enabled)
                lock (_clientLock)
                {
                    try
                    {
                        _receiveResult = _udpClient.BeginReceive(ReceiveCallback, null);
                        Error = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Cannot receive", ex);
                        Error = ex;
                    }
                }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            var hasNewData = false;

            lock (_clientLock)
            {
                try
                {
                    var remoteEndpoint = new IPEndPoint(IPAddress.Any, Port);
                    var bytesRead = _udpClient.EndReceive(asyncResult, ref remoteEndpoint);

                    if (Enabled)
                        if (bytesRead.Length > 0)
                        {
                            _incomingData.Add(bytesRead);
                            hasNewData = true;
                            while (_incomingData.Count > MaxDataSize)
                            {
                                //Purge messages from the end of the list to prevent overflow
                                _logger.LogError("Too many incoming messages to handle: " + _incomingData.Count);
                                _incomingData.RemoveAt(_incomingData.Count - 1);
                            }
                        } //If not enabled, these bytes just get lost
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error receiving from stream", ex);
                    Error = ex;
                }

                if (_receiveResult == asyncResult)
                    //log.Debug("Clearing receive Result");
                    _receiveResult = null;
            }

            if (hasNewData && DataReceived != null && !_disposed) DataReceived(this, new EventArgs());

            if (Enabled) ReceiveData();
        }

        /// <summary>
        ///     Fetches and removes (pops) the next available group of bytes as received on this link in order (FIFO)
        /// </summary>
        /// <returns>null if the link is not Enabled or there is no data currently queued to return, an array of bytes otherwise.</returns>
        public byte[] GetMessage()
        {
            if (_disposed) throw new ObjectDisposedException("Cannot get message from disposed AsyncUdpLink");

            //Return null if the link is not enabled
            if (!Enabled) return null;

            byte[] newMessage = null;
            lock (_incomingData)
            {
                if (HasData)
                {
                    newMessage = _incomingData[0];
                    _incomingData.RemoveAt(0);
                }
            }

            return newMessage;
        }
    }
}