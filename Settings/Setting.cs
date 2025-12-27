// Settings/Setting.cs
// Options UI for "Smart Traveler".
// All user-facing strings are in lang/LocaleEN.cs.

namespace RiderControl
{
    using System;
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;
    using UnityEngine;

    [FileLocation("ModsSettings/SmartTraveler/SmartTraveler")]
    [SettingsUIGroupOrder(
        BehaviorGroup,
        DebugGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        AboutInfoGroup,
        AboutLinksGroup
    )]
    [SettingsUIShowGroupName(
        BehaviorGroup,
        DebugGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        AboutLinksGroup
    )]
    public sealed class Setting : ModSetting
    {
        public const string ActionsTab = "Actions";
        public const string StatusTab = "Status";
        public const string AboutTab = "About";

        public const string BehaviorGroup = "Druthers";
        public const string DebugGroup = "Debug";
        public const string CityScanGroup = "CityScan";
        public const string TaxiScanGroup = "TaxiScan";
        public const string LastUpdateGroup = "LastUpdate";

        public const string AboutInfoGroup = "Info";
        public const string AboutLinksGroup = "Support Links";

        private const string UrlParadox =
            "https://mods.paradoxplaza.com/authors/River-mochi/cities_skylines_2?games=cities_skylines_2&orderBy=desc&sortBy=best&time=alltime";

        private const string UrlDiscord = "https://discord.gg/HTav7ARPs2";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        // ---- Actions ----

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTaxiUsage
        {
            get; set;
        }

