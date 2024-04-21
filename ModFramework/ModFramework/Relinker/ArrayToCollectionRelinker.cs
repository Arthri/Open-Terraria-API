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
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModFramework.Relinker
{
    public static partial class Extensions
    {
        public static void AddTask<T>(this ModFwModder modder, TypeDefinition sourceType)
            where T : ArrayToCollectionRelinker
        {
            modder.AddTask<T>(sourceType);
        }

        public static void RelinkAsCollection(this TypeDefinition sourceType, ModFwModder modder)
        {
            modder.AddTask<ArrayToCollectionRelinker>(sourceType);
        }
    }

    [MonoMod.MonoModIgnore]
    public class ArrayToCollectionRelinker : RelinkTask
    {
        public TypeDefinition Type { get; set; }

        public TypeReference ICollectionRef { get; set; }
        public TypeDefinition ICollectionDef { get; set; }
        public GenericInstanceType ICollectionGen { get; set; }
        public GenericInstanceType ICollectionTItem { get; set; }
        public TypeReference CollectionRef { get; set; }
        public TypeDefinition CollectionDef { get; set; }
        public GenericInstanceType CollectionGen { get; set; }

        public MethodReference CreateCollectionMethod { get; set; }

        protected ArrayToCollectionRelinker(ModFwModder modder, TypeDefinition type)
            : base(modder)
        {
            this.Type = type;

            //ICollectionRef = modder.FindType($"ModFramework.{nameof(ModFramework.ICollection<object>)}`1");
            //CollectionRef = modder.FindType($"ModFramework.{nameof(ModFramework.DefaultCollection<object>)}`1");

            //var asdasd = modder.GetReference(() => ICollection<object>);
            ICollectionRef = modder.Module.ImportReference(typeof(ICollection<>));
            CollectionRef = modder.Module.ImportReference(typeof(DefaultCollection<>));


            ICollectionDef = ICollectionRef.Resolve();
            CollectionDef = CollectionRef.Resolve();

            ICollectionGen = new(ICollectionRef);
            ICollectionTItem = new(ICollectionRef);
            CollectionGen = new(CollectionRef);

            ICollectionGen.GenericArguments.Clear();
            ICollectionGen.GenericArguments.Add(type);
            CollectionGen.GenericArguments.Clear();
            CollectionGen.GenericArguments.Add(type);
            ICollectionTItem.GenericArguments.Clear();
            ICollectionTItem.GenericArguments.Add(ICollectionDef.GenericParameters[0]);

            CreateCollectionMethod = modder.GetReference(() => DefaultCollection<object>.CreateCollection(0, 0, ""));
            CreateCollectionMethod.ReturnType = ICollectionTItem;
            CreateCollectionMethod.DeclaringType = CollectionGen;

            if (modder.LogVerboseEnabled) System.Console.WriteLine($"[ModFw] Relinking to collection {type.FullName}=>{ICollectionDef.FullName}");
        }

        public override void Relink(FieldDefinition field)
        {
            if (field.FieldType is ArrayType array)
                if (array.ElementType.FullName == this.Type.FullName)
                    field.FieldType = ICollectionGen;
        }

        public override void Relink(PropertyDefinition property)
        {
            if (property.PropertyType is ArrayType array)
                if (array.ElementType.FullName == this.Type.FullName)
                    property.PropertyType = ICollectionGen;
        }

        public override void Relink(MethodBody body, Instruction instr)
        {
            if (body.Method.ReturnType is ArrayType arrayType && arrayType.ElementType.FullName == this.Type.FullName)
                body.Method.ReturnType = ICollectionGen;

            RelinkConstructors(body, instr);
            RemapFields(body, instr);
            RemapMethods(body, instr);
        }

        public void RelinkConstructors(MethodBody body, Instruction instr)
        {
            if (instr.OpCode == OpCodes.Newobj && instr.Operand is MethodReference ctorMethod)
            {
                if (ctorMethod.DeclaringType is ArrayType array)
                {
                    if (array.ElementType.FullName == this.Type.FullName)
                    {
                        body.GetILProcessor().InsertBefore(instr, Instruction.Create(OpCodes.Ldstr, body.Method.FullName));
                        instr.OpCode = OpCodes.Call;
                        instr.Operand = CreateCollectionMethod;
                    }
                }
            }
        }

        public void RemapFields(MethodBody body, Instruction instr)
        {
            if (instr.Operand is FieldReference field
                && field.FieldType is ArrayType fieldArray
                && fieldArray.ElementType.FullName == this.Type.FullName
            )
            {
                field.FieldType = this.ICollectionGen;
            }
        }

        public void RemapMethods(MethodBody body, Instruction instr)
        {
            if (instr.Operand is MethodReference methodRef)
            {
                if (methodRef.DeclaringType is ArrayType methodArray && methodArray.ElementType.FullName == this.Type.FullName
                )
                {
                    methodRef.DeclaringType = this.ICollectionGen;
                    methodRef.ReturnType = methodRef.Name == "Get" ? this.ICollectionDef.GenericParameters[0] : methodRef.Module.TypeSystem.Void;

                    if (methodRef.Name == "Set")
                    {
                        methodRef.Parameters[2].ParameterType = this.ICollectionDef.GenericParameters[0];
                    }
                    methodRef.Name = $"{methodRef.Name.ToLower()}_Item";

                    instr.OpCode = OpCodes.Callvirt;
                }

                if (methodRef.ReturnType is ArrayType arrayType && arrayType.ElementType.FullName == this.Type.FullName)
                {
                    methodRef.ReturnType = ICollectionGen;
                }
            }
        }
    }
}
