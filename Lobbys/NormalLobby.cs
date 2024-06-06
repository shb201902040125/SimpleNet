using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet.Lobbys
{
    internal class NormalLobby : Lobby
    {
        internal static bool TryCreate(Dictionary<string, string> paramters, [NotNullWhen(true)] out NormalLobby? normalLobby, [NotNullWhen(false)] out string? failReason)
        {
            bool useIPV6 = paramters.ContainsKey("ipv6");
            bool useLocal = paramters.TryGetValue("local", out string? localName);
            normalLobby = new NormalLobby();
            normalLobby.StartListen(0, useIPV6 ? 0 : null, useLocal ? localName : null);
            failReason = null;
            return true;
        }
    }
}
