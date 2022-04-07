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

using ModFramework;
using Mono.Cecil;
using MonoMod;
using System.IO;
using System.Linq;

/// <summary>
/// @doc Patches RailSDK to load on netcore
/// </summary>
[Modification(ModType.PreMerge, "Patching RailSDK")]
[MonoMod.MonoModIgnore]
void PatchRailSDK(MonoModder modder)
{
	var sw = modder.Module.Resources.Single(r => r.Name.EndsWith("RailSDK.NET.dll", System.StringComparison.CurrentCultureIgnoreCase));
	var er = sw as EmbeddedResource;
	AssemblyDefinition asm;
	byte[] newbin;
	using (var bin = er.GetResourceStream())
	{
		asm = AssemblyDefinition.ReadAssembly(bin);

		asm.MainModule.Architecture = TargetArchitecture.I386;
		asm.MainModule.Attributes = ModuleAttributes.ILOnly;

		using (var ms = new MemoryStream())
		{
			asm.Write(ms);
			newbin = ms.ToArray();
		}
	}

	var newres = new EmbeddedResource(er.Name, er.Attributes, newbin);
	modder.Module.Resources.Remove(sw);
	modder.Module.Resources.Add(newres);
}