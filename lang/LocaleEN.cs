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

            // Show "Smart Traveler (0.5.0)" style title
            if (!string.IsNullOrEmpty(Mod.ModVersion))
            {
                title = title + " (" + Mod.ModVersion + ")";
            }

            return new Dictionary<string, string>
            {
                // Mod name in options
                { m_Setting.GetSettingsLocaleID(), title },
 
                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.ActionsTab), "Actions" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab),   "About" },
 
                // Groups - Actions
                { m_Setting.GetOptionGroupLocaleID(Setting.BehaviorGroup), "Druthers" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup),    "Debug / Logging" },
                { m_Setting.GetOptionGroupLocaleID(Setting.StatusGroup),   "Status (updates ~every 10s)" },
 
                // Groups - About
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutInfoGroup),  "Info" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutLinksGroup), "Support Links" },
 
                // ----------------------------
                // Actions tab - Druthers
                // ----------------------------
 
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiUsage)), "Cims Block taxi use" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiUsage)),
                    "Prevents cims from choosing taxis.\n" +
                    "Also clears any cims currently waiting for a taxi so they re-route using other modes."
                },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockCommutersToo)), "Commuters block taxi use" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockCommutersToo)),
                    "When enabled, commuters (CitizenFlags.Commuter) are also prevented from using taxis."
                },
 
                // ----------------------------
                // Actions tab - Debug / Logging
                // ----------------------------
 
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable verbose taxi logging" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)),
                    "When enabled, logs a periodic TaxiSummary line to help diagnose remaining taxi activity.\n" +
                    "Disable for normal gameplay."
                },
 
                // ----------------------------
                // Actions tab - Status (compact)
                // ----------------------------


                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusSnapshotMeta)), "Snapshot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusSnapshotMeta)), "How fresh the status is." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusResidents)), "Residents" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusResidents)), "IgnoreTaxi coverage and how many residents are marked by this mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusCommuters)), "Commuters" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusCommuters)), "Commuter coverage and whether commuters are included." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusRequests)), "Taxi requests" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusRequests)), "TaxiRequest counts by type." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiFleet)), "Taxi fleet" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiFleet)), "Taxi counts by state flags." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiFlags)), "Taxi flags" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiFlags)), "Outside/disabled/dispatch buffer counts." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusPassengers)), "Passengers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusPassengers)), "Sanity check: are IgnoreTaxi residents still in taxi passenger buffers?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWaiting)), "Waiting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWaiting)), "WaitingTransport total and the TaxiStand subset." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusLastUpdate)), "Last update (work done)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusLastUpdate)), "What the system changed in the most recent simulation update." },
 
                // ----------------------------
                // About tab - Info
                // ----------------------------
 
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameDisplay)), "Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameDisplay)), "Display name of this mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionDisplay)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionDisplay)), "Current mod version." },
 
                // ----------------------------
                // About tab - Links
                // ----------------------------
 
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
