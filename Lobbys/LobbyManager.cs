using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet.Lobbys
{
    internal class LobbyManager
    {
        static Dictionary<string, Lobby> _lobbys = [];
        static object _lock = new();
        internal static bool Close(string uniqueLable, [NotNullWhen(false)] out string? failReason)
        {
            lock (_lock)
            {
                failReason = null;
                if (_lobbys.TryGetValue(uniqueLable, out var lobby))
                {
                    bool flag = lobby.Close();
                    if (!flag)
                    {
                        failReason = "Other connections exist in the lobby and cannot be closed";
                    }
                    else
                    {
                        _lobbys.Remove(uniqueLable);
                    }
                    return flag;
                }
                failReason = "Target lobby does not exist";
                return false;
            }
        }
        internal static bool Create(string uniqueLbale, string lobbyType, Dictionary<string, string>? paramters, [NotNullWhen(true)] out Lobby? lobby, [NotNullWhen(false)] out string? failReason)
        {
            lock (_lock)
            {
                lobby = null;
                failReason = null;
                switch (lobbyType)
                {
                    default:
                        {
                            failReason = "Invalid lobby type";
                            return false;
                        }
                }
            }
        }
        internal static bool FindLobby(string uniqueLabel, [NotNullWhen(true)] out Lobby? lobby, [NotNullWhen(false)] out string? failReason)
        {
            lock (_lock)
            {
                failReason = null;
                if (_lobbys.TryGetValue(uniqueLabel, out lobby))
                {
                    return true;
                }
                failReason = "Target lobby does not exist";
                return false;
            }
        }
    }
}
