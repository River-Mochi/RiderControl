// File: Systems/MovingAwayFixSystem.cs
// Purpose: Clears ResidentFlags.IgnoreTransport for moving-away cims so they can use public transport
//          instead of walking to an outside connection.

namespace RiderControl
{
    using Game;
    using Game.Agents;
    using Game.Citizens;
    using Game.Common;
    using Game.Creatures;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Entities;
    using UTime = UnityEngine.Time;

    public sealed partial class MovingAwayFixSystem : GameSystemBase
    {
        // Real-time throttle to avoid constant full scans in giant cities.
        private const double kMinSecondsBetweenRuns = 30.0;

        private double m_LastRunRealtime;
        private ComponentLookup<HouseholdMember> m_HouseholdMemberLookup;
        private ComponentLookup<MovingAway> m_MovingAwayLookup;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_LastRunRealtime = 0.0;
            m_HouseholdMemberLookup = GetComponentLookup<HouseholdMember>(isReadOnly: true);
            m_MovingAwayLookup = GetComponentLookup<MovingAway>(isReadOnly: true);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // Moderate interval; real-time throttle does the heavy lifting.
            return 512;
        }

        protected override void OnUpdate()
        {
            Setting? s = Setting.s_Instance;
            if (s == null || !s.MovingAwayFixHighwayWalkers)
                return;

            double now = UTime.realtimeSinceStartupAsDouble;
            if (now - m_LastRunRealtime < kMinSecondsBetweenRuns)
                return;

            m_LastRunRealtime = now;

            m_HouseholdMemberLookup.Update(this);
            m_MovingAwayLookup.Update(this);

            int cleared = 0;

            foreach ((RefRW<Resident> res, Entity resEntity) in SystemAPI
                         .Query<RefRW<Resident>>()
                         .WithEntityAccess()
                         .WithNone<Deleted, Temp>())
            {
                ResidentFlags flags = res.ValueRO.m_Flags;
                if ((flags & ResidentFlags.IgnoreTransport) == 0)
                    continue;

                Entity citizen = res.ValueRO.m_Citizen;
                if (citizen == Entity.Null || !m_HouseholdMemberLookup.HasComponent(citizen))
                    continue;

                Entity household = m_HouseholdMemberLookup[citizen].m_Household;
                if (household == Entity.Null || !m_MovingAwayLookup.HasComponent(household))
                    continue;

                flags &= ~ResidentFlags.IgnoreTransport;
                res.ValueRW.m_Flags = flags;
                cleared++;
            }

            // Optional: log only when debug logging is enabled and something actually changed.
            if (s.EnableDebugLogging && cleared > 0)
            {
                Mod.s_Log.Info($"[ST] MovingAwayFix: cleared IgnoreTransport for {cleared:N0} moving-away cims.");
            }
        }
    }
}