        // Phase B (optional): disables TaxiStand-driven taxi demand by clearing TaxiStand WaitingPassengers.
        // This is only meaningful when BlockTaxiUsage is enabled, so hide it otherwise.
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsTaxiBlockingOff))]
        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTaxiStandDemand
        {
            get; set;
        }

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockCommuters
        {
            get; set;
        }

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTourists
        {
            get; set;
        }

        [SettingsUISection(ActionsTab, DebugGroup)]
        public bool EnableDebugLogging
        {
            get; set;
        }

        // ---- Status ----

        // CITY SCAN

        [SettingsUISection(StatusTab, CityScanGroup)]
        public string StatusMonthlyPassengers1 =>
            $"Taxi {RiderControlSystem.s_InfoTaxiTourist:N0}T/{RiderControlSystem.s_InfoTaxiCitizen:N0}C | Bus {RiderControlSystem.s_InfoBusTourist:N0}T/{RiderControlSystem.s_InfoBusCitizen:N0}C | Tram {RiderControlSystem.s_InfoTramTourist:N0}T/{RiderControlSystem.s_InfoTramCitizen:N0}C | Subway {RiderControlSystem.s_InfoSubwayTourist:N0}T/{RiderControlSystem.s_InfoSubwayCitizen:N0}C";

        [SettingsUISection(StatusTab, CityScanGroup)]
        public string StatusMonthlyPassengers2 =>
            $"Train {RiderControlSystem.s_InfoTrainTourist:N0}T/{RiderControlSystem.s_InfoTrainCitizen:N0}C | Ship {RiderControlSystem.s_InfoShipTourist:N0}T/{RiderControlSystem.s_InfoShipCitizen:N0}C | Ferry {RiderControlSystem.s_InfoFerryTourist:N0}T/{RiderControlSystem.s_InfoFerryCitizen:N0}C | Air {RiderControlSystem.s_InfoAirTourist:N0}T/{RiderControlSystem.s_InfoAirCitizen:N0}C | Total {RiderControlSystem.s_InfoTotalTourist:N0}T/{RiderControlSystem.s_InfoTotalCitizen:N0}C";

        [SettingsUISection(StatusTab, CityScanGroup)]
        public string StatusWaiting =>
            $"WaitingTransport {RiderControlSystem.s_StatusWaitingTransportTotal:N0} | TaxiStandPassengers {RiderControlSystem.s_StatusWaitingTaxiStandTotal:N0}";

        [SettingsUISection(StatusTab, CityScanGroup)]
        public string StatusTaxiSupply =>
            $"Taxis {RiderControlSystem.s_StatusTaxisTotal:N0} | Depots {RiderControlSystem.s_StatusTaxiDepotsTotal:N0} | DispatchCenters {RiderControlSystem.s_StatusTaxiDepotsWithDispatchCenter:N0} | Stands {RiderControlSystem.s_StatusTaxiStandsTotal:N0}";

        // TAXI SCAN

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        public string StatusRequests =>
            $"Customer {RiderControlSystem.s_StatusReqCustomer:N0} | Outside {RiderControlSystem.s_StatusReqOutside:N0} | None {RiderControlSystem.s_StatusReqNone:N0} | Stand {RiderControlSystem.s_StatusReqStand:N0}";

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        public string StatusPassengers =>
            $"Passengers {RiderControlSystem.s_StatusPassengerTotal:N0} | ResidentIgnoreTaxi {RiderControlSystem.s_StatusPassengerIgnoreTaxi:N0}/{RiderControlSystem.s_StatusPassengerHasResident:N0}";

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        public string StatusTaxiFleet =>
            $"Transporting {RiderControlSystem.s_StatusTaxiTransporting:N0} | Boarding {RiderControlSystem.s_StatusTaxiBoarding:N0} | Returning {RiderControlSystem.s_StatusTaxiReturning:N0} | Dispatched {RiderControlSystem.s_StatusTaxiDispatched:N0} | EnRoute {RiderControlSystem.s_StatusTaxiEnRoute:N0} | Parked {RiderControlSystem.s_StatusTaxiParked:N0} | Accident {RiderControlSystem.s_StatusTaxiAccident:N0}";

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        public string StatusTaxiFlags =>
            $"FromOutside {RiderControlSystem.s_StatusTaxiFromOutside:N0} | Disabled {RiderControlSystem.s_StatusTaxiDisabled:N0} | WithDispatch {RiderControlSystem.s_StatusTaxiWithDispatchBuffer:N0}";

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        public string StatusTaxiStands =>
            $"Stands {RiderControlSystem.s_StatusTaxiStandsTotal:N0} | StandPassengers {RiderControlSystem.s_StatusWaitingTaxiStandTotal:N0} | StandRequests {RiderControlSystem.s_StatusReqStand:N0}";

        // LAST UPDATE

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        public string StatusCoverage =>
            $"Residents {RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusResidentsTotal:N0} | Marked {RiderControlSystem.s_StatusResidentsForcedMarker:N0} | Commuters {RiderControlSystem.s_StatusCommutersIgnoreTaxi:N0}/{RiderControlSystem.s_StatusCommutersTotal:N0} | Tourists {RiderControlSystem.s_StatusTouristsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusTouristsTotal:N0}";

        // Split into 2 rows so it fits nicely
        [SettingsUISection(StatusTab, LastUpdateGroup)]
        public string StatusWorkDone1 =>
            $"IgnoreTaxiApplied {RiderControlSystem.s_StatusLastAppliedIgnoreTaxi:N0} | RideNeederRemoved {RiderControlSystem.s_StatusLastRemovedRideNeeder:N0} | TaxiLaneCleared {RiderControlSystem.s_StatusLastClearedTaxiLaneWaiting:N0}";

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        public string StatusWorkDone2 =>
            $"TaxiStandCleared {RiderControlSystem.s_StatusLastClearedTaxiStandWaiting:N0} | CommutersSkipped {RiderControlSystem.s_StatusLastSkippedCommuters:N0} | TouristsSkipped {RiderControlSystem.s_StatusLastSkippedTourists:N0}";

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        public string StatusSnapshotMeta =>
            $"Snapshots {RiderControlSystem.s_StatusSnapshotCount:N0} | Age {RiderControlSystem.s_StatusSecondsSinceSnapshot:0.0}s";

        // ---- About ----

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
                    return;
                try
                {
                    Application.OpenURL(UrlParadox);
                }
                catch (Exception) { }
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
                    return;
                try
                {
                    Application.OpenURL(UrlDiscord);
                }
                catch (Exception) { }
            }
        }

        public override void SetDefaults()
        {
            BlockTaxiUsage = true;
            BlockTaxiStandDemand = true;
            BlockCommuters = true;
            BlockTourists = true;
            EnableDebugLogging = false;
        }

        // Used by SettingsUIHideByCondition.
        public bool IsTaxiBlockingOff()
        {
            return !BlockTaxiUsage;
        }
    }
}
