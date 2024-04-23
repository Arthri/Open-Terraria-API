/*
Copyright (C) 2020 DeathCradle, James Puleo

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

using ModFramework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.Initializers;

[MonoMod.MonoModIgnore]
file static class B384680188CA4A9083017801C2A34C95
{
    /// <summary>
    /// @doc Removes the netMode check on AchievementInitializer, so that it can be ran on servers
    /// </summary>
    /// <remarks>
    /// https://github.com/Pryaxis/TShock/commit/794bff5ef772b0c951c29349922360e9d41c11d7
    /// </remarks>
    [Modification(ModType.PreMerge, "Allowing AchievementInitializer to run on servers")]
    [MonoMod.MonoModIgnore]
    static void PatchAchievementInitializer(ModFwModder modder)
    {
        var m_Load = modder.GetMethodDefinition(() => AchievementInitializer.Load());

        var cursor = new ILCursor(new ILContext(m_Load));
        Instruction endIf = null!;
        while (cursor.TryGotoNext(
            i => i.MatchCall<Main>($"get_{nameof(Main.netMode)}") || i.MatchLdsfld<Main>(nameof(Main.netMode)),
            i => i.MatchLdcI4(2),
            i => i.OpCode == OpCodes.Bne_Un || i.OpCode == OpCodes.Bne_Un_S && ((endIf = (i.Operand as Instruction)!) != null)
        ))
        {
            cursor.RemoveWhile(i => i.Offset < endIf.Offset, false);
        }
    }
}
