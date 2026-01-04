// File: Systems/RiderControlSystem.Status.cs
// Status snapshot + InfoView-matching passenger statistics.

namespace RiderControl
{
    using Game.Agents;          // MovingAway
    using Game.Citizens;        // HouseholdMember, Household
    using Game.City;            // CityStatisticsSystem, UITransportConfigurationPrefab, UITransportSummaryItem, TransportType
    using Game.Common;          // Deleted
    using Game.Creatures;       // ResidentFlags, Passenger, HumanCurrentLane, Resident
    using Game.Events;          // InvolvedInAccident
    using Game.Prefabs;         // PrefabSystem, PrefabRef
    using Game.Routes;          // TaxiStand, WaitingPassengers
    using Game.Simulation;      // TaxiRequest, TaxiRequestType, ServiceDispatch
    using Game.Tools;           // Temp
    using Game.Vehicles;        // Taxi, TaxiFlags, ParkedCar
    using System;               // DateTime, Math
    using System.Globalization; // CultureInfo
    using Unity.Entities;       // SystemAPI, EntityQuery
    using BuildingTransportDepot = Game.Buildings.TransportDepot;
    using CreatureResident = Game.Creatures.Resident;
    using UTime = UnityEngine.Time;

    public partial class RiderControlSystem
    {
        private const double kAutoRefreshMinSeconds = 240.0;
        private const double kAgeShowSecondsMaxSeconds = 3600.0;

        private const string kCityScanNotReady =
            "No transit processed yet. Open a city and let the simulation run.";

        private const string kTaxiScanNotReady =
            "Stats not available yet. Open a city and let the simulation run a few minutes.";

        private const string kActivityNotReady =
            "No activity recorded yet.";

        private static bool s_StatusRefreshRequested;
        private static bool s_StatusForceRefresh;

        internal static double s_StatusLastSnapshotRealtime;
        internal static string s_StatusLastSnapshotClock = "n/a";

        internal static int s_StatusResidentsTotal;
        internal static int s_StatusResidentsIgnoreTaxi;
        internal static int s_StatusResidentsForcedMarker;

        internal static int s_StatusCommutersTotal;
        internal static int s_StatusCommutersIgnoreTaxi;

        internal static int s_StatusTouristsTotal;
        internal static int s_StatusTouristsIgnoreTaxi;

        internal static int s_StatusWaitingTransportTotal;
        internal static int s_StatusWaitingTaxiStandTotal;

        internal static int s_StatusHouseholdsTotal;
        internal static int s_StatusHouseholdsCommuter;
        internal static int s_StatusHouseholdsTourist;
        internal static int s_StatusHouseholdsHomeless;
        internal static int s_StatusHouseholdsMovingAway;
        internal static int s_StatusResidentsInMovingAwayHousehold;

        internal static int s_InfoTaxiTourist;
        internal static int s_InfoTaxiCitizen;
        internal static int s_InfoBusTourist;
        internal static int s_InfoBusCitizen;
        internal static int s_InfoTramTourist;
        internal static int s_InfoTramCitizen;
        internal static int s_InfoTrainTourist;
        internal static int s_InfoTrainCitizen;
        internal static int s_InfoSubwayTourist;
        internal static int s_InfoSubwayCitizen;
        internal static int s_InfoShipTourist;
        internal static int s_InfoShipCitizen;
        internal static int s_InfoFerryTourist;
        internal static int s_InfoFerryCitizen;
        internal static int s_InfoAirTourist;
        internal static int s_InfoAirCitizen;
        internal static int s_InfoTotalTourist;
        internal static int s_InfoTotalCitizen;

        internal static int s_StatusReqStand;
        internal static int s_StatusReqCustomer;
        internal static int s_StatusReqOutside;
        internal static int s_StatusReqNone;

        internal static int s_StatusReqCustomerSeekerHasResident;
        internal static int s_StatusReqCustomerSeekerIgnoreTaxi;
        internal static int s_StatusReqOutsideSeekerHasResident;
        internal static int s_StatusReqOutsideSeekerIgnoreTaxi;

