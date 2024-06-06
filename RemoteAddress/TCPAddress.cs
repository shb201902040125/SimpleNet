using System.Net.Sockets;
using System.Net;

namespace SimpleNet.RemoteAddress
{
    public class TCPAddress : SNRemoteAddress
    {
        internal readonly IPAddress _address;
        internal readonly int _port;
        public IPAddress IPAddress => new(_address.GetAddressBytes());
        public int Port => _port;
        public TCPAddress(IPAddress address, int port)
        {
            Type = address.AddressFamily == AddressFamily.InterNetwork
                ? AddressType.IPV4
                : address.AddressFamily == AddressFamily.InterNetworkV6
                    ? AddressType.IPV6
                    : throw new ArgumentException($"Must be IPV4/IPV6 address:{address}");
            _address = address;
            _port = port;
        }
        public override string ToString()
        {
            return _address.ToString() + ":" + _port;
        }
    }
}
