using static OTAPI.Hooks.NetMessage;

namespace Terraria
{
    internal partial class patch_NetMessage : NetMessage
    {
        private extern static void orig_greetPlayer(int plr);

        public new static void greetPlayer(int plr)
        {
            var args = new GreetPlayerEventArgs
            {
                Player = plr,
            };
            if (PreGreetPlayer?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_greetPlayer(args.Player);
                PostGreetPlayer?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class NetMessage
        {
            public class GreetPlayerEventArgs
            {
                public int Player { get; set; }
            }

            public static PreHookHandler<GreetPlayerEventArgs>? PreGreetPlayer;

            public static PostHookHandler<GreetPlayerEventArgs>? PostGreetPlayer;
        }
    }
}
