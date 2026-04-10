using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace ChiselingQoLPatches.CurrentMaterialHud
{
    [HarmonyPatch]
    public static class HudHotbarRecomposeActiveSlotHoverTextPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudHotbar), "RecomposeActiveSlotHoverText")]
        public static void RecomposeActiveSlotHoverTextPatch(HudHotbar __instance, int newSlotIndex)
        {
            var capi = AccessTools.Field(typeof(HudHotbar), "capi").GetValue(__instance) as ClientCoreAPI;
            var activeSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            if(activeSlot.Itemstack?.Item is ItemChisel)
            {
                string name = activeSlot.Itemstack.GetName();
                GuiElementHoverText elem = __instance.Composers["hotbar"]?.GetHoverText("iteminfoHover");

                int materialId = activeSlot.Itemstack.Attributes.GetInt("materialId", -1);
                if (materialId == -1)
                {
                    elem.SetNewText(name + " (No material selected)");
                    return;
                }
                Block block = capi.World.BlockAccessor.GetBlock(materialId);
                string blockName = Lang.GetMatching(block.Code?.Domain + ":" + block.ItemClass.Name() + "-" + block.Code?.Path);
                elem.SetNewText(name + $" ({blockName})");
            }
        }
    }
}
