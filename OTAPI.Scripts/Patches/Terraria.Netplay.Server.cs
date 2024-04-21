using Terraria.Net.Sockets;
using static OTAPI.Hooks.Netplay;

namespace Terraria
{
    internal partial class patch_Netplay : Netplay
    {
        private static extern void orig_StartServer();

        public new static void StartServer()
        {
            var args = new StartServerEventArgs
            {
            };
            if (PreStartServer?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_StartServer();
                PostStartServer?.Invoke(args);
            }
        }

        private static extern void orig_OnConnectionAccepted(ISocket client);

        public new static void OnConnectionAccepted(ISocket client)
        {
            var args = new ConnectionAcceptedEventArgs
            {
                Socket = client,
            };
            if (PreConnectionAccepted?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_OnConnectionAccepted(args.Socket);
                PostConnectionAccepted?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class Netplay
        {
            public class StartServerEventArgs
            {
            }

            public static PreHookHandler<StartServerEventArgs>? PreStartServer;

            public static PostHookHandler<StartServerEventArgs>? PostStartServer;

            public class ConnectionAcceptedEventArgs
            {
                public ISocket Socket { get; set; }
            }

            public static PreHookHandler<ConnectionAcceptedEventArgs>? PreConnectionAccepted;

            public static PostHookHandler<ConnectionAcceptedEventArgs>? PostConnectionAccepted;
        }
    }
}
