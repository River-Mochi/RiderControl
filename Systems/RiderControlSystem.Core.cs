// File: Systems/RiderControlSystem.Core.cs
// Purpose: Demand-side taxi control (SAFE variant):
// - Incrementally applies ResidentFlags.IgnoreTaxi for selected residents (batched)
// - Unwinds taxi waiting states so cims don't freeze (interval-based)
// - Taxi-stand demand block tick lives in RiderControlSystem.BlockTaxiStands.cs (Core doesn't own its interval)
// Notes:
// - Never touch Deleted/Temp entities.
// - Never use SystemAPI from static methods (Entities source-gen limitation).

namespace RiderControl
{
    using Game;
    using Game.Citizens;        // HouseholdMember, CommuterHousehold, TouristHousehold
    using Game.Common;          // Deleted
    using Game.Creatures;       // ResidentFlags, HumanCurrentLane, CreatureLaneFlags, RideNeeder
    using Game.Pathfind;        // PathOwner, PathFlags
    using Game.Routes;          // TaxiStand, BoardingVehicle
   // using Game.Simulation;
    using Game.Tools;           // Temp
    using Game.Vehicles;        // Taxi
    using Unity.Collections;
    using Unity.Entities;
    using CreatureResident = Game.Creatures.Resident;
    using UTime = UnityEngine.Time;

    internal struct IgnoreTaxiMark : IComponentData
    {
    }
    public partial class RiderControlSystem : GameSystemBase
    {
        // -----------------------
        // Knobs (perf + behavior)
        // -----------------------

        // Batch size for applying/removing IgnoreTaxi each update (limits hitching in huge cities).
        private const int kMarkBatchPerUpdate = 2000;

        // Unstick taxi waiting states on an interval (not every frame).
        private const float kUnstickIntervalSeconds = 1.0f;

        // -----------------------
        // Timers
        // -----------------------

        private float m_UnstickTimer;

