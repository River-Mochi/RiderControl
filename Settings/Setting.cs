// Settings/Setting.cs
// Options UI for "Rider Control".
// All user-facing strings are in lang/LocaleEN.cs.

namespace RiderControl
{
    using System;
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;
    using UnityEngine;

    [FileLocation("ModsSettings/RiderControl/RiderControl")]
    [SettingsUIGroupOrder(
        BehaviorGroup,
        DebugGroup,
        AboutInfoGroup,
        AboutLinksGroup
    )]
    [SettingsUIShowGroupName(
        BehaviorGroup,
        DebugGroup,
        AboutLinksGroup
    )]
    public sealed class Setting : ModSetting
    {
        // ---- Tabs ----
        public const string ActionsTab = "Actions";
        public const string AboutTab = "About";

        // ---- Groups ----
        public const string BehaviorGroup = "Behavior";
        public const string DebugGroup = "Debug";
        public const string AboutInfoGroup = "Info";
        public const string AboutLinksGroup = "Support Links";

        // ---- External links ----
        private const string UrlParadox =
            "https://mods.paradoxplaza.com/authors/River-mochi/cities_skylines_2?games=cities_skylines_2&orderBy=desc&sortBy=best&time=alltime";

        private const string UrlDiscord = "https://discord.gg/HTav7ARPs2";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        // ---- Actions tab ----

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTaxiUsage
        {
            get; set;
        }

        [SettingsUISection(ActionsTab, DebugGroup)]
        public bool EnableDebugLogging
        {
            get; set;
        }

        // ---- About tab ----

        [SettingsUISection(AboutTab, AboutInfoGroup)]
        public string NameDisplay => Mod.ModName;

        [SettingsUISection(AboutTab, AboutInfoGroup)]
        public string VersionDisplay => Mod.ModVersion;

        [SettingsUIButtonGroup(AboutLinksGroup)]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, AboutLinksGroup)]
        public bool OpenParadoxMods
        {
            set
            {
                if (!value)
                {
                    return;
                }

                try
                {
                    Application.OpenURL(UrlParadox);
                }
                catch (Exception)
                {
                }
            }
        }

        [SettingsUIButtonGroup(AboutLinksGroup)]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, AboutLinksGroup)]
        public bool OpenDiscord
        {
            set
            {
                if (!value)
                {
                    return;
                }

                try
                {
                    Application.OpenURL(UrlDiscord);
                }
                catch (Exception)
                {
                }
            }
        }

        public override void SetDefaults()
        {
            BlockTaxiUsage = true;
            EnableDebugLogging = false;
        }
    }
}
