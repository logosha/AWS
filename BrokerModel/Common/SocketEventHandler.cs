namespace Shared.Common
{
    public struct SocketEventHandlerArgs
    {
        public byte[] msg;
        public string chnlName;
        public StreamMessageType type;
    }
}