        protected override void OnCreate()
        {
            base.OnCreate();

            InitStatusSystemsOnCreate();

            // Only run after a real city is loaded.
            Enabled = false;
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            bool isRealGame =
                mode == GameMode.Game &&
                (purpose == Colossal.Serialization.Entities.Purpose.NewGame ||
                 purpose == Colossal.Serialization.Entities.Purpose.LoadGame);

            if (!isRealGame)
                return;

            m_UnstickTimer = 0f;

            ResetDebugOnCityLoaded();
            ResetStatusOnCityLoaded();
            ResetBlockTaxiStandsOnCityLoaded(); // interval/timer lives in BlockTaxiStands.cs

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

            // -----------------------
            // OFF: unwind in batches
            // -----------------------
            if (!setting.BlockTaxiUsage)
            {
                UnmarkIgnoreTaxiBatch(out _);

                // If the stand toggle is on but main feature is off, do nothing (and reset its timer).
                TickBlockTaxiStandDemandInterval(enabled: false);

                TickStatusSnapshot();

                if (setting.EnableDebugLogging)
                    TickDebugLogging(setting, 10f, 0);

                return;
            }

            // -----------------------
            // ON: apply + maintain
            // -----------------------
            ApplyIgnoreTaxiBatch(setting, out appliedIgnoreTaxi, out skippedCommuters, out skippedTourists);

            // Unstick pass (interval-based)
            m_UnstickTimer += UTime.unscaledDeltaTime;
            if (m_UnstickTimer >= kUnstickIntervalSeconds)
            {
                m_UnstickTimer = 0f;

                UnstickTaxiLaneWaiters(out clearedTaxiLaneWaiting, out clearedRideNeederLinks);
                UnstickTaxiQueues(out clearedTaxiStandWaiting);
            }

            // Taxi-stand demand block (interval lives in BlockTaxiStands.cs).
            clearedTaxiStandWaitingPassengers =
                TickBlockTaxiStandDemandInterval(enabled: setting.BlockTaxiStandDemand);

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

        // -----------------------
        // Helpers
        // -----------------------

        private void UnmarkIgnoreTaxiBatch(out int unmarkedCount)
        {
            unmarkedCount = 0;

            using NativeList<Entity> toUnmark = new NativeList<Entity>(Allocator.Temp);

            int processed = 0;
            foreach ((RefRW<CreatureResident> resident, Entity entity) in SystemAPI
                         .Query<RefRW<CreatureResident>>()
                         .WithAll<IgnoreTaxiMark>()
                         .WithNone<Deleted, Temp>()
                         .WithEntityAccess())
            {
                resident.ValueRW.m_Flags &= ~ResidentFlags.IgnoreTaxi;
                toUnmark.Add(entity);

                processed++;
                if (processed >= kMarkBatchPerUpdate)
                    break;
            }

            if (toUnmark.Length > 0)
            {
                EntityManager.RemoveComponent<IgnoreTaxiMark>(toUnmark.AsArray());
                unmarkedCount = toUnmark.Length;
            }
        }

        private void ApplyIgnoreTaxiBatch(
            Setting setting,
            out int applied,
            out int skippedCommuters,
            out int skippedTourists)
        {
            applied = 0;
            skippedCommuters = 0;
            skippedTourists = 0;

            using NativeList<Entity> toMark = new NativeList<Entity>(Allocator.Temp);

            int processed = 0;
            foreach ((RefRW<CreatureResident> resident, Entity entity) in SystemAPI
                         .Query<RefRW<CreatureResident>>()
                         .WithNone<IgnoreTaxiMark, Deleted, Temp>()
                         .WithEntityAccess())
            {
                // Skip commuter/tourist households if those blocks are OFF.
                Entity citizenEntity = resident.ValueRO.m_Citizen;
                if (citizenEntity != Entity.Null && SystemAPI.HasComponent<HouseholdMember>(citizenEntity))
                {
                    Entity household =
                        SystemAPI.GetComponentRO<HouseholdMember>(citizenEntity).ValueRO.m_Household;

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

                // Apply IgnoreTaxi and mark so we never rescan the whole population.
                resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                toMark.Add(entity);
                applied++;

                processed++;
                if (processed >= kMarkBatchPerUpdate)
                    break;
            }

            if (toMark.Length > 0)
                EntityManager.AddComponent<IgnoreTaxiMark>(toMark.AsArray());
        }

        private void UnstickTaxiLaneWaiters(out int clearedTaxiLaneWaiting, out int clearedRideNeederLinks)
        {
            clearedTaxiLaneWaiting = 0;
            clearedRideNeederLinks = 0;

            // NOTE: Not all RideNeeder entities are guaranteed to be residents; so we keep this broad,
            // but if the entity is a resident we also enforce IgnoreTaxi "when it matters" (no global sweep).
            foreach ((RefRW<RideNeeder> rn,
                      RefRW<HumanCurrentLane> lane,
                      RefRW<PathOwner> pathOwner,
                      Entity e) in SystemAPI
                         .Query<RefRW<RideNeeder>, RefRW<HumanCurrentLane>, RefRW<PathOwner>>()
                         .WithNone<Deleted, Temp>()
                         .WithEntityAccess())
            {
                var taxiWaitMask = CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi;

                if ((lane.ValueRO.m_Flags & taxiWaitMask) != taxiWaitMask)
                    continue;

                // Enforce IgnoreTaxi for any resident we touch here (cheap + targeted).
                if (SystemAPI.HasComponent<CreatureResident>(e))
                {
                    RefRW<CreatureResident> resident = SystemAPI.GetComponentRW<CreatureResident>(e);
                    if ((resident.ValueRO.m_Flags & ResidentFlags.IgnoreTaxi) == 0)
                        resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                }

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
        }

        private void UnstickTaxiQueues(out int clearedTaxiStandWaiting)
        {
            clearedTaxiStandWaiting = 0;

            foreach ((RefRW<CreatureResident> resident,
                      RefRW<HumanCurrentLane> lane,
                      RefRW<PathOwner> pathOwner) in SystemAPI
                         .Query<RefRW<CreatureResident>, RefRW<HumanCurrentLane>, RefRW<PathOwner>>()
                         .WithNone<Deleted, Temp>())
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

                // Enforce IgnoreTaxi for the resident we are un-sticking (targeted, no sweep).
                if ((resident.ValueRO.m_Flags & ResidentFlags.IgnoreTaxi) == 0)
                    resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;

                resident.ValueRW.m_Flags &= ~ResidentFlags.WaitingTransport;
                lane.ValueRW.m_QueueEntity = Entity.Null;
                lane.ValueRW.m_Flags &= ~(CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi);

                pathOwner.ValueRW.m_State &= ~PathFlags.Failed;
                pathOwner.ValueRW.m_State |= PathFlags.Obsolete;

                clearedTaxiStandWaiting++;
            }
        }
    }
}
