// File: Systems/RiderControlSystem.Core.cs
// Purpose: Demand-side control (SAFE variant):
// - Forces ResidentFlags.IgnoreTaxi (removes taxi from route selection)
// - Unwinds taxi waiting states so cims do not freeze
// - DOES NOT destroy TaxiRequest entities (prevents ECB playback crashes)
// Notes:
// - Unity.Entities source generator needs certain namespaces visible from this file,
//   because the system is partial and SystemAPI queries live in multiple files.

namespace RiderControl
{
   // using Colossal.Serialization.Entities;
    using Game;
    using Game.Citizens;
    using Game.Common;     // Deleted, Temp (needed by generator + query caching)
    using Game.Creatures;  // ResidentFlags, CreatureLaneFlags, Resident, HumanCurrentLane, RideNeeder
    using Game.Pathfind;   // PathOwner, PathFlags
    using Game.Routes;     // TaxiStand, BoardingVehicle
    using Game.Simulation; // TaxiRequest, TaxiRequestType, ServiceDispatch (needed by generator)
    using Game.Tools;
    using Game.Vehicles;   // Taxi
    using Unity.Collections;
    using Unity.Entities;
    using CreatureResident = Game.Creatures.Resident;
    using UTime = UnityEngine.Time;

    internal struct IgnoreTaxiMark : IComponentData
    {
    }

    public partial class RiderControlSystem : GameSystemBase
    {
        // Batch limits to avoid hitching on huge cities.
        private const int kMarkBatchPerUpdate = 2000;
        private const float kUnstickIntervalSeconds = 1.0f;
        private const float kReapplyIntervalSeconds = 2.0f;

        // Cached queries (also ensures the generator “sees” these namespaces from THIS file).
        private EntityQuery m_ResidentsWithoutMarkQuery;
        private EntityQuery m_ResidentsWithMarkQuery;
        private EntityQuery m_RideNeederQuery;
        private EntityQuery m_ResidentLaneQuery;
        private EntityQuery m_TaxiStandQuery;
        private EntityQuery m_TaxiRequestQuery;

        private float m_UnstickTimer;
        private float m_ReapplyTimer;

        protected override void OnCreate()
        {
            base.OnCreate();

            InitStatusSystemsOnCreate();

            // Cache queries with QueryBuilder (preferred for systems).
            m_ResidentsWithoutMarkQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureResident>()
                .WithNone<IgnoreTaxiMark, Deleted, Temp>()
                .Build();

            m_ResidentsWithMarkQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureResident, IgnoreTaxiMark>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_RideNeederQuery = SystemAPI.QueryBuilder()
                .WithAll<RideNeeder, HumanCurrentLane, PathOwner>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_ResidentLaneQuery = SystemAPI.QueryBuilder()
                .WithAll<CreatureResident, HumanCurrentLane, PathOwner>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_TaxiStandQuery = SystemAPI.QueryBuilder()
                .WithAll<TaxiStand, WaitingPassengers>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_TaxiRequestQuery = SystemAPI.QueryBuilder()
                .WithAll<TaxiRequest>()
                .WithNone<Deleted, Temp>()
                .Build();

            // Only run after a city is loaded.
            Enabled = false;
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            bool isRealGame =
                mode == GameMode.Game &&
                (purpose == Colossal.Serialization.Entities.Purpose.NewGame || purpose == Colossal.Serialization.Entities.Purpose.LoadGame);

            if (!isRealGame)
                return;

            m_UnstickTimer = 0f;
            m_ReapplyTimer = 0f;

            ResetDebugOnCityLoaded();
            ResetStatusOnCityLoaded();

            Enabled = true;

#if DEBUG
            Mod.s_Log.Info($"{Mod.ModTag} RiderControlSystem enabled (city load complete).");
#endif
        }

