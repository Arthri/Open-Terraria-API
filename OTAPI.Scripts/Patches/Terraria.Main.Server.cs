/*
Copyright (C) 2020 DeathCradle

This file is part of Open Terraria API v3 (OTAPI)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using static OTAPI.Hooks.Main;

/// <summary>
/// @doc Enable Terraria.Main.NeverSleep calls based off RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
/// </summary>
/// <summary>
/// @doc Fixes auto save when Terraria.Main.autoSave is false
/// </summary>
namespace Terraria
{
    class patch_Main : Terraria.Main
    {
        public extern void orig_NeverSleep();
        public void NeverSleep()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) orig_NeverSleep();
        }

        public extern void orig_YouCanSleepNow();
        public void YouCanSleepNow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) orig_YouCanSleepNow();
        }

        // this was implemented in v2. by looking at the vanilla code it will auto
        // save regardless of this flag, so we enforce the check here in case
        // its disabled for a reason.
        public static extern void orig_DoUpdate_AutoSave();
        public static void DoUpdate_AutoSave()
        {
            if(autoSave)
            {
                orig_DoUpdate_AutoSave();
            }
        }

        private extern void orig_Update(GameTime gameTime);

        protected void Update(GameTime gameTime)
        {
            var args = new UpdateEventArgs
            {
                Instance = this,
                GameTime = gameTime,
            };
            if (PreUpdate?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_Update(args.GameTime);
                PostUpdate?.Invoke(args);
            }
        }

        private extern void orig_Initialize();

        protected new void Initialize()
        {
            var args = new InitializeEventArgs
            {
                Instance = this,
            };
            if (PreInitialize?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_Initialize();
                PostInitialize?.Invoke(args);
            }
        }

        private static extern void orig_startDedInput();

        public static void startDedInput()
        {
            var args = new StartDedicatedInputEventArgs
            {
            };
            if (PreStartDedInput?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_startDedInput();
                PostStartDedInput?.Invoke(args);
            }
        }

        private extern static void orig_checkXMas();

        public static void checkXMas()
        {
            var args = new CheckChristmasEventArgs
            {
            };
            if (PreCheckChristmas?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_checkXMas();
                PostCheckChristmas?.Invoke(args);
            }
        }

        private extern static void orig_checkHalloween();

        public static void checkHalloween()
        {
            var args = new CheckHalloweenEventArgs
            {
            };
            if (PreCheckHalloween?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_checkHalloween();
                PostCheckHalloween?.Invoke(args);
            }
        }

        private extern void orig_DedServ();

        public new void DedServ()
        {
            var args = new DedicatedServerEventArgs
            {
                Instance = this,
            };
            if (PreDedicatedServer?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_DedServ();
                PostDedicatedServer?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class Main
        {
            public class UpdateEventArgs
            {
                public Terraria.Main Instance { get; init; }

                public GameTime GameTime { get; set; }
            }

            public static PreHookHandler<UpdateEventArgs>? PreUpdate;

            public static PostHookHandler<UpdateEventArgs>? PostUpdate;

            public class InitializeEventArgs
            {
                public Terraria.Main Instance { get; init; }
            }

            public static PreHookHandler<InitializeEventArgs>? PreInitialize;

            public static PostHookHandler<InitializeEventArgs>? PostInitialize;

            public class StartDedicatedInputEventArgs
            {
            }

            public static PreHookHandler<StartDedicatedInputEventArgs>? PreStartDedInput;

            public static PostHookHandler<StartDedicatedInputEventArgs>? PostStartDedInput;

            public class CheckChristmasEventArgs
            {
            }

            public static PreHookHandler<CheckChristmasEventArgs>? PreCheckChristmas;

            public static PostHookHandler<CheckChristmasEventArgs>? PostCheckChristmas;

            public class CheckHalloweenEventArgs
            {
            }

            public static PreHookHandler<CheckHalloweenEventArgs>? PreCheckHalloween;

            public static PostHookHandler<CheckHalloweenEventArgs>? PostCheckHalloween;

            public class DedicatedServerEventArgs
            {
                public Terraria.Main Instance { get; init; }
            }

            public static PreHookHandler<DedicatedServerEventArgs>? PreDedicatedServer;

            public static PostHookHandler<DedicatedServerEventArgs>? PostDedicatedServer;
        }
    }
}
