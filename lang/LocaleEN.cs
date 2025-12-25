// lang/LocaleEN.cs
// English (en-US) for Options UI.

namespace RiderControl
{
    using System.Collections.Generic;
    using Colossal;

    public sealed class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            string title = Mod.ShortName;

            if (!string.IsNullOrEmpty(Mod.ModVersion))
            {
                title = title + " (" + Mod.ModVersion + ")";
            }

            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), title },
 
                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.ActionsTab), "Actions" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "About" },
 
                // Groups - Actions
                { m_Setting.GetOptionGroupLocaleID(Setting.BehaviorGroup), "Behavior" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "Debug" },
 
                // Groups - About
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutInfoGroup), "Info" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutLinksGroup), "Support Links" },
 
                // Action toggle
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiUsage)), "Cims don't call taxis" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiUsage)),
                    "Prevents cims from choosing taxis.\n" +
                    "Also clears any cims currently waiting for a taxi so they re-route using other modes."
                },
 
                // Debug toggle
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable debug logging" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)),
                    "Logs a periodic taxi summary to help diagnose taxi behavior.\n" +
                    "Disable for normal gameplay."
                },
 
                // About tab
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameDisplay)), "Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameDisplay)), "Display name of this mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionDisplay)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionDisplay)), "Current mod version." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxMods)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxMods)), "Open Paradox Mods website for the author's mods." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscord)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscord)), "Open Discord community support in a browser." },
            };
        }

        public void Unload()
        {
        }
    }
}
