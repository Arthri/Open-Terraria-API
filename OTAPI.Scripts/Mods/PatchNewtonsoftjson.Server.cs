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

#if tModLoader_V1_4
#warning Newtonsoft.Json upgrade not needed on TML1.4
#else
using ModFramework;
using Mono.Cecil;
using MonoMod;
using System.Collections.Generic;
using System.Linq;

[MonoMod.MonoModIgnore]
class B384680188CA4A9083017801C2A34C95
{
    /// <summary>
    /// @doc A mod to update Newtonsoft.Json assembly references.
    /// </summary>
    [Modification(ModType.PostPatch, "Upgrading Newtonsoft.Json")]
    [MonoMod.MonoModIgnore]
    void PatchNewtonsoftJson(MonoModder modder)
    {
        var desired = typeof(Newtonsoft.Json.JsonConvert).Assembly.GetName().Version;

        //Update the references to match what is installed
        foreach (var reference in modder.Module.AssemblyReferences)
        {
            if (reference.Name == "Newtonsoft.Json")
            {
                reference.Version = desired;
                break;
            }
        }

        //Remove the embedded Newtonsoft resource
        modder.Module.Resources.Remove(
            modder.Module.Resources.Single(x => x.Name.EndsWith("Newtonsoft.Json.dll"))
        );
    }

    [Modification(ModType.PreMerge, "Deduplicating Newtonsoft.Json references")]
    void DeduplicateNewtonsoftJson(MonoModder modder)
    {
        AssemblyNameReference firstFound = null;
        var referencesToRemove = new List<AssemblyNameReference>();
        foreach (var reference in modder.Module.AssemblyReferences)
        {
            if (firstFound is null)
            {
                firstFound = reference;
            }
            else
            {
                referencesToRemove.Add(reference);
            }
        }

        foreach (var reference in referencesToRemove)
        {
            modder.Module.AssemblyReferences.Remove(reference);
        }
    }
}
#endif