        protected override void OnUpdate()
        {
            var setting = Mod.Setting;
            if (setting is null)
            {
                Enabled = false;
                return;
            }

            int appliedIgnoreTaxi = 0;
            int skippedCommuters = 0;
            int skippedTourists = 0;

            int clearedTaxiLaneWaiting = 0;
            int clearedTaxiStandWaiting = 0;

            int clearedRideNeederLinks = 0;
            int clearedTaxiStandWaitingPassengers = 0;

            // --- OFF: undo only what we marked (batched) ---
            if (!setting.BlockTaxiUsage)
            {
                using NativeList<Entity> toUnmark = new NativeList<Entity>(Allocator.Temp);

                int processed = 0;
                foreach ((RefRW<CreatureResident> resident, Entity entity) in SystemAPI
                             .Query<RefRW<CreatureResident>>()
                             .WithAll<IgnoreTaxiMark>()
                             .WithEntityAccess())
                {
                    resident.ValueRW.m_Flags &= ~ResidentFlags.IgnoreTaxi;
                    toUnmark.Add(entity);

                    processed++;
                    if (processed >= kMarkBatchPerUpdate)
                        break;
                }

                if (toUnmark.Length > 0)
                    EntityManager.RemoveComponent<IgnoreTaxiMark>(toUnmark.AsArray());

                // Keep status ticking while we unwind in batches.
                TickStatusSnapshot();

                if (setting.EnableDebugLogging)
                    TickDebugLogging(setting, 10f, 0);

                return;
            }

            // --- ON: mark + force IgnoreTaxi (batched) ---
            using (NativeList<Entity> toMark = new NativeList<Entity>(Allocator.Temp))
            {
                int processed = 0;

                foreach ((RefRW<CreatureResident> resident, Entity entity) in SystemAPI
                             .Query<RefRW<CreatureResident>>()
                             .WithNone<IgnoreTaxiMark>()
                             .WithEntityAccess())
                {
                    // Skip commuter/tourist households if those blocks are OFF.
                    Entity citizenEntity = resident.ValueRO.m_Citizen;
                    if (citizenEntity != Entity.Null && SystemAPI.HasComponent<HouseholdMember>(citizenEntity))
                    {
                        Entity household = SystemAPI.GetComponentRO<HouseholdMember>(citizenEntity).ValueRO.m_Household;

                        if (household != Entity.Null)
                        {
                            if (!setting.BlockCommuters && SystemAPI.HasComponent<CommuterHousehold>(household))
                            {
                                skippedCommuters++;
                                continue;
                            }

                            if (!setting.BlockTourists && SystemAPI.HasComponent<TouristHousehold>(household))
                            {
                                skippedTourists++;
                                continue;
                            }
                        }
                    }

                    resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                    toMark.Add(entity);
                    appliedIgnoreTaxi++;

                    processed++;
                    if (processed >= kMarkBatchPerUpdate)
                        break;
                }

                if (toMark.Length > 0)
                    EntityManager.AddComponent<IgnoreTaxiMark>(toMark.AsArray());
            }

            // Periodic re-apply for already-marked residents (avoids full scan every frame).
            m_ReapplyTimer += UTime.unscaledDeltaTime;
            if (m_ReapplyTimer >= kReapplyIntervalSeconds)
            {
                m_ReapplyTimer = 0f;

                foreach (RefRW<CreatureResident> resident in SystemAPI
                             .Query<RefRW<CreatureResident>>()
                             .WithAll<IgnoreTaxiMark>())
                {
                    if ((resident.ValueRO.m_Flags & ResidentFlags.IgnoreTaxi) == 0)
                        resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                }
            }

            // Unstick logic (run on an interval, not every frame).
            m_UnstickTimer += UTime.unscaledDeltaTime;
            if (m_UnstickTimer >= kUnstickIntervalSeconds)
            {
                m_UnstickTimer = 0f;

                // 3) Unstick taxi-wait posture using RideNeeder as the filter.
                foreach ((RefRW<RideNeeder> rn,
                          RefRW<HumanCurrentLane> lane,
                          RefRW<PathOwner> pathOwner) in SystemAPI
                             .Query<RefRW<RideNeeder>, RefRW<HumanCurrentLane>, RefRW<PathOwner>>())
                {
                    var taxiWaitMask = CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi;

                    if ((lane.ValueRO.m_Flags & taxiWaitMask) != taxiWaitMask)
                        continue;

                    lane.ValueRW.m_Flags &= ~taxiWaitMask;
                    lane.ValueRW.m_QueueEntity = Entity.Null;

                    if (rn.ValueRO.m_RideRequest != Entity.Null)
                    {
                        rn.ValueRW.m_RideRequest = Entity.Null;
                        clearedRideNeederLinks++;
                    }

                    pathOwner.ValueRW.m_State &= ~PathFlags.Failed;
                    pathOwner.ValueRW.m_State |= PathFlags.Obsolete;

                    clearedTaxiLaneWaiting++;
                }

                // 4) Unstick residents waiting at taxi stands / taxi boarding vehicles.
                foreach ((RefRW<CreatureResident> resident,
                          RefRW<HumanCurrentLane> lane,
                          RefRW<PathOwner> pathOwner) in SystemAPI
                             .Query<RefRW<CreatureResident>, RefRW<HumanCurrentLane>, RefRW<PathOwner>>())
                {
                    if ((resident.ValueRO.m_Flags & ResidentFlags.WaitingTransport) == 0)
                        continue;

                    Entity q = lane.ValueRO.m_QueueEntity;
                    if (q == Entity.Null)
                        continue;

                    bool isTaxiQueue = SystemAPI.HasComponent<TaxiStand>(q);

                    if (!isTaxiQueue && SystemAPI.HasComponent<BoardingVehicle>(q))
                    {
                        BoardingVehicle bv = SystemAPI.GetComponentRO<BoardingVehicle>(q).ValueRO;
                        if (bv.m_Vehicle != Entity.Null && SystemAPI.HasComponent<Taxi>(bv.m_Vehicle))
                            isTaxiQueue = true;
                    }

                    if (!isTaxiQueue)
                        continue;

                    resident.ValueRW.m_Flags &= ~ResidentFlags.WaitingTransport;
                    lane.ValueRW.m_QueueEntity = Entity.Null;
                    lane.ValueRW.m_Flags &= ~(CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi);

                    pathOwner.ValueRW.m_State &= ~PathFlags.Failed;
                    pathOwner.ValueRW.m_State |= PathFlags.Obsolete;

                    clearedTaxiStandWaiting++;
                }

                // 5) Stand-side block (optional).
                if (setting.BlockTaxiStandDemand)
                    clearedTaxiStandWaitingPassengers = TickBlockTaxiStandDemand();
            }

            // Status fields (defined in Status partial).
            s_StatusLastAppliedIgnoreTaxi = appliedIgnoreTaxi;
            s_StatusLastSkippedCommuters = skippedCommuters;
            s_StatusLastSkippedTourists = skippedTourists;
            s_StatusLastClearedTaxiLaneWaiting = clearedTaxiLaneWaiting;
            s_StatusLastClearedTaxiStandWaiting = clearedTaxiStandWaiting;
            s_StatusLastRemovedRideNeeder = clearedRideNeederLinks;

            TickStatusSnapshot();

            if (setting.EnableDebugLogging)
                TickDebugLogging(setting, 10f, clearedTaxiStandWaitingPassengers);
        }
    }
}
