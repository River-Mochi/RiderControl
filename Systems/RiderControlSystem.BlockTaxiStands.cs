// File: Systems/RiderControlSystem.BlockTaxiStands.cs
// Purpose: When enabled, neutralize TaxiStand-driven taxi demand/supply.
// Notes:
// - Runs INSIDE RiderControlSystem, so SystemAPI is valid.
// - No Entities.ForEach.
// - Avoids SystemAPI.GetBufferRW (not present in Entities 1.3); uses HasBuffer + GetBuffer.

namespace RiderControl
{
    using Game.Common;
    using Game.Pathfind;
    using Game.Routes;
    using Game.Simulation;
    using Game.Tools;
    using Game.Vehicles;
    using Unity.Entities;

    public partial class RiderControlSystem
    {
        /// <summary>
        /// Hard-stops TaxiStand-driven taxi demand/supply by:
        /// - zeroing WaitingPassengers history (so RouteUtils.GetMaxTaxiCount(...) trends to 0)
        /// - clearing TaxiStand.RequireVehicles
        /// - destroying TaxiRequestType.Stand requests
        /// - detaching any taxis currently assigned to the stand so they leave
        /// </summary>
        private int TickBlockTaxiStandDemand(EntityCommandBuffer ecb)
        {
            int clearedWaitingCount = 0;

            // 1) Kill ALL Stand-type TaxiRequest entities (this is the real “demand” object TaxiDispatch uses).
            // We only do this when the feature toggle is on (caller controls that).
            foreach ((RefRO<TaxiRequest> req, Entity reqEntity) in SystemAPI
                         .Query<RefRO<TaxiRequest>>()
                         .WithEntityAccess())
            {
                if (req.ValueRO.m_Type != TaxiRequestType.Stand)
                    continue;

                // Extra safety: only stand requests whose seeker is a TaxiStand entity.
                Entity seeker = req.ValueRO.m_Seeker;
                if (seeker != Entity.Null && SystemAPI.HasComponent<TaxiStand>(seeker))
                {
                    ecb.DestroyEntity(reqEntity);
                }
            }

            // 2) Neutralize the stand itself + detach any staged taxis.
            foreach ((RefRW<TaxiStand> stand, RefRW<WaitingPassengers> waiting, Entity standEntity) in SystemAPI
                         .Query<RefRW<TaxiStand>, RefRW<WaitingPassengers>>()
                         .WithEntityAccess()
                         .WithNone<Temp, Deleted>())
            {
                // (a) Reset the *entire* waiting history, not just m_Count.
                // This is the key to driving RouteUtils.GetMaxTaxiCount(waitingPassengers) to 0.
                if (waiting.ValueRO.m_Count > 0)
                {
                    clearedWaitingCount += waiting.ValueRO.m_Count;
                }

                waiting.ValueRW.m_Count = 0;
                waiting.ValueRW.m_OngoingAccumulation = 0;
                waiting.ValueRW.m_ConcludedAccumulation = 0;
                waiting.ValueRW.m_SuccessAccumulation = 0;
                waiting.ValueRW.m_AverageWaitingTime = 0;

                // (b) Clear “RequireVehicles” so the stand stops advertising supply needs.
                stand.ValueRW.m_Flags &= ~TaxiStandFlags.RequireVehicles;

                // (c) If the stand is holding a taxi request entity pointer, clear it (and destroy if it exists).
                Entity heldReq = stand.ValueRO.m_TaxiRequest;
                if (heldReq != Entity.Null)
                {
                    stand.ValueRW.m_TaxiRequest = Entity.Null;
                    if (SystemAPI.Exists(heldReq))
                    {
                        ecb.DestroyEntity(heldReq);
                    }
                }

                // (d) Clear dispatched-request buffer entries on the stand (and destroy the request entities).
                if (SystemAPI.HasBuffer<DispatchedRequest>(standEntity))
                {
                    DynamicBuffer<DispatchedRequest> requests = SystemAPI.GetBuffer<DispatchedRequest>(standEntity);
                    for (int i = 0; i < requests.Length; i++)
                    {
                        Entity r = requests[i].m_VehicleRequest;
                        if (r != Entity.Null && SystemAPI.Exists(r))
                        {
                            ecb.DestroyEntity(r);
                        }
                    }

                    requests.Clear();
                }

                // (e) Detach any taxis currently assigned to this stand and force them to re-route/return.
                if (SystemAPI.HasBuffer<RouteVehicle>(standEntity))
                {
                    DynamicBuffer<RouteVehicle> vehicles = SystemAPI.GetBuffer<RouteVehicle>(standEntity);

                    for (int i = vehicles.Length - 1; i >= 0; i--)
                    {
                        Entity taxiEntity = vehicles[i].m_Vehicle;

                        if (taxiEntity == Entity.Null || !SystemAPI.Exists(taxiEntity))
                        {
                            vehicles.RemoveAt(i);
                            continue;
                        }

                        // Only touch real taxis.
                        if (SystemAPI.HasComponent<Taxi>(taxiEntity))
                        {
                            RefRW<Taxi> taxi = SystemAPI.GetComponentRW<Taxi>(taxiEntity);

                            TaxiFlags flags = taxi.ValueRO.m_State;
                            flags &= ~(TaxiFlags.Arriving |
                                       TaxiFlags.Requested |
                                       TaxiFlags.Dispatched |
                                       TaxiFlags.Boarding |
                                       TaxiFlags.Disembarking |
                                       TaxiFlags.Transporting);

                            flags |= TaxiFlags.Returning;

                            taxi.ValueRW.m_State = flags;
                            taxi.ValueRW.m_TargetRequest = Entity.Null;
                        }

                        // Break the “this vehicle belongs to this route (stand)” link.
                        if (SystemAPI.HasComponent<CurrentRoute>(taxiEntity))
                        {
                            RefRW<CurrentRoute> cr = SystemAPI.GetComponentRW<CurrentRoute>(taxiEntity);
                            if (cr.ValueRO.m_Route == standEntity)
                            {
                                cr.ValueRW.m_Route = Entity.Null;
                            }
                        }

                        // Force a path refresh.
                        if (SystemAPI.HasComponent<PathOwner>(taxiEntity))
                        {
                            RefRW<PathOwner> po = SystemAPI.GetComponentRW<PathOwner>(taxiEntity);
                            po.ValueRW.m_State &= ~PathFlags.Failed;
                            po.ValueRW.m_State |= PathFlags.Obsolete;
                        }

                        // Remove from the stand’s vehicle list so TaxiStandSystem stops counting it.
                        vehicles.RemoveAt(i);
                    }
                }
            }

            return clearedWaitingCount;
        }
    }
}
