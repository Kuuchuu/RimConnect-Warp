using HarmonyLib;
using RimConnection;
using RimConnection.Settings;
using UnityEngine;
using Verse;

namespace RimConnect___Warp
{
    public class WarpSettings : ModSettings
    {
        public bool useCustom = false;
        public string customURL = UrlOverridePatches.OVERRIDE_URL;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref useCustom, "warp_useCustom", false);
            Scribe_Values.Look(ref customURL, "warp_customURL", UrlOverridePatches.OVERRIDE_URL);
        }
    }

    public class WarpMod : Mod
    {
        public static WarpMod instance;
        public static WarpSettings settings;

        public WarpMod(ModContentPack content) : base(content)
        {
            instance = this;
            settings = GetSettings<WarpSettings>();
            Harmony harmony = new Harmony("com.leora.rimconnect.warp");
            harmony.PatchAll();

            // Retry init if we missed first patch due to late load
            LongEventHandler.QueueLongEvent(() =>
            {
                if (settings.useCustom)
                {
                    RimConnectAPI.ChangeBaseURL(settings.customURL);
                    ServerInitialise.Init();
                }
            }, "WarpReinit", false, null);
        }
    }

    public static class UrlOverridePatches
    {
        public const string OVERRIDE_URL = "http://127.0.0.1:8082/";
    }

    [HarmonyPatch(typeof(ServerInitialise), nameof(ServerInitialise.Init))]
    static class Patch_ServerInit
    {
        static void Prefix()
        {
            if (WarpMod.settings.useCustom)
                RimConnectAPI.ChangeBaseURL(WarpMod.settings.customURL);
        }

        static void Postfix()
        {
            WarpMod.instance.WriteSettings();
        }
    }

    [HarmonyPatch(typeof(RimConnectSettings), nameof(RimConnectSettings.DoWindowContents))]
    static class Patch_RimConnectSettingsUI
    {
        static void Postfix(Rect rect)
        {
            float rowHeight = 24f;
            float gap = WidgetRow.LabelGap;
            float y = rect.y + rect.height - (rowHeight * 4 + gap * 3);
            float x = rect.x;
            float width = rect.width;

            Widgets.Label(new Rect(x, y, width, rowHeight), "<b>Server Override (Warp)</b>");
            y += rowHeight + gap;

            if (Widgets.RadioButtonLabeled(new Rect(x, y, width, rowHeight), "Default Server", !WarpMod.settings.useCustom))
                WarpMod.settings.useCustom = false;
            y += rowHeight + gap;

            if (Widgets.RadioButtonLabeled(new Rect(x, y, width, rowHeight), "Custom Server", WarpMod.settings.useCustom))
                WarpMod.settings.useCustom = true;
            y += rowHeight + gap;

            Widgets.Label(new Rect(x, y, 80f, rowHeight), "URL:");
            WarpMod.settings.customURL = Widgets.TextField(
                new Rect(x + 80f + gap, y, width - 80f - gap, rowHeight),
                WarpMod.settings.customURL);
        }
    }

    [HarmonyPatch(typeof(Mod), nameof(Mod.WriteSettings))]
    static class Patch_Mod_WriteSettings
    {
        static void Prefix(Mod __instance)
        {
            // Only write Warp settings if RimConnect's settings page is the one being closed
            if (__instance.GetType().FullName == "RimConnection.RimConnection")
            {
                WarpMod.instance.WriteSettings();
            }
        }
    }


}
