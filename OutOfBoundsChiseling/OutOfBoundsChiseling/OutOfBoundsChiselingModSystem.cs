using HarmonyLib;
using OutOfBoundsChiseling.Events;
using OutOfBoundsChiseling.Systems.Microblock;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace OutOfBoundsChiseling
{
    public class OutOfBoundsChiselingModSystem : ModSystem
    {
        public Harmony harmony;
        public static string ModID;

        public static IClientNetworkChannel ClientNetworkChannel;
        public static IServerNetworkChannel ServerNetworkChannel;

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
                .RegisterMessageType<TakeOutBlockPacket>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            ServerNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
                .SetMessageHandler<AddMaterialPacket>(BEChiselPatch.OnAddMaterialPacket)
                .SetMessageHandler<PlaceBEChiselPacket>(BEChiselPatch.OnPlaceBEChiselPacket)
                .SetMessageHandler<SetBlockPacket>(BEChiselPatch.OnSetBlockPacket)
                .SetMessageHandler<TakeOutBlockPacket>(BEChiselPatch.OnTakeOutBlockPacket);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            ClientNetworkChannel = api.Network.GetChannel(Mod.Info.ModID);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
        }
    }
}
