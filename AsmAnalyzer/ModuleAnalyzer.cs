using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Microsoft.Extensions.Logging;

namespace AsmAnalyzer;

internal class ModuleAnalyzer(ILogger Logger, ModuleDefinition Module, HashSet<MetadataToken> FlaggedReferences)
{
    private IDisposable? BeginMemberScope<T>(T member)
        where T : IMetadataMember, IFullNameProvider
    {
        return Logger.BeginScope("{FullName}(0x{MetadataToken})", member.FullName, member.MetadataToken);
    }

    private IDisposable? BeginPartMemberScope<T>(T member)
        where T : IMetadataMember, INameProvider
    {
        return Logger.BeginScope("{Name}(0x{MetadataToken})", member.Name, member.MetadataToken);
    }

    private bool IsFlaggedReference(ITypeDefOrRef reference)
    {
        return FlaggedReferences.Contains(reference.MetadataToken);
    }

    private void LogFlagged(MetadataToken token)
    {
        Logger.LogError("Matched flagged token {MetadataToken}", token);
    }

    private void LogFlagged(IMetadataMember member)
    {
        LogFlagged(member.MetadataToken);
    }

    public void Analyze()
    {
        Logger.LogInformation("Searching for flagged type references...");
        AnalyzeAssemblyReferences(Module.AssemblyReferences);
        AnalyzeCustomAttributes(Module.CustomAttributes);
        AnalyzeFileReferences(Module.FileReferences);
        AnalyzeManifestResources(Module.Resources);

        foreach (var type in Module.TopLevelTypes)
        {
            AnalyzeTypeDefinition(type);
        }
    }

    private void AnalyzeAssemblyReferences(IList<AssemblyReference> references)
    {
        if (references.Count <= 0)
        {
            return;
        }

        using (Logger.BeginScope("Assembly References"))
        {
            foreach (var reference in references)
            {
                using (BeginMemberScope(reference))
                {
                    AnalyzeCustomAttributes(reference.CustomAttributes);
                }
            }
        }
    }

    void AnalyzeFileReferences(IList<FileReference> references)
    {
        if (references.Count <= 0)
        {
            return;
        }

        using (Logger.BeginScope("File References"))
            foreach (var reference in references)
            {
                using (BeginMemberScope(reference))
                {
                    AnalyzeCustomAttributes(reference.CustomAttributes);
                }
            }
    }

    void AnalyzeManifestResources(IList<ManifestResource> resources)
    {
        if (resources.Count <= 0)
        {
            return;
        }

        using (Logger.BeginScope("Manifest Resources"))
            foreach (var resource in resources)
            {
                using (BeginPartMemberScope(resource))
                {
                    AnalyzeCustomAttributes(resource.CustomAttributes);
                    if (resource.Implementation is not null)
                    {
                        using var ___ = Logger.BeginScope("Implementation {Implementation}({MetadataToken})", resource.Implementation.FullName, resource.Implementation.MetadataToken);
                        AnalyzeCustomAttributes(resource.Implementation.CustomAttributes);
                    }
                }
            }
    }

    void AnalyzeCustomAttributes(IList<CustomAttribute> attributes)
    {
        if (attributes.Count <= 0)
        {
            return;
        }

        using (Logger.BeginScope("Custom Attributes"))
        {
            foreach (var attribute in attributes)
            {
                var memberScope = attribute.Constructor is null
                    ? Logger.BeginScope("0x{MetadataToken}", attribute.MetadataToken)
                    : BeginMemberScope(attribute.Constructor)
                    ;
                using (memberScope)
                {
                    if (attribute.Constructor is not null)
                    {
                        using (Logger.BeginScope("Constructor"))
                        {
                            if (attribute.Constructor.DeclaringType is not null)
                            {
                                AnalyzeTypeReference(attribute.Constructor.DeclaringType);
                            }
                            if (attribute.Constructor.Signature is not null)
                            {
                                AnalyzeMethodSignature(attribute.Constructor.Signature);
                            }
                        }
                    }

                    if (attribute.Signature is not null)
                    {
                        foreach (var argument in attribute.Signature.FixedArguments)
                        {
                            using (Logger.BeginScope("Fixed Arguments"))
                            {
                                AnalyzeCustomAttributeArgument(argument);
                            }
                        }

                        foreach (var argument in attribute.Signature.NamedArguments)
                        {
                            using (Logger.BeginScope("Named Arguments"))
                            {
                                AnalyzeTypeSignature(argument.ArgumentType);
                                AnalyzeCustomAttributeArgument(argument.Argument);
                            }
                        }
                    }
                }
            }
        }
    }

