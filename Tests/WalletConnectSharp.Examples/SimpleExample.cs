using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Core;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Storage;
using static WalletConnectSharp.Examples.SimpleExample;

namespace WalletConnectSharp.Examples
{
    public class SimpleExample : IExample
    {

        static string FileLocation = AppDomain.CurrentDomain.BaseDirectory + "wc-session.json";

        public string Name
        {
            get { return "simple_example"; }
        }

        public async Task Execute(string[] args)
        {
            if(File.Exists(FileLocation))
                File.Delete(FileLocation);

            var client = await WalletConnectSignClient.Init(new SignClientOptions()
            {
                ProjectId = "39f3dc0a2c604ec9885799f9fc5feb7c",
                Metadata = new Metadata()
                {
                    Description = "An example dapp to showcase WalletConnectSharpv2",
                    Icons = new[] { "https://walletconnect.com/meta/favicon.ico" },
                    Name = "WalletConnectSharpv2 Dapp Example",
                    Url = "https://walletconnect.com"
                },
                // Uncomment to disable persistant storage
                Storage = new FileSystemStorage(FileLocation)
            });

            var dappConnectOptions = new ConnectOptions()
            {
                RequiredNamespaces = {
                {
                    "eip155", new ProposedNamespace()
                    {
                        Methods = new[]
                        {
                            "eth_sendTransaction",
                            "personal_sign",
                            "wallet_addEthereumChain",
                            "wallet_switchEthereumChain"
                        },
                        Chains = new[]
                        {
                            "eip155:1",
                        }
                    }
                }
            }
            };

            var connectData = await client.Connect(dappConnectOptions);

            Console.WriteLine(connectData.Uri);


            await connectData.Approval;


            await AddEthereumChain(client);


            while (true)
            {
                await Task.Delay(2000);
            }
        }

        public static async Task<string> AddEthereumChain(WalletConnectSignClient client)
        {
            var dappConnectOptions = new ConnectOptions()
            {
                RequiredNamespaces = {
                {
                    "eip155", new ProposedNamespace()
                    {
                        Methods = new[]
                        {
                            "eth_sendTransaction",
                            "personal_sign",
                            "wallet_addEthereumChain",
                            "wallet_switchEthereumChain"
                        },
                        Chains = new[]
                        {
                            "eip155:1"
                        }
                    }
                }
            }
            };

            var sessionData = client.Find(dappConnectOptions.RequiredNamespaces).First(x => x.Acknowledged ?? false);
            var currentAddress = client.AddressProvider.CurrentAddress();

            var Ack = await client.UpdateSession(sessionData.Topic, new Namespaces
        {
            { "eip1193",
                new Namespace(){
                Chains = new[]{ "eip155:100", "eip155:250" },
                Methods = new[]
                {
                    "eth_sendTransaction",
                    "personal_sign",
                    "wallet_addEthereumChain",
                    "wallet_switchEthereumChain"
                },
                Accounts = new[]{ "eip155:100:" + currentAddress.Address, currentAddress.ChainId + ":" +currentAddress.Address },
                }
            },
            { "eip155",
                new Namespace(){
                Chains = new[]{ "eip155:100", "eip155:250" },
                Methods = new[]
                {
                    "eth_sendTransaction",
                    "personal_sign",
                    "wallet_addEthereumChain",
                    "wallet_switchEthereumChain"
                },
                Accounts = new[]{ "eip155:100:" + currentAddress.Address, currentAddress.ChainId + ":" +currentAddress.Address },
                }
            }
        });

            var namespaces = sessionData.Namespaces;

            var ethereumChain = BuildChainBody(false); // Toggle to True to use Fantom. False to use Gnosis

            var caip2ChainId = $"eip155:{ethereumChain.chainIdDecimal}";

            if (!dappConnectOptions.RequiredNamespaces.TryGetValue("eip155", out var @namespace)
                    || !@namespace.Chains.Contains(caip2ChainId))
            {
                var request = new WalletAddEthereumChain(ethereumChain);

                var response = await client.Request<WalletAddEthereumChain, string>(sessionData.Topic, request);

                return response;
            }

            var data = new WalletSwitchEthereumChain(ethereumChain.chainIdHex);
            return await client.Request<WalletSwitchEthereumChain, string>(sessionData.Topic, data);

        }

        public static EthereumChain BuildChainBody(bool useFantomData)
        {
            if (useFantomData)
            {
                var nativeCurrency = new Currency("Fantom", "FTM", 18);

                return new EthereumChain("250", "Fantom", nativeCurrency, new[] { "https://1rpc.io/ftm" });
            }
            else
            {
                var nativeCurrency = new Currency("Gnosis", "XDAI", 18);

                return new EthereumChain("100", "Gnosis", nativeCurrency, new[] { "https://rpc.gnosis.gateway.fm/" });
            }
        }

        [RpcMethod("wallet_addEthereumChain")]
        [RpcRequestOptions(Clock.ONE_MINUTE, 99990)]
        public class WalletAddEthereumChain : List<object>
        {
            public WalletAddEthereumChain(EthereumChain chain) : base(new[] { chain })
            {
            }

            public WalletAddEthereumChain()
            {
            }
        }

        [RpcMethod("wallet_switchEthereumChain")]
        [RpcRequestOptions(Clock.ONE_MINUTE, 1234)]
        public class WalletSwitchEthereumChain : List<object>
        {
            public WalletSwitchEthereumChain(string chainId) : base(new[] { new { chainId } })
            {
            }

            public WalletSwitchEthereumChain()
            {
            }
        }

        [Serializable]
        public readonly struct Currency
        {
            public readonly string name;
            public readonly string symbol;
            public readonly int decimals;

            public Currency(string name, string symbol, int decimals)
            {
                this.name = name;
                this.symbol = symbol;
                this.decimals = decimals;
            }
        }

        [Serializable]
        public class EthereumChain
        {
            [JsonProperty("chainId")]
            public string chainIdHex;

            [JsonProperty("chainName")]
            public string name;

            [JsonProperty("nativeCurrency")]
            public Currency nativeCurrency;

            [JsonProperty("rpcUrls")]
            public string[] rpcUrls;

            [JsonProperty("blockExplorerUrls", NullValueHandling = NullValueHandling.Ignore)]
            public string[] blockExplorerUrls;

            [JsonIgnore]
            public string chainIdDecimal;

            public EthereumChain()
            {
            }

            public EthereumChain(string chainId, string name, Currency nativeCurrency, string[] rpcUrls, string[] blockExplorerUrls = null)
            {
                chainIdDecimal = chainId;
                chainIdHex = ToHex(chainId);
                this.name = name;
                this.nativeCurrency = nativeCurrency;
                this.rpcUrls = rpcUrls;
                this.blockExplorerUrls = blockExplorerUrls;
            }

            public string ToHex(string str)
            {
                return $"0x{int.Parse(str):X}";
            }
        }

    }
}
