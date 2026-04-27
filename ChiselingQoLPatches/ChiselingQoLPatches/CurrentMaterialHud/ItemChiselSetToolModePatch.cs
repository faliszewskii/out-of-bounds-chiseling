using HarmonyLib;
using ChiselingQoLPatches.CurrentMaterialHud.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace ChiselingQoLPatches.CurrentMaterialHud
{
    [HarmonyPatch(typeof(ItemChisel))]
    [HarmonyPatch("SetToolMode")]
    public static class ItemChiselSetToolModePatch
    {
        static readonly MethodInfo m_TriggerHotbarHudOnSetToolMode = AccessTools.Method(typeof(ItemChiselSetToolModePatch), nameof(TriggerHotbarHudOnSetToolMode));

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var found = false;
            var materialIdBranch = false;
            CodeInstruction previousInstruction = null;
            var continueLabel = generator.DefineLabel();
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldstr && (string)instruction.operand == "materialId")
                {
                    materialIdBranch = true;
                }
                if (materialIdBranch && instruction.opcode == OpCodes.Callvirt && previousInstruction?.opcode == OpCodes.Ldarg_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2); 
                    yield return new CodeInstruction(OpCodes.Call, m_TriggerHotbarHudOnSetToolMode);

                    found = true;
                    materialIdBranch = false;
                }
                previousInstruction = instruction;
            }
            if (found is false)
                throw new Exception("Didn't find a place to inject the method!");
        }

        public static void TriggerHotbarHudOnSetToolMode(IPlayer byPlayer)
        {
            int slot = byPlayer.InventoryManager.ActiveHotbarSlotNumber;
            int materialId = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetInt("materialId", -1);
            ChiselingQoLPatchesModSystem.ServerNetworkChannel.SendPacket(new ShowHotbarHudPacket { Slot = slot, MaterialId = materialId}, byPlayer as IServerPlayer);
        }

        public static void OnShowHotbarHudPacket(ICoreClientAPI capi, ShowHotbarHudPacket packet)
        {
            capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.SetInt("materialId", packet.MaterialId);
            var LoadedGuis = AccessTools.Field(typeof(ClientMain), "LoadedGuis").GetValue(capi.World) as List<GuiDialog>;
            foreach (var dialog in LoadedGuis)
            {
                if (dialog is HudHotbar)
                {
                    (dialog as HudHotbar).ShowHotbarHud(packet.Slot);
                }
            }
        }
    }
}
