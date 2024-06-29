using SimpleNet.NetSocket;
using SimpleNet.RemoteAddress;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet.Lobbys
{
    public class NormalLobby : Lobby
    {
        public enum MessageType : sbyte
        {
            GetIP,
            BroadCast,
            ToIP
        }
        public enum CallBackType : sbyte
        {
            GetIP_Success,
            GetIP_Fail,
            BroadCast,
            FromIP
        }
        internal static bool TryCreate(Dictionary<string, string> paramters, [NotNullWhen(true)] out NormalLobby? normalLobby, [NotNullWhen(false)] out string? failReason)
        {
            bool useIPV6 = paramters.ContainsKey("ipv6");
            bool useLocal = paramters.TryGetValue("local", out string? localName);
            normalLobby = new NormalLobby();
            normalLobby.StartListen(0, useIPV6 ? 0 : null, useLocal ? localName : null);
            failReason = null;
            return true;
        }
        protected override void HandleMemory(MemoryStream memory, SNSocket socket)
        {
            using BinaryReader reader = new(memory);
            using MemoryStream reply = new();
            using BinaryWriter writer = new(reply);
            string dialogueLabel = reader.ReadString();
#if DEBUG
            Console.WriteLine(dialogueLabel);
#endif
            writer.Write(dialogueLabel);
            sbyte messageType = reader.ReadSByte();
            switch ((MessageType)messageType)
            {
                case MessageType.GetIP:
                    {
#if DEBUG
                        Console.WriteLine("Request GetIP");
#endif
                        if (Program.LocalIP is not null)
                        {
                            writer.Write((sbyte)CallBackType.GetIP_Success);
                            writer.Write(Program.LocalIP);
                        }
                        else
                        {
                            writer.Write((sbyte)CallBackType.GetIP_Fail);
                        }
                        break;
                    }
                case MessageType.BroadCast:
                    {
                        writer.Write((sbyte)CallBackType.BroadCast);
                        writer.Write(socket.RemoteAddress.ToString() ?? "Anonymous");
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
                        int read = 0;
                        while ((read = reader.Read(buffer)) != 0)
                        {
                            writer.Write(buffer[0..read]);
                        }
                        ArrayPool<byte>.Shared.Return(buffer);
                        using BufferArray<byte> bufferArray2 = new(reply.ToArray());
                        foreach (var acceptedSocket in _acceptedSockets)
                        {
                            if (acceptedSocket == socket || !acceptedSocket.IsConnected)
                            {
                                continue;
                            }
                            acceptedSocket.AsyncSend(bufferArray2);
                        }
                        return;
                    }
                case MessageType.ToIP:
                    {
                        string[] ips = reader.ReadString().Split(" ");
                        writer.Write((sbyte)CallBackType.FromIP);
                        writer.Write(socket.RemoteAddress.ToString() ?? "Anonymous");
                        var buffer = ArrayPool<byte>.Shared.Rent(1024);
                        int read = 0;
                        while ((read = reader.Read(buffer)) != 0)
                        {
                            writer.Write(buffer[0..read]);
                        }
                        ArrayPool<byte>.Shared.Return(buffer);
                        using BufferArray<byte> bufferArray3 = new(reply.ToArray());
                        foreach (string ip in ips)
                        {
                            var target = _acceptedSockets.Find(socket => socket.IsConnected && socket.ToString() == ip);
                            target?.AsyncSend(bufferArray3);
                        }
                        return;
                    }
            }
            using BufferArray<byte> bufferArray = new(reply.ToArray());
            socket.AsyncSend(bufferArray);
        }
        public static async void Send_GetIP(string? dialogueLabel, SNSocket serverSocket, Action<string?, BufferArray<byte>?> callback)
        {
            dialogueLabel ??= "GetIP";
            using (MemoryStream stream1 = new())
            {
                using (BinaryWriter writer = new(stream1))
                {
                    writer.Write(dialogueLabel);
                    writer.Write((sbyte)MessageType.GetIP);
                    using BufferArray<byte> buffer = new(stream1.ToArray());
                    serverSocket.AsyncSend(buffer);
                }
            }
            var result = await serverSocket.AsyncRecive();
            if (result.Item1 == null)
            {
                return;
            }
            using (MemoryStream stream2 = new(result.Item1))
            {
                using (BinaryReader reader = new(stream2))
                {
                    if (reader.ReadString() != dialogueLabel)
                    {
                        callback(null, result.Item1);
                        return;
                    }
                    CallBackType callBackType = (CallBackType)reader.ReadSByte();
                    if (callBackType == CallBackType.GetIP_Success)
                    {
                        callback(reader.ReadString(), null);
                    }
                    else
                    {
                        callback(null, null);
                    }
                }
            }
        }
        public static void Send_BroadCast(string? dialogueLabel, SNSocket serverSocket, BufferArray<byte> data)
        {
            dialogueLabel ??= "BroadCast";
            using (MemoryStream stream1 = new())
            {
                using (BinaryWriter writer = new(stream1))
                {
                    writer.Write(dialogueLabel);
                    writer.Write((sbyte)MessageType.BroadCast);
                    writer.Write(data);
                    using BufferArray<byte> buffer = new(stream1.ToArray());
                    serverSocket.AsyncSend(buffer);
                }
            }
        }
        public static void Send_ToIP(string? dialogueLabel, SNSocket serverSocket, string[] ips, BufferArray<byte> data)
        {
            dialogueLabel ??= "ToIP";
            using (MemoryStream stream1 = new())
            {
                using (BinaryWriter writer = new(stream1))
                {
                    writer.Write(dialogueLabel);
                    writer.Write((sbyte)MessageType.ToIP);
                    List<string> collection = [];
                    foreach (string ip in ips)
                    {
                        if (IPEndPoint.TryParse(ip, out _))
                        {
                            collection.Add(ip);
                        }
                    }
                    writer.Write(string.Join(" ", collection));
                    writer.Write(data);
                    using BufferArray<byte> buffer = new(stream1.ToArray());
                    serverSocket.AsyncSend(buffer);
                }
            }
        }
        public static bool IsBoradcastOrFromIP(byte[] origData, CallBackType callBackType, [NotNullWhen(true)] out string? from, [NotNullWhen(true)] out BufferArray<byte>? data)
        {
            from = null;
            data = null;
            if (callBackType is not CallBackType.BroadCast or CallBackType.FromIP)
            {
                return false;
            }
            try
            {
                using (MemoryStream stream = new(origData))
                {
                    using (BinaryReader reader = new(stream))
                    {
                        _ = reader.ReadString();
                        if (reader.ReadSByte() != (sbyte)callBackType)
                        {
                            return false;
                        }
                        from = reader.ReadString();
                        data = new(new byte[stream.Length - stream.Position], false);
                        var buffer = ArrayPool<byte>.Shared.Rent(1024);
                        int read = 0, ptr = 0;
                        while ((read = reader.Read(buffer)) != 0)
                        {
                            Array.Copy(buffer, 0, data, ptr, read);
                            ptr += read;
                        }
                        ArrayPool<byte>.Shared.Return(buffer);
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}