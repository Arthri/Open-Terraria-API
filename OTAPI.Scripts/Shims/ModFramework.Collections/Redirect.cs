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
using ModFramework;

namespace ModFramework.Collections
{
    [MonoMod.MonoModIgnore]
    static partial class Mods
    {
        /// <summary>
        /// @doc Redirects ModFramework.Collections to OTAPI, because it should be merged into it by then
        /// </summary>
        [Modification(ModType.PrePatch, "Relinking ModFramework.Collections")]
        public static void RedirectAssembly(ModFwModder modder)
        {
            modder.RelinkAssembly("ModFramework.Collections");
        }
    }
}
