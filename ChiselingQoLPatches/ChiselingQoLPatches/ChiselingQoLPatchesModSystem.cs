using HarmonyLib;
using ChiselingQoLPatches.Config;
using ChiselingQoLPatches.CurrentMaterialHud;
using ChiselingQoLPatches.CurrentMaterialHud.Events;
using ChiselingQoLPatches.OutOfBoundsChiseling;
using ChiselingQoLPatches.OutOfBoundsChiseling.Events;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace ChiselingQoLPatches
{
    public class ChiselingQoLPatchesModSystem : ModSystem
    {
        public Harmony harmony;
        public static string ModID;

        public static IClientNetworkChannel ClientNetworkChannel;
        public static IServerNetworkChannel ServerNetworkChannel;

        public OutOfBoundsChiselingConfig config;

        public ICoreClientAPI capi;

        // TODO Try to move away from that much client-server communication (This might cause flickering rn)
        // TODO Proper material block names in error
        // TODO Rename to Chiseling utilities/fixes or sth similar
        // TODO Show current material
        // TODO If material not present in bec -> add to it
        // TODO Picking should work also on already estabilished becs.
        // TODO Player should be able to see currently selected material
        // TODO Removing last voxel should make the block drop all its constituent blocks.


        public override void Start(ICoreAPI api)
        {
            ModID = Mod.Info.ModID;

            if (!Harmony.HasAnyPatches(ModID))
            {
                harmony = new Harmony(ModID);                
                harmony.PatchAll();
            }

            api.Network
                .RegisterChannel(Mod.Info.ModID)
                .RegisterMessageType<AddMaterialPacket>()
                .RegisterMessageType<PlaceBEChiselPacket>()
                .RegisterMessageType<SetBlockPacket>()
                .RegisterMessageType<TakeOutBlockPacket>()
                .RegisterMessageType<ShowHotbarHudPacket>();

        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            ServerNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
                .SetMessageHandler<AddMaterialPacket>(BEChiselOnBlockInteractPatch.OnAddMaterialPacket)
                .SetMessageHandler<PlaceBEChiselPacket>(BEChiselOnBlockInteractPatch.OnPlaceBEChiselPacket)
                .SetMessageHandler<SetBlockPacket>(BEChiselOnBlockInteractPatch.OnSetBlockPacket)
                .SetMessageHandler<TakeOutBlockPacket>(BEChiselOnBlockInteractPatch.OnTakeOutBlockPacket);

            TryToLoadConfig(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            ClientNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
                .SetMessageHandler<ShowHotbarHudPacket>((ShowHotbarHudPacket p) => ItemChiselSetToolModePatch.OnShowHotbarHudPacket(capi, p));

            config = new OutOfBoundsChiselingConfig();
            config.ConsumeBlockOnOutOfBounds = api.World.Config.GetBool("ChiselingQoLPatches:ConsumeBlockOnExtension", config.ConsumeBlockOnOutOfBounds);
            config.DropMaterials = api.World.Config.GetString("ChiselingQoLPatches:DropMaterials");

        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
        }
        private void TryToLoadConfig(ICoreServerAPI api)
        {            
            try
            {
                config = api.LoadModConfig<OutOfBoundsChiselingConfig>("OutOfBoundsChiselingConfig.json");
                if (config == null)
                {
                    config = new OutOfBoundsChiselingConfig();
                }
                //Save a copy of the mod config.
                api.StoreModConfig<OutOfBoundsChiselingConfig>(config, "OutOfBoundsChiselingConfig.json");
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Could not load config! Loading default settings instead.");
                Mod.Logger.Error(e);
                config = new OutOfBoundsChiselingConfig();
            }

            api.World.Config.SetBool("ChiselingQoLPatches:ConsumeBlockOnExtension", config.ConsumeBlockOnOutOfBounds);
            api.World.Config.SetString("ChiselingQoLPatches:DropMaterials", config.DropMaterials.ToString());
        }
    }
}
