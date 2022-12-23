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
#pragma warning disable CS8321 // Local function is declared but never used

using ModFramework;
using ModFramework.Relinker;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Terraria;

/// <summary>
/// @doc Transforms the thread-unsafe fields of Terraria.Main into thread-safe properties
/// </summary>
[Modification(ModType.PreMerge, "Mapping Thread-Unsafe Fields to Properties")]
[MonoMod.MonoModIgnore]
void ThreadSafeProperties(ModFwModder modder)
{
    var f_netMode = modder.GetFieldDefinition(() => Main.netMode);
    RemapAsThreadSafeProperty(f_netMode, modder);
    f_netMode.Constant = 2;

    var f_myPlayer = modder.GetFieldDefinition(() => Main.myPlayer);
    f_myPlayer.Constant = 255;
    RemapAsThreadSafeProperty(f_myPlayer, modder);
}

static PropertyDefinition RemapAsThreadSafeProperty(FieldDefinition field, ModFwModder modder)
{
    var getter = GenerateThreadSafeGetter(field);
    field.DeclaringType.Methods.Add(getter);

    var setter = GenerateThreadSafeSetter(field);
    field.DeclaringType.Methods.Add(setter);

    var property = new PropertyDefinition(field.Name, PropertyAttributes.None, field.FieldType)
    {
        HasThis = !field.IsStatic,
        GetMethod = getter,
        SetMethod = setter,
    };

    var threadStaticAttribute = field.DeclaringType.Module.GetCoreLibMethod("System", "ThreadStaticAttribute", ".ctor");
    threadStaticAttribute.HasThis = true;
    field.CustomAttributes.Add(new CustomAttribute(threadStaticAttribute));
    field.CustomAttributes.Add(CreateCompilerGeneratedAttribute(field));
    field.DeclaringType.Properties.Add(property);

    modder.AddTask<FieldToPropertyRelinker>(field, property);

    field.Name = $"<{field.Name}>k__BackingField";
    field.IsPrivate = true;

    return property;
}

static CustomAttribute CreateCompilerGeneratedAttribute(IMemberDefinition member)
{
    var attr = member.DeclaringType.Module.GetCoreLibMethod("System.Runtime.CompilerServices", "CompilerGeneratedAttribute", ".ctor");
    attr.HasThis = true;
    return new(attr);
}

static MethodDefinition GenerateThreadSafeGetter(FieldDefinition field)
{
    MethodDefinition method = new("get_" + field.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, field.FieldType)
    {
        Body =
        {
            InitLocals = true,
        },
        CustomAttributes =
        {
            CreateCompilerGeneratedAttribute(field),
        },
        HasThis = !field.IsStatic,
        IsGetter = true,
        IsStatic = field.IsStatic,
        SemanticsAttributes = MethodSemanticsAttributes.Getter,
    };

    var il = method.Body.GetILProcessor();

    if (field.IsStatic)
    {
        il.Append(il.Create(OpCodes.Ldsfld, field));
    }
    else
    {
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Ldfld, field));
    }

    il.Append(il.Create(OpCodes.Ret));

    return method;
}

static MethodDefinition GenerateThreadSafeSetter(FieldDefinition field)
{
    MethodDefinition method = new("set_" + field.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, field.DeclaringType.Module.TypeSystem.Void)
    {
        Body =
        {
            InitLocals = true,
        },
        CustomAttributes =
        {
            CreateCompilerGeneratedAttribute(field),
        },
        HasThis = !field.IsStatic,
        IsSetter = true,
        IsStatic = field.IsStatic,
        SemanticsAttributes = MethodSemanticsAttributes.Setter,
    };

    method.Parameters.Add(new("value", ParameterAttributes.None, field.FieldType));

    var il = method.Body.GetILProcessor();

    if (field.IsStatic)
    {
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Stsfld, field));
    }
    else
    {
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Ldarg_1));
        il.Append(il.Create(OpCodes.Stfld, field));
    }

    il.Append(il.Create(OpCodes.Ret));

    return method;
}
