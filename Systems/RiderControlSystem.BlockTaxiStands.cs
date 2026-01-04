// File: Systems/RiderControlSystem.BlockTaxiStands.cs
// Purpose: When enabled, neutralize TaxiStand-driven taxi demand/supply (SAFE variant).
// Notes:
// - No ECB, no DestroyEntity.
// - Marks request-like entities with Deleted (game cleans up).
// - Clears WaitingPassengers and detaches RouteVehicle entries.
// - Does NOT touch DispatchedRequest (namespace/type varies by game version; avoid compile breaks).

namespace RiderControl
{
    using Game.Common;      // Deleted, Temp
    using Game.Pathfind;    // PathOwner, PathFlags
    using Game.Routes;      // TaxiStand, TaxiStandFlags, WaitingPassengers, RouteVehicle, CurrentRoute
    using Game.Simulation;  // TaxiRequest, TaxiRequestType
    using Game.Tools;       // Temp
    using Unity.Collections;
    using Unity.Entities;

    public partial class RiderControlSystem
    {
        private int TickBlockTaxiStandDemand()
        {
            int clearedWaitingCount = 0;

            using NativeParallelHashSet<Entity> toDelete = new NativeParallelHashSet<Entity>(256, Allocator.Temp);

            // 1) Collect Stand-type TaxiRequest entities whose seeker is a TaxiStand.
            foreach ((RefRO<TaxiRequest> req, Entity reqEntity) in SystemAPI
                         .Query<RefRO<TaxiRequest>>()
                         .WithEntityAccess()
                         .WithNone<Deleted, Temp>())
            {
                if (req.ValueRO.m_Type != TaxiRequestType.Stand)
                    continue;

                Entity seeker = req.ValueRO.m_Seeker;
                if (seeker != Entity.Null && SystemAPI.HasComponent<TaxiStand>(seeker))
                    toDelete.Add(reqEntity);
            }

            // 2) Reset stand state + waiting history + detach staged vehicles.
            foreach ((RefRW<TaxiStand> stand, RefRW<WaitingPassengers> waiting, Entity standEntity) in SystemAPI
                         .Query<RefRW<TaxiStand>, RefRW<WaitingPassengers>>()
                         .WithNone<Deleted, Temp>()
                         .WithEntityAccess())
            {
                int count = waiting.ValueRO.m_Count;
                if (count > 0)
                    clearedWaitingCount += count;

                waiting.ValueRW.m_Count = 0;
                waiting.ValueRW.m_OngoingAccumulation = 0;
                waiting.ValueRW.m_ConcludedAccumulation = 0;
                waiting.ValueRW.m_SuccessAccumulation = 0;
                waiting.ValueRW.m_AverageWaitingTime = 0;

                stand.ValueRW.m_Flags &= ~TaxiStandFlags.RequireVehicles;

                Entity heldReq = stand.ValueRO.m_TaxiRequest;
                if (heldReq != Entity.Null)
                {
                    stand.ValueRW.m_TaxiRequest = Entity.Null;
                    toDelete.Add(heldReq);
                }

                // RouteVehicle detach (prevents “stuck routing” to the stand).
                if (SystemAPI.HasBuffer<RouteVehicle>(standEntity))
                {
                    DynamicBuffer<RouteVehicle> vehicles = SystemAPI.GetBuffer<RouteVehicle>(standEntity);

                    for (int i = vehicles.Length - 1; i >= 0; i--)
                    {
                        Entity veh = vehicles[i].m_Vehicle;

                        if (veh == Entity.Null || !EntityManager.Exists(veh))
                        {
                            vehicles.RemoveAt(i);
                            continue;
                        }

                        if (SystemAPI.HasComponent<CurrentRoute>(veh))
                        {
                            RefRW<CurrentRoute> cr = SystemAPI.GetComponentRW<CurrentRoute>(veh);
                            if (cr.ValueRO.m_Route == standEntity)
                                cr.ValueRW.m_Route = Entity.Null;
                        }

                        if (SystemAPI.HasComponent<PathOwner>(veh))
                        {
                            RefRW<PathOwner> po = SystemAPI.GetComponentRW<PathOwner>(veh);
                            po.ValueRW.m_State &= ~PathFlags.Failed;
                            po.ValueRW.m_State |= PathFlags.Obsolete;
                        }

                        vehicles.RemoveAt(i);
                    }
                }
            }

            // 3) Mark collected request-like entities as Deleted (game-owned cleanup).
            foreach (Entity e in toDelete)
            {
                if (e == Entity.Null || !EntityManager.Exists(e))
                    continue;

                if (!EntityManager.HasComponent<Deleted>(e))
                    EntityManager.AddComponent<Deleted>(e);
            }

            return clearedWaitingCount;
        }
    }
}
