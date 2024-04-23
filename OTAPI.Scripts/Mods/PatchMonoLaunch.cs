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
#pragma warning disable CS8321 // Local function is declared but never used

#if !tModLoader_V1_4
#warning MonoMod patch only needed for TML
#else
using System;
using System.Linq;
using ModFramework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;

[MonoMod.MonoModIgnore]
class B384680188CA4A9083017801C2A34C95
{
    /// <summary>
    /// @doc A mod that changes the base path of native assembly loading
    /// </summary>
    [Modification(ModType.PostPatch, "Updating native assembly loading")]
    [MonoMod.MonoModIgnore]
    static void PathMonoLaunch(MonoModder modder)
    {
        var mth = modder.Module.GetType("MonoLaunch").Methods.Single(m => m.Name == "ResolveNativeLibrary");
        var rnl = modder.GetILCursor(mth);
        var gbd = modder.GetMethodDefinition(() => patch_MonoLaunch.GetBaseDirectory(), followRedirect: true);

        rnl.GotoNext(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference mref && mref.Name == "get_CurrentDirectory");

        rnl.Next.Operand = gbd;
    }
}

public static partial class patch_MonoLaunch
{
    public static string GetBaseDirectory()
        => Environment.CurrentDirectory; // dont change the path by default. allow consumers to redirect this call instead.
}
#endif