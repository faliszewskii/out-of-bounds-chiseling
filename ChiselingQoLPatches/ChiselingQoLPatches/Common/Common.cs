using ChiselingQoLPatches.OutOfBoundsChiseling.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ChiselingQoLPatches.Common
{
    internal class Common
    {
        internal static void OnAddMaterialPacket(IServerPlayer byPlayer, AddMaterialPacket packet)
        {
            var bec = (BlockEntityChisel)byPlayer.Entity.Api.World.BlockAccessor.GetBlockEntity(packet.Pos);
            SetCurrentMaterialToBEC(bec, packet.BlockId);
        }
        internal static void SetCurrentMaterialToBEC(BlockEntityChisel bec, int blockId)
        {
            Block block = bec.Api.World.GetBlock(blockId);
            if (!bec.BlockIds.Contains(blockId))
            {
                bec.AddMaterial(block, out _, false);
                if (bec.Api.Side == EnumAppSide.Client)
                {
                    ChiselingQoLPatchesModSystem.ClientNetworkChannel.SendPacket(new AddMaterialPacket { Pos = bec.Pos, BlockId = blockId });
                }
            }
            bec.SetNowMaterialId(blockId);
        }

        internal static void OnTakeOutBlockPacket(IServerPlayer byPlayer, TakeOutBlockPacket packet)
        {
            byPlayer.InventoryManager.Find((slot) =>
            {
                if (slot?.Itemstack?.Block is not null && slot.Itemstack.Id == packet.blockId)
                {
                    slot.TakeOut(packet.quantity);
                    byPlayer.InventoryManager.NotifySlot(byPlayer, slot);
                    slot.MarkDirty();
                    return true;
                }
                return false;
            });
        }
    }
}
