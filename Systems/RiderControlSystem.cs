// Systems/RiderControlSystem.cs
// Demand-side control:
// - Forces ResidentFlags.IgnoreTaxi (removes taxi from route selection)
// - Unwinds taxi waiting states so cims do not freeze
// - Status snapshot for Options UI (Actions tab bottom group)
// - Optional periodic debug logging (EnableDebugLogging)

namespace RiderControl
{
    using Game;
    using Game.Citizens;
    using Game.Creatures;
    using Game.Pathfind;
    using Game.Routes;
    using Game.SceneFlow;
    using Game.Simulation;
    using Game.Vehicles;
    using Unity.Collections;
    using Unity.Entities;
    using LoadPurpose = Colossal.Serialization.Entities.Purpose;

    internal struct RiderControlForcedIgnoreTaxi : IComponentData
    {
    }

    public partial class RiderControlSystem : GameSystemBase
    {
        // Status snapshot updated on a timer (cheap)
        private const float kStatusIntervalSeconds = 10f;

        private EndFrameBarrier m_EndFrameBarrier = null!;
        private float m_DebugTimerSeconds;
        private float m_StatusTimerSeconds;

        // -------------------------
        // STATUS FIELDS (read by Setting.cs)
        // -------------------------

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
        internal static int s_StatusTaxiFromOutside;
        internal static int s_StatusTaxiDisabled;
        internal static int s_StatusTaxiWithDispatchBuffer;

        internal static int s_StatusPassengerTotal;
        internal static int s_StatusPassengerHasResident;
        internal static int s_StatusPassengerIgnoreTaxi;

        // “Last update” activity counts
        internal static int s_StatusLastAppliedIgnoreTaxi;
        internal static int s_StatusLastSkippedCommuters;
        internal static int s_StatusLastSkippedTourists;
        internal static int s_StatusLastClearedTaxiLaneWaiting;
        internal static int s_StatusLastClearedTaxiStandWaiting;
        internal static int s_StatusLastRemovedRideNeeder;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Only run after a city is loaded.
            Enabled = false;
        }

        protected override void OnGameLoadingComplete(LoadPurpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            bool isRealGame =
                mode == GameMode.Game &&
                (purpose == LoadPurpose.NewGame || purpose == LoadPurpose.LoadGame);

            if (!isRealGame)
            {
                return;
            }

            m_DebugTimerSeconds = 0f;
            m_StatusTimerSeconds = 0f;
            s_StatusSnapshotCount = 0;
            s_StatusSecondsSinceSnapshot = 0f;

            Enabled = true;

#if DEBUG
            Mod.s_Log.Info($"{Mod.ModTag} RiderControlSystem enabled (city load complete).");
#endif
        }

