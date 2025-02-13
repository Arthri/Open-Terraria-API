﻿/*
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

using System.Collections.Generic;
using static OTAPI.Hooks.RemoteClient;

/// <summary>
/// @doc Adds a Terraria.RemoteClient.Data variable for data storage.
/// </summary>
namespace Terraria
{
    partial class patch_RemoteClient : Terraria.RemoteClient
    {
        public Dictionary<string, object> Data { get; set; }

        [MonoMod.MonoModConstructor]
        patch_RemoteClient()
        {
            Data = new Dictionary<string, object>();
        }

        private extern void orig_Reset();
        public void Reset()
        {
            var args = new ResetEventArgs
            {
                RemoteClient = this,
            };
            if (PreReset?.Invoke(args) != OTAPI.HookResult.Cancel)
            {
                orig_Reset();
                Data.Clear();
                PostReset?.Invoke(args);
            }
        }
    }
}

namespace OTAPI
{
    partial class Hooks
    {
        public static partial class RemoteClient
        {
            public class ResetEventArgs
            {
                public Terraria.RemoteClient RemoteClient { get; init; }
            }

            public static PreHookHandler<ResetEventArgs>? PreReset;

            public static PostHookHandler<ResetEventArgs>? PostReset;
        }
    }
}
