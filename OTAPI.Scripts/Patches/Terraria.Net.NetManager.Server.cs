#if tModLoader_V1_4
using Terraria.Net;
using Terraria.Net.Sockets;
using static OTAPI.Hooks.Net.NetManager;

namespace Terraria.Net
{
    internal class patch_NetManager : NetManager
    {
        private extern void orig_SendData(ISocket socket, NetPacket packet);

        private new void SendData(ISocket socket, NetPacket packet)
        {
            var args = new SendDataEventArgs
            {
                Instance = this,
                Socket = socket,
                Packet = packet,
            };
            if (PreSendData?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SendData(args.Socket, args.Packet);
                PostSendData?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class Net
        {
            public static partial class NetManager
            {
                public class SendDataEventArgs
                {
                    public Terraria.Net.NetManager Instance { get; init; }

                    public ISocket Socket { get; set; }

                    public NetPacket Packet { get; set; }
                }

                public static PreHookHandler<SendDataEventArgs>? PreSendData;

                public static PostHookHandler<SendDataEventArgs>? PostSendData;
            }
        }
    }
}
#endif
