// File: Systems/RiderControlSystem.BlockTaxiStands.cs
// Purpose: When enabled, neutralize TaxiStand-driven taxi demand/supply.
// Notes:
// - Main-thread SystemAPI.Query iteration (no EntityQuery foreach, no ToEntityArray allocations).
// - Resets WaitingPassengers so RouteUtils.GetMaxTaxiCount(...) trends to 0.
// - Clears TaxiStand.RequireVehicles + clears/destroys TaxiRequest entities tied to stands.
// - Detaches RouteVehicle list entries and forces path refresh via PathOwner (no TaxiFlags dependency).

namespace RiderControl
{
    using Game.Common;
    using Game.Pathfind;
    using Game.Routes;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Entities;

    public partial class RiderControlSystem
    {
        private int TickBlockTaxiStandDemand(EntityCommandBuffer ecb)
        {
            int clearedWaitingCount = 0;

            // 1) Destroy ALL Stand-type TaxiRequest entities whose seeker is a TaxiStand.
            foreach ((RefRO<TaxiRequest> req, Entity reqEntity) in SystemAPI
                         .Query<RefRO<TaxiRequest>>()
                         .WithEntityAccess())
            {
                if (req.ValueRO.m_Type != TaxiRequestType.Stand)
                {
                    continue;
                }

                Entity seeker = req.ValueRO.m_Seeker;
                if (seeker != Entity.Null && SystemAPI.HasComponent<TaxiStand>(seeker))
                {
                    ecb.DestroyEntity(reqEntity);
                }
            }

            // 2) Reset stand state + waiting history + clear buffers and detach staged vehicles.
            foreach ((RefRW<TaxiStand> stand, RefRW<WaitingPassengers> waiting, Entity standEntity) in SystemAPI
                         .Query<RefRW<TaxiStand>, RefRW<WaitingPassengers>>()
                         .WithNone<Temp, Deleted>()
                         .WithEntityAccess())
            {
                int count = waiting.ValueRO.m_Count;
                if (count > 0)
                {
                    clearedWaitingCount += count;
                }

                // Reset the actual WaitingPassengers fields that exist in Game.Routes.WaitingPassengers.
                waiting.ValueRW.m_Count = 0;
                waiting.ValueRW.m_OngoingAccumulation = 0;
                waiting.ValueRW.m_ConcludedAccumulation = 0;
                waiting.ValueRW.m_SuccessAccumulation = 0;
                waiting.ValueRW.m_AverageWaitingTime = 0;

                // Clear "RequireVehicles" so the stand stops advertising supply needs.
                stand.ValueRW.m_Flags &= ~TaxiStandFlags.RequireVehicles;

                // If the stand holds a request entity reference, clear it (and destroy it if it exists).
                Entity heldReq = stand.ValueRO.m_TaxiRequest;
                if (heldReq != Entity.Null)
                {
                    stand.ValueRW.m_TaxiRequest = Entity.Null;

                    if (SystemAPI.Exists(heldReq))
                    {
                        ecb.DestroyEntity(heldReq);
                    }
                }

                // Clear dispatched request buffer entries (and destroy request entities).
                if (SystemAPI.HasBuffer<DispatchedRequest>(standEntity))
                {
                    DynamicBuffer<DispatchedRequest> requests = SystemAPI.GetBuffer<DispatchedRequest>(standEntity);

                    for (int i = requests.Length - 1; i >= 0; i--)
                    {
                        Entity r = requests[i].m_VehicleRequest;
                        if (r != Entity.Null && SystemAPI.Exists(r))
                        {
                            ecb.DestroyEntity(r);
                        }
                    }

                    requests.Clear();
                }

                // Detach any vehicles currently linked to this route and force a path refresh.
                if (SystemAPI.HasBuffer<RouteVehicle>(standEntity))
                {
                    DynamicBuffer<RouteVehicle> vehicles = SystemAPI.GetBuffer<RouteVehicle>(standEntity);

                    for (int i = vehicles.Length - 1; i >= 0; i--)
                    {
                        Entity veh = vehicles[i].m_Vehicle;

                        if (veh == Entity.Null || !SystemAPI.Exists(veh))
                        {
                            vehicles.RemoveAt(i);
                            continue;
                        }

                        // Break "vehicle belongs to this route" link.
                        if (SystemAPI.HasComponent<CurrentRoute>(veh))
                        {
                            RefRW<CurrentRoute> cr = SystemAPI.GetComponentRW<CurrentRoute>(veh);
                            if (cr.ValueRO.m_Route == standEntity)
                            {
                                cr.ValueRW.m_Route = Entity.Null;
                            }
                        }

                        // Force a path refresh so the vehicle naturally leaves/repicks behavior.
                        if (SystemAPI.HasComponent<PathOwner>(veh))
                        {
                            RefRW<PathOwner> po = SystemAPI.GetComponentRW<PathOwner>(veh);
                            po.ValueRW.m_State &= ~PathFlags.Failed;
                            po.ValueRW.m_State |= PathFlags.Obsolete;
                        }

                        // Remove from the stand’s vehicle list.
                        vehicles.RemoveAt(i);
                    }
                }
            }

            return clearedWaitingCount;
        }
    }
}
