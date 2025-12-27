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
                { m_Setting.GetOptionTabLocaleID(Setting.StatusTab),  "Status" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab),   "About" },
 
                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.BehaviorGroup), "Taxi Usage" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup),    "Debug / Logging" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CityScanGroup), "CITY SCAN (updates ~every 60s)" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TaxiScanGroup), "TAXI SCAN" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LastUpdateGroup), "LAST UPDATE" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutInfoGroup),  "Info" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutLinksGroup), "Support Links" },
 
                // Druthers
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiUsage)), "Citizens - no taxi" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiUsage)),
                    "Prevents cims from choosing taxis.\n" +
                    "Also clears any cims currently waiting for a taxi so they re-route using other modes." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiStandDemand)), "Taxi stands: block stand demand" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiStandDemand)),
                    "Safer stand handling: clears TaxiStand \"Passengers waiting\"" +
                    "so TaxiStandSystem stops requesting taxis.\n" +
                "Hidden unless \"Cims Block taxi use\" is ON." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockCommuters)), "Commuters - no taxi" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockCommuters)), "When enabled, commuters are also prevented from using taxis." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTourists)), "Tourists - no taxi" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTourists)), "When enabled, tourists are also prevented from using taxis." },
 
                // Debug
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable verbose taxi logging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "When enabled, logs a periodic TaxiSummary line to help diagnose remaining taxi activity.\nDisable for normal gameplay." },
 
                // Status labels
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyPassengers1)), "Passengers (per month)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyPassengers1)), "Matches InfoView passenger table (tourists/citizens per month)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyPassengers2)), "Passengers (per month) (2)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyPassengers2)), "More InfoView passenger rows + Total." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiSupply)), "Taxi supply" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiSupply)),
                    "Counts of TAXI: depots, dispatch centers, and stands in the city." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusSnapshotMeta)), "Snapshot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusSnapshotMeta)), "How fresh the status is." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusRequests)), "Taxi requests" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusRequests)), "TaxiRequest counts by type." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiFleet)), "Taxi fleet" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiFleet)), "Taxi counts by state flags." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiFlags)), "Taxi flags" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiFlags)), "Outside/disabled/dispatch buffer counts." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusPassengers)), "Passengers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusPassengers)), "Taxi passenger sanity check." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWaiting)), "Waiting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWaiting)), "WaitingTransport total and TaxiStand passengers waiting." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusCoverage)), "Coverage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusCoverage)), "IgnoreTaxi coverage and household classification counts." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWorkDone1)), "Work done" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWorkDone1)), "What the system changed in the most recent simulation update." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWorkDone2)), "Work done (2)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWorkDone2)), "More work counters from the most recent simulation update." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiStands)), "Taxi stands" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiStands)), "Taxi stand count and stand passenger/request snapshot." },


                // About
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
