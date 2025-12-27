// Systems/RiderControlSystem.Core.cs
// Demand-side control:
// - Forces ResidentFlags.IgnoreTaxi (removes taxi from route selection)
// - Unwinds taxi waiting states so cims do not freeze
// - Cancels dispatchable TaxiRequest entities (non-stand)
// - Delegates Status and Debug helpers to partial files

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
            int clearedTaxiStandWaitingPassengers = 0;

            if (!setting.BlockTaxiUsage)
            {
                // Undo only for entities marked by this mod.
                foreach ((RefRW<Resident> resident, Entity entity) in SystemAPI
                             .Query<RefRW<Resident>>()
                             .WithAll<IgnoreTaxiMark>()
                             .WithEntityAccess())
                {
                    resident.ValueRW.m_Flags &= ~ResidentFlags.IgnoreTaxi;
                    ecb.RemoveComponent<IgnoreTaxiMark>(entity);
                }

                // Keep status fresh-ish even when not blocking.
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
                        // Toggle OFF => early bail (allow taxis). Toggle ON => apply IgnoreTaxi below.
                        if (!setting.BlockCommuters && SystemAPI.HasComponent<CommuterHousehold>(household))
                        {
                            skippedCommuters++;
                            continue;
                        }

                        // Toggle OFF => early bail (allow taxis). Toggle ON => apply IgnoreTaxi below.
                        if (!setting.BlockTourists && SystemAPI.HasComponent<TouristHousehold>(household))
                        {
                            skippedTourists++;
                            continue;
                        }
                    }
                }

                resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                ecb.AddComponent<IgnoreTaxiMark>(entity);
                appliedIgnoreTaxi++;
            }

            // 1b) Re-apply IgnoreTaxi for already-marked residents.
            // Some vanilla systems can clear IgnoreTaxi; this makes it stick.
            foreach (RefRW<Resident> resident in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithAll<IgnoreTaxiMark>())
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

            // 3b) Phase B (optional): hard stop taxi-stand pickups:
            // TaxiStand "Passengers waiting" is tracked on the TaxiStand via WaitingPassengers.m_Count, and TaxiAISystem
            // uses it even when there are no TaxiRequest entities. Clearing it prevents stand-based taxi trips.
            if (setting.BlockTaxiStandDemand)
            {
                foreach (RefRW<WaitingPassengers> waiting in SystemAPI.Query<RefRW<WaitingPassengers>>().WithAll<TaxiStand>())
                {
                    int c = waiting.ValueRO.m_Count;
                    if (c <= 0)
                        continue;

                    clearedTaxiStandWaitingPassengers += c;
                    waiting.ValueRW.m_Count = 0;
                    waiting.ValueRW.m_OngoingAccumulation = 0;
                }
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

                    // Extra safety: if the seeker has RideNeeder and it points at this request, clear it.
                    // (Entity.Null is a valid ECS sentinel, not a C# null.)
                    Entity seeker = reqRef.ValueRO.m_Seeker;
                    if (seeker != Entity.Null && SystemAPI.HasComponent<RideNeeder>(seeker))
                    {
                        RefRW<RideNeeder> rn = SystemAPI.GetComponentRW<RideNeeder>(seeker);
                        if (rn.ValueRO.m_RideRequest == reqEntity)
                        {
                            rn.ValueRW.m_RideRequest = Entity.Null;
                        }
                    }

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
                TickDebugLogging(setting, 10f, clearedTaxiStandWaitingPassengers);
            }
        }
    }
}
