using Microsoft.Xna.Framework;
using Terraria.Localization;
using static OTAPI.Hooks.Chat.ChatHelper;

namespace Terraria.Chat
{
    internal partial class patch_ChatHelper
    {
        private static extern void orig_BroadcastChatMessage(NetworkText text, Color color, int excludedPlayer = -1);

        public static void BroadcastChatMessage(NetworkText text, Color color, int excludedPlayer = -1)
        {
            var args = new BroadcastChatMessageEventArgs
            {
                Text = text,
                Color = color,
                ExcludedPlayer = excludedPlayer,
            };
            if (PreBroadcastChatMessage?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_BroadcastChatMessage(args.Text, args.Color, args.ExcludedPlayer);
                PostBroadcastChatMessage?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class Chat
        {
            public static partial class ChatHelper
            {
                public class BroadcastChatMessageEventArgs
                {
                    public NetworkText Text { get; set; }

                    public Color Color { get; set; }

                    public int ExcludedPlayer { get; set; }
                }

                public static PreHookHandler<BroadcastChatMessageEventArgs>? PreBroadcastChatMessage;

                public static PostHookHandler<BroadcastChatMessageEventArgs>? PostBroadcastChatMessage;
            }
        }
    }
}
