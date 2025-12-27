// Systems/RiderControlSystem.Status.cs
// Status snapshot + InfoView-matching passenger statistics.

namespace RiderControl
{
    using Game.Citizens;
    using Game.Common;
    using Game.Creatures;
    using Game.Events;
    using Game.Prefabs;
    using Game.Routes;
    using Game.Simulation;
    using Game.Tools;
    using Game.Vehicles;
    using Unity.Entities;
    using BuildingTransportDepot = Game.Buildings.TransportDepot;
    using CreatureResident = Game.Creatures.Resident;

    public partial class RiderControlSystem
    {
        // Status snapshot updated on a timer (cheap; heavy scans are throttled).
        private const float kStatusIntervalSeconds = 60f;

        private float m_StatusTimerSeconds;
        private CityStatisticsSystem? m_CityStatisticsSystem;
        private PrefabSystem? m_PrefabSystem;
        private EntityQuery m_TransportConfigQuery;
        private UITransportConfigurationPrefab? m_TransportConfig;

        // -----------------------------------
        // STATUS FIELDS (read by Setting.cs)
        // -----------------------------------

        internal static int s_StatusSnapshotCount;
        internal static float s_StatusSecondsSinceSnapshot;

        internal static int s_StatusResidentsTotal;
        internal static int s_StatusResidentsIgnoreTaxi;
        internal static int s_StatusResidentsForcedMarker;

        internal static int s_StatusCommutersTotal;
        internal static int s_StatusCommutersIgnoreTaxi;

        internal static int s_StatusTouristsTotal;
        internal static int s_StatusTouristsIgnoreTaxi;

        internal static int s_StatusWaitingTransportTotal;
        internal static int s_StatusWaitingTaxiStandTotal;

        // Transportation InfoView passenger table (tourists/citizens per month)
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

        internal static int s_StatusTaxiDepotsTotal;
        internal static int s_StatusTaxiDepotsWithDispatchCenter;
        internal static int s_StatusTaxiStandsTotal;


        // “Last update” activity counts (written by OnUpdate in RiderControlSystem.Core.cs)
        internal static int s_StatusLastAppliedIgnoreTaxi;
        internal static int s_StatusLastSkippedCommuters;
        internal static int s_StatusLastSkippedTourists;
        internal static int s_StatusLastClearedTaxiLaneWaiting;
        internal static int s_StatusLastClearedTaxiStandWaiting;
        internal static int s_StatusLastRemovedRideNeeder;

        private void InitStatusSystemsOnCreate()
        {
            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_TransportConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
        }

        private void ResetStatusOnCityLoaded()
        {
            m_StatusTimerSeconds = 0f;
            s_StatusSnapshotCount = 0;
            s_StatusSecondsSinceSnapshot = 0f;

            s_InfoTaxiTourist = 0;
            s_InfoTaxiCitizen = 0;
            s_InfoBusTourist = 0;
            s_InfoBusCitizen = 0;
            s_InfoTramTourist = 0;
            s_InfoTramCitizen = 0;
            s_InfoTrainTourist = 0;
            s_InfoTrainCitizen = 0;
            s_InfoSubwayTourist = 0;
            s_InfoSubwayCitizen = 0;
            s_InfoShipTourist = 0;
            s_InfoShipCitizen = 0;
            s_InfoFerryTourist = 0;
            s_InfoFerryCitizen = 0;
            s_InfoAirTourist = 0;
            s_InfoAirCitizen = 0;
            s_InfoTotalTourist = 0;
            s_InfoTotalCitizen = 0;

            s_StatusTaxiStandsTotal = 0;

            // Cache the same transport config prefab used by TransportInfoviewUISystem (best-effort).
            // If this fails (e.g. during partial loads), StatusInfoViewMonthly will remain 0s.
            try
            {
                if (m_PrefabSystem != null)
                {
                    m_TransportConfig = m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_TransportConfigQuery);
                }
            }
            catch
            {
                m_TransportConfig = null;
            }
        }

        private void TickStatusSnapshot()
        {
            // Use unscaled time so Status still updates while paused in the Options menu.
            float dt = UnityEngine.Time.unscaledDeltaTime;

            s_StatusSecondsSinceSnapshot += dt;
            m_StatusTimerSeconds += dt;

            if (m_StatusTimerSeconds < kStatusIntervalSeconds)
            {
                return;
            }

            m_StatusTimerSeconds = 0f;
            s_StatusSecondsSinceSnapshot = 0f;
            s_StatusSnapshotCount++;

            UpdateStatusSnapshot();
        }

