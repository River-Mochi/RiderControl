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
        StatusGroup,
        AboutInfoGroup,
        AboutLinksGroup
    )]
    [SettingsUIShowGroupName(
        BehaviorGroup,
        DebugGroup,
        StatusGroup,
        AboutLinksGroup
    )]
    public sealed class Setting : ModSetting
    {
        public const string ActionsTab = "Actions";
        public const string AboutTab = "About";

        public const string BehaviorGroup = "Druthers";
        public const string DebugGroup = "Debug";
        public const string StatusGroup = "Status";

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

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockCommutersToo
        {
            get; set;
        }

        [SettingsUISection(ActionsTab, BehaviorGroup)]
        public bool BlockTouristsToo
        {
            get; set;
        }

        [SettingsUISection(ActionsTab, DebugGroup)]
        public bool EnableDebugLogging
        {
            get; set;
        }

        // ---- Status (compact) ----

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusSnapshotMeta =>
            $"snapshots={RiderControlSystem.s_StatusSnapshotCount} | age={RiderControlSystem.s_StatusSecondsSinceSnapshot:0.0}s";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusResidents =>
            $"ignoreTaxi={RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusResidentsTotal:N0} | marked={RiderControlSystem.s_StatusResidentsForcedMarker:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusCommuters =>
            $"ignoreTaxi={RiderControlSystem.s_StatusCommutersIgnoreTaxi:N0}/{RiderControlSystem.s_StatusCommutersTotal:N0} | toggle={BlockCommutersToo}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusTourists =>
            $"ignoreTaxi={RiderControlSystem.s_StatusTouristsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusTouristsTotal:N0} | toggle={BlockTouristsToo}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusRequests =>
            $"stand={RiderControlSystem.s_StatusReqStand:N0} | customer={RiderControlSystem.s_StatusReqCustomer:N0} | outside={RiderControlSystem.s_StatusReqOutside:N0} | none={RiderControlSystem.s_StatusReqNone:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusTaxiFleet =>
            $"taxis={RiderControlSystem.s_StatusTaxisTotal:N0} | transporting={RiderControlSystem.s_StatusTaxiTransporting:N0} | boarding={RiderControlSystem.s_StatusTaxiBoarding:N0} | returning={RiderControlSystem.s_StatusTaxiReturning:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusTaxiFlags =>
            $"fromOutside={RiderControlSystem.s_StatusTaxiFromOutside:N0} | disabled={RiderControlSystem.s_StatusTaxiDisabled:N0} | withDispatch={RiderControlSystem.s_StatusTaxiWithDispatchBuffer:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusPassengers =>
            $"residentIgnoreTaxi={RiderControlSystem.s_StatusPassengerIgnoreTaxi:N0}/{RiderControlSystem.s_StatusPassengerHasResident:N0} | total={RiderControlSystem.s_StatusPassengerTotal:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusWaiting =>
            $"waitingTransport={RiderControlSystem.s_StatusWaitingTransportTotal:N0} | taxiStandWaiters={RiderControlSystem.s_StatusWaitingTaxiStandTotal:N0}";

        // Split into 2 rows so it fits nicely
        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusWorkDone1 =>
            $"ignoreTaxiApplied={RiderControlSystem.s_StatusLastAppliedIgnoreTaxi:N0} | rideNeederRemoved={RiderControlSystem.s_StatusLastRemovedRideNeeder:N0} | taxiLaneCleared={RiderControlSystem.s_StatusLastClearedTaxiLaneWaiting:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusWorkDone2 =>
            $"taxiStandCleared={RiderControlSystem.s_StatusLastClearedTaxiStandWaiting:N0} | commutersSkipped={RiderControlSystem.s_StatusLastSkippedCommuters:N0} | touristsSkipped={RiderControlSystem.s_StatusLastSkippedTourists:N0}";

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
            BlockCommutersToo = true;
            BlockTouristsToo = true;
            EnableDebugLogging = false;
        }
    }
}
