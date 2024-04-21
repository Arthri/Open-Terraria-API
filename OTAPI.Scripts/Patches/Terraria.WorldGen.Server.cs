#if !tModLoader_V1_4
using Terraria;
using static OTAPI.Hooks.WorldGen;

namespace Terraria
{
    internal class patch_WorldGen : WorldGen
    {
        private extern static void orig_StartHardmode();

        public new static void StartHardmode()
        {
            var args = new StartHardmodeEventArgs
            {
            };
            if (PreStartHardmode?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_StartHardmode();
                PostStartHardmode?.Invoke(args);
            }
        }

        private extern static void orig_SpreadGrass(int i, int j, int dirt = 0, int grass = 2, bool repeat = true, TileColorCache color = default(TileColorCache));

        public new static void SpreadGrass(int i, int j, int dirt = 0, int grass = 2, bool repeat = true, TileColorCache color = default(TileColorCache))
        {
            var args = new SpreadGrassEventArgs
            {
                X = i,
                Y = j,
                Dirt = dirt,
                Grass = grass,
                Repeat = repeat,
                Color = color,
            };
            if (PreSpreadGrass?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SpreadGrass(args.X, args.Y, args.Dirt, args.Grass, args.Repeat, args.Color);
                PostSpreadGrass?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class WorldGen
        {
            public class StartHardmodeEventArgs
            {
            }

            public static PreHookHandler<StartHardmodeEventArgs>? PreStartHardmode;

            public static PostHookHandler<StartHardmodeEventArgs>? PostStartHardmode;

            public class SpreadGrassEventArgs
            {
                public int X { get; set; }

                public int Y { get; set; }

                public int Dirt { get; set; }

                public int Grass { get; set; }

                public bool Repeat { get; set; }

                public TileColorCache Color { get; set; }
            }

            public static PreHookHandler<SpreadGrassEventArgs>? PreSpreadGrass;

            public static PostHookHandler<SpreadGrassEventArgs>? PostSpreadGrass;
        }
    }
}
#endif
