using ChiselingQoLPatches.CurrentMaterialHud.Events;
using ChiselingQoLPatches.OutOfBoundsChiseling.Events;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using static ChiselingQoLPatches.Common.Common;

namespace ChiselingQoLPatches.AddMaterialAutomatically
{
    public static class AutomaticallyAddMaterialToBec
    {
        public static bool AddMaterialToBec(BlockEntityChisel bec, int materialId, IPlayer byPlayer)
        {
            if (bec.BlockIds.IndexOf(materialId) < 0)
            {
                var config = byPlayer.Entity.Api.ModLoader.GetModSystem<ChiselingQoLPatchesModSystem>().config;
                if (config.UseBlocksFromInventory && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    var playerHasMaterial = byPlayer.InventoryManager.Find(slot => slot?.Itemstack?.Block is not null && slot.Itemstack.Id == materialId);
                    if (!playerHasMaterial)
                    {
                        (byPlayer.Entity.Api.World.Api as ICoreClientAPI)?.TriggerIngameError(byPlayer, "no-material", Lang.Get(ChiselingQoLPatchesModSystem.ModID + ":no-material"));
                        return false;
                    }
                    ChiselingQoLPatchesModSystem.ClientNetworkChannel.SendPacket(new TakeOutBlockPacket { blockId = materialId, quantity = 1 });
                }
                SetCurrentMaterialToBEC(bec, materialId);            
            }
            return true;
        }
    }
}
