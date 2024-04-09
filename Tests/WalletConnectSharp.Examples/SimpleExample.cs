using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Core;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Storage;

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

            var addEthChainBody = BuildChainBody(false);

            var caip2ChainId = addEthChainBody.Caip2ChainId;

            if (!dappConnectOptions.RequiredNamespaces.TryGetValue("eip155", out var @namespace)
                    || !@namespace.Chains.Contains(caip2ChainId))
            {
                var request = new WalletAddEthereumChain(addEthChainBody);

                var response = await client.Request<WalletAddEthereumChain, string>(sessionData.Topic, request);

                return response;
            }

            var data = new WalletSwitchEthereumChain(addEthChainBody.ChainID);
            return await client.Request<WalletSwitchEthereumChain, string>(sessionData.Topic, data);

        }

        public static EthereumChain BuildChainBody(bool useFantomData)
        {
            if (useFantomData)
            {
                var nativeCurrency = new Currency()
                {
                    Name = "Fantom",
                    Symbol = "FTM",
                    Decimals = 18
                };

                return new EthereumChain()
                {
                    Caip2ChainId = "eip155:" + 250,
                    ChainID = "0x" + 250.ToString("X"),
                    ChainName = "Fantom Opera",
                    RpcUrls = new List<string> { "https://1rpc.io/ftm" },
                    NativeCurrency = nativeCurrency
                };
            }
            else
            {
                // Build objects with Gnosis data
                // Example:
                var nativeCurrency = new Currency()
                {
                    Name = "Gnosis",
                    Symbol = "GNO",
                    Decimals = 18
                };

                return new EthereumChain()
                {
                    Caip2ChainId = "eip155:" + 100,
                    ChainID = "0x" + 100.ToString("X"),
                    ChainName = "Gnosis Chain",
                    RpcUrls = new List<string> { "https://gnosis.drpc.org" },
                    NativeCurrency = nativeCurrency
                };
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

        public class Currency
        {
            [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }

            [JsonProperty("symbol")]
            public string Symbol { get; set; }

            [JsonProperty("decimals")]
            public uint Decimals { get; set; }
        };

        [RpcMethod("wallet_addEthereumChain"), RpcRequestOptions(Clock.ONE_MINUTE, 99999)]
        public class EthereumChain
        {
            public string Caip2ChainId { get; set; }

            [JsonProperty("chainId")]
            public string ChainID { get; set; }

            [JsonProperty("chainName")]
            public string ChainName { get; set; }

            [JsonProperty("rpcUrls")]
            public List<string> RpcUrls { get; set; }

            [JsonProperty("iconUrls", NullValueHandling = NullValueHandling.Ignore)]
            public string IconUrls { get; set; }

            [JsonProperty("nativeCurrency")]
            public Currency NativeCurrency { get; set; }

            [JsonProperty("blockExplorerUrls", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> BlockExplorerUrls { get; set; }
        };

    }
}