        protected override void OnUpdate()
        {
            GameManager gm = GameManager.instance;
            if (gm == null || !gm.gameMode.IsGame())
            {
                Enabled = false;
                return;
            }

            Setting? setting = Mod.Setting;
            if (setting == null)
            {
                Enabled = false;
                return;
            }

            EntityCommandBuffer ecb = m_EndFrameBarrier.CreateCommandBuffer();

            int appliedIgnoreTaxi = 0;
            int skippedCommuters = 0;
            int skippedTourists = 0;
            int clearedTaxiLaneWaiting = 0;
            int clearedTaxiStandWaiting = 0;
            int removedRideNeeder = 0;

            if (!setting.BlockTaxiUsage)
            {
                // Undo only for entities we marked.
                foreach ((RefRW<Resident> resident, Entity entity) in SystemAPI
                             .Query<RefRW<Resident>>()
                             .WithAll<RiderControlForcedIgnoreTaxi>()
                             .WithEntityAccess())
                {
                    resident.ValueRW.m_Flags &= ~ResidentFlags.IgnoreTaxi;
                    ecb.RemoveComponent<RiderControlForcedIgnoreTaxi>(entity);
                }

                // Keep status fresh-ish even when not blocking.
                TickStatusSnapshot();

                if (setting.EnableDebugLogging)
                {
                    TickDebugLogging(setting, 10f);
                }

                return;
            }

            // 1) Force IgnoreTaxi for unmarked residents (optionally allow commuter/tourist households).
            foreach ((RefRW<Resident> resident, Entity entity) in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithNone<RiderControlForcedIgnoreTaxi>()
                         .WithEntityAccess())
            {
                Entity citizenEntity = resident.ValueRO.m_Citizen;

                if (citizenEntity != Entity.Null && SystemAPI.HasComponent<HouseholdMember>(citizenEntity))
                {
                    Entity household = SystemAPI.GetComponentRO<HouseholdMember>(citizenEntity).ValueRO.m_Household;

                    if (household != Entity.Null)
                    {
                        // Allow commuter households if toggle is OFF.
                        if (!setting.BlockCommutersToo && SystemAPI.HasComponent<CommuterHousehold>(household))
                        {
                            skippedCommuters++;
                            continue;
                        }

                        // Allow tourist households if toggle is OFF.
                        if (!setting.BlockTouristsToo && SystemAPI.HasComponent<TouristHousehold>(household))
                        {
                            skippedTourists++;
                            continue;
                        }
                    }
                }

                resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                ecb.AddComponent<RiderControlForcedIgnoreTaxi>(entity);
                appliedIgnoreTaxi++;
            }

            // 1b) Re-apply IgnoreTaxi for already-marked residents.
            // Some vanilla systems can clear IgnoreTaxi; this makes it stick.
            foreach (RefRW<Resident> resident in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithAll<RiderControlForcedIgnoreTaxi>())
            {
                if ((resident.ValueRO.m_Flags & ResidentFlags.IgnoreTaxi) == 0)
                {
                    resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                }
            }

            // 2) Unstick taxi-lane waiting (RideNeeder path).
            // (No structural changes here; just flags/state updates.)
            foreach ((RefRW<HumanCurrentLane> lane, RefRW<PathOwner> pathOwner) in SystemAPI
                         .Query<RefRW<HumanCurrentLane>, RefRW<PathOwner>>())
            {
                if ((lane.ValueRO.m_Flags & CreatureLaneFlags.Taxi) == 0)
                {
                    continue;
                }

                lane.ValueRW.m_Flags &= ~(CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi);

                pathOwner.ValueRW.m_State &= ~PathFlags.Failed;
                pathOwner.ValueRW.m_State |= PathFlags.Obsolete;

                clearedTaxiLaneWaiting++;
            }

            // 3) Unstick TaxiStand waiting (WaitingTransport + queueEntity is TaxiStand).
            // Only affects taxi stands; does not break bus/train waiting.
            foreach ((RefRW<Resident> resident, RefRW<HumanCurrentLane> lane, RefRW<PathOwner> pathOwner) in SystemAPI
                         .Query<RefRW<Resident>, RefRW<HumanCurrentLane>, RefRW<PathOwner>>())
            {
                if ((resident.ValueRO.m_Flags & ResidentFlags.WaitingTransport) == 0)
                {
                    continue;
                }

                Entity queueEntity = lane.ValueRO.m_QueueEntity;
                if (queueEntity == Entity.Null)
                {
                    continue;
                }

                bool isTaxiQueue = false;

                if (SystemAPI.HasComponent<TaxiStand>(queueEntity))
                {
                    isTaxiQueue = true;
                }
                else if (SystemAPI.HasComponent<BoardingVehicle>(queueEntity))
                {
                    BoardingVehicle bv = SystemAPI.GetComponentRO<BoardingVehicle>(queueEntity).ValueRO;
                    if (bv.m_Vehicle != Entity.Null && SystemAPI.HasComponent<Taxi>(bv.m_Vehicle))
                    {
                        isTaxiQueue = true;
                    }
                }

                if (!isTaxiQueue)
                {
                    continue;
                }

                resident.ValueRW.m_Flags &= ~(ResidentFlags.WaitingTransport | ResidentFlags.NoLateDeparture);

                lane.ValueRW.m_QueueEntity = Entity.Null;
                lane.ValueRW.m_QueueArea = default;

                pathOwner.ValueRW.m_State &= ~PathFlags.Failed;
                pathOwner.ValueRW.m_State |= PathFlags.Obsolete;

                clearedTaxiStandWaiting++;
            }

            // 4+5) Safe structural changes:
            // - Remove RideNeeder (immediate)
            // - Cancel TaxiRequests (immediate), skipping Stand to avoid tug-of-war with TaxiStandSystem
            using (NativeList<Entity> rideNeeders = new NativeList<Entity>(Allocator.Temp))
            using (NativeList<Entity> taxiRequests = new NativeList<Entity>(Allocator.Temp))
            {
                foreach ((RefRO<RideNeeder> _, Entity e) in SystemAPI.Query<RefRO<RideNeeder>>().WithEntityAccess())
                {
                    rideNeeders.Add(e);
                }

                foreach ((RefRO<TaxiRequest> reqRef, Entity reqEntity) in SystemAPI.Query<RefRO<TaxiRequest>>().WithEntityAccess())
                {
                    if (reqRef.ValueRO.m_Type == TaxiRequestType.Stand)
                        continue;

                    // Cancel Customer / Outside / None
                    taxiRequests.Add(reqEntity);
                }

                for (int i = 0; i < rideNeeders.Length; i++)
                {
                    Entity e = rideNeeders[i];
                    if (EntityManager.Exists(e) && EntityManager.HasComponent<RideNeeder>(e))
                    {
                        EntityManager.RemoveComponent<RideNeeder>(e);
                        removedRideNeeder++;
                    }
                }

                for (int i = 0; i < taxiRequests.Length; i++)
                {
                    Entity e = taxiRequests[i];
                    if (EntityManager.Exists(e))
                    {
                        EntityManager.DestroyEntity(e);
                    }
                }
            }

            // Update “last activity” status fields
            s_StatusLastAppliedIgnoreTaxi = appliedIgnoreTaxi;
            s_StatusLastSkippedCommuters = skippedCommuters;
            s_StatusLastSkippedTourists = skippedTourists;
            s_StatusLastClearedTaxiLaneWaiting = clearedTaxiLaneWaiting;
            s_StatusLastClearedTaxiStandWaiting = clearedTaxiStandWaiting;
            s_StatusLastRemovedRideNeeder = removedRideNeeder;

            // Status snapshot (expensive queries) on a timer
            TickStatusSnapshot();

            if (setting.EnableDebugLogging)
            {
                TickDebugLogging(setting, 10f);
            }
        }

