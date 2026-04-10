using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ChiselingQoLPatches.VoxelSelectionFix
{


    [HarmonyPatch(typeof(BlockEntityChisel))]
    [HarmonyPatch(nameof(BlockEntityChisel.PickBlockMaterial))]
    public static class BEChiselPickBlockMaterialPatch
    {
        static readonly MethodInfo m_VoxelSelectionFix = AccessTools.Method(typeof(BEChiselPickBlockMaterialPatch), nameof(VoxelSelectionFix));

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var found = false;
            CodeInstruction previousInstruction = null;
            var continueLabel = generator.DefineLabel();
            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (instruction.opcode == OpCodes.Stloc_3 && previousInstruction?.opcode == OpCodes.Newobj)
                {

                    // VoxelSelectionFix(byPlayer, ref voxPos);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, m_VoxelSelectionFix);

                    found = true;
                }
                previousInstruction = instruction;
            }
            if (found is false)
                throw new Exception("Didn't find a place to inject code in BlockEntityChisel.PickBlockMaterial!");
        }

        internal static void VoxelSelectionFix(IPlayer byPlayer, Vec3i voxPos)
        {
            if (byPlayer.CurrentBlockSelection.Face == BlockFacing.UP) voxPos.ReduceY();
            if (byPlayer.CurrentBlockSelection.Face == BlockFacing.EAST) voxPos.ReduceX();
            if (byPlayer.CurrentBlockSelection.Face == BlockFacing.SOUTH) voxPos.ReduceZ();
        }
    }
}
