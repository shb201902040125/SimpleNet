using SimpleNet.NetSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet.Lobbys
{
    public class NormalLobby : Lobby
    {
        enum MessageType:sbyte
        {
            GetIP
        }
        enum CallBackType:sbyte
        {
            GetIP_Success,
            GetIP_Fail,
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
            base.HandleMemory(memory, socket);
        }
        public static async void Send_GetIP(SNSocket serverSocket, Action<string?> callback)
        {
            using (MemoryStream stream1 = new())
            {
                using (BinaryWriter writer = new(stream1))
                {
                    writer.Write((sbyte)MessageType.GetIP);
                    serverSocket.AsyncSend(stream1.ToArray());
                }
            }
            Tuple<byte[]?, object?> result = await serverSocket.AsyncRecive();
            if (result.Item1 == null)
            {
                return;
            }
            using (MemoryStream stream2 = new(result.Item1))
            {
                using (BinaryReader reader = new(stream2))
                {
                    CallBackType callBackType = (CallBackType)reader.ReadSByte();
                    if (callBackType == CallBackType.GetIP_Success)
                    {
                        callback(reader.ReadString());
                    }
                    else
                    {
                        callback(null);
                    }
                }
            }
        }
    }
}
