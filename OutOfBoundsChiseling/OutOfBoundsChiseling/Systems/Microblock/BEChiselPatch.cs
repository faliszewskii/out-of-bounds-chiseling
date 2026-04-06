using HarmonyLib;
using OutOfBoundsChiseling.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using VSSurvivalMod.Systems.ChiselModes;

namespace OutOfBoundsChiseling.Systems.Microblock
{
    [HarmonyPatch(typeof(BlockEntityChisel))]
    [HarmonyPatch("OnBlockInteract")]
    public static class BEChiselPatch
    {
        static readonly MethodInfo m_OutOfBoundsChiseling = AccessTools.Method(typeof(BEChiselPatch), nameof(OutOfBoundsChiseling));

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            // execute OutOfBoundsChiseling function right after calculating voxelPos
            var found = false;
            CodeInstruction previousInstruction = null;
            var continueLabel = generator.DefineLabel();
            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (instruction.opcode == OpCodes.Stloc_2 && previousInstruction?.opcode == OpCodes.Newobj)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // BlockEntityChisel this,
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // IPlayer byPlayer,
                    yield return new CodeInstruction(OpCodes.Ldarg_2); // BlockSelection blockSel, 
                    yield return new CodeInstruction(OpCodes.Ldarg_3); // bool isBreak,
                    yield return new CodeInstruction(OpCodes.Ldloc_2); // Vec3i voxelPos,
                    yield return new CodeInstruction(OpCodes.Call, m_OutOfBoundsChiseling);

                    // Continue if false
                    yield return new CodeInstruction(OpCodes.Brfalse_S, continueLabel);
                    yield return new CodeInstruction(OpCodes.Ret);
                    var continueInstruction = new CodeInstruction(OpCodes.Nop);
                    continueInstruction.labels.Add(continueLabel);
                    yield return continueInstruction;

