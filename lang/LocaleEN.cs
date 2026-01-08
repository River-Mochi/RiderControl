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

                { m_Setting.GetOptionGroupLocaleID(Setting.CityScanGroup), "CITY TRANSIT (per month)" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TaxiScanGroup), "TAXI SCAN" },
                { m_Setting.GetOptionGroupLocaleID(Setting.LastUpdateGroup), "LAST UPDATE" },

#if DEBUG
                { m_Setting.GetOptionGroupLocaleID(Setting.AdvancedDebugGroup), "ADVANCED DEBUG (DEV)" },
#endif

                { m_Setting.GetOptionGroupLocaleID(Setting.AboutInfoGroup),  "Info" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AboutLinksGroup), "Support Links" },

                // Behavior
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiUsage)), "Residents: ignore taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiUsage)),
                    "**Enabled [ ✓ ]** means **Residents** ignore taxis.\n" +
                    "Also clears cims currently waiting for a taxi so they re-route to other methods.\n" +
                    "Note: a very small number may still appear because some trip planners like Leisure System can randomly allow taxi routing.\n" +
                    "Disabled = vanilla taxi use." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockCommuters)), "Commuters: ignore taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockCommuters)),
                    "**Enabled [ ✓ ]** means **Commuters** ignore taxis.\n" +
                    "Hidden and <disabled> unless [Residents: ignore taxis] is Enabled [ ✓ ]\n" +
                    "Even if you left Commuters checked, it will be OFF when Residents is OFF." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTourists)), "Tourists: ignore taxis" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTourists)),
                    "**Enabled [ ✓ ]** means **Tourists** ignore taxis.\n" +
                    "Hidden and <disabled> unless [Residents: ignore taxis] is Enabled [ ✓ ]\n" +
                    "Even if you left Tourists checked, it will be OFF when Residents toggle is OFF." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiStandDemand)), "Taxi stands: block demand (alpha)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiStandDemand)),
                    "This toggle is new (Alpha):\n" +
                    "Clears TaxiStand **Passengers waiting** so the stand stops requesting stand-by taxis.\n" +
                    "Hidden and disabled unless [Residents: ignore taxis] is enabled [ ✓ ]\n" +
                    "Even if you left taxi stands checked, it will be OFF when Residents is OFF." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FixMovingAwayHighwayWalkers)), "Moving-away fix (highway walkers)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FixMovingAwayHighwayWalkers)),
                    "Fixes Moving-Away cims with flag **Ignore Transport** so they can take public transport instead of walking to the outside connection.\n" +
                    "Tip: add a direct bus connection to outside connection for best results." },

                // Debug
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable verbose taxi logging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)),
                    "When enabled, logs a periodic TaxiSummary line to help debug remaining taxi activity.\n" +
                    "Disable for normal gameplay or it can hurt performance." },

                // ----- STATUS TAB -----

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNotReadyCityScan)),
                  "City scan not available yet." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNotReadyCityScan)),
                  "Open a city and let the simulation run a few minutes, then reopen Options → Status.\n" +
                  "The value will show '-' until stats are ready." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNotReadyTaxiScan)),
                  "Taxi scan not available yet." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNotReadyTaxiScan)),
                  "Open a city and let the simulation run a few minutes.\n" +
                  "The value will show '-' until stats are ready." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusNotReadyLastUpdate)),
                  "No activity recorded yet." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusNotReadyLastUpdate)),
                  "Once a scan completes, this section shows what changed in the last update.\n" +
                  "The value will show '-' until activity exists." },

                // CITY SCAN (values are numbers-only)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyPassengers1)), "Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyPassengers1)),
                    "InfoView passenger table (per month).\n" +
                    "Order: Taxi | Bus | Tram | Train | Subway | Air." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyTourists)), "Tourists" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyTourists)),
                    "InfoView passenger table (per month).\n" +
                    "Order: Taxi | Bus | Tram | Train | Subway | Air." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusMonthlyTotal)), "Totals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusMonthlyTotal)),
                    "Order: Riders waiting now | Total/mo tourists | Total/mo citizens." },

                // TAXI SCAN (values are numbers-only)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiSupply)), "Taxi supply" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiSupply)),
                    "Order: Taxis | Depots | Dispatch centers | Stands" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusRequests)), "Taxi requests" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusRequests)),
                    "TaxiRequest counts by type.\n" +
                    "Order: Customer | Outside | None." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusPassengers)), "Passengers" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusPassengers)),
                    "Taxi passenger sanity check.\n" +
                    "Order: Total | IgnoreTaxi | Passengers with Resident." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiFleet)), "Taxi states" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiFleet)),
                    "What taxis are doing now.\n" +
                    "Order: Transport | Boarding | Return | Dispatched | En route | Parked" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusTaxiStands)), "Taxi stands" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusTaxiStands)),
                    "Stand pressure.\n" +
                    "Order: Waiting at stands | Stand requests." },

                // LAST UPDATE (values are numbers-only)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusCoverage1)), "IgnoreTaxi coverage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusCoverage1)),
                    "Order: Residents IgnoreTaxi | Total" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusCoverage2)), "IgnoreTaxi coverage (groups)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusCoverage2)),
                    "Order: IgnoreTaxi commuters | Total | IgnoreTaxi tourists | Total." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWorkDone1)), "Work done" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWorkDone1)),
                    "What changed in the last update.\n" +
                    "Order: IgnoreTaxi applied | RideNeeder cleared | Taxi-lane waiting cleared." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusWorkDone2)), "Work done (2)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusWorkDone2)),
                    "More counters from the last update.\n" +
                    "Order: Taxi-stand waiting cleared | Commuters skipped | Tourists skipped." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusSnapshotMeta)), "Snapshot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusSnapshotMeta)),
                    "Order: Last update time | Age.\n" +
                    "Status only updates while Options UI is open (throttled); has no constant in-City cost." },

#if DEBUG
                // Advanced Debug (DEV builds only) — values are numbers-only
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusDebugMarkedCoverage)), "Marked / coverage (dev)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusDebugMarkedCoverage)),
                    "DEV sanity check.\n" +
                    "Order: Marked by this mod | IgnoreTaxi now | Total residents." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.StatusDebugTaxiFlags)), "Taxi flags (dev)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.StatusDebugTaxiFlags)),
                    "DEV sanity check.\n" +
                    "Order: With dispatch buffer | From outside | Disabled." },
#endif

                // About
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameDisplay)), "Mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameDisplay)), "Display name of this mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionDisplay)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionDisplay)), "Current mod version." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxMods)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxMods)), "Opens Paradox Mods website for the author's mods." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscord)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscord)), "Opens Discord community support in a browser." },
            };
        }

        public void Unload()
        {
        }
    }
}
