using SimpleNet.NetSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleNet.Lobbys
{
    public class MainLobby : Lobby
    {
        public enum MessageType : sbyte
        {
            FindLobby,
            CreateLobby,
            CloseLobby,
            GetIP
        }
        public enum CallBackType : sbyte
        {
            FindLobby_Success,
            FindLobby_Fail,
            CreateLobby_Success,
            CreateLobby_Fail,
            CloseLobby_Success,
            CloseLobby_Fail,
            GetIP_Success,
            GetIP_Fail
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
                case MessageType.FindLobby:
                    {
                        string uniqueLabel = reader.ReadString();
#if DEBUG
                        Console.WriteLine("Request Find Lobby With:" + uniqueLabel);
#endif
                        if (LobbyManager.FindLobby(uniqueLabel, out Lobby? lobby, out string? failReason))
                        {
                            switch (socket.RemoteAddress.Type)
                            {
                                case RemoteAddress.SNRemoteAddress.AddressType.IPV4:
                                    {
                                        if (lobby.IPV4Port.HasValue)
                                        {
                                            writer.Write((sbyte)CallBackType.FindLobby_Success);
                                            writer.Write(lobby.IPV4Port.Value);
                                        }
                                        else
                                        {
                                            writer.Write((sbyte)CallBackType.FindLobby_Fail);
                                            writer.Write("This lobby does not support IPV4");
                                        }
                                        break;
                                    }
                                case RemoteAddress.SNRemoteAddress.AddressType.IPV6:
                                    {
                                        if (lobby.IPV6Port.HasValue)
                                        {
                                            writer.Write((sbyte)CallBackType.FindLobby_Success);
                                            writer.Write(lobby.IPV6Port.Value);
                                        }
                                        else
                                        {
                                            writer.Write((sbyte)CallBackType.FindLobby_Fail);
                                            writer.Write("This lobby does not support IPV6");
                                        }
                                        break;
                                    }
                                case RemoteAddress.SNRemoteAddress.AddressType.NamedPipeStream:
                                    {
                                        if (lobby.LocalPipeName != null)
                                        {
                                            writer.Write((sbyte)CallBackType.FindLobby_Success);
                                            writer.Write(lobby.LocalPipeName);
                                        }
                                        else
                                        {
                                            writer.Write((sbyte)CallBackType.FindLobby_Fail);
                                            writer.Write("This lobby does not support NamedPipeStream");
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            writer.Write((sbyte)CallBackType.FindLobby_Fail);
                            writer.Write(failReason);
                        }
                        break;
                    }
                case MessageType.CreateLobby:
                    {
                        string uniqueLabel = reader.ReadString();
                        string lobbyType = reader.ReadString();
                        Dictionary<string, string>? paramters = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadString());
#if DEBUG
                        string output = $"Request Create Lobby With:{uniqueLabel}_{lobbyType}";
                        if (paramters != null)
                        {
                            foreach (var pair in paramters)
                            {
                                output += $"\n{pair.Key}:{pair.Value}";
                            }
                        }
                        Console.WriteLine(output);
#endif
                        if (LobbyManager.Create(uniqueLabel, lobbyType, paramters, out Lobby? lobby, out string? failReason))
                        {
                            switch (socket.RemoteAddress.Type)
                            {
                                case RemoteAddress.SNRemoteAddress.AddressType.IPV4:
                                    {
                                        if (lobby.IPV4Port.HasValue)
                                        {
                                            writer.Write((sbyte)CallBackType.CreateLobby_Success);
                                            writer.Write(lobby.IPV4Port.Value);
                                        }
                                        else
                                        {
                                            writer.Write((sbyte)CallBackType.CreateLobby_Fail);
                                            writer.Write("This lobby does not support IPV4");
                                        }
                                        break;
                                    }
                                case RemoteAddress.SNRemoteAddress.AddressType.IPV6:
                                    {
                                        if (lobby.IPV6Port.HasValue)
                                        {
                                            writer.Write((sbyte)CallBackType.CreateLobby_Success);
                                            writer.Write(lobby.IPV6Port.Value);
                                        }
                                        else
                                        {
                                            writer.Write((sbyte)CallBackType.CreateLobby_Fail);
                                            writer.Write("This lobby does not support IPV6");
                                        }
                                        break;
                                    }
                                case RemoteAddress.SNRemoteAddress.AddressType.NamedPipeStream:
                                    {
                                        if (lobby.LocalPipeName != null)
                                        {
                                            writer.Write((sbyte)CallBackType.CreateLobby_Success);
                                            writer.Write(lobby.LocalPipeName);
                                        }
                                        else
                                        {
                                            writer.Write((sbyte)CallBackType.CreateLobby_Fail);
                                            writer.Write("This lobby does not support NamedPipeStream");
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            writer.Write((sbyte)CallBackType.CreateLobby_Fail);
                            writer.Write(failReason);
                        }
                        break;
                    }
                case MessageType.CloseLobby:
                    {
                        string uniqueLabel = reader.ReadString();
#if DEBUG
                        Console.WriteLine("Request Close Lobby With:" + uniqueLabel);
#endif
                        if (LobbyManager.Close(uniqueLabel, out string? failReason))
                        {
                            writer.Write((sbyte)CallBackType.CloseLobby_Success);
                        }
                        else
                        {
                            writer.Write((sbyte)CallBackType.CloseLobby_Fail);
                            writer.Write(failReason);
                        }
                        break;
                    }
                case MessageType.GetIP:
                    {
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
            }
            using BufferArray<byte> buffer = new(reply.ToArray());
            socket.AsyncSend(buffer);
#if DEBUG
            Console.WriteLine("Replyed");
#endif
        }
        public static async void Send_FindLobby(string? dialogueLabel, string uniqueLabel, SNSocket serverSocket, Action<int?, int?, string?, string?, BufferArray<byte>?> callback)
        {
            dialogueLabel ??= "FindLobby";
            using (MemoryStream stream1 = new())
            {
                using (BinaryWriter writer = new(stream1))
                {
                    writer.Write(dialogueLabel);
                    writer.Write((sbyte)MessageType.FindLobby);
                    writer.Write(uniqueLabel);
                    using BufferArray<byte> buffer = new(stream1.ToArray());
                    serverSocket.AsyncSend(buffer);
#if DEBUG
                    Console.WriteLine($"Send_FindLobby:{uniqueLabel}");
#endif
                }
            }
            var result = await serverSocket.AsyncRecive();
            if (result.Item1 == null)
            {
#if DEBUG
                Console.WriteLine($"Error Reply:{uniqueLabel}");
#endif
                return;
            }
            using (MemoryStream stream2 = new(result.Item1))
            {
                using (BinaryReader reader = new(stream2))
                {
                    if (reader.ReadString() != dialogueLabel)
                    {
                        callback(null, null, null, null, result.Item1);
                        return;
                    }
                    CallBackType callBackType = (CallBackType)reader.ReadSByte();
                    if (callBackType == CallBackType.FindLobby_Success)
                    {
#if DEBUG
                        Console.WriteLine($"FindLobby_Success:{uniqueLabel}");
#endif
                        switch (serverSocket.RemoteAddress.Type)
                        {
                            case RemoteAddress.SNRemoteAddress.AddressType.IPV4:
                                {
                                    callback(reader.ReadInt32(), null, null, null, null);
                                    break;
                                }
                            case RemoteAddress.SNRemoteAddress.AddressType.IPV6:
                                {
                                    callback(null, reader.ReadInt32(), null, null, null);
                                    break;
                                }
                            case RemoteAddress.SNRemoteAddress.AddressType.NamedPipeStream:
                                {
                                    callback(null, null, reader.ReadString(), null, null);
                                    break;
                                }
                        }
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine($"FindLobby_Fail:{uniqueLabel}");
#endif
                        callback(null, null, null, reader.ReadString(), null);
                    }
                }
            }
        }
        public static async void Send_CreateLobby(string? dialogueLabel, string uniqueLabel, string lobbytype, Dictionary<string, string>? paramters, SNSocket serverSocket, Action<int?, int?, string?, string?, BufferArray<byte>?> callback)
        {
            dialogueLabel ??= "CreateLobby";
            using (MemoryStream stream1 = new())
            {
                using (BinaryWriter writer = new(stream1))
                {
                    writer.Write(dialogueLabel);
                    writer.Write((sbyte)MessageType.CreateLobby);
                    writer.Write(uniqueLabel);
                    writer.Write(lobbytype);
                    string jsonParamters = JsonSerializer.Serialize(paramters ?? []);
                    writer.Write(jsonParamters);
                    using BufferArray<byte> buffer=new(stream1.ToArray());
                    serverSocket.AsyncSend(buffer);
#if DEBUG
                    Console.WriteLine($"Send_CreateLobby:{uniqueLabel}-{lobbytype}-{(paramters is null ? "HasParamters" : "NoParamters")}");
#endif
                }
            }
            var result = await serverSocket.AsyncRecive();
            if (result.Item1 == null)
            {
#if DEBUG
                Console.WriteLine($"ErrorReply:{uniqueLabel}");
#endif
                return;
            }
            using (MemoryStream stream2 = new(result.Item1))
            {
                using (BinaryReader reader = new(stream2))
                {
                    if (reader.ReadString() != dialogueLabel)
                    {
                        callback(null, null, null, null, result.Item1);
                        return;
                    }
                    CallBackType callBackType = (CallBackType)reader.ReadSByte();
                    if (callBackType == CallBackType.CreateLobby_Success)
                    {
#if DEBUG
                        Console.WriteLine($"CreateLobby_Fail:{uniqueLabel}");
#endif
                        switch (serverSocket.RemoteAddress.Type)
                        {
                            case RemoteAddress.SNRemoteAddress.AddressType.IPV4:
                                {
                                    callback(reader.ReadInt32(), null, null, null, null);
                                    break;
                                }
                            case RemoteAddress.SNRemoteAddress.AddressType.IPV6:
                                {
                                    callback(null, reader.ReadInt32(), null, null, null);
                                    break;
                                }
                            case RemoteAddress.SNRemoteAddress.AddressType.NamedPipeStream:
                                {
                                    callback(null, null, reader.ReadString(), null, null);
                                    break;
                                }
                        }
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine($"CreateLobby_Fail:{uniqueLabel}");
#endif
                        callback(null, null, null, reader.ReadString(), null);
                    }
                }
            }
        }
        public static async void Send_CloseLobby(string? dialogueLabel, string uniqueLabel, SNSocket serverSocket, Action<bool?, byte[]?> callback)
        {
            dialogueLabel ??= "CloseLobby";
            using (MemoryStream stream1 = new())
            {
                using (BinaryWriter writer = new(stream1))
                {
                    writer.Write(dialogueLabel);
                    writer.Write((sbyte)MessageType.CloseLobby);
                    writer.Write(uniqueLabel);
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
                    if (callBackType == CallBackType.CloseLobby_Success)
                    {
                        callback(true, null);
                    }
                    else
                    {
                        callback(false, null);
                    }
                }
            }
        }
        public static async void Send_GetIP(string? dialogueLabel, SNSocket serverSocket, Action<string?, byte[]?> callback)
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
    }
}