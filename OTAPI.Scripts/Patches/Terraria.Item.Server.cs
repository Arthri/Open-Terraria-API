#if !tModLoader_V1_4
using Terraria.GameContent.Items;
#endif
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

        private extern void orig_SetDefaults(int Type, bool noMatCheck = false
        #if !tModLoader_V1_4
        , ItemVariant variant = null
        #endif
        );

        public new void SetDefaults(int Type, bool noMatCheck = false
        #if !tModLoader_V1_4
        , ItemVariant variant = null
        #endif
        )
        {
            var args = new SetDefaultsEventArgs
            {
                Item = this,
                Type = Type,
                NoMaterialCheck = noMatCheck,
                #if !tModLoader_V1_4
                Variant = variant,
                #endif
            };
            if (PreSetDefaults?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SetDefaults(args.Type, args.NoMaterialCheck
                #if !tModLoader_V1_4
                , args.Variant
                #endif
                );
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

                #if !tModLoader_V1_4
                public ItemVariant Variant { get; set; }
                #endif
            }

            public static PreHookHandler<SetDefaultsEventArgs>? PreSetDefaults;

            public static PostHookHandler<SetDefaultsEventArgs>? PostSetDefaults;
        }
    }
}