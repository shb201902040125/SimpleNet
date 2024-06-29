using SimpleNet.RemoteAddress;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet.NetSocket
{
    public abstract class SNSocket
    {
        public abstract SNRemoteAddress RemoteAddress { get; }
        public abstract bool IsConnected { get; }
        public abstract Task AsyncConnect();
        public abstract void AsyncSend(BufferArray<byte> data);
        public abstract Task<Tuple<BufferArray<byte>?, object?>> AsyncRecive();
        public abstract void Close();
    }
    public class TCPSocket : SNSocket
    {
        private TcpClient _connection;
        private TCPAddress _address;
        public TCPSocket(TcpClient client)
        {
            _connection = client;
            _connection.NoDelay = true;
            _address = client.Client.RemoteEndPoint is IPEndPoint iPEndPoint
                ? new TCPAddress(iPEndPoint.Address, iPEndPoint.Port)
                : throw new ArgumentException("In theory tcpClient addresses should not run here.");
        }
        public override SNRemoteAddress RemoteAddress => _address;
        public override bool IsConnected => _connection.Connected;
        public override void Close()
        {
            _connection.Close();
        }
        public override async Task AsyncConnect()
        {
            if (IsConnected)
            {
                return;
            }
            await _connection.ConnectAsync(_address._address, _address._port);
        }
        public override void AsyncSend(BufferArray<byte> data)
        {
            byte[] _data = data;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_data.Length + 4);
            int len = _data.Length;
            buffer[0] = (byte)(len & 0xFF000000);
            buffer[1] = (byte)(len & 0xFF0000);
            buffer[2] = (byte)(len & 0xFF00);
            buffer[3] = (byte)(len & 0xFF);
            Buffer.BlockCopy(_data, 0, buffer, 4, _data.Length);
            _connection.GetStream().BeginWrite(buffer, 0, buffer.Length,
                res =>
                {
                    (res.AsyncState as Action)?.Invoke();
                },
                () =>
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                });
        }
        public override async Task<Tuple<BufferArray<byte>?, object?>> AsyncRecive()
        {
            try
            {
                if (!IsConnected)
                {
                    return Tuple.Create<BufferArray<byte>?, object?>(null, "Socket Closed");
                }
                var stream = _connection.GetStream();
                byte[] array = new byte[4];
                stream.Read(array);
                int length = array[3] + array[2] * 0xFF + array[1] * 0xFF00 + array[0] * 0xFF0000;
                byte[] data = new byte[length];
                byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
                int len = 0, read = 0;
                while (len < length)
                {
                    read = await stream.ReadAsync(buffer, 0, 1024);
                    Buffer.BlockCopy(buffer, 0, data, len, Math.Min(data.Length - len, read));
                    len += read;
                }
                ArrayPool<byte>.Shared.Return(buffer);
                return Tuple.Create<BufferArray<byte>?, object?>(new(data, false), "Safe Arrival");
            }
            catch (Exception ex)
            {
                return Tuple.Create<BufferArray<byte>?, object?>(null, ex.ToString());
            }
        }
    }
    public class LocalSocket : SNSocket
    {
        private NamedPipeServerStream _connection;
        private LocalAddress _address;
        public LocalSocket(string pipeName)
        {
            _connection = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances);
            _address = new LocalAddress(pipeName);
        }
        public override SNRemoteAddress RemoteAddress => _address;
        public override bool IsConnected => _connection.IsConnected;
        public override async Task AsyncConnect()
        {
            if (IsConnected)
            {
                return;
            }
            await _connection.WaitForConnectionAsync();
        }
        public override async Task<Tuple<BufferArray<byte>?, object?>> AsyncRecive()
        {
            try
            {
                if (!IsConnected)
                {
                    return Tuple.Create<BufferArray<byte>?, object?>(null, "Socket Closed");
                }
                var stream = _connection;
                byte[] array = new byte[4];
                stream.Read(array);
                int length = array[3] + array[2] * 0xFF + array[1] * 0xFF00 + array[0] * 0xFF0000;
                byte[] data = new byte[length];
                byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
                int len = 0, read = 0;
                while (len < length)
                {
                    read = await stream.ReadAsync(buffer, 0, 1024);
                    Buffer.BlockCopy(buffer, 0, data, len, Math.Min(data.Length - len, read));
                    len += read;
                }
                ArrayPool<byte>.Shared.Return(buffer);
                return Tuple.Create<BufferArray<byte>?, object?>(new(data, false), "Safe Arrival");
            }
            catch (Exception ex)
            {
                return Tuple.Create<BufferArray<byte>?, object?>(null, ex.ToString());
            }
        }
        public override void AsyncSend(BufferArray<byte> data)
        {
            byte[] _data = data;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_data.Length + 4);
            int len = _data.Length;
            buffer[0] = (byte)(len & 0xFF000000);
            buffer[1] = (byte)(len & 0xFF0000);
            buffer[2] = (byte)(len & 0xFF00);
            buffer[3] = (byte)(len & 0xFF);
            Buffer.BlockCopy(_data, 0, buffer, 4, _data.Length);
            _connection.BeginWrite(buffer, 0, buffer.Length,
                res =>
                {
                    (res.AsyncState as Action)?.Invoke();
                },
                () =>
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                });
        }
        public override void Close()
        {
            _connection.Close();
        }
    }
}
