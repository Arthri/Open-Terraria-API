using Terraria;
using static OTAPI.Hooks.NPC;

namespace Terraria
{
    internal class patch_NPC : NPC
    {
        private extern void orig_SetDefaults(int Type, NPCSpawnParams spawnparams = default);

        public new void SetDefaults(int Type, NPCSpawnParams spawnparams = default)
        {
            var args = new SetDefaultsEventArgs
            {
                NPC = this,
                Type = Type,
                SpawnParameters = spawnparams,
            };
            if (PreSetDefaults?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SetDefaults(args.Type, args.SpawnParameters);
                PostSetDefaults?.Invoke(args);
            }
        }

        private extern void orig_SetDefaultsFromNetId(int Type, NPCSpawnParams spawnparams = default);

        public new void SetDefaultsFromNetId(int Type, NPCSpawnParams spawnparams = default)
        {
            var args = new SetDefaultsFromNetIdEventArgs
            {
                NPC = this,
                Type = Type,
                SpawnParameters = spawnparams,
            };
            if (PreSetDefaultsFromNetId?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SetDefaultsFromNetId(args.Type, args.SpawnParameters);
                PostSetDefaultsFromNetId?.Invoke(args);
            }
        }

        private extern double orig_StrikeNPC(int Damage, float knockBack, int hitDirection, bool crit = false, bool noEffect = false, bool fromNet = false, Entity entity = null);

        public new double StrikeNPC(int Damage, float knockBack, int hitDirection, bool crit = false, bool noEffect = false, bool fromNet = false, Entity entity = null)
        {
            var args = new StrikeNPCEventArgs
            {
                NPC = this,
                Attacker = entity,
                Damage = Damage,
                Knockback = knockBack,
                HitDirection = hitDirection,
                IsCritical = crit,
                HasNoEffect = noEffect,
                FromNetwork = fromNet,
            };

            if (PreStrikeNPC?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_StrikeNPC(args.Damage, args.Knockback, args.HitDirection, args.IsCritical, args.HasNoEffect, args.FromNetwork, args.Attacker);
                PostStrikeNPC?.Invoke(args);
            }

            return default;
        }

        private extern void orig_Transform(int newType);

        public new void Transform(int newType)
        {
            var args = new TransformEventArgs
            {
                NPC = this,
                NewType = newType,
            };
            if (PreTransform?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_Transform(args.NewType);
                PostTransform?.Invoke(args);
            }
        }

        private extern void orig_AI();

        public new void AI()
        {
            var args = new AIEventArgs
            {
                NPC = this,
            };
            if (PreAI?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_AI();
                PostAI?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class NPC
        {
            public class SetDefaultsEventArgs
            {
                public Terraria.NPC NPC { get; init; }

                public int Type { get; set; }

                public NPCSpawnParams SpawnParameters { get; set; }
            }

            public static PreHookHandler<SetDefaultsEventArgs>? PreSetDefaults;
            
            public static PostHookHandler<SetDefaultsEventArgs>? PostSetDefaults;

            public class SetDefaultsFromNetIdEventArgs
            {
                public Terraria.NPC NPC { get; init; }

                public int Type { get; set; }

                public NPCSpawnParams SpawnParameters { get; set; }
            }

            public static PreHookHandler<SetDefaultsFromNetIdEventArgs>? PreSetDefaultsFromNetId;

            public static PostHookHandler<SetDefaultsFromNetIdEventArgs>? PostSetDefaultsFromNetId;

            public class StrikeNPCEventArgs
            {
                public Terraria.NPC NPC { get; init;  }

                public Terraria.Entity Attacker { get; set; }

                public int Damage { get; set; }

                public float Knockback { get; set; }

                public int HitDirection { get; set; }

                public bool IsCritical { get; set; }

                public bool HasNoEffect { get; set; }

                public bool FromNetwork { get; set; }
            }

            public static PreHookHandler<StrikeNPCEventArgs>? PreStrikeNPC;

            public static PostHookHandler<StrikeNPCEventArgs>? PostStrikeNPC;

            public class TransformEventArgs
            {
                public Terraria.NPC NPC { get; init; }

                public int NewType { get; set; }
            }

            public static PreHookHandler<TransformEventArgs>? PreTransform;

            public static PostHookHandler<TransformEventArgs>? PostTransform;

            public class AIEventArgs
            {
                public Terraria.NPC NPC { get; init; }
            }

            public static PreHookHandler<AIEventArgs>? PreAI;

            public static PostHookHandler<AIEventArgs>? PostAI;
        }
    }
}
