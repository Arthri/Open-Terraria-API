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
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using System;
using static OTAPI.Hooks.Projectile;

namespace Terraria
{
    internal class patch_Projectile : Projectile
    {
        private extern void orig_AI();

        public new void AI()
        {
            var args = new AIEventArgs()
            {
                Projectile = this,
            };
            if (PreAI?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_AI();
                PostAI?.Invoke(args);
            }
        }

        private extern void orig_SetDefaults(int Type);

        public new void SetDefaults(int Type)
        {
            var args = new SetDefaultsEventArgs()
            {
                Projectile = this,
            };
            if (PreSetDefaults?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_SetDefaults(args.Type);
                PostSetDefaults?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    public partial class Hooks
    {
        public static partial class Projectile
        {
            public class AIEventArgs : EventArgs
            {
                public Terraria.Projectile Projectile { get; init; } = null!;
            }

            public static PreHookHandler<AIEventArgs>? PreAI;

            public static PostHookHandler<AIEventArgs>? PostAI;

            public class SetDefaultsEventArgs : EventArgs
            {
                public Terraria.Projectile Projectile { get; init; } = null!;

                public int Type { get; set; }
            }

            public static PreHookHandler<SetDefaultsEventArgs>? PreSetDefaults;

            public static PostHookHandler<SetDefaultsEventArgs>? PostSetDefaults;
        }
    }
}