        internal static int s_StatusTaxisTotal;
        internal static int s_StatusTaxiTransporting;
        internal static int s_StatusTaxiBoarding;
        internal static int s_StatusTaxiReturning;
        internal static int s_StatusTaxiDispatched;
        internal static int s_StatusTaxiEnRoute;
        internal static int s_StatusTaxiParked;
        internal static int s_StatusTaxiAccident;
        internal static int s_StatusTaxiFromOutside;
        internal static int s_StatusTaxiDisabled;
        internal static int s_StatusTaxiWithDispatchBuffer;

        internal static int s_StatusPassengerTotal;
        internal static int s_StatusPassengerHasResident;
        internal static int s_StatusPassengerIgnoreTaxi;

        internal static int s_StatusTaxiStandsTotal;
        internal static int s_StatusTaxiDepotsTotal;
        internal static int s_StatusTaxiDepotsWithDispatchCenter;

        internal static int s_StatusLastAppliedIgnoreTaxi;
        internal static int s_StatusLastSkippedCommuters;
        internal static int s_StatusLastSkippedTourists;
        internal static int s_StatusLastClearedTaxiLaneWaiting;
        internal static int s_StatusLastClearedTaxiStandWaiting;
        internal static int s_StatusLastRemovedRideNeeder;

        private CityStatisticsSystem? m_CityStatisticsSystem;
        private PrefabSystem? m_PrefabSystem;
        private EntityQuery m_TransportConfigQuery;
        private UITransportConfigurationPrefab? m_TransportConfig;

        private void InitStatusSystemsOnCreate()
        {
            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_TransportConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
        }

        private void ResetStatusOnCityLoaded()
        {
            s_StatusLastSnapshotRealtime = 0.0;
            s_StatusLastSnapshotClock = "n/a";

            s_StatusRefreshRequested = false;
            s_StatusForceRefresh = false;

            ClearSnapshotValues();

            try
            {
                if (m_PrefabSystem != null)
                {
                    m_TransportConfig =
                        m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_TransportConfigQuery);
                }
            }
            catch
            {
                m_TransportConfig = null;
            }
        }

        private void TickStatusSnapshot()
        {
            if (!s_StatusRefreshRequested)
                return;

            double now = UTime.realtimeSinceStartupAsDouble;

            if (!s_StatusForceRefresh && s_StatusLastSnapshotRealtime > 0.0)
            {
                double age = Math.Max(0.0, now - s_StatusLastSnapshotRealtime);
                if (age < kAutoRefreshMinSeconds)
                    return;
            }

            s_StatusRefreshRequested = false;
            s_StatusForceRefresh = false;

            UpdateStatusSnapshot();

            s_StatusLastSnapshotRealtime = now;
            try
            {
                s_StatusLastSnapshotClock =
                    DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                s_StatusLastSnapshotClock = "n/a";
            }
        }

