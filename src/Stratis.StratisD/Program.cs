using System;
using System.Threading.Tasks;
using System.Linq;
using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Utilities;

namespace Stratis.StratisD
{
    public class Program
    {
        private const string DefaultStratisUri = "http://localhost:37221";

        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            try
            {
                string agent = "BiblePayStratis";
                Network network = false ? Network.StratisTest : Network.BiblepayMain;
                // BIBLEPAY - TODO - ADD 5 SEED NODES SO USER CAN SYNC THE VERY FIRST TIME
                // FOR NOW, WE ARE ADDING ONE HARDCODED NODE (dns1.biblepay.org)
                args = args.Append("-addnode=144.202.45.226").ToArray();
              
                var nodeSettings = new NodeSettings(network, protocolVersion:ProtocolVersion.ALT_PROTOCOL_VERSION, agent, args:args);
                //nodeSettings.ApiUri = new Uri(string.IsNullOrEmpty(apiUri) ? DefaultStratisUri : apiUri);


                IFullNode node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePowConsensus()
                    .UseMempool()
                    .UseWallet()
                    .UseApi()
                    .AddRPC()
                    .Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }
    }
}
