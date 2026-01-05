// File: Settings/Setting.cs
// Options UI for "Smart Traveler".
// All user-facing strings are in lang/LocaleEN.cs.

namespace RiderControl
{
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;
    using System;
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

        public const string BehaviorGroup = "Behavior";
        public const string DebugGroup = "Debug";
        public const string CityScanGroup = "CityScan";
        public const string TaxiScanGroup = "TaxiScan";
        public const string LastUpdateGroup = "LastUpdate";

        public const string AboutInfoGroup = "Info";
        public const string AboutLinksGroup = "Support Links";

        private const string UrlParadox =
            "https://mods.paradoxplaza.com/authors/River-mochi/cities_skylines_2?games=cities_skylines_2&orderBy=desc&sortBy=best&time=alltime";

        private const string UrlDiscord = "https://discord.gg/HTav7ARPs2";

        private bool m_BlockTaxiUsage = true;

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        // ---- Actions ----

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTaxiUsage
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead(); // Status update on Options-open (throttled)
                return m_BlockTaxiUsage;
            }
            set
            {
                m_BlockTaxiUsage = value;

                if (!m_BlockTaxiUsage)
                {
                    BlockTaxiStandDemand = false;
                    BlockCommuters = false;
                    BlockTourists = false;
                }
            }
        }

        [SettingsUIHideByCondition(typeof(Setting), nameof(IsTaxiBlockingOff))]
        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockCommuters
        {
            get; set;
        }

        [SettingsUIHideByCondition(typeof(Setting), nameof(IsTaxiBlockingOff))]
        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTourists
        {
            get; set;
        }

        // Alpha phase: disables TaxiStand-driven taxi demand by clearing TaxiStand WaitingPassengers.
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsTaxiBlockingOff))]
        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTaxiStandDemand
        {
            get; set;
        }

        public bool IsStatusReady()
        {
            return RiderControlSystem.s_StatusLastSnapshotRealtime > 0.0;
        }

        public bool IsStatusNotReady()
        {
            return !IsStatusReady();
        }



        [SettingsUISection(ActionsTab, DebugGroup)]
        public bool EnableDebugLogging
        {
            get; set;
        }

        // ---- Status ----

        // Button: manual refresh (also happens automatically when Status rows are read, throttled).
        [SettingsUIButton]
        [SettingsUISection(StatusTab, CityScanGroup)]
        public bool RefreshStatus
        {
            set
            {
                if (!value)
                    return;

                RiderControlSystem.RequestStatusRefresh(force: true);
            }
        }

        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusReady))]
        [SettingsUISection(StatusTab, CityScanGroup)]
        public string StatusNotReadyCityScan
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return string.Empty;
            }
        }

        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusReady))]
        [SettingsUISection(StatusTab, TaxiScanGroup)]
        public string StatusNotReadyTaxiScan
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return string.Empty;
            }
        }

        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusReady))]
        [SettingsUISection(StatusTab, LastUpdateGroup)]
        public string StatusNotReadyLastUpdate
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return string.Empty;
            }
        }


        // CITY SCAN

        [SettingsUISection(StatusTab, CityScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusMonthlyPassengers1
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"Taxi {RiderControlSystem.s_InfoTaxiCitizen:N0} | Bus {RiderControlSystem.s_InfoBusCitizen:N0} | Tram {RiderControlSystem.s_InfoTramCitizen:N0} | Subway {RiderControlSystem.s_InfoSubwayCitizen:N0} | Train {RiderControlSystem.s_InfoTrainCitizen:N0}";
            }
        }

        [SettingsUISection(StatusTab, CityScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusMonthlyTourists
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"Taxi {RiderControlSystem.s_InfoTaxiTourist:N0} | Bus {RiderControlSystem.s_InfoBusTourist:N0} | Tram {RiderControlSystem.s_InfoTramTourist:N0} | Subway {RiderControlSystem.s_InfoSubwayTourist:N0} | Train {RiderControlSystem.s_InfoTrainTourist:N0}";
            }
        }

        [SettingsUISection(StatusTab, CityScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusMonthlyTotal
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"Waiting Transport {RiderControlSystem.s_StatusWaitingTransportTotal:N0} | All Transit Use {RiderControlSystem.s_InfoTotalTourist:N0}T/ {RiderControlSystem.s_InfoTotalCitizen:N0}C";
            }
        }

        // TAXI SCAN

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiSupply
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.s_StatusTaxisTotal:N0} TAXIS | {RiderControlSystem.s_StatusTaxiDepotsTotal:N0} DEPOTS | {RiderControlSystem.s_StatusTaxiDepotsWithDispatchCenter:N0} DispatchCenter | {RiderControlSystem.s_StatusTaxiStandsTotal:N0} STANDS";
            }
        }


        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusRequests
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.s_StatusReqCustomer:N0} Customer | {RiderControlSystem.s_StatusReqOutside:N0} Outside | {RiderControlSystem.s_StatusReqNone:N0} None";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusPassengers
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.s_StatusPassengerTotal:N0} Total | {RiderControlSystem.s_StatusPassengerIgnoreTaxi:N0}/{RiderControlSystem.s_StatusPassengerHasResident:N0} Resident (IgnoreTaxi)";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiFleet
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"Transport {RiderControlSystem.s_StatusTaxiTransporting:N0} | Boarding {RiderControlSystem.s_StatusTaxiBoarding:N0} | Return {RiderControlSystem.s_StatusTaxiReturning:N0} | Dispatch {RiderControlSystem.s_StatusTaxiDispatched:N0} | EnRoute {RiderControlSystem.s_StatusTaxiEnRoute:N0} | Parked {RiderControlSystem.s_StatusTaxiParked:N0}";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiFlags
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.s_StatusTaxiWithDispatchBuffer:N0} WithDispatch | {RiderControlSystem.s_StatusTaxiFromOutside:N0} FromOutside | {RiderControlSystem.s_StatusTaxiDisabled:N0} Disabled";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiStands
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.s_StatusWaitingTaxiStandTotal:N0} Waiting at stand | {RiderControlSystem.s_StatusReqStand:N0} Stand Requests (for up to 3 parked taxis)";
            }
        }


        // LAST UPDATE

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusCoverage
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"Residents {RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusResidentsTotal:N0} | Marked {RiderControlSystem.s_StatusResidentsForcedMarker:N0} | Commuters {RiderControlSystem.s_StatusCommutersIgnoreTaxi:N0}/{RiderControlSystem.s_StatusCommutersTotal:N0} | Tourists {RiderControlSystem.s_StatusTouristsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusTouristsTotal:N0}";
            }
        }

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusWorkDone1 =>
            $"IgnoreTaxiApplied {RiderControlSystem.s_StatusLastAppliedIgnoreTaxi:N0} | RideNeederRemoved {RiderControlSystem.s_StatusLastRemovedRideNeeder:N0} | TaxiLaneCleared {RiderControlSystem.s_StatusLastClearedTaxiLaneWaiting:N0}";

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusWorkDone2 =>
            $"TaxiStandCleared {RiderControlSystem.s_StatusLastClearedTaxiStandWaiting:N0} | CommutersSkipped {RiderControlSystem.s_StatusLastSkippedCommuters:N0} | TouristsSkipped {RiderControlSystem.s_StatusLastSkippedTourists:N0}";



        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusSnapshotMeta =>
            $"Last {RiderControlSystem.GetStatusLastStampText()} | Age {RiderControlSystem.GetStatusAgeText()}";

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
            BlockCommuters = true;
            BlockTourists = true;
            BlockTaxiStandDemand = true;
            EnableDebugLogging = false;
        }

        // Used by SettingsUIHideByCondition.
        public bool IsTaxiBlockingOff()
        {
            return !BlockTaxiUsage;
        }
    }
}
