// File: lang/LocaleEN.cs
// English (en-US) for Options UI.

namespace RiderControl
{
    using Colossal;
    using System.Collections.Generic;

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
                { m_Setting.GetOptionGroupLocaleID(Setting.BehaviorGroup), "Behavior" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup),    "Debug / Logging" },

                // STATUS groups
                { m_Setting.GetOptionGroupLocaleID(Setting.CityScanGroup), "CITY TRANSIT (per month)" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TaxiScanGroup), "TAXI SCAN" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LastUpdateGroup), "LAST UPDATE" },

                // Status-only debug group (only appears in DEBUG builds)
                { m_Setting.GetOptionGroupLocaleID(Setting.AdvancedDebugGroup), "ADVANCED DEBUG (dev)" },

                { m_Setting.GetOptionGroupLocaleID(Setting.AboutInfoGroup),  "Info" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutLinksGroup), "Support Links" },

                // Behavior
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiUsage)), "Residents: ignore taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiUsage)),
                    "**Enabled [ ✓ ]** means **Residents** ignore taxis; reduces most local taxi use.\n" +
                    "Also clears cims currently waiting for a taxi so they re-route to other methods.\n" +
                    "Note: a very small number may still appear because some trip planners (e.g. leisure trips) can randomly allow taxi routing.\n" +
                    "Disabled = vanilla taxi use." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockCommuters)), "Commuters: ignore taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockCommuters)),
                    "**Enabled [ ✓ ]** means **Commuters** ignore taxis.\n" +
                    "Hidden and <disabled> unless [Residents: ignore taxis] is Enabled [ ✓ ].\n" +
                    "Even if you left Commuters checked, it will be OFF when Residents is OFF." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTourists)), "Tourists: ignore taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTourists)),
                    "**Enabled [ ✓ ]** means **Tourists** ignore taxis.\n" +
                    "Hidden and <disabled> unless [Residents: ignore taxis] is Enabled [ ✓ ].\n" +
                    "Even if you left Tourists checked, it will be OFF when Residents is OFF." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiStandDemand)), "Taxi stands: block demand (alpha)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiStandDemand)),
                    "This toggle is new (Alpha):\n" +
                    "Clears TaxiStand **Passengers waiting** so the stand stops requesting stand-by taxis.\n" +
                    "Hidden and disabled unless [Residents: ignore taxis] is enabled [ ✓ ].\n" +
                    "Even if you left taxi stands checked, it will be OFF when Residents is OFF." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MovingAwayFixHighwayWalkers)), "Moving-away fix (highway walkers)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MovingAwayFixHighwayWalkers)),
                    "Fixes Moving-Away cims with flag **Ignore Transport** so they can take public transport instead of walking all the way to an Outside Connection.\n" +
                    "Tip: add a direct Bus connection to an Outside Connection for best results." },

                // Debug
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable verbose taxi logging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)),
                    "When enabled, logs a periodic TaxiSummary line to help diagnose remaining taxi activity.\n" +
                    "Disable for normal gameplay or it will decrease performance." },

                // ----- STATUS TAB -----

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNotReadyCityScan)),
                  "City scan not available yet." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNotReadyCityScan)),
                  "Open a city and let the simulation run a few minutes, then reopen Options → Status." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNotReadyTaxiScan)),
                  "Taxi scan not available yet." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNotReadyTaxiScan)),
                  "Open a city and let the simulation run a few minutes, then reopen Options → Status." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNotReadyLastUpdate)),
                  "No activity recorded yet." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNotReadyLastUpdate)),
                  "Once a scan completes, this section will show what changed in the last update." },

                // CITY SCAN
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyPassengers1)), "Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyPassengers1)),
                    "Transportation InfoView passenger table (per month)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyTourists)), "Tourists" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyTourists)),
                    "Transportation InfoView passenger table (per month)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyTotal)), "All Types" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyTotal)), "Total passengers per month + total riders currently waiting." },

                // TAXI SCAN
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiSupply)), "Taxi supply" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiSupply)), "Total taxis, depots, dispatch centers, and stands in the city." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusRequests)), "Taxi requests" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusRequests)), "TaxiRequest counts by type." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusPassengers)), "Passengers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusPassengers)), "Taxi passenger sanity check." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiFleet)), "State" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiFleet)), "What taxis are doing now." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiFlags)), "Taxi flags (basic)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiFlags)), "Counts for FromOutside and Disabled taxis." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiStands)), "Taxi stands" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiStands)), "How many are waiting at taxi stands, and stand-driven request count." },

                // LAST UPDATE

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusCoverage1)), "Ignore Flags" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusCoverage1)), "Ignore Taxi coverage by Resident citizens." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusCoverage2)), "Ignore Flags" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusCoverage2)), "Ignore Taxi coverage by Commuters and Tourists ." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWorkDone1)), "Work done" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWorkDone1)), "What the system changed per last update." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWorkDone2)), "Work done (2)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWorkDone2)), "More work counters per last update." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusSnapshotMeta)), "Snapshot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusSnapshotMeta)),
                    "Snapshot: when status was updated and how old it is.\n" +
                    "Snapshot auto-refresh is requested when the Status page is read (throttled)." },

#if DEBUG
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusDebugDispatchQueue)), "Dispatch queue (ServiceDispatch buffer)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusDebugDispatchQueue)),
                    "DEBUG ONLY.\n" +
                    "Counts taxis whose ServiceDispatch dynamic buffer is non-empty.\n" +
                    "Useful for diagnosing dispatch backlog / stuck taxi behavior. Not a gameplay metric." },
#endif

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
