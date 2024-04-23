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
using MonoMod.Cil;
using System;
using Terraria;

/// <summary>
/// @doc Transforms the thread-unsafe fields of Terraria.Main into thread-safe properties
/// </summary>
[Modification(ModType.PreMerge, "Mapping Thread-Unsafe Fields to Properties")]
[MonoMod.MonoModIgnore]
void ThreadSafeProperties(ModFwModder modder)
{
    var f_netMode = modder.GetFieldDefinition(() => Main.netMode);
    RemapAsThreadSafeProperty(f_netMode, modder, 2);

    var f_myPlayer = modder.GetFieldDefinition(() => Main.myPlayer);
    RemapAsThreadSafeProperty(f_myPlayer, modder, 255);
}

[MonoMod.MonoModIgnore]
static PropertyDefinition RemapAsThreadSafeProperty(FieldDefinition field, ModFwModder modder, int constant)
{
    // Add a new field which indicates if the specified field has been initialized yet
    var f_Initialized = new FieldDefinition(
        $"<{field.Name}>k__Initialized",
        FieldAttributes.Private
        ,
        field.Module.TypeSystem.Boolean
    )
    {
        CustomAttributes =
        {
            CreateCompilerGeneratedAttribute(field),
            CreateThreadStaticAttribute(field),
        },
        IsStatic = field.IsStatic,
    };
    field.DeclaringType.Fields.Add(f_Initialized);

    var getter = CreateGetter(field, constant, f_Initialized);
    field.DeclaringType.Methods.Add(getter);

    var setter = CreateSetter(field, f_Initialized);
    field.DeclaringType.Methods.Add(setter);

    var property = new PropertyDefinition(field.Name, PropertyAttributes.None, field.FieldType)
    {
        HasThis = !field.IsStatic,
        GetMethod = getter,
        SetMethod = setter,
    };

    field.CustomAttributes.Add(CreateCompilerGeneratedAttribute(field));
    field.CustomAttributes.Add(CreateThreadStaticAttribute(field));
    field.DeclaringType.Properties.Add(property);
    field.Name = $"<{field.Name}>k__BackingField";
    field.IsPrivate = true;
    modder.AddTask<FieldToPropertyRelinker>(field, property);

    return property;
}

[MonoMod.MonoModIgnore]
static CustomAttribute CreateThreadStaticAttribute(IMemberDefinition member)
{
    var attr = member.DeclaringType.Module.GetCoreLibMethod("System", "ThreadStaticAttribute", ".ctor");
    attr.HasThis = true;
    return new(attr);
}

[MonoMod.MonoModIgnore]
static CustomAttribute CreateCompilerGeneratedAttribute(IMemberDefinition member)
{
    var attr = member.DeclaringType.Module.GetCoreLibMethod("System.Runtime.CompilerServices", "CompilerGeneratedAttribute", ".ctor");
    attr.HasThis = true;
    return new(attr);
}

[MonoMod.MonoModIgnore]
static void Emit_ld(ILCursor cursor, FieldDefinition field)
{
    if (field.IsStatic)
    {
        cursor.Emit(OpCodes.Ldsfld, field);
    }
    else
    {
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, field);
    }
}

[MonoMod.MonoModIgnore]
static void Emit_st(ILCursor cursor, FieldDefinition field, Action<ILCursor> emitValue)
{
    if (field.IsStatic)
    {
        emitValue(cursor);
        cursor.Emit(OpCodes.Stsfld, field);
    }
    else
    {
        cursor.Emit(OpCodes.Ldarg_0);
        emitValue(cursor);
        cursor.Emit(OpCodes.Stfld, field);
    }
}

[MonoMod.MonoModIgnore]
static ILCursor Emit_ldc_i4(ILCursor cursor, int value) => value switch
{
    -1 => cursor.Emit(OpCodes.Ldc_I4_M1),
    0 => cursor.Emit(OpCodes.Ldc_I4_0),
    1 => cursor.Emit(OpCodes.Ldc_I4_1),
    2 => cursor.Emit(OpCodes.Ldc_I4_2),
    3 => cursor.Emit(OpCodes.Ldc_I4_3),
    4 => cursor.Emit(OpCodes.Ldc_I4_4),
    5 => cursor.Emit(OpCodes.Ldc_I4_5),
    6 => cursor.Emit(OpCodes.Ldc_I4_6),
    7 => cursor.Emit(OpCodes.Ldc_I4_7),
    8 => cursor.Emit(OpCodes.Ldc_I4_8),
    >= sbyte.MinValue and <= sbyte.MaxValue => cursor.Emit(OpCodes.Ldc_I4_S, value),
    _ => cursor.Emit(OpCodes.Ldc_I4, value),
};

[MonoMod.MonoModIgnore]
static MethodDefinition CreateGetter(FieldDefinition field, int constant, FieldDefinition f_Initialized)
{
    var getter = new MethodDefinition(
        $"get_{field.Name}",
        MethodAttributes.Public
        | MethodAttributes.HideBySig
        | MethodAttributes.SpecialName
        ,
        field.FieldType
    )
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

    var cursor = new ILCursor(new ILContext(getter));

    // if (f_Initialized)
    Emit_ld(cursor, f_Initialized);
    cursor.Emit(OpCodes.Brfalse_S, Instruction.Create(OpCodes.Nop));
    // Store instruction to modify jump destination later on
    var i_if1 = cursor.Prev;
    {
        // return field
        Emit_ld(cursor, field);
        cursor.Emit(OpCodes.Ret);
    }
    // else
    {
        var pos = cursor.Index;
        // Duplicate value to return later
        // field = constant
        Emit_st(cursor, field, cursor => Emit_ldc_i4(cursor, constant).Emit(OpCodes.Dup));
        // Rewrite jump target
        i_if1.Operand = cursor.Instrs[pos];
        // f_Initialized = true
        Emit_st(cursor, f_Initialized, static cursor => cursor.Emit(OpCodes.Ldc_I4_1));
        // return constant
        cursor.Emit(OpCodes.Ret);
    }

    return getter;
}

[MonoMod.MonoModIgnore]
static MethodDefinition CreateSetter(FieldDefinition field, FieldDefinition f_Initialized)
{
    var setter = new MethodDefinition(
        $"set_{field.Name}",
        MethodAttributes.Public
        | MethodAttributes.HideBySig
        | MethodAttributes.SpecialName
        ,
        field.DeclaringType.Module.TypeSystem.Void
    )
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
        Parameters =
        {
            new("value", ParameterAttributes.None, field.FieldType),
        },
        SemanticsAttributes = MethodSemanticsAttributes.Setter,
    };

    var cursor = new ILCursor(new ILContext(setter));

    // field = value
    Emit_st(cursor, field, cursor => cursor.Emit(field.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));
    // f_Initialized = true
    Emit_st(cursor, f_Initialized, static cursor => cursor.Emit(OpCodes.Ldc_I4_1));
    cursor.Emit(OpCodes.Ret);

    return setter;
}
