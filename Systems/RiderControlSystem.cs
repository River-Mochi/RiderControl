// Systems/RiderControlSystem.cs
// Demand-side control:
// - Forces ResidentFlags.IgnoreTaxi (removes taxi from route selection)
// - Unwinds any current taxi waiting state so cims do not freeze
//
// Uses Unity Entities 1.3 SystemAPI.Query patterns (no EntityManager.CreateEntityQuery).

namespace RiderControl
{
    using Game;
    using Game.Creatures;
    using Game.Pathfind;
    using Unity.Entities;

    internal struct RiderControlForcedIgnoreTaxi : IComponentData
    {
    }

    public partial class RiderControlSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier = null!;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        }

        protected override void OnUpdate()
        {
            var settings = Mod.Settings;
            var ecb = m_EndFrameBarrier.CreateCommandBuffer();

            if (!settings.BlockTaxiUsage)
            {
                // Only undo what RiderControl applied.
                foreach (var (resident, entity) in SystemAPI
                             .Query<RefRW<Resident>>()
                             .WithAll<RiderControlForcedIgnoreTaxi>()
                             .WithEntityAccess())
                {
                    resident.ValueRW.m_Flags &= ~ResidentFlags.IgnoreTaxi;
                    ecb.RemoveComponent<RiderControlForcedIgnoreTaxi>(entity);
                }

                return;
            }

            // 1) Remove taxi as a pathfinding option.
            foreach (var (resident, entity) in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithNone<RiderControlForcedIgnoreTaxi>()
                         .WithEntityAccess())
            {
                resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                ecb.AddComponent<RiderControlForcedIgnoreTaxi>(entity);
            }

            // 2) Unstick anyone currently waiting on a taxi lane.
            // Mirrors vanilla ResidentAISystem failure escape hatch:
            // - clear Taxi + ParkingSpace flags
            // - force re-path (Obsolete)
            // - remove RideNeeder so RideNeederSystem won't spawn TaxiRequest
            foreach (var (lane, pathOwner, entity) in SystemAPI
                         .Query<RefRW<HumanCurrentLane>, RefRW<PathOwner>>()
                         .WithEntityAccess())
            {
                if ((lane.ValueRO.m_Flags & CreatureLaneFlags.Taxi) == 0)
                {
                    continue;
                }

                lane.ValueRW.m_Flags &= ~(CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi);

                pathOwner.ValueRW.m_State &= ~PathFlags.Failed;
                pathOwner.ValueRW.m_State |= PathFlags.Obsolete;

                ecb.RemoveComponent<RideNeeder>(entity);
            }

            // 3) Extra safety: remove any remaining RideNeeder while enabled.
            // (Correct SystemAPI usage: query a component, not Entity.)
            foreach (var entity in SystemAPI
                         .Query<RefRO<RideNeeder>>()
                         .WithEntityAccess())
            {
                ecb.RemoveComponent<RideNeeder>(entity);
            }
        }
    }
}
