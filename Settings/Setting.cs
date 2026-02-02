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

#if DEBUG
    [FileLocation("ModsSettings/SmartTraveler/SmartTraveler")]
    [SettingsUIGroupOrder(
        BehaviorGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        AdvancedDebugGroup,
        AboutInfoGroup,
        DebugGroup,
        AboutLinksGroup
    )]
    [SettingsUIShowGroupName(
        BehaviorGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        AdvancedDebugGroup,
        DebugGroup,
        AboutLinksGroup
    )]
#else
    [FileLocation("ModsSettings/SmartTraveler/SmartTraveler")]
    [SettingsUIGroupOrder(
        BehaviorGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        AboutInfoGroup,
        DebugGroup,
        AboutLinksGroup
    )]
    [SettingsUIShowGroupName(
        BehaviorGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        DebugGroup,
        AboutLinksGroup
    )]
#endif
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

        // Status (DEBUG builds only)
        public const string AdvancedDebugGroup = "AdvancedDebug";

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

        // Independent fix (player choice): moving-away walkers.
        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool FixMovingAwayHighwayWalkers
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

        // Moved to About tab (requested).
        [SettingsUISection(AboutTab, DebugGroup)]
        public bool EnableDebugLogging
        {
            get; set;
        }

        // ---- Status ----

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

        // CITY SCAN (labels restored inside value)
        [SettingsUISection(StatusTab, CityScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusMonthlyPassengers1
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Taxi {RiderControlSystem.s_InfoTaxiCitizen:N0} | Bus {RiderControlSystem.s_InfoBusCitizen:N0} | Tram {RiderControlSystem.s_InfoTramCitizen:N0}\n" +
                    $"Train {RiderControlSystem.s_InfoTrainCitizen:N0} | Subway {RiderControlSystem.s_InfoSubwayCitizen:N0} | Air {RiderControlSystem.s_InfoAirCitizen:N0}";
            }
        }

        [SettingsUISection(StatusTab, CityScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusMonthlyTourists
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Taxi {RiderControlSystem.s_InfoTaxiTourist:N0} | Bus {RiderControlSystem.s_InfoBusTourist:N0} | Tram {RiderControlSystem.s_InfoTramTourist:N0}\n" +
                    $"Train {RiderControlSystem.s_InfoTrainTourist:N0} | Subway {RiderControlSystem.s_InfoSubwayTourist:N0} | Air {RiderControlSystem.s_InfoAirTourist:N0}";
            }
        }

        [SettingsUISection(StatusTab, CityScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusMonthlyTotal
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"WaitNow {RiderControlSystem.s_StatusWaitingTransportTotal:N0} | Tour/mo {RiderControlSystem.s_InfoTotalTourist:N0} | Cit/mo {RiderControlSystem.s_InfoTotalCitizen:N0}";
            }
        }

        // TAXI SCAN (labels restored inside value)

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiSupply
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Taxis {RiderControlSystem.s_StatusTaxisTotal:N0} | Depots {RiderControlSystem.s_StatusTaxiDepotsTotal:N0} | Dispatch {RiderControlSystem.s_StatusTaxiDepotsWithDispatchCenter:N0} | Stands {RiderControlSystem.s_StatusTaxiStandsTotal:N0}";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusPassengers
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Total {RiderControlSystem.s_StatusPassengerTotal:N0} | IgnoreTaxi {RiderControlSystem.s_StatusPassengerIgnoreTaxi:N0} | HasResident {RiderControlSystem.s_StatusPassengerHasResident:N0}";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusRequests
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Cust {RiderControlSystem.s_StatusReqCustomer:N0} | Outside {RiderControlSystem.s_StatusReqOutside:N0} | None {RiderControlSystem.s_StatusReqNone:N0}";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiFleet
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Trans {RiderControlSystem.s_StatusTaxiTransporting:N0} | Board {RiderControlSystem.s_StatusTaxiBoarding:N0} | Return {RiderControlSystem.s_StatusTaxiReturning:N0}\n" +
                    $"Dispatch {RiderControlSystem.s_StatusTaxiDispatched:N0} | EnRoute {RiderControlSystem.s_StatusTaxiEnRoute:N0} | Parked {RiderControlSystem.s_StatusTaxiParked:N0}";
            }
        }

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiStands
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Waiting {RiderControlSystem.s_StatusWaitingTaxiStandTotal:N0} | StandReq {RiderControlSystem.s_StatusReqStand:N0}";
            }
        }

        // LAST UPDATE (labels restored inside value)

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusCoverage1
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"IgnoreTaxi {RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusResidentsTotal:N0}";
            }
        }

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusCoverage2
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Comm {RiderControlSystem.s_StatusCommutersIgnoreTaxi:N0}/{RiderControlSystem.s_StatusCommutersTotal:N0} | " +
                    $"Tour {RiderControlSystem.s_StatusTouristsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusTouristsTotal:N0}";
            }
        }

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusWorkDone1
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"Applied {RiderControlSystem.s_StatusLastAppliedIgnoreTaxi:N0} | " +
                    $"RideClear {RiderControlSystem.s_StatusLastRemovedRideNeeder:N0} | " +
                    $"LaneClear {RiderControlSystem.s_StatusLastClearedTaxiLaneWaiting:N0}";
            }
        }

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusWorkDone2
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"StandClear {RiderControlSystem.s_StatusLastClearedTaxiStandWaiting:N0} | " +
                    $"SkipComm {RiderControlSystem.s_StatusLastSkippedCommuters:N0} | " +
                    $"SkipTour {RiderControlSystem.s_StatusLastSkippedTourists:N0}";
            }
        }

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusSnapshotMeta
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"At {RiderControlSystem.GetStatusLastStampText()} | Age {RiderControlSystem.GetStatusAgeText()}";
            }
        }

#if DEBUG
        // ---- Status → Advanced Debug (DEV builds only) ----

        [SettingsUISection(StatusTab, AdvancedDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusDebugMarkedCoverage
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"Marked {RiderControlSystem.s_StatusResidentsForcedMarker:N0} | IgnoreTaxi {RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0} | Total {RiderControlSystem.s_StatusResidentsTotal:N0}";
            }
        }

        [SettingsUISection(StatusTab, AdvancedDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusDebugTaxiFlags
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"DispatchBuf {RiderControlSystem.s_StatusTaxiWithDispatchBuffer:N0} | Outside {RiderControlSystem.s_StatusTaxiFromOutside:N0} | Disabled {RiderControlSystem.s_StatusTaxiDisabled:N0}";
            }
        }
#endif

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
                try { Application.OpenURL(UrlParadox); }
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
                try { Application.OpenURL(UrlDiscord); }
                catch (Exception) { }
            }
        }

        public override void SetDefaults()
        {
            BlockTaxiUsage = true;
            BlockCommuters = true;
            BlockTourists = true;
            BlockTaxiStandDemand = true;

            FixMovingAwayHighwayWalkers = false;

            EnableDebugLogging = false;
        }

        // Used by SettingsUIHideByCondition.
        public bool IsTaxiBlockingOff()
        {
            return !BlockTaxiUsage;
        }
    }
}