        private void TickStatusSnapshot()
        {
            float dt = SystemAPI.Time.DeltaTime;

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
            s_StatusTaxiFromOutside = 0;
            s_StatusTaxiDisabled = 0;
            s_StatusTaxiWithDispatchBuffer = 0;

            s_StatusPassengerTotal = 0;
            s_StatusPassengerHasResident = 0;
            s_StatusPassengerIgnoreTaxi = 0;

            // Residents + commuter/tourist counts (household-based)
            foreach ((RefRO<Resident> residentRef, Entity e) in SystemAPI.Query<RefRO<Resident>>().WithEntityAccess())
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

            // WaitingTransport totals + TaxiStand subset
            foreach ((RefRO<Resident> residentRef, RefRO<HumanCurrentLane> laneRef) in SystemAPI.Query<RefRO<Resident>, RefRO<HumanCurrentLane>>())
            {
                if ((residentRef.ValueRO.m_Flags & ResidentFlags.WaitingTransport) == 0)
                    continue;

                s_StatusWaitingTransportTotal++;

                Entity q = laneRef.ValueRO.m_QueueEntity;
                if (q != Entity.Null && SystemAPI.HasComponent<TaxiStand>(q))
                    s_StatusWaitingTaxiStandTotal++;
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
                        if (SystemAPI.HasComponent<Resident>(req.m_Seeker))
                        {
                            s_StatusReqCustomerSeekerHasResident++;
                            ResidentFlags rf = SystemAPI.GetComponentRO<Resident>(req.m_Seeker).ValueRO.m_Flags;
                            if ((rf & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusReqCustomerSeekerIgnoreTaxi++;
                        }
                        break;

                    case TaxiRequestType.Outside:
                        s_StatusReqOutside++;
                        if (SystemAPI.HasComponent<Resident>(req.m_Seeker))
                        {
                            s_StatusReqOutsideSeekerHasResident++;
                            ResidentFlags rf = SystemAPI.GetComponentRO<Resident>(req.m_Seeker).ValueRO.m_Flags;
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
            foreach ((RefRO<Taxi> taxiRef, Entity taxiEntity) in SystemAPI.Query<RefRO<Taxi>>().WithEntityAccess())
            {
                s_StatusTaxisTotal++;

                TaxiFlags flags = taxiRef.ValueRO.m_State;

                if ((flags & TaxiFlags.Transporting) != 0)
                    s_StatusTaxiTransporting++;
                if ((flags & TaxiFlags.Boarding) != 0)
                    s_StatusTaxiBoarding++;
                if ((flags & TaxiFlags.Returning) != 0)
                    s_StatusTaxiReturning++;
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

                        if (SystemAPI.HasComponent<Resident>(p))
                        {
                            s_StatusPassengerHasResident++;
                            ResidentFlags rf = SystemAPI.GetComponentRO<Resident>(p).ValueRO.m_Flags;
                            if ((rf & ResidentFlags.IgnoreTaxi) != 0)
                                s_StatusPassengerIgnoreTaxi++;
                        }
                    }
                }
            }
        }

        private void TickDebugLogging(Setting setting, float intervalSeconds)
        {
            m_DebugTimerSeconds += SystemAPI.Time.DeltaTime;
            if (m_DebugTimerSeconds < intervalSeconds)
            {
                return;
            }

            m_DebugTimerSeconds = 0f;

            // Reuse snapshot numbers to keep debug logging cheap.
            Mod.s_Log.Info(
                $"{Mod.ModTag} TaxiSummary: " +
                $"taxis={s_StatusTaxisTotal}, transporting={s_StatusTaxiTransporting}, boarding={s_StatusTaxiBoarding}, returning={s_StatusTaxiReturning}, " +
                $"fromOutside={s_StatusTaxiFromOutside}, disabled={s_StatusTaxiDisabled}, withServiceDispatch={s_StatusTaxiWithDispatchBuffer}, " +
                $"requests[stand={s_StatusReqStand}, customer={s_StatusReqCustomer}, outside={s_StatusReqOutside}, none={s_StatusReqNone}], " +
                $"custSeekers(ignoreTaxi={s_StatusReqCustomerSeekerIgnoreTaxi}/{s_StatusReqCustomerSeekerHasResident}), " +
                $"outSeekers(ignoreTaxi={s_StatusReqOutsideSeekerIgnoreTaxi}/{s_StatusReqOutsideSeekerHasResident}), " +
                $"passengers(ignoreTaxi={s_StatusPassengerIgnoreTaxi}/{s_StatusPassengerHasResident}, totalPassengers={s_StatusPassengerTotal}), " +
                $"residents(ignoreTaxi={s_StatusResidentsIgnoreTaxi}/{s_StatusResidentsTotal}, forcedMarker={s_StatusResidentsForcedMarker}), " +
                $"commuters(household ignoreTaxi={s_StatusCommutersIgnoreTaxi}/{s_StatusCommutersTotal}, blockCommutersToo={setting.BlockCommutersToo}), " +
                $"tourists(household ignoreTaxi={s_StatusTouristsIgnoreTaxi}/{s_StatusTouristsTotal}, blockTouristsToo={setting.BlockTouristsToo}), " +
                $"waitingTransport(total={s_StatusWaitingTransportTotal}, taxiStand={s_StatusWaitingTaxiStandTotal}), " +
                $"blockTaxiUsage={setting.BlockTaxiUsage}");
        }
    }
}
