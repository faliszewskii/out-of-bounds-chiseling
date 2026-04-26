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
using static ChiselingQoLPatches.Common.Common;

namespace ChiselingQoLPatches
{
    public class ChiselingQoLPatchesModSystem : ModSystem
    {
        public Harmony harmony;
        public static string ModID;

        public static IClientNetworkChannel ClientNetworkChannel;
        public static IServerNetworkChannel ServerNetworkChannel;

        public ChiselingQoLPatchesConfig config;

        public ICoreClientAPI capi;


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
                .SetMessageHandler<AddMaterialPacket>(OnAddMaterialPacket)
                .SetMessageHandler<PlaceBEChiselPacket>(BEChiselOnBlockInteractPatch.OnPlaceBEChiselPacket)
                .SetMessageHandler<SetBlockPacket>(BEChiselOnBlockInteractPatch.OnSetBlockPacket)
                .SetMessageHandler<TakeOutBlockPacket>(OnTakeOutBlockPacket);

            TryToLoadConfig(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            ClientNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
                .SetMessageHandler<ShowHotbarHudPacket>((ShowHotbarHudPacket p) => ItemChiselSetToolModePatch.OnShowHotbarHudPacket(capi, p));

            config = new ChiselingQoLPatchesConfig();
            config.UseBlocksFromInventory = api.World.Config.GetBool("ChiselingQoLPatches:UseBlocksFromInventory", config.UseBlocksFromInventory);
            config.DropMaterialsOnLastVoxel = api.World.Config.GetBool("ChiselingQoLPatches:DropMaterialsOnLastVoxel", config.DropMaterialsOnLastVoxel);

        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
        }
        private void TryToLoadConfig(ICoreServerAPI api)
        {            
            try
            {
                config = api.LoadModConfig<ChiselingQoLPatchesConfig>("ChiselingQoLPatches.json");
                if (config == null)
                {
                    config = new ChiselingQoLPatchesConfig();
                }
                //Save a copy of the mod config.
                api.StoreModConfig<ChiselingQoLPatchesConfig>(config, "ChiselingQoLPatches.json");
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Could not load config! Loading default settings instead.");
                Mod.Logger.Error(e);
                config = new ChiselingQoLPatchesConfig();
            }

            api.World.Config.SetBool("ChiselingQoLPatches:UseBlocksFromInventory", config.UseBlocksFromInventory);
            api.World.Config.SetBool("ChiselingQoLPatches:DropMaterialsOnLastVoxel", config.DropMaterialsOnLastVoxel);
        }
    }
}
