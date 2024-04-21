using Terraria.GameContent.Items;
using static OTAPI.Hooks.Item;

namespace Terraria
{
    internal class patch_Item : Item
    {
        private extern void orig_netDefaults(int type);

        public new void netDefaults(int type)
        {
            var args = new NetDefaultsEventArgs
            {
                Item = this,
                Type = type,
            };
            if (PreNetDefaults?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_netDefaults(args.Type);
                PostNetDefaults?.Invoke(args);
            }
        }

        private extern void orig_SetDefaults(int Type, bool noMatCheck = false, ItemVariant variant = null);

        public new void SetDefaults(int Type, bool noMatCheck = false, ItemVariant variant = null)
        {
            var args = new SetDefaultsEventArgs
            {
                Item = this,
                Type = Type,
                NoMaterialCheck = noMatCheck,
                Variant = variant,
            };
            if (PreSetDefaults?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SetDefaults(args.Type, args.NoMaterialCheck, args.Variant);
                PostSetDefaults?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class Item
        {
            public class NetDefaultsEventArgs
            {
                public Terraria.Item Item { get; init; }

                public int Type { get; set; }
            }

            public static PreHookHandler<NetDefaultsEventArgs>? PreNetDefaults;

            public static PostHookHandler<NetDefaultsEventArgs>? PostNetDefaults;

            public class SetDefaultsEventArgs
            {
                public Terraria.Item Item { get; init; }

                public int Type { get; set; }

                public bool NoMaterialCheck { get; set; }

                public ItemVariant Variant { get; set; }
            }

            public static PreHookHandler<SetDefaultsEventArgs>? PreSetDefaults;

            public static PostHookHandler<SetDefaultsEventArgs>? PostSetDefaults;
        }
    }
}