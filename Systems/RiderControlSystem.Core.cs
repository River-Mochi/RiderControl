// File: Systems/RiderControlSystem.Core.cs
// Purpose: Demand-side control:
// - Forces ResidentFlags.IgnoreTaxi (removes taxi from route selection)
// - Unwinds taxi waiting states so cims do not freeze
// - Cancels dispatchable TaxiRequest entities (non-stand)
// - Delegates Status and Debug helpers to partial files

using Game;
using Game.Citizens;
using Game.Creatures;
using Game.Pathfind;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using LoadPurpose = Colossal.Serialization.Entities.Purpose;

namespace RiderControl
{
    internal struct IgnoreTaxiMark : IComponentData
    {
    }

    public partial class RiderControlSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier = null!;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            InitStatusSystemsOnCreate();

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

            ResetDebugOnCityLoaded();
            ResetStatusOnCityLoaded();

            Enabled = true;

#if DEBUG
            Mod.s_Log.Info($"{Mod.ModTag} RiderControlSystem enabled (city load complete).");
#endif
        }

        protected override void OnUpdate()
        {
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

            int clearedRideNeederLinks = 0;
            int destroyedTaxiRequests = 0;

            int clearedTaxiStandWaitingPassengers = 0;

            if (!setting.BlockTaxiUsage)
            {
                // Undo only for entities marked by this mod.
                foreach ((RefRW<Resident> resident, Entity entity) in SystemAPI
                             .Query<RefRW<Resident>>()
                             .WithAll<IgnoreTaxiMark>()
                             .WithEntityAccess())
                {
                    resident.ValueRW.m_Flags &= ~global::Game.Creatures.ResidentFlags.IgnoreTaxi;
                    ecb.RemoveComponent<IgnoreTaxiMark>(entity);
                }

                TickStatusSnapshot();

                if (setting.EnableDebugLogging)
                {
                    TickDebugLogging(setting, 10f, 0);
                }

                return;
            }

            // 1) Force IgnoreTaxi for unmarked residents (optionally allow commuter/tourist households).
            foreach ((RefRW<Resident> resident, Entity entity) in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithNone<IgnoreTaxiMark>()
                         .WithEntityAccess())
            {
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

                resident.ValueRW.m_Flags |= global::Game.Creatures.ResidentFlags.IgnoreTaxi;
                ecb.AddComponent<IgnoreTaxiMark>(entity);
                appliedIgnoreTaxi++;
            }

            // 2) Re-apply IgnoreTaxi for already-marked residents (cheap “keep it on”).
            foreach (RefRW<Resident> resident in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithAll<IgnoreTaxiMark>())
            {
                if ((resident.ValueRO.m_Flags & global::Game.Creatures.ResidentFlags.IgnoreTaxi) == 0)
                {
                    resident.ValueRW.m_Flags |= global::Game.Creatures.ResidentFlags.IgnoreTaxi;
                }
            }

            // 3) Unstick targeted taxi-wait posture using RideNeeder as the filter.
            foreach ((RefRW<RideNeeder> rn,
                      RefRW<HumanCurrentLane> lane,
                      RefRW<PathOwner> pathOwner) in SystemAPI
                         .Query<RefRW<RideNeeder>, RefRW<HumanCurrentLane>, RefRW<PathOwner>>())
            {
                var taxiWaitMask = global::Game.Creatures.CreatureLaneFlags.ParkingSpace | global::Game.Creatures.CreatureLaneFlags.Taxi;

                if ((lane.ValueRO.m_Flags & taxiWaitMask) != taxiWaitMask)
                {
                    continue;
                }

                lane.ValueRW.m_Flags &= ~taxiWaitMask;
                lane.ValueRW.m_QueueEntity = Entity.Null;

                if (rn.ValueRO.m_RideRequest != Entity.Null)
                {
                    rn.ValueRW.m_RideRequest = Entity.Null;
                    clearedRideNeederLinks++;
                }

                pathOwner.ValueRW.m_State &= ~global::Game.Pathfind.PathFlags.Failed;
                pathOwner.ValueRW.m_State |= global::Game.Pathfind.PathFlags.Obsolete;

                clearedTaxiLaneWaiting++;
            }

            // 4) Unstick residents waiting at taxi stands / taxi boarding vehicles.
            foreach ((RefRW<Resident> resident,
                      RefRW<HumanCurrentLane> lane,
                      RefRW<PathOwner> pathOwner) in SystemAPI
                         .Query<RefRW<Resident>, RefRW<HumanCurrentLane>, RefRW<PathOwner>>())
            {
                if ((resident.ValueRO.m_Flags & global::Game.Creatures.ResidentFlags.WaitingTransport) == 0)
                {
                    continue;
                }

                Entity q = lane.ValueRO.m_QueueEntity;
                if (q == Entity.Null)
                {
                    continue;
                }

                bool isTaxiQueue = SystemAPI.HasComponent<TaxiStand>(q);

                if (!isTaxiQueue && SystemAPI.HasComponent<BoardingVehicle>(q))
                {
                    BoardingVehicle bv = SystemAPI.GetComponentRO<BoardingVehicle>(q).ValueRO;

                    // Yes: Taxi is a component in Game.Vehicles; with `using Game.Vehicles;` this is correct.
                    // If VS still acts up, change to: SystemAPI.HasComponent<global::Game.Vehicles.Taxi>(...)
                    if (bv.m_Vehicle != Entity.Null && SystemAPI.HasComponent<Taxi>(bv.m_Vehicle))
                    {
                        isTaxiQueue = true;
                    }
                }

                if (!isTaxiQueue)
                {
                    continue;
                }

                resident.ValueRW.m_Flags &= ~global::Game.Creatures.ResidentFlags.WaitingTransport;

                lane.ValueRW.m_QueueEntity = Entity.Null;
                lane.ValueRW.m_Flags &= ~(global::Game.Creatures.CreatureLaneFlags.ParkingSpace | global::Game.Creatures.CreatureLaneFlags.Taxi);

                pathOwner.ValueRW.m_State &= ~global::Game.Pathfind.PathFlags.Failed;
                pathOwner.ValueRW.m_State |= global::Game.Pathfind.PathFlags.Obsolete;

                clearedTaxiStandWaiting++;
            }

            // 5) Cancel non-stand TaxiRequest entities so dispatch doesn’t keep feeding taxis.
            foreach ((RefRO<TaxiRequest> reqRef, Entity reqEntity) in SystemAPI
                         .Query<RefRO<TaxiRequest>>()
                         .WithEntityAccess())
            {
                if (reqRef.ValueRO.m_Type == TaxiRequestType.Stand)
                {
                    continue;
                }

                ecb.DestroyEntity(reqEntity);
                destroyedTaxiRequests++;
            }

            // 6) Stand-side block (optional).
            if (setting.BlockTaxiStandDemand)
            {
                clearedTaxiStandWaitingPassengers = TickBlockTaxiStandDemand(ecb);
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
            {
                TickDebugLogging(setting, 10f, clearedTaxiStandWaitingPassengers);
            }
        }
    }
}
