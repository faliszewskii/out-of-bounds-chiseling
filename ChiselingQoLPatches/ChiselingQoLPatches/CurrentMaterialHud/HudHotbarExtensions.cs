using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.Client.NoObf;

namespace ChiselingQoLPatches.CurrentMaterialHud
{
    public static class HudHotbarExtensions
    {
        public static void ShowHotbarHud(this HudHotbar __instance, int slot)
        {
            AccessTools.Field(typeof(HudHotbar), "prevIndex").SetValue(__instance, -1);
            var capi = AccessTools.Field(typeof(HudHotbar), "capi").GetValue(__instance) as ClientCoreAPI;
            var RecomposeActiveSlotHoverText = AccessTools.Method(typeof(HudHotbar), "RecomposeActiveSlotHoverText");
            (capi.World as ClientMain).EnqueueMainThreadTask(delegate
            {
                RecomposeActiveSlotHoverText.Invoke(__instance, [slot]);
            }, "recomposeslothovertextPlus");
        }
    }
}