    void AnalyzeCustomAttributeArgument(CustomAttributeArgument argument)
    {
        AnalyzeTypeSignature(argument.ArgumentType);
        foreach (var element in argument.Elements)
        {
            if (element is null)
            {
                continue;
            }

            var elementType = element.GetType();
            if (elementType.IsPrimitive || element is string or Utf8String)
            {
                continue;
            }

            switch (element)
            {
                case TypeSignature ts:
                {
                    AnalyzeTypeSignature(ts);
                }
                break;

                case BoxedArgument ba:
                {
                    AnalyzeTypeSignature(ba.Type);
                }
                break;

                default:
                    throw new NotSupportedException($"Unsupported custom attribute element type {elementType}");
            }
        }
    }

    void AnalyzeTypeReference(ITypeDefOrRef type)
    {
        if (type is TypeSpecification { Signature: GenericParameterSignature })
        {
            return;
        }

        if (type.MetadataToken.Table is not TableIndex.TypeRef)
        {
            //# TypeDefs and everything else is analyzed by module crawler
            return;
        }

        do
        {
            using var _ = Logger.BeginScope("TypeReference {Type}(0x{MetadataToken})", type.FullName, type.MetadataToken);
            if (IsFlaggedReference(type))
            {
                LogFlagged(type);
            }
            type = type.DeclaringType!;
        }
        while (type is not null);

        //# Analyzed later by module crawler
        //# AnalyzeTypeDefinition(rType);
    }

    private readonly HashSet<MetadataToken> AnalyzedDefinitions = [];

    void AnalyzeTypeDefinition(TypeDefinition type)
    {
        if (AnalyzedDefinitions.Contains(type.MetadataToken))
        {
            Logger.LogTrace("Skipped: currently analyzing or already analyzed.");
            return;
        }
        AnalyzedDefinitions.Add(type.MetadataToken);

        using var _ = Logger.BeginScope("Type Definition {Type}(0x{MetadataToken})", type.FullName, type.MetadataToken);

        AnalyzeCustomAttributes(type.CustomAttributes);
        AnalyzeGenericParameters(type.GenericParameters);

        if (type.Fields.Count > 0)
        {
            using (Logger.BeginScope("Fields"))
            {
                foreach (var field in type.Fields)
                {
                    AnalyzeField(field);
                }
            }
        }

        if (type.Events.Count > 0)
        {
            using (Logger.BeginScope("Events"))
            {
                foreach (var ev in type.Events)
                {
                    AnalyzeEvent(ev);
                }
            }
        }

        if (type.Properties.Count > 0)
        {
            using (Logger.BeginScope("Properties"))
            {
                foreach (var property in type.Properties)
                {
                    AnalyzeProperty(property);
                }
            }
        }

        if (type.Methods.Count > 0)
        {
            using (Logger.BeginScope("Methods"))
            {
                foreach (var method in type.Methods)
                {
                    AnalyzeMethod(method);
                }
            }
        }

        if (type.MethodImplementations.Count > 0)
        {
            using (Logger.BeginScope("MethodImpls"))
            {
                foreach (var method in type.MethodImplementations)
                {
                    var implementation = (Name: "<NUL>", MetadataToken: MetadataToken.Zero);
                    var declaration = implementation;
                    if (method.Body is not null)
                    {
                        implementation = (method.Body.FullName, method.Body.MetadataToken);
                    }
                    if (method.Declaration is not null)
                    {
                        declaration = (method.Declaration.FullName, method.Declaration.MetadataToken);
                    }
                    using var __ = Logger.BeginScope(
                        "{Implementation.FullName}(0x{Implementation.MetadataToken}) for {Declaration.FullName}(0x{Declaration.MetadataToken})",
                        implementation.Name,
                        implementation.MetadataToken,
                        declaration.Name,
                        declaration.MetadataToken
                    );

                    if (method.Body is not null && method.Body.Module?.MetadataToken == Module.MetadataToken)
                    {
                        var rBody = method.Body.Resolve();
                        if (rBody is null)
                        {
                            Logger.LogError("Unable to resolve in-module method body {Method}(0x{MetadataToken})", method.Body.FullName, method.Body.MetadataToken);
                            continue;
                        }
                        AnalyzeMethod(rBody);
                    }

                    //# Declaration is analyzed separately
                }
            }
        }

        if (type.NestedTypes.Count > 0)
        {
            using (Logger.BeginScope("NestedTypes"))
            {
                foreach (var nType in type.NestedTypes)
                {
                    AnalyzeTypeDefinition(nType);
                }
            }
        }
    }

