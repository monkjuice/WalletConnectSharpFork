namespace Nethereum.WalletConnect.Models
{
    public class WCSessionData
    {
        public string peerId;
        public ClientMeta peerMeta;
        public bool approved;
        public int chainId;
        public string[] accounts;
    }
}