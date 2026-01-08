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
        DebugGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        AdvancedDebugGroup,
        AboutInfoGroup,
        AboutLinksGroup
    )]
    [SettingsUIShowGroupName(
        BehaviorGroup,
        DebugGroup,
        CityScanGroup,
        TaxiScanGroup,
        LastUpdateGroup,
        AdvancedDebugGroup,
        AboutLinksGroup
    )]
#else
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

        [SettingsUISection(ActionsTab, DebugGroup)]
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

        // CITY SCAN (numbers-only; meanings in LocaleEN)
        [SettingsUISection(StatusTab, CityScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusMonthlyPassengers1
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"{RiderControlSystem.s_InfoTaxiCitizen:N0} | " +
                    $"{RiderControlSystem.s_InfoBusCitizen:N0} | " +
                    $"{RiderControlSystem.s_InfoTramCitizen:N0} | " +
                    $"{RiderControlSystem.s_InfoTrainCitizen:N0} | " +
                    $"{RiderControlSystem.s_InfoSubwayCitizen:N0} | " +
                    $"{RiderControlSystem.s_InfoAirCitizen:N0}";
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
                    $"{RiderControlSystem.s_InfoTaxiTourist:N0} | " +
                    $"{RiderControlSystem.s_InfoBusTourist:N0} | " +
                    $"{RiderControlSystem.s_InfoTramTourist:N0} | " +
                    $"{RiderControlSystem.s_InfoTrainTourist:N0} | " +
                    $"{RiderControlSystem.s_InfoSubwayTourist:N0} | " +
                    $"{RiderControlSystem.s_InfoAirTourist:N0}";
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
                    $"{RiderControlSystem.s_StatusWaitingTransportTotal:N0} | " +
                    $"{RiderControlSystem.s_InfoTotalTourist:N0} | " +
                    $"{RiderControlSystem.s_InfoTotalCitizen:N0}";
            }
        }

        // TAXI SCAN (numbers-only; meanings in LocaleEN)

        [SettingsUISection(StatusTab, TaxiScanGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusTaxiSupply
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return
                    $"{RiderControlSystem.s_StatusTaxisTotal:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiDepotsTotal:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiDepotsWithDispatchCenter:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiStandsTotal:N0}";
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
                    $"{RiderControlSystem.s_StatusPassengerTotal:N0} | " +
                    $"{RiderControlSystem.s_StatusPassengerIgnoreTaxi:N0} | " +
                    $"{RiderControlSystem.s_StatusPassengerHasResident:N0}";
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
                    $"{RiderControlSystem.s_StatusReqCustomer:N0} | " +
                    $"{RiderControlSystem.s_StatusReqOutside:N0} | " +
                    $"{RiderControlSystem.s_StatusReqNone:N0}";
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
                    $"{RiderControlSystem.s_StatusTaxiTransporting:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiBoarding:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiReturning:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiDispatched:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiEnRoute:N0} | " +
                    $"{RiderControlSystem.s_StatusTaxiParked:N0}";
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
                    $"{RiderControlSystem.s_StatusWaitingTaxiStandTotal:N0} | " +
                    $"{RiderControlSystem.s_StatusReqStand:N0}";
            }
        }

        // LAST UPDATE (numbers-only; meanings in LocaleEN)

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusCoverage1
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0} | {RiderControlSystem.s_StatusResidentsTotal:N0}";
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
                    $"{RiderControlSystem.s_StatusCommutersIgnoreTaxi:N0} | {RiderControlSystem.s_StatusCommutersTotal:N0} | " +
                    $"{RiderControlSystem.s_StatusTouristsIgnoreTaxi:N0} | {RiderControlSystem.s_StatusTouristsTotal:N0}";
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
                    $"{RiderControlSystem.s_StatusLastAppliedIgnoreTaxi:N0} | " +
                    $"{RiderControlSystem.s_StatusLastRemovedRideNeeder:N0} | " +
                    $"{RiderControlSystem.s_StatusLastClearedTaxiLaneWaiting:N0}";
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
                    $"{RiderControlSystem.s_StatusLastClearedTaxiStandWaiting:N0} | " +
                    $"{RiderControlSystem.s_StatusLastSkippedCommuters:N0} | " +
                    $"{RiderControlSystem.s_StatusLastSkippedTourists:N0}";
            }
        }

        [SettingsUISection(StatusTab, LastUpdateGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusSnapshotMeta
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.GetStatusLastStampText()} | {RiderControlSystem.GetStatusAgeText()}";
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
                return $"{RiderControlSystem.s_StatusResidentsForcedMarker:N0} | {RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0} | {RiderControlSystem.s_StatusResidentsTotal:N0}";
            }
        }

        [SettingsUISection(StatusTab, AdvancedDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsStatusNotReady))]
        public string StatusDebugTaxiFlags
        {
            get
            {
                RiderControlSystem.AutoRequestStatusRefreshOnRead();
                return $"{RiderControlSystem.s_StatusTaxiWithDispatchBuffer:N0} | {RiderControlSystem.s_StatusTaxiFromOutside:N0} | {RiderControlSystem.s_StatusTaxiDisabled:N0}";
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