        private void UpdateStatusSnapshot()
        {
            // Reset
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

            // Transportation InfoView passenger table (per month).
            UpdateStatusMonthlyPassengers();

            // CITY SCAN: taxi depots + stands in city
            UpdateStatusTaxiDepotAndStandCounts();

            // Residents + commuter/tourist counts (household-based)
            foreach ((RefRO<CreatureResident> residentRef, Entity e) in SystemAPI.Query<RefRO<CreatureResident>>().WithEntityAccess())
            {
                s_StatusResidentsTotal++;

                ResidentFlags rf = residentRef.ValueRO.m_Flags;
                bool ignoreTaxi = (rf & ResidentFlags.IgnoreTaxi) != 0;
                if (ignoreTaxi)
                    s_StatusResidentsIgnoreTaxi++;

                if (SystemAPI.HasComponent<RiderControlForcedIgnoreTaxi>(e))
                    s_StatusResidentsForcedMarker++;

                Entity citizenEntity = residentRef.ValueRO.m_Citizen;
                if (citizenEntity == Entity.Null || !SystemAPI.HasComponent<HouseholdMember>(citizenEntity))
                    continue;

                Entity household = SystemAPI.GetComponentRO<HouseholdMember>(citizenEntity).ValueRO.m_Household;
                if (household == Entity.Null)
                    continue;

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

            // WaitingTransport totals (all transit waiters, not taxi-specific)
            foreach ((RefRO<CreatureResident> residentRef, RefRO<HumanCurrentLane> _) in SystemAPI.Query<RefRO<CreatureResident>, RefRO<HumanCurrentLane>>())
            {
                if ((residentRef.ValueRO.m_Flags & ResidentFlags.WaitingTransport) == 0)
                    continue;

                s_StatusWaitingTransportTotal++;
            }

            // TaxiStand waiters (matches the TaxiStand tooltip "Passengers waiting")
            // Note: Taxi stands track waiters via WaitingPassengers.m_Count on the TaxiStand entity,
            // not via HumanCurrentLane.m_QueueEntity.
            foreach (RefRO<WaitingPassengers> waiting in SystemAPI.Query<RefRO<WaitingPassengers>>().WithAll<TaxiStand>())
            {
                int count = waiting.ValueRO.m_Count;
                if (count > 0)
                {
                    s_StatusWaitingTaxiStandTotal += count;
                }
            }

            // TaxiRequest breakdown + seeker IgnoreTaxi sanity
            foreach (RefRO<TaxiRequest> reqRef in SystemAPI.Query<RefRO<TaxiRequest>>())
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
                            ResidentFlags rf = SystemAPI.GetComponentRO<CreatureResident>(req.m_Seeker).ValueRO.m_Flags;
                            if ((rf & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusReqCustomerSeekerIgnoreTaxi++;
                        }
                        break;

                    case TaxiRequestType.Outside:
                        s_StatusReqOutside++;
                        if (SystemAPI.HasComponent<CreatureResident>(req.m_Seeker))
                        {
                            s_StatusReqOutsideSeekerHasResident++;
                            ResidentFlags rf = SystemAPI.GetComponentRO<CreatureResident>(req.m_Seeker).ValueRO.m_Flags;
                            if ((rf & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusReqOutsideSeekerIgnoreTaxi++;
                        }
                        break;

                    default:
                        s_StatusReqNone++;
                        break;
                }
            }

            // Taxi fleet + passengers
            foreach ((RefRO<Game.Vehicles.Taxi> taxiRef, Entity taxiEntity) in SystemAPI.Query<RefRO<Game.Vehicles.Taxi>>().WithEntityAccess())
            {
                s_StatusTaxisTotal++;

                TaxiFlags flags = taxiRef.ValueRO.m_State;

                // Mirror the in-game UI state selection for taxis (VehicleUIUtils.GetStateKey):
                // Accident > Parked > Returning > Dispatched > Boarding > Transporting > EnRoute
                if (SystemAPI.HasComponent<InvolvedInAccident>(taxiEntity))
                {
                    s_StatusTaxiAccident++;
                }
                else if (SystemAPI.HasComponent<ParkedCar>(taxiEntity))
                {
                    s_StatusTaxiParked++;
                }
                else if ((flags & TaxiFlags.Returning) != 0)
                {
                    s_StatusTaxiReturning++;
                }
                else if ((flags & TaxiFlags.Dispatched) != 0)
                {
                    s_StatusTaxiDispatched++;
                }
                else if ((flags & TaxiFlags.Boarding) != 0)
                {
                    s_StatusTaxiBoarding++;
                }
                else if ((flags & TaxiFlags.Transporting) != 0)
                {
                    s_StatusTaxiTransporting++;
                }
                else
                {
                    // This is what the vanilla UI shows as "En Route".
                    s_StatusTaxiEnRoute++;
                }
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
                            ResidentFlags rf = SystemAPI.GetComponentRO<CreatureResident>(p).ValueRO.m_Flags;
                            if ((rf & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusPassengerIgnoreTaxi++;
                        }
                    }
                }
            }
        }

        private void UpdateStatusMonthlyPassengers()
        {
            if (m_CityStatisticsSystem == null)
            {
                return;
            }

            // Reset (these are snapshot values, not accumulators)
            s_InfoTaxiTourist = 0;
            s_InfoTaxiCitizen = 0;
            s_InfoBusTourist = 0;
            s_InfoBusCitizen = 0;
            s_InfoTramTourist = 0;
            s_InfoTramCitizen = 0;
            s_InfoTrainTourist = 0;
            s_InfoTrainCitizen = 0;
            s_InfoSubwayTourist = 0;
            s_InfoSubwayCitizen = 0;
            s_InfoShipTourist = 0;
            s_InfoShipCitizen = 0;
            s_InfoFerryTourist = 0;
            s_InfoFerryCitizen = 0;
            s_InfoAirTourist = 0;
            s_InfoAirCitizen = 0;
            s_InfoTotalTourist = 0;
            s_InfoTotalCitizen = 0;

            // Best-effort lazy init (in case the prefab wasn't ready at load-complete).
            if (m_TransportConfig == null && m_PrefabSystem != null)
            {
                try
                {
                    m_TransportConfig = m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_TransportConfigQuery);
                }
                catch
                {
                    m_TransportConfig = null;
                }
            }

            if (m_TransportConfig == null)
            {
                return;
            }

            UITransportSummaryItem[] items = m_TransportConfig.m_PassengerSummaryItems;
            for (int i = 0; i < items.Length; i++)
            {
                UITransportSummaryItem item = items[i];
                int citizen = 0;
                int tourist = 0;
                try
                {
                    citizen = m_CityStatisticsSystem.GetStatisticValue(item.m_Statistic);
                    tourist = m_CityStatisticsSystem.GetStatisticValue(item.m_Statistic, 1);
                }
                catch
                {
                    // Some items may point at unsupported statistics; ignore.
                }

                int total = citizen + tourist;
                if (total > 0)
                {
                    s_InfoTotalCitizen += citizen;
                    s_InfoTotalTourist += tourist;
                }

                switch (item.m_Type)
                {
                    case TransportType.Taxi:
                        s_InfoTaxiCitizen = citizen;
                        s_InfoTaxiTourist = tourist;
                        break;
                    case TransportType.Bus:
                        s_InfoBusCitizen = citizen;
                        s_InfoBusTourist = tourist;
                        break;
                    case TransportType.Tram:
                        s_InfoTramCitizen = citizen;
                        s_InfoTramTourist = tourist;
                        break;
                    case TransportType.Train:
                        s_InfoTrainCitizen = citizen;
                        s_InfoTrainTourist = tourist;
                        break;
                    case TransportType.Subway:
                        s_InfoSubwayCitizen = citizen;
                        s_InfoSubwayTourist = tourist;
                        break;
                    case TransportType.Ship:
                        s_InfoShipCitizen = citizen;
                        s_InfoShipTourist = tourist;
                        break;
                    case TransportType.Ferry:
                        s_InfoFerryCitizen = citizen;
                        s_InfoFerryTourist = tourist;
                        break;
                    case TransportType.Airplane:
                        s_InfoAirCitizen = citizen;
                        s_InfoAirTourist = tourist;
                        break;
                }
            }
        }

