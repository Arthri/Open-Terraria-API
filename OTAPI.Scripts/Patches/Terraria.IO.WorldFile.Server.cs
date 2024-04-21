using static OTAPI.Hooks.IO.WorldFile;

namespace Terraria.IO
{
    internal partial class patch_WorldFile : WorldFile
    {
        private extern static void orig_SaveWorld(bool useCloudSaving, bool resetTime = false);
        public new static void SaveWorld(bool useCloudSaving, bool resetTime = false)
        {
            var args = new SaveWorldEventArgs
            {
                UseCloudSaving = useCloudSaving,
                ResetTime = resetTime,
            };
            if (PreSaveWorld?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SaveWorld(args.UseCloudSaving, args.ResetTime);
                PostSaveWorld?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    public static partial class Hooks
    {
        public static partial class IO
        {
            public static partial class WorldFile
            {
                public class SaveWorldEventArgs
                {
                    public bool UseCloudSaving { get; set; }

                    public bool ResetTime { get; set; }
                }

                public static PreHookHandler<SaveWorldEventArgs>? PreSaveWorld;

                public static PostHookHandler<SaveWorldEventArgs>? PostSaveWorld;
            }
        }
    }
}