        private void UpdateStatusSnapshot()
        {
            ClearSnapshotValues();

            UpdateStatusMonthlyPassengers();
            UpdateStatusTaxiDepotAndStandCounts();

            foreach ((RefRO<Household> _, Entity h) in SystemAPI
                         .Query<RefRO<Household>>()
                         .WithEntityAccess()
                         .WithNone<Deleted, Temp>())
            {
                s_StatusHouseholdsTotal++;

                if (SystemAPI.HasComponent<CommuterHousehold>(h))
                    s_StatusHouseholdsCommuter++;

                if (SystemAPI.HasComponent<TouristHousehold>(h))
                    s_StatusHouseholdsTourist++;

                if (SystemAPI.HasComponent<HomelessHousehold>(h))
                    s_StatusHouseholdsHomeless++;

                if (SystemAPI.HasComponent<MovingAway>(h))
                    s_StatusHouseholdsMovingAway++;
            }

            foreach ((RefRO<CreatureResident> residentRef, Entity e) in SystemAPI
                         .Query<RefRO<CreatureResident>>()
                         .WithEntityAccess()
                         .WithNone<Deleted, Temp>())
            {
                s_StatusResidentsTotal++;

                ResidentFlags rf = residentRef.ValueRO.m_Flags;
                bool ignoreTaxi = (rf & ResidentFlags.IgnoreTaxi) != 0;
                if (ignoreTaxi)
                    s_StatusResidentsIgnoreTaxi++;

                if (SystemAPI.HasComponent<IgnoreTaxiMark>(e))
                    s_StatusResidentsForcedMarker++;

                Entity citizenEntity = residentRef.ValueRO.m_Citizen;
                if (citizenEntity == Entity.Null || !SystemAPI.HasComponent<HouseholdMember>(citizenEntity))
                    continue;

                Entity household = SystemAPI.GetComponentRO<HouseholdMember>(citizenEntity).ValueRO.m_Household;
                if (household == Entity.Null)
                    continue;

                if (SystemAPI.HasComponent<MovingAway>(household))
                    s_StatusResidentsInMovingAwayHousehold++;

                if (SystemAPI.HasComponent<CommuterHousehold>(household))
                {
                    s_StatusCommutersTotal++;
                    if (ignoreTaxi)
                        s_StatusCommutersIgnoreTaxi++;
                }

                if (SystemAPI.HasComponent<TouristHousehold>(household))
                {
                    s_StatusTouristsTotal++;
                    if (ignoreTaxi)
                        s_StatusTouristsIgnoreTaxi++;
                }
            }

            foreach ((RefRO<CreatureResident> residentRef, RefRO<HumanCurrentLane> _) in SystemAPI
                         .Query<RefRO<CreatureResident>, RefRO<HumanCurrentLane>>()
                         .WithNone<Deleted, Temp>())
            {
                if ((residentRef.ValueRO.m_Flags & ResidentFlags.WaitingTransport) == 0)
                    continue;

                s_StatusWaitingTransportTotal++;
            }

            foreach (RefRO<WaitingPassengers> waiting in SystemAPI
                         .Query<RefRO<WaitingPassengers>>()
                         .WithAll<TaxiStand>()
                         .WithNone<Deleted, Temp>())
            {
                int count = waiting.ValueRO.m_Count;
                if (count > 0)
                    s_StatusWaitingTaxiStandTotal += count;
            }

            foreach (RefRO<TaxiRequest> reqRef in SystemAPI.Query<RefRO<TaxiRequest>>().WithNone<Deleted, Temp>())
            {
                TaxiRequest req = reqRef.ValueRO;

                switch (req.m_Type)
                {
                    case TaxiRequestType.Stand:
                        s_StatusReqStand++;
                        break;

                    case TaxiRequestType.Customer:
                        s_StatusReqCustomer++;
                        if (SystemAPI.HasComponent<CreatureResident>(req.m_Seeker))
                        {
                            s_StatusReqCustomerSeekerHasResident++;
                            ResidentFlags seekerFlags =
                                SystemAPI.GetComponentRO<CreatureResident>(req.m_Seeker).ValueRO.m_Flags;
                            if ((seekerFlags & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusReqCustomerSeekerIgnoreTaxi++;
                        }
                        break;

                    case TaxiRequestType.Outside:
                        s_StatusReqOutside++;
                        if (SystemAPI.HasComponent<CreatureResident>(req.m_Seeker))
                        {
                            s_StatusReqOutsideSeekerHasResident++;
                            ResidentFlags seekerFlags =
                                SystemAPI.GetComponentRO<CreatureResident>(req.m_Seeker).ValueRO.m_Flags;
                            if ((seekerFlags & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusReqOutsideSeekerIgnoreTaxi++;
                        }
                        break;

                    default:
                        s_StatusReqNone++;
                        break;
                }
            }

            foreach ((RefRO<Game.Vehicles.Taxi> taxiRef, Entity taxiEntity) in SystemAPI
                         .Query<RefRO<Game.Vehicles.Taxi>>()
                         .WithEntityAccess()
                         .WithNone<Deleted, Temp>())
            {
                s_StatusTaxisTotal++;

                TaxiFlags flags = taxiRef.ValueRO.m_State;

                if (SystemAPI.HasComponent<InvolvedInAccident>(taxiEntity))
                    s_StatusTaxiAccident++;
                else if (SystemAPI.HasComponent<ParkedCar>(taxiEntity))
                    s_StatusTaxiParked++;
                else if ((flags & TaxiFlags.Returning) != 0)
                    s_StatusTaxiReturning++;
                else if ((flags & TaxiFlags.Dispatched) != 0)
                    s_StatusTaxiDispatched++;
                else if ((flags & TaxiFlags.Boarding) != 0)
                    s_StatusTaxiBoarding++;
                else if ((flags & TaxiFlags.Transporting) != 0)
                    s_StatusTaxiTransporting++;
                else
                    s_StatusTaxiEnRoute++;

                if ((flags & TaxiFlags.FromOutside) != 0)
                    s_StatusTaxiFromOutside++;

                if ((flags & TaxiFlags.Disabled) != 0)
                    s_StatusTaxiDisabled++;

                if (SystemAPI.HasBuffer<ServiceDispatch>(taxiEntity))
                {
                    DynamicBuffer<ServiceDispatch> buf = SystemAPI.GetBuffer<ServiceDispatch>(taxiEntity);
                    if (buf.IsCreated && buf.Length > 0)
                        s_StatusTaxiWithDispatchBuffer++;
                }

                if (SystemAPI.HasBuffer<Passenger>(taxiEntity))
                {
                    DynamicBuffer<Passenger> passengers = SystemAPI.GetBuffer<Passenger>(taxiEntity);
                    for (int i = 0; i < passengers.Length; i++)
                    {
                        Entity p = passengers[i].m_Passenger;
                        s_StatusPassengerTotal++;

                        if (SystemAPI.HasComponent<CreatureResident>(p))
                        {
                            s_StatusPassengerHasResident++;
                            ResidentFlags pf = SystemAPI.GetComponentRO<CreatureResident>(p).ValueRO.m_Flags;
                            if ((pf & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusPassengerIgnoreTaxi++;
                        }
                    }
                }
            }
        }

        private void UpdateStatusMonthlyPassengers()
        {
            if (m_CityStatisticsSystem == null)
                return;

            s_InfoTaxiTourist = 0; s_InfoTaxiCitizen = 0;
            s_InfoBusTourist = 0; s_InfoBusCitizen = 0;
            s_InfoTramTourist = 0; s_InfoTramCitizen = 0;
            s_InfoTrainTourist = 0; s_InfoTrainCitizen = 0;
            s_InfoSubwayTourist = 0; s_InfoSubwayCitizen = 0;
            s_InfoShipTourist = 0; s_InfoShipCitizen = 0;
            s_InfoFerryTourist = 0; s_InfoFerryCitizen = 0;
            s_InfoAirTourist = 0; s_InfoAirCitizen = 0;
            s_InfoTotalTourist = 0; s_InfoTotalCitizen = 0;

            if (m_TransportConfig == null && m_PrefabSystem != null)
            {
                try
                {
                    m_TransportConfig =
                        m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_TransportConfigQuery);
                }
                catch
                {
                    m_TransportConfig = null;
                }
            }

            if (m_TransportConfig == null)
                return;

            UITransportSummaryItem[] items = m_TransportConfig.m_PassengerSummaryItems;
            for (int i = 0; i < items.Length; i++)
            {
                UITransportSummaryItem item = items[i];

                int citizen;
                int tourist;

                try
                {
                    citizen = m_CityStatisticsSystem.GetStatisticValue(item.m_Statistic);
                    tourist = m_CityStatisticsSystem.GetStatisticValue(item.m_Statistic, 1);
                }
                catch
                {
                    continue;
                }

                s_InfoTotalCitizen += citizen;
                s_InfoTotalTourist += tourist;

                switch (item.m_Type)
                {
                    case TransportType.Taxi: s_InfoTaxiCitizen = citizen; s_InfoTaxiTourist = tourist; break;
                    case TransportType.Bus: s_InfoBusCitizen = citizen; s_InfoBusTourist = tourist; break;
                    case TransportType.Tram: s_InfoTramCitizen = citizen; s_InfoTramTourist = tourist; break;
                    case TransportType.Train: s_InfoTrainCitizen = citizen; s_InfoTrainTourist = tourist; break;
                    case TransportType.Subway: s_InfoSubwayCitizen = citizen; s_InfoSubwayTourist = tourist; break;
                    case TransportType.Ship: s_InfoShipCitizen = citizen; s_InfoShipTourist = tourist; break;
                    case TransportType.Ferry: s_InfoFerryCitizen = citizen; s_InfoFerryTourist = tourist; break;
                    case TransportType.Airplane: s_InfoAirCitizen = citizen; s_InfoAirTourist = tourist; break;
                }
            }
        }

        private void UpdateStatusTaxiDepotAndStandCounts()
        {
            s_StatusTaxiStandsTotal = 0;
            s_StatusTaxiDepotsTotal = 0;
            s_StatusTaxiDepotsWithDispatchCenter = 0;

            foreach ((RefRO<TaxiStand> _, Entity e) in SystemAPI
                         .Query<RefRO<TaxiStand>>()
                         .WithEntityAccess()
                         .WithNone<Deleted, Temp>())
            {
                s_StatusTaxiStandsTotal++;
            }

            foreach ((RefRO<BuildingTransportDepot> depot, RefRO<PrefabRef> prefabRef) in SystemAPI
                         .Query<RefRO<BuildingTransportDepot>, RefRO<PrefabRef>>()
                         .WithNone<Deleted, Temp>())
            {
                Entity prefab = prefabRef.ValueRO.m_Prefab;
                if (prefab == Entity.Null || !SystemAPI.HasComponent<Game.Prefabs.TransportDepotData>(prefab))
                    continue;

                Game.Prefabs.TransportDepotData data =
                    SystemAPI.GetComponentRO<Game.Prefabs.TransportDepotData>(prefab).ValueRO;

                if (data.m_TransportType != Game.Prefabs.TransportType.Taxi)
                    continue;

                s_StatusTaxiDepotsTotal++;

                if ((depot.ValueRO.m_Flags & Game.Buildings.TransportDepotFlags.HasDispatchCenter) != 0)
                    s_StatusTaxiDepotsWithDispatchCenter++;
            }
        }

        private static void ClearSnapshotValues()
        {
            s_StatusHouseholdsTotal = 0;
            s_StatusHouseholdsCommuter = 0;
            s_StatusHouseholdsTourist = 0;
            s_StatusHouseholdsHomeless = 0;
            s_StatusHouseholdsMovingAway = 0;
            s_StatusResidentsInMovingAwayHousehold = 0;

            s_StatusResidentsTotal = 0;
            s_StatusResidentsIgnoreTaxi = 0;
            s_StatusResidentsForcedMarker = 0;

            s_StatusCommutersTotal = 0;
            s_StatusCommutersIgnoreTaxi = 0;

            s_StatusTouristsTotal = 0;
            s_StatusTouristsIgnoreTaxi = 0;

            s_StatusWaitingTransportTotal = 0;
            s_StatusWaitingTaxiStandTotal = 0;

            s_StatusReqStand = 0;
            s_StatusReqCustomer = 0;
            s_StatusReqOutside = 0;
            s_StatusReqNone = 0;

            s_StatusReqCustomerSeekerHasResident = 0;
            s_StatusReqCustomerSeekerIgnoreTaxi = 0;
            s_StatusReqOutsideSeekerHasResident = 0;
            s_StatusReqOutsideSeekerIgnoreTaxi = 0;

            s_StatusTaxisTotal = 0;
            s_StatusTaxiTransporting = 0;
            s_StatusTaxiBoarding = 0;
            s_StatusTaxiReturning = 0;
            s_StatusTaxiDispatched = 0;
            s_StatusTaxiEnRoute = 0;
            s_StatusTaxiParked = 0;
            s_StatusTaxiAccident = 0;
            s_StatusTaxiFromOutside = 0;
            s_StatusTaxiDisabled = 0;
            s_StatusTaxiWithDispatchBuffer = 0;

            s_StatusPassengerTotal = 0;
            s_StatusPassengerHasResident = 0;
            s_StatusPassengerIgnoreTaxi = 0;

            s_StatusTaxiStandsTotal = 0;
            s_StatusTaxiDepotsTotal = 0;
            s_StatusTaxiDepotsWithDispatchCenter = 0;

            s_InfoTaxiTourist = 0; s_InfoTaxiCitizen = 0;
            s_InfoBusTourist = 0; s_InfoBusCitizen = 0;
            s_InfoTramTourist = 0; s_InfoTramCitizen = 0;
            s_InfoTrainTourist = 0; s_InfoTrainCitizen = 0;
            s_InfoSubwayTourist = 0; s_InfoSubwayCitizen = 0;
            s_InfoShipTourist = 0; s_InfoShipCitizen = 0;
            s_InfoFerryTourist = 0; s_InfoFerryCitizen = 0;
            s_InfoAirTourist = 0; s_InfoAirCitizen = 0;
            s_InfoTotalTourist = 0; s_InfoTotalCitizen = 0;
        }

        internal static void AutoRequestStatusRefreshOnRead()
        {
            if (s_StatusLastSnapshotRealtime <= 0.0)
            {
                s_StatusRefreshRequested = true;
                return;
            }

            double now = UTime.realtimeSinceStartupAsDouble;
            double age = now - s_StatusLastSnapshotRealtime;
            if (age >= kAutoRefreshMinSeconds)
                s_StatusRefreshRequested = true;
        }

        internal static void RequestStatusRefresh(bool force)
        {
            s_StatusRefreshRequested = true;
            if (force)
                s_StatusForceRefresh = true;
        }

        internal static bool HasSnapshot() => s_StatusLastSnapshotRealtime > 0.0;

        internal static string GetStatusLastStampText() =>
            string.IsNullOrEmpty(s_StatusLastSnapshotClock) ? "n/a" : s_StatusLastSnapshotClock;

        internal static double GetStatusAgeSeconds()
        {
            if (s_StatusLastSnapshotRealtime <= 0.0)
                return -1.0;

            double now = UTime.realtimeSinceStartupAsDouble;
            return Math.Max(0.0, now - s_StatusLastSnapshotRealtime);
        }

        internal static string GetStatusAgeText()
        {
            double age = GetStatusAgeSeconds();
            if (age < 0.0)
                return "n/a";

            long sec = (long)Math.Round(age, MidpointRounding.AwayFromZero);

            if (sec < 60)
                return $"{sec}s";

            if (sec < (long)kAgeShowSecondsMaxSeconds)
            {
                long m = sec / 60;
                long s = sec % 60;
                return $"{m}m {s}s";
            }

            long hr = sec / 3600;
            long min = (sec % 3600) / 60;
            return $"{hr}h {min}m";
        }

        internal static string GetCityScanNotReadyText() => kCityScanNotReady;
        internal static string GetTaxiScanNotReadyText() => kTaxiScanNotReady;

        internal static bool HasActivity()
        {
            return s_StatusLastAppliedIgnoreTaxi != 0
                || s_StatusLastSkippedCommuters != 0
                || s_StatusLastSkippedTourists != 0
                || s_StatusLastClearedTaxiLaneWaiting != 0
                || s_StatusLastClearedTaxiStandWaiting != 0
                || s_StatusLastRemovedRideNeeder != 0;
        }

        internal static string GetActivityNotReadyText() => kActivityNotReady;
    }
}
