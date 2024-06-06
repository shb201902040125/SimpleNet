using SimpleNet.NetSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace SimpleNet.Lobbys
{
    public class Lobby
    {
        protected CancellationToken listenToken;
        public string UniqueID { get; protected set; } = Guid.NewGuid().ToString();
        public int? IPV4Port;
        public int? IPV6Port;
        public string? LocalPipeName;
        protected List<SNSocket> _acceptedSockets = [];
        protected bool _connectSuccessAtLeastOne = false;
        public virtual void StartListen(int? ipv4Port, int? ipv6Port, string? localPipeName)
        {
            if (ipv4Port == null && ipv6Port == null && localPipeName == null)
            {
                return;
            }
            StopListen();
            listenToken = new CancellationToken();
            if (ipv4Port != null)
            {
                Task.Factory.StartNew(ListenIPV4, ipv4Port.Value, listenToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            if (ipv6Port != null)
            {
                Task.Factory.StartNew(ListenIPV6, ipv6Port.Value, listenToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            if (localPipeName != null)
            {
                Task.Factory.StartNew(ListenLocal, localPipeName, listenToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
        public virtual void StopListen()
        {
            listenToken.ThrowIfCancellationRequested();
        }
        private async void ListenIPV4(object? state)
        {
            if (state is not int port)
            {
                return;
            }
            TcpListener listener = new(IPAddress.Any, port);
            listener.Start();
            IPV4Port = ((IPEndPoint)listener.LocalEndpoint).Port;
#if DEBUG
            Console.WriteLine($"IPV4 Listen[Port:{port}] Start");
#endif
            try
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync(listenToken);
                    Task task = new(SocketListenLoop, new TCPSocket(client), listenToken, TaskCreationOptions.LongRunning);
                    task.Start();
                }
            }
            finally
            {
                listener.Stop();
            }
        }
        private async void ListenIPV6(object? state)
        {
            if (state is not int port)
            {
                return;
            }
            TcpListener listener = new(IPAddress.IPv6Any, port);
            listener.Start();
            IPV6Port = ((IPEndPoint)listener.LocalEndpoint).Port;
#if DEBUG
            Console.WriteLine($"IPV6 Listen[Port:{port}] Start");
#endif
            try
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync(listenToken);
                    Task task = new(SocketListenLoop, new TCPSocket(client), listenToken, TaskCreationOptions.LongRunning);
                    task.Start();
                }
            }
            finally
            {
                listener.Stop();
            }
        }
        private async void ListenLocal(object? state)
        {
            if (state is not string localPipeName)
            {
                return;
            }
#if DEBUG
            Console.WriteLine($"Local Listen[Name:{localPipeName}] Start");
#endif
            LocalPipeName= localPipeName;
            try
            {
                while (true)
                {
                    LocalSocket socket = new(localPipeName);
                    await socket.AsyncConnect();
                    Task task = new(SocketListenLoop, socket, listenToken, TaskCreationOptions.LongRunning);
                    task.Start();
                }
            }
            finally
            {
            }
        }
        protected virtual async void SocketListenLoop(object? state)
        {
            if (state is not SNSocket socket)
            {
                return;
            }
            _acceptedSockets.Add(socket);
            await socket.AsyncConnect();
            _connectSuccessAtLeastOne = true;
#if DEBUG
            Console.WriteLine($"Socket Accept:{socket.GetType().Name}-{socket.RemoteAddress}");
#endif
            try
            {
                while (socket.IsConnected)
                {
                    var result = await socket.AsyncRecive();
                    if (result.Item1 == null)
                    {
                        continue;
                    }
                    HandleMemory(new MemoryStream(result.Item1), socket);
                }
            }
            finally
            {
                _acceptedSockets.Remove(socket);
                socket.Close();
            }
        }
        protected virtual void HandleMemory(MemoryStream memory, SNSocket socket)
        {
            memory.Dispose();
        }
        public virtual bool Close(bool force = false)
        {
            if (force)
            {
                listenToken.ThrowIfCancellationRequested();
                return true;
            }
            else
            {
                if (!_connectSuccessAtLeastOne)
                {
                    return false;
                }
                if (_acceptedSockets.Any(socket => socket.IsConnected))
                {
                    return false;
                }
                listenToken.ThrowIfCancellationRequested();
                return true;
            }
        }
    }
}
