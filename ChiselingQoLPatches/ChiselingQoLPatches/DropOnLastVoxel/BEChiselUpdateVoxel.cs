using HarmonyLib;
using ChiselingQoLPatches.CurrentMaterialHud.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;


namespace ChiselingQoLPatches.DropOnLastVoxel
{
    [HarmonyPatch(typeof(BlockEntityChisel))]
    [HarmonyPatch("UpdateVoxel")]
    public static class BEChiselUpdateVoxel
    {
        static readonly MethodInfo m_DropBECMaterials = AccessTools.Method(typeof(BEChiselUpdateVoxel), nameof(DropBECMaterials));

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;

            for (int i = 0; i < codes.Count; i++)
            {
                var instruction = codes[i];
                yield return instruction;

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo m && m.Name == "SetBlock")
                {
                    if (i >= 3 && codes[i - 3].opcode == OpCodes.Ldc_I4_0)
                    {
                        found = true;

                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, m_DropBECMaterials);
                    }
                }

            }

            if (found is false)
                throw new Exception("Didn't find a place to inject the method!");
        }

        public static void DropBECMaterials(this BlockEntityChisel __instance, IPlayer byPlayer)
        {
            var config = byPlayer.Entity.Api.ModLoader.GetModSystem<ChiselingQoLPatchesModSystem>().config;
            if (!config.UseBlocksFromInventory || !config.DropMaterialsOnLastVoxel) return;
            if (__instance.Api.World.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack[] drops = [.. __instance.BlockIds.Select(id => new ItemStack(id, EnumItemClass.Block, 1, new(), __instance.Api.World))];

                var pos = __instance.Pos;
                foreach (var drop in drops)
                {
                    if(!byPlayer.InventoryManager.TryGiveItemstack(drop, true)) 
                    {
                        __instance.Api.World.SpawnItemEntity(drop.Clone(), pos, null);
                    }
                }
            }
        }
    }
}