    void AnalyzeField(FieldDefinition field)
    {
        using (BeginMemberScope(field))
        {
            AnalyzeCustomAttributes(field.CustomAttributes);
            if (field.Signature is not null)
            {
                AnalyzeFieldSignature(field.Signature);
            }
        }
    }

    void AnalyzeFieldSignature(FieldSignature signature)
    {
        AnalyzeTypeSignature(signature.FieldType);
    }

    void AnalyzeEvent(EventDefinition ev)
    {
        using (BeginMemberScope(ev))
        {
            AnalyzeCustomAttributes(ev.CustomAttributes);
            if (ev.EventType is not null)
            {
                AnalyzeTypeReference(ev.EventType);
            }
            //# Analyzed later
            //# ev.AddMethod
            //# ev.RemoveMethod
            //# ev.FireMethod
        }
    }

    void AnalyzeProperty(PropertyDefinition property)
    {
        using (BeginMemberScope(property))
        {
            AnalyzeCustomAttributes(property.CustomAttributes);
            if (property.Signature is not null)
            {
                AnalyzeMethodSignature(property.Signature);
            }
            //# Analyzed later
            //# property.GetMethod
            //# property.SetMethod
        }
    }

    void AnalyzeMethod(MethodDefinition method)
    {
        using (BeginMemberScope(method))
        {
            AnalyzeCustomAttributes(method.CustomAttributes);
            AnalyzeGenericParameters(method.GenericParameters);
            AnalyzeParameterDefinitions(method.ParameterDefinitions);
            if (method.Signature is not null)
            {
                AnalyzeMethodSignature(method.Signature);
            }
            if (method.CilMethodBody is { } body)
            {
                AnalyzeMethodBody(body);
            }
        }
    }

    void AnalyzeMethodBody(CilMethodBody body)
    {
        if (body.LocalVariables.Count > 0)
        {
            using (Logger.BeginScope("Locals"))
            {
                foreach (var local in body.LocalVariables)
                {
                    using (Logger.BeginScope(local.Index))
                    {
                        AnalyzeTypeSignature(local.VariableType);
                    }
                }
            }
        }

        if (body.ExceptionHandlers.Count > 0)
        {
            using (Logger.BeginScope("Exception Handlers"))
            {
                foreach (var eh in body.ExceptionHandlers)
                {
                    using (Logger.BeginScope("{Type} ({Start}, {End})", eh.HandlerType, eh.HandlerStart?.Offset, eh.HandlerEnd?.Offset))
                    {
                        if (eh.ExceptionType is not null)
                        {
                            AnalyzeTypeReference(eh.ExceptionType);
                        }
                    }
                }
            }

            if (body.Instructions.Count > 0)
            {
                using (Logger.BeginScope("Instructions"))
                {
                    foreach (var instruction in body.Instructions)
                    {
                        using (Logger.BeginScope("0x{Offset} {OpCode}", instruction.Offset.ToString("X8"), instruction.OpCode))
                        if (instruction.Operand is TypeSignature ts)
                        {
                            AnalyzeTypeSignature(ts);
                        }
                        else if (instruction.Operand is TypeReference tr)
                        {
                            AnalyzeTypeReference(tr);
                        }
                        else if (instruction.Operand is MemberReference mr)
                        {
                            if (mr.Signature is MethodSignature ms)
                            {
                                AnalyzeMethodSignature(ms);
                            }
                            else if (mr.Signature is FieldSignature fs)
                            {
                                AnalyzeFieldSignature(fs);
                            }
                            else
                            {
                                throw new NotSupportedException($"Unsupported member reference {mr.GetType().FullName}");
                            }
                        }
                    }
                }
            }
        }
    }

