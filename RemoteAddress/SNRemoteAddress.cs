using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet.RemoteAddress
{
    public abstract class SNRemoteAddress
    {
        public enum AddressType
        {
            IPV4,
            IPV6,
            NamedPipeStream,
            Steam
        }
        public AddressType Type { get; protected set; }
    }
}
