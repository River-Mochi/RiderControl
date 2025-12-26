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
        // ---- Tabs ----
        public const string ActionsTab = "Actions";
        public const string AboutTab = "About";

        // ---- Groups ----
        public const string BehaviorGroup = "Druthers";
        public const string DebugGroup = "Debug";
        public const string StatusGroup = "Status";

        // About tab groups
        public const string AboutInfoGroup = "Info";
        public const string AboutLinksGroup = "Support Links";

        // ---- External links ----
        private const string UrlParadox =
            "https://mods.paradoxplaza.com/authors/River-mochi/cities_skylines_2?games=cities_skylines_2&orderBy=desc&sortBy=best&time=alltime";

        private const string UrlDiscord = "https://discord.gg/HTav7ARPs2";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        // ---- Actions tab ----

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

        // -------------------------
        // Actions tab: STATUS (compact + readable)
        // -------------------------

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusSnapshotMeta =>
            $"snapshots={RiderControlSystem.s_StatusSnapshotCount} | age={RiderControlSystem.s_StatusSecondsSinceSnapshot:0.0}s";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusResidents =>
            $"residents ignoreTaxi={RiderControlSystem.s_StatusResidentsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusResidentsTotal:N0} | marked={RiderControlSystem.s_StatusResidentsForcedMarker:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusCommuters =>
            $"commuters(household) ignoreTaxi={RiderControlSystem.s_StatusCommutersIgnoreTaxi:N0}/{RiderControlSystem.s_StatusCommutersTotal:N0} | blockCommutersToo={BlockCommutersToo}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusTourists =>
            $"tourists(household) ignoreTaxi={RiderControlSystem.s_StatusTouristsIgnoreTaxi:N0}/{RiderControlSystem.s_StatusTouristsTotal:N0} | blockTouristsToo={BlockTouristsToo}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusRequests =>
            $"requests stand={RiderControlSystem.s_StatusReqStand:N0} | customer={RiderControlSystem.s_StatusReqCustomer:N0} | outside={RiderControlSystem.s_StatusReqOutside:N0} | none={RiderControlSystem.s_StatusReqNone:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusTaxiFleet =>
            $"taxis={RiderControlSystem.s_StatusTaxisTotal:N0} | transporting={RiderControlSystem.s_StatusTaxiTransporting:N0} | boarding={RiderControlSystem.s_StatusTaxiBoarding:N0} | returning={RiderControlSystem.s_StatusTaxiReturning:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusTaxiFlags =>
            $"fromOutside={RiderControlSystem.s_StatusTaxiFromOutside:N0} | disabled={RiderControlSystem.s_StatusTaxiDisabled:N0} | withDispatch={RiderControlSystem.s_StatusTaxiWithDispatchBuffer:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusPassengers =>
            $"passengers residentIgnoreTaxi={RiderControlSystem.s_StatusPassengerIgnoreTaxi:N0}/{RiderControlSystem.s_StatusPassengerHasResident:N0} | totalPassengers={RiderControlSystem.s_StatusPassengerTotal:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusWaiting =>
            $"waitingTransport={RiderControlSystem.s_StatusWaitingTransportTotal:N0} | taxiStandWaiters={RiderControlSystem.s_StatusWaitingTaxiStandTotal:N0}";

        [SettingsUISection(ActionsTab, StatusGroup)]
        public string StatusLastUpdate =>
            $"lastUpdate ignoreTaxiApplied={RiderControlSystem.s_StatusLastAppliedIgnoreTaxi:N0} | " +
            $"commutersSkipped={RiderControlSystem.s_StatusLastSkippedCommuters:N0} | " +
            $"touristsSkipped={RiderControlSystem.s_StatusLastSkippedTourists:N0} | " +
            $"taxiLaneCleared={RiderControlSystem.s_StatusLastClearedTaxiLaneWaiting:N0} | " +
            $"taxiStandCleared={RiderControlSystem.s_StatusLastClearedTaxiStandWaiting:N0} | " +
            $"rideNeederRemoved={RiderControlSystem.s_StatusLastRemovedRideNeeder:N0}";

        // ---- About tab ----

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