                    found = true;
                }
                previousInstruction = instruction;
            }
            if (found is false)
                throw new Exception("Didn't find a place to inject OutOfBoundsChiseling in BlockEntityChisel.OnBlockInteract!");
        }

        private static bool OutOfBoundsChiseling(this BlockEntityChisel __instance, IPlayer byPlayer, BlockSelection blockSel, bool isBreak, Vec3i voxelPos)
        {

            // blockSel.SelectionBoxIndex is strictly tied to the boxes from this and only this BEChisel.
            // If we want to get voxelPos for different BEChisel we have to calculate it first in original BEChisel
            // and only here pass it to correct one
            var facing = blockSel.Face;
            var modeData = __instance.GetChiselModeData(byPlayer);
            Vec3i addAtPos = voxelPos.Clone().Add(modeData.ChiselSize * facing.Normali.X, modeData.ChiselSize * facing.Normali.Y, modeData.ChiselSize * facing.Normali.Z);

            if (!isBreak && modeData is OneByChiselMode or TwoByChiselMode or FourByChiselMode or EightByChiselModeData
                && (addAtPos.X < 0 || addAtPos.X >= 16 || addAtPos.Y < 0 || addAtPos.Y >= 16 || addAtPos.Z < 0 || addAtPos.Z >= 16))
            {
                // We need to overcompensate for addAtPos calculation in ChiselMode.Apply
                int offset = 16 + modeData.ChiselSize;
                addAtPos.X += addAtPos.X < 0 ? offset : addAtPos.X >= 16 ? -offset : 0;
                addAtPos.Y += addAtPos.Y < 0 ? offset : addAtPos.Y >= 16 ? -offset : 0;
                addAtPos.Z += addAtPos.Z < 0 ? offset : addAtPos.Z >= 16 ? -offset : 0;

                BlockPos atBlockPos = __instance.Pos.Copy().Offset(facing);
                Block atBlock = byPlayer.Entity.World.BlockAccessor.GetBlock(atBlockPos);

                int blockId = GetCurrentMaterialBlockId(byPlayer);
                if(blockId < 0)
                {
                    blockId = __instance.BlockIds.First();
                }

                var bec = atBlock is BlockChisel ? byPlayer.Entity.Api.World.BlockAccessor.GetBlockEntity(atBlockPos) as BlockEntityChisel : null;

                var playerHasMaterial = byPlayer.InventoryManager.Find((ItemSlot slot) => slot?.Itemstack?.Block is not null && slot.Itemstack.Id == blockId);
                var blockHasMaterial = bec is not null ? bec.BlockIds.Contains(blockId) : false;

                if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative && !playerHasMaterial && ! blockHasMaterial)
                {
                    var materialName = byPlayer.Entity.Api.World.BlockAccessor.GetBlock(blockId).Code.GetName();
                    (byPlayer.Entity.Api.World.Api as ICoreClientAPI)?.TriggerIngameError(byPlayer, "no-material", Lang.Get(OutOfBoundsChiselingModSystem.ModID + ":no-material", materialName));
                    return true;
                }

                if (bec is not null)
                {
                    SetCurrentMaterialToBEC(bec, blockId);
                    AccessTools.Method(typeof(BlockEntityChisel), "UpdateVoxel").Invoke(bec, [byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot, addAtPos, facing, isBreak]);
                }
                else
                {
                    if (!TryPlaceBEChisel(byPlayer.Entity, blockId, atBlockPos, out bec))
                    {
                        (byPlayer.Entity.Api.World.Api as ICoreClientAPI)?.TriggerIngameError(byPlayer, "couldnt-place-bec", Lang.Get(OutOfBoundsChiselingModSystem.ModID + ":couldnt-place-bec"));
                        return false;
                    }
                    OutOfBoundsChiselingModSystem.ClientNetworkChannel.SendPacket(new PlaceBEChiselPacket { blockId = blockId, atPos = atBlockPos });

                    AccessTools.Method(typeof(BlockEntityChisel), "UpdateVoxel").Invoke(bec, [byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot, addAtPos, facing, isBreak]);

                    if (isBEChiselEmpty(bec))
                    {
                        byPlayer.Entity.Api.World.BlockAccessor.SetBlock(0, atBlockPos);
                        OutOfBoundsChiselingModSystem.ClientNetworkChannel.SendPacket(new SetBlockPacket { blockId = 0, atPos = atBlockPos });
                    }
                    else
                    {
                        OutOfBoundsChiselingModSystem.ClientNetworkChannel.SendPacket(new TakeOutBlockPacket { blockId = blockId, quantity = 1 });                        
                    }
                }
                return true;
            }
            return false;
        }

        private static int GetCurrentMaterialBlockId(IPlayer byPlayer)
        {
            return byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetInt("materialId", -1);
        }

        private static void SetCurrentMaterialToBEC(BlockEntityChisel bec, int blockId)
        {
            Block block = bec.Api.World.GetBlock(blockId);
            if (!bec.BlockIds.Contains(blockId))
            {
                bec.AddMaterial(block, out _, false);
                if (bec.Api.Side == EnumAppSide.Client)
                {
                    OutOfBoundsChiselingModSystem.ClientNetworkChannel.SendPacket(new AddMaterialPacket { Pos = bec.Pos, BlockId = blockId });
                }
            }
            bec.SetNowMaterialId(blockId);
        }

        private static bool TryPlaceBEChisel(Entity byEntity, int blockId, BlockPos atPos, out BlockEntityChisel be)
        {
            be = null;
            Block atBlock = byEntity.World.BlockAccessor.GetBlock(atPos);
            if (atBlock.Replaceable < 6000) return false;

            Block chiseledblock = byEntity.World.GetBlock(new AssetLocation("chiseledblock"));
            byEntity.World.BlockAccessor.SetBlock(chiseledblock.BlockId, atPos);
            be = byEntity.Api.World.BlockAccessor.GetBlockEntity(atPos) as BlockEntityChisel;
            if (be == null) return false;

            be.WasPlaced(byEntity.Api.World.GetBlock(blockId), null);
            be.SetEmptyData();
            be.SetNowMaterialId(0);

            return true;
        }

        private static bool isBEChiselEmpty(BlockEntityChisel be)
        {
            return be.VoxelCuboids.Count == 0;
        }

        public static void SetEmptyData(this BlockEntityMicroBlock __instance)
        {
            BoolArray16x16x16 Voxels = new();
            byte[,,] VoxelMaterial = new byte[16, 16, 16];
            AccessTools.Method(
                typeof(BlockEntityMicroBlock),
                "RebuildCuboidList",
                new Type[] { typeof(BoolArray16x16x16), typeof(byte[,,]) })
                .Invoke(__instance, [Voxels, VoxelMaterial]);

            if (__instance.Api.Side == EnumAppSide.Client)
            {
                //RegenMesh();
                __instance.MarkMeshDirty();
            }

            __instance.RegenSelectionBoxes(__instance.Api.World, null);
            __instance.MarkDirty(true);
        }


        internal static void OnAddMaterialPacket(IServerPlayer byPlayer, AddMaterialPacket packet)
        {
            var bec = (BlockEntityChisel)byPlayer.Entity.Api.World.BlockAccessor.GetBlockEntity(packet.Pos);
            SetCurrentMaterialToBEC(bec, GetCurrentMaterialBlockId(byPlayer));
        }

        internal static void OnPlaceBEChiselPacket(IServerPlayer byPlayer, PlaceBEChiselPacket packet)
        {
            if (!TryPlaceBEChisel(byPlayer.Entity, packet.blockId, packet.atPos, out _))
            {
                throw new Exception("Failed to place BEChisel on the server side!");
            }
        }
        internal static void OnSetBlockPacket(IServerPlayer byPlayer, SetBlockPacket packet)
        {
            byPlayer.Entity.Api.World.BlockAccessor.SetBlock(packet.blockId, packet.atPos);
        }
        internal static void OnTakeOutBlockPacket(IServerPlayer byPlayer, TakeOutBlockPacket packet)
        {
            byPlayer.InventoryManager.Find((ItemSlot slot) =>
            {
                if (slot.Itemstack.Block is not null && slot.Itemstack.Id == packet.blockId)
                {
                    slot.TakeOut(packet.quantity);
                    slot.MarkDirty();
                    return true;
                }
                return false;
            });
        }
    }
}
