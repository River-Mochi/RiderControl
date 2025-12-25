## RiderControl (CS2) — Stop Cims Calling Taxis
 
**RiderControl** is a Cities: Skylines II mod that disables taxi usage on the *rider (demand)* side.  
When enabled, cims will choose other travel options (walk, bike, private car, public transport, etc.) instead of requesting taxis.
 
### What this mod does
 
- **Prevents new taxi selection** by forcing `ResidentFlags.IgnoreTaxi`, which causes `RouteUtils.GetTaxiMethods(...)` to effectively remove `PathMethod.Taxi` from trip planning.
- **Prevents “stuck waiting for taxi” behavior** by clearing `CreatureLaneFlags.Taxi` (and `ParkingSpace`) and forcing a re-route (`PathFlags.Obsolete`) for any cim currently in a taxi-wait state.
- **Reduces outside-connection taxi traffic** by preventing the rider-side creation of taxi requests that would otherwise trigger dispatch/spawning.
 
### Vanilla taxi flow (how taxis work in CS2)
 
At a high level, taxis are driven by **requests** created by cims:
 
1. **A cim’s trip planning includes “Taxi” as an allowed method**  
   - When taxi is allowed, the pathfinding / trip logic may pick it based on cost/weights and availability.
 
2. **Cim enters a taxi-wait / taxi-pickup state**  
   - In the simulation, cims that decide to use taxi can end up marked as a taxi “ride needer” (waiting to be served).
 
3. **A `TaxiRequest` entity gets created**  
   - `RideNeederSystem` periodically turns that waiting state into an actual `TaxiRequest` entity.
   - Requests can be regular “customer” requests or “outside” flavored requests when involved with outside connection lanes.
 
4. **Dispatching and spawning happens to satisfy the request**
   - `TaxiDispatchSystem` matches requests to taxi service providers.
   - `TransportDepotAISystem` can spawn taxis (including from outside connections) to fulfill demand.
 
**Key point:** in vanilla, taxis show up because **cims create demand**, which produces **requests**, which triggers **dispatch/spawn**.
 
### How RiderControl changes the flow
 
When the checkbox is enabled:
 
1. **Remove taxi from cim decision-making**
   - RiderControl sets `ResidentFlags.IgnoreTaxi`, so taxi is not considered a valid travel method for cims.
 
2. **Cancel / unwind any current taxi waiting**
   - If a cim is already in a taxi-related waiting state, RiderControl clears taxi wait flags and forces re-pathing immediately.
   - This avoids the “frozen cim waiting forever” problem that can happen if taxis never arrive.
 
3. **Requests stop being generated**
   - With taxi demand removed and taxi waiters cleared, the game stops producing new `TaxiRequest` entities for normal city travel.
   - With no requests to satisfy, taxi dispatch/spawn activity drops sharply (including many cases that originate from outside connections).
 
### Expected results
 
- **Fewer taxis over time** (existing taxis may remain briefly, but fewer new ones should appear).
- **Reduced traffic from taxi fleets**, especially in cities where taxis were heavily used.
- **No stuck cims** waiting indefinitely for a taxi (they should re-route).
 
### Notes / future ideas
 
- This mod focuses on **demand-side** control (rider behavior). It does not hard-delete taxi vehicles.
- There is an optional future hard-guard point inside `TransportDepotAISystem` where `TransportDepotData.m_TransportType == TransportType.Taxi` and `TaxiFlags.FromOutside` are used. RiderControl doesn’t need that for phase 1 because eliminating demand + requests is usually sufficient.
 
### Install / Usage
 
- Install the mod.
- In **Options → Rider Control → Actions**, ensure **“Cims don’t call taxis”** is enabled (default: ON).
- Load a city and let simulation run for a few minutes to see taxi traffic decline.