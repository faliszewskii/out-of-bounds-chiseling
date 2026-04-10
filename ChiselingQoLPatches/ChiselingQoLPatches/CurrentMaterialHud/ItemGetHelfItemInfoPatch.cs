using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace ChiselingQoLPatches.CurrentMaterialHud
{
    [HarmonyPatch]
    public static class ItemGetHelfItemInfoPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Item), "GetHeldItemInfo")]
        public static void GetHeldItemInfoPatch(Item __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if(__instance is ItemChisel)
            {
                int materialId = inSlot.Itemstack.Attributes.GetAsInt("materialId", -1);
                if (materialId == -1)
                {
                    dsc.Append("(No material selected)\n");
                    return;
                }
                Block block = world.BlockAccessor.GetBlock(materialId);
                string blockName = Lang.GetMatching(block.Code?.Domain + ":" + block.ItemClass.Name() + "-" + block.Code?.Path);
                dsc.Append($"({blockName})\n");
            }
            
        }
    }
}