    void AnalyzeGenericParameters(IList<GenericParameter> parameters)
    {
        if (parameters.Count <= 0)
        {
            return;
        }

        using (Logger.BeginScope("Generic Parameters"))
        {
            foreach (var parameter in parameters)
            {
                using (Logger.BeginScope("T{Index}, {Parameter}(0x{MetadataToken})", parameter.Number, parameter.Name?.ToString() ?? "<Unnamed>", parameter.MetadataToken))
                {
                    AnalyzeCustomAttributes(parameter.CustomAttributes);
                    if (parameter.Constraints.Count > 0)
                    {
                        using (Logger.BeginScope("Constraints"))
                        {
                            foreach (var constraint in parameter.Constraints)
                            {
                                using (Logger.BeginScope("{Constraint}(0x{MetadataToken})", constraint.Constraint?.FullName ?? "<Unknown>", constraint.MetadataToken))
                                {
                                    AnalyzeCustomAttributes(constraint.CustomAttributes);
                                    if (constraint.Constraint is not null)
                                    {
                                        AnalyzeTypeReference(constraint.Constraint);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void AnalyzeTypeSignatures(IList<TypeSignature> signatures)
    {
        if (signatures.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < signatures.Count; i++)
        {
            using (Logger.BeginScope("{Index}", i))
            {
                AnalyzeTypeSignature(signatures[i]);
            }
        }
    }

    void AnalyzeTypeSignature(TypeSignature signature)
    {
        if (signature.ElementType is
            //# Not interested in core library types
            ElementType.Void or ElementType.Boolean or ElementType.Char or ElementType.I1 or ElementType.U1 or ElementType.I2 or ElementType.U2 or ElementType.I4 or ElementType.U4 or ElementType.I8 or ElementType.U8 or ElementType.R4 or ElementType.R8 or ElementType.String or ElementType.TypedByRef or ElementType.I or ElementType.U or ElementType.Object
            //# Not interested in generic parameters
            or ElementType.Var or ElementType.MVar
            //# Not interested in varargs
            or ElementType.Sentinel
        )
        {
            return;
        }

        switch (signature)
        {
            case TypeDefOrRefSignature tSignature:
            {
                AnalyzeTypeReference(tSignature.Type);
            }
            break;

            case TypeSpecificationSignature pSignature:
            {
                using (Logger.BeginScope("{SignatureType}", signature.GetType().Name))
                {
                    AnalyzeTypeSignature(pSignature.BaseType);
                }
            }
            break;

            case GenericInstanceTypeSignature gSignature:
            {
                AnalyzeTypeReference(gSignature.GenericType);
                if (gSignature.TypeArguments.Count > 0)
                {
                    using (Logger.BeginScope("Generic Arguments"))
                    {
                        AnalyzeTypeSignatures(gSignature.TypeArguments);
                    }
                }
            }
            break;

            case FunctionPointerTypeSignature fSignature:
            {
                AnalyzeMethodSignature(fSignature.Signature);
            }
            break;

            default:
                throw new NotSupportedException($"Invalid or unsupported signature type {signature.GetType()}");
        }
    }

    void AnalyzeMethodSignature(MethodSignatureBase signature)
    {
        using (Logger.BeginScope("Method Signature"))
        {
            if (signature.SentinelParameterTypes is [_, ..])
            {
                using (Logger.BeginScope("Sentinels"))
                {
                    AnalyzeTypeSignatures(signature.SentinelParameterTypes);
                }
            }

            if (signature.ParameterTypes.Count > 0)
            {
                using (Logger.BeginScope("Parameters"))
                {
                    AnalyzeTypeSignatures(signature.ParameterTypes);
                }
            }

            using (Logger.BeginScope("Return"))
            {
                AnalyzeTypeSignature(signature.ReturnType);
            }
        }
    }

    void AnalyzeParameterDefinitions(IList<ParameterDefinition> definitions)
    {
        if (definitions.Count <= 0)
        {
            return;
        }

        using (Logger.BeginScope("Parameter Definitions"))
        {
            foreach (var definition in definitions)
            {
                using (Logger.BeginScope("{Sequence}, {DefinitionName}(0x{MetadataToken})", definition.Sequence, definition.Name?.ToString() ?? "<Unnamed>", definition.MetadataToken))
                {
                    AnalyzeCustomAttributes(definition.CustomAttributes);
                }
            }
        }
    }
}
