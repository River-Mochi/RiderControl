// File: Systems/MovingAwayFixSystem.cs
// Purpose: Clears ResidentFlags.IgnoreTransport for Moving-away cims so they can use public transport
//          instead of walking all the way to the outside connection.

namespace RiderControl
{
    using Game;
    using Game.Agents;              // MovingAway (on Household)
    using Game.Citizens;            // HouseholdMember
    using Game.Common;              // Deleted
    using Game.Creatures;           // Resident, ResidentFlags
    using Game.Pathfind;            // PathOwner, PathFlags
    using Game.Tools;               // Temp
    using Unity.Burst.Intrinsics;   // v128 (IJobChunk signature)
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public sealed partial class MovingAwayFixSystem : GameSystemBase
    {
        private EntityQuery m_Query;

        private ComponentLookup<HouseholdMember> m_HouseholdMemberLookup;
        private ComponentLookup<MovingAway> m_MovingAwayLookup;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 1024; // 512 = more frequent, 1024 = less frequent
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            // Only residents currently “active” enough to have a path.
            m_Query = SystemAPI.QueryBuilder()
                .WithAll<Resident, PathOwner>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_HouseholdMemberLookup = GetComponentLookup<HouseholdMember>(isReadOnly: true);
            m_MovingAwayLookup = GetComponentLookup<MovingAway>(isReadOnly: true);

            RequireForUpdate(m_Query);
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

#if DEBUG
            RiderControlSystem.s_DebugLastMovingAwayFixCleared = 0;
            RiderControlSystem.s_DebugTotalMovingAwayFixCleared = 0;
#endif

            Enabled = true;
        }

        protected override void OnUpdate()
        {
            var setting = Mod.Setting;
            if (setting == null || !setting.FixMovingAwayHighwayWalkers)
                return;

            m_HouseholdMemberLookup.Update(this);
            m_MovingAwayLookup.Update(this);

#if DEBUG
            // Dev builds: complete so Advanced Debug shows real counts immediately.
            NativeArray<int> cleared = new NativeArray<int>(1, Allocator.TempJob);
#endif

            JobHandle handle = new ClearIgnoreTransportJob
            {
                ResidentType = SystemAPI.GetComponentTypeHandle<Resident>(isReadOnly: false),
                PathOwnerType = SystemAPI.GetComponentTypeHandle<PathOwner>(isReadOnly: false),
                HouseholdMembers = m_HouseholdMemberLookup,
                MovingAways = m_MovingAwayLookup,
#if DEBUG
                ClearedCount = cleared,
#endif
            }
#if DEBUG
            .Schedule(m_Query, Dependency);
#else
            .ScheduleParallel(m_Query, Dependency);
#endif

#if DEBUG
            handle.Complete();

            int c = cleared[0];
            cleared.Dispose();

            RiderControlSystem.s_DebugLastMovingAwayFixCleared = c;
            if (c > 0)
                RiderControlSystem.s_DebugTotalMovingAwayFixCleared += c;

            if (setting.EnableDebugLogging && c > 0)
                Mod.s_Log.Info($"{Mod.ModTag} MovingAwayFix: cleared {c:N0} IgnoreTransport flags.");
#endif

            Dependency = handle;
        }

        private struct ClearIgnoreTransportJob : IJobChunk
        {
            public ComponentTypeHandle<Resident> ResidentType;
            public ComponentTypeHandle<PathOwner> PathOwnerType;

            [ReadOnly] public ComponentLookup<HouseholdMember> HouseholdMembers;
            [ReadOnly] public ComponentLookup<MovingAway> MovingAways;

#if DEBUG
            public NativeArray<int> ClearedCount; // length 1
#endif

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Resident> residents = chunk.GetNativeArray(ref ResidentType);
                NativeArray<PathOwner> pathOwners = chunk.GetNativeArray(ref PathOwnerType);

                for (int i = 0; i < residents.Length; i++)
                {
                    Resident r = residents[i];

                    if ((r.m_Flags & ResidentFlags.IgnoreTransport) == 0)
                        continue;

                    Entity citizen = r.m_Citizen;
                    if (citizen == Entity.Null || !HouseholdMembers.HasComponent(citizen))
                        continue;

                    Entity household = HouseholdMembers[citizen].m_Household;
                    if (household == Entity.Null || !MovingAways.HasComponent(household))
                        continue;

                    r.m_Flags &= ~ResidentFlags.IgnoreTransport;
                    residents[i] = r;

                    // Optional nudge: repath sooner.
                    PathOwner po = pathOwners[i];
                    po.m_State &= ~PathFlags.Failed;
                    po.m_State |= PathFlags.Obsolete;
                    pathOwners[i] = po;

#if DEBUG
                    ClearedCount[0] = ClearedCount[0] + 1;
#endif
                }
            }
        }
    }
}
