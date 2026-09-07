#pragma warning disable CS8321 // Local function is called by ModFramework.

#if Terraria_1458_OrAbove || TerrariaServer_1458_OrAbove
using System;
using System.Linq;
using ModFramework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Terraria.GameContent.NetModules;

/// <summary>
/// @doc Fix the research count serializer's capacity: an Int16 item id and UInt16 count need four bytes.
/// </summary>
[Modification(ModType.PreMerge, "Fixing creative unlock packet capacity")]
[MonoMod.MonoModIgnore]
void FixCreativeUnlockPacketSize(ModFwModder modder)
{
    var method = modder.GetMethodDefinition(() => NetCreativeUnlocksModule.SerializeItemSacrifice(0, 0));
    var allocation = method.Body.Instructions.Single(instruction =>
        instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference called &&
        called.Name == "CreatePacket");
    var capacity = allocation.Previous;
    if (capacity.OpCode == OpCodes.Ldc_I4_3)
        capacity.OpCode = OpCodes.Ldc_I4_4;
    else if (capacity.OpCode != OpCodes.Ldc_I4_4)
        throw new InvalidOperationException("Unexpected creative unlock packet allocation; review the serializer layout.");
}
#endif
