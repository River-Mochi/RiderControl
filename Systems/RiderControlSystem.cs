// Systems/RiderControlSystem.cs
// Demand-side control:
// - Forces ResidentFlags.IgnoreTaxi (removes taxi from route selection)
// - Unwinds any current taxi waiting state so cims do not freeze
// - Optional debug logging to confirm what's still generating taxi usage
 
namespace RiderControl
{
    using Game;
    using Game.Creatures;
    using Game.Objects;
    using Game.Pathfind;
    using Game.Simulation;
    using Game.Vehicles;
    using Unity.Entities;
 
    internal struct RiderControlForcedIgnoreTaxi : IComponentData
    {
    }
 
    public partial class RiderControlSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier = null!;
        private float m_DebugTimerSeconds;
 
        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        }
 
        protected override void OnUpdate()
        {
            Setting? setting = Mod.Setting;
            if (setting == null)
            {
                return;
            }
 
            EntityCommandBuffer ecb = m_EndFrameBarrier.CreateCommandBuffer();
 
            if (!setting.BlockTaxiUsage)
            {
                foreach ((RefRW<Resident> resident, Entity entity) in SystemAPI
                             .Query<RefRW<Resident>>()
                             .WithAll<RiderControlForcedIgnoreTaxi>()
                             .WithEntityAccess())
                {
                    resident.ValueRW.m_Flags &= ~ResidentFlags.IgnoreTaxi;
                    ecb.RemoveComponent<RiderControlForcedIgnoreTaxi>(entity);
                }
 
                if (setting.EnableDebugLogging)
                {
                    TickDebugLogging(setting, 10f);
                }
 
                return;
            }
 
            // 1) Remove taxi as a pathfinding option for residents.
            foreach ((RefRW<Resident> resident, Entity entity) in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithNone<RiderControlForcedIgnoreTaxi>()
                         .WithEntityAccess())
            {
                resident.ValueRW.m_Flags |= ResidentFlags.IgnoreTaxi;
                ecb.AddComponent<RiderControlForcedIgnoreTaxi>(entity);
            }
 
            // 2) Unstick anyone currently waiting on a taxi lane.
            foreach ((RefRW<HumanCurrentLane> lane, RefRW<PathOwner> pathOwner, Entity entity) in SystemAPI
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
            foreach ((RefRO<RideNeeder> _, Entity entity) in SystemAPI
                         .Query<RefRO<RideNeeder>>()
                         .WithEntityAccess())
            {
                ecb.RemoveComponent<RideNeeder>(entity);
            }
 
            if (setting.EnableDebugLogging)
            {
                TickDebugLogging(setting, 10f);
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
            LogTaxiSummary(setting);
        }
 
        private void LogTaxiSummary(Setting setting)
        {
            int taxis = 0;
            int dispatched = 0;
            int requested = 0;
            int boarding = 0;
            int transporting = 0;
            int returning = 0;
            int fromOutside = 0;
            int disabled = 0;
 
            int stopped = 0;
            int parkedCar = 0;
            int withDispatchBuffer = 0;
 
            foreach ((RefRO<Taxi> taxiRef, Entity taxiEntity) in SystemAPI
                         .Query<RefRO<Taxi>>()
                         .WithEntityAccess())
            {
                taxis++;
 
                TaxiFlags flags = taxiRef.ValueRO.m_State;
 
                if ((flags & TaxiFlags.Dispatched) != 0)
                {
                    dispatched++;
                }
 
                if ((flags & TaxiFlags.Requested) != 0)
                {
                    requested++;
                }
 
                if ((flags & TaxiFlags.Boarding) != 0)
                {
                    boarding++;
                }
 
                if ((flags & TaxiFlags.Transporting) != 0)
                {
                    transporting++;
                }
 
                if ((flags & TaxiFlags.Returning) != 0)
                {
                    returning++;
                }
 
                if ((flags & TaxiFlags.FromOutside) != 0)
                {
                    fromOutside++;
                }
 
                if ((flags & TaxiFlags.Disabled) != 0)
                {
                    disabled++;
                }
 
                if (SystemAPI.HasComponent<Stopped>(taxiEntity))
                {
                    stopped++;
                }
 
                if (SystemAPI.HasComponent<ParkedCar>(taxiEntity))
                {
                    parkedCar++;
                }
 
                if (SystemAPI.HasBuffer<ServiceDispatch>(taxiEntity))
                {
                    DynamicBuffer<ServiceDispatch> buf = SystemAPI.GetBuffer<ServiceDispatch>(taxiEntity);
                    if (buf.IsCreated && buf.Length > 0)
                    {
                        withDispatchBuffer++;
                    }
                }
            }
 
            int rideNeeders = 0;
            foreach (RefRO<RideNeeder> _ in SystemAPI.Query<RefRO<RideNeeder>>())
            {
                rideNeeders++;
            }
 
            int taxiRequests = 0;
            foreach (RefRO<TaxiRequest> _ in SystemAPI.Query<RefRO<TaxiRequest>>())
            {
                taxiRequests++;
            }
 
            Mod.s_Log.Info(
                $"{Mod.ModTag} TaxiSummary: taxis={taxis}, transporting={transporting}, boarding={boarding}, dispatched={dispatched}, requested={requested}, " +
                $"returning={returning}, fromOutside={fromOutside}, disabled={disabled}, stopped={stopped}, parkedCar={parkedCar}, " +
                $"withServiceDispatch={withDispatchBuffer}, rideNeeders={rideNeeders}, taxiRequests={taxiRequests}, " +
                $"blockTaxiUsage={setting.BlockTaxiUsage}");
        }
    }
}
