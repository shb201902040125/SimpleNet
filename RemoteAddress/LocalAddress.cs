namespace SimpleNet.RemoteAddress
{
    public class LocalAddress : SNRemoteAddress
    {
        internal readonly string _label;
        public string Label => _label;
        public LocalAddress(string label)
        {
            _label = label;
            Type = AddressType.NamedPipeStream;
        }
        public override string ToString()
        {
            return _label;
        }
    }
}