        private void UpdateStatusTaxiDepotAndStandCounts()
        {
            s_StatusTaxiStandsTotal = 0;
            s_StatusTaxiDepotsTotal = 0;
            s_StatusTaxiDepotsWithDispatchCenter = 0;

            // Taxi stands (includes built-in station taxi stops)
            foreach ((RefRO<TaxiStand> _, Entity e) in SystemAPI.Query<RefRO<TaxiStand>>().WithEntityAccess().WithNone<Deleted, Temp>())
            {
                if (EntityManager.Exists(e))
                {
                    s_StatusTaxiStandsTotal++;
                }
            }
            // Taxi depots (total + dispatch center count).
            // NOTE: do NOT infer "small vs large" from capacity, since other mods can change capacity.
            foreach ((RefRO<BuildingTransportDepot> depot, RefRO<PrefabRef> prefabRef) in SystemAPI
                         .Query<RefRO<BuildingTransportDepot>, RefRO<PrefabRef>>()
                         .WithNone<Deleted, Temp>())
            {
                Entity prefab = prefabRef.ValueRO.m_Prefab;
                if (prefab == Entity.Null || !SystemAPI.HasComponent<Game.Prefabs.TransportDepotData>(prefab))
                    continue;

                Game.Prefabs.TransportDepotData data = SystemAPI.GetComponentRO<Game.Prefabs.TransportDepotData>(prefab).ValueRO;
                if (data.m_TransportType != Game.Prefabs.TransportType.Taxi)
                    continue;

                s_StatusTaxiDepotsTotal++;

                if ((depot.ValueRO.m_Flags & Game.Buildings.TransportDepotFlags.HasDispatchCenter) != 0)
                    s_StatusTaxiDepotsWithDispatchCenter++;
            }

        }
    }
}
