// Systems/RiderControlSystem.Debug.cs
// Optional debug logging helpers (controlled by settings).

namespace RiderControl
{
    using Game.Citizens;
    using Game.City;
    using Game.Companies;
    using Game.Creatures;
    using Game.Vehicles;
    using Unity.Entities;

    public partial class RiderControlSystem
    {
        private const int kDebugPassengerDetailMax = 8;

        private float m_DebugTimerSeconds;

        private void ResetDebugOnCityLoaded()
        {
            m_DebugTimerSeconds = 0f;
        }

        private void TickDebugLogging(Setting setting, float intervalSeconds, int clearedTaxiStandWaitingPassengers)
        {
            // Use unscaled time so debug logging still ticks while paused in the Options menu.
            m_DebugTimerSeconds += UnityEngine.Time.unscaledDeltaTime;
            if (m_DebugTimerSeconds < intervalSeconds)
            {
                return;
            }

            m_DebugTimerSeconds = 0f;

            int dailyTaxiCitizen = 0;
            int dailyTaxiTourist = 0;
            try
            {
                if (m_CityStatisticsSystem != null)
                {
                    dailyTaxiCitizen = m_CityStatisticsSystem.GetStatisticValue(StatisticType.PassengerCountTaxi, (int)PassengerType.Citizen);
                    dailyTaxiTourist = m_CityStatisticsSystem.GetStatisticValue(StatisticType.PassengerCountTaxi, (int)PassengerType.Tourist);
                }
            }
            catch
            {
                // Never let stats access crash simulation.
            }

            // Reuse snapshot numbers to keep debug logging cheap.
            Mod.s_Log.Info(
                $"{Mod.ModTag} TaxiSummary: " +
                $"taxis={s_StatusTaxisTotal}, transporting={s_StatusTaxiTransporting}, boarding={s_StatusTaxiBoarding}, returning={s_StatusTaxiReturning}, dispatched={s_StatusTaxiDispatched}, enRoute={s_StatusTaxiEnRoute}, parked={s_StatusTaxiParked}, accident={s_StatusTaxiAccident}, " +
                $"fromOutside={s_StatusTaxiFromOutside}, disabled={s_StatusTaxiDisabled}, withServiceDispatch={s_StatusTaxiWithDispatchBuffer}, " +
                $"requests[stand={s_StatusReqStand}, customer={s_StatusReqCustomer}, outside={s_StatusReqOutside}, none={s_StatusReqNone}], " +
                $"custSeekers(ignoreTaxi={s_StatusReqCustomerSeekerIgnoreTaxi}/{s_StatusReqCustomerSeekerHasResident}), " +
                $"outSeekers(ignoreTaxi={s_StatusReqOutsideSeekerIgnoreTaxi}/{s_StatusReqOutsideSeekerHasResident}), " +
                $"passengers(ignoreTaxi={s_StatusPassengerIgnoreTaxi}/{s_StatusPassengerHasResident}, totalPassengers={s_StatusPassengerTotal}), " +
                $"residents(ignoreTaxi={s_StatusResidentsIgnoreTaxi}/{s_StatusResidentsTotal}, forcedMarker={s_StatusResidentsForcedMarker}), " +
                $"commuters(ignoreTaxi={s_StatusCommutersIgnoreTaxi}/{s_StatusCommutersTotal}, blockCommuters={setting.BlockCommuters}), " +
                $"tourists(ignoreTaxi={s_StatusTouristsIgnoreTaxi}/{s_StatusTouristsTotal}, blockTourists={setting.BlockTourists}), " +
                $"waitingTransport(total={s_StatusWaitingTransportTotal}, taxiStand={s_StatusWaitingTaxiStandTotal}), " +
                $"statsDailyTaxi(citizen={dailyTaxiCitizen}, tourist={dailyTaxiTourist}, approxPerMonth={30 * (dailyTaxiCitizen + dailyTaxiTourist)}), " +
                $"clearedTaxiStandPassengers={clearedTaxiStandWaitingPassengers}, " +
                $"blockTaxiUsage={setting.BlockTaxiUsage}");

            // If there are still taxi passengers, log who/why (purpose/components) so we can find the source system.
            LogActiveTaxiPassengers();
        }

        private void LogActiveTaxiPassengers()
        {
            if (!Mod.s_Log.isDebugEnabled)
            {
                // Verbose option may be enabled while the global log level is higher than Debug;
                // keep this detailed scan behind Debug to avoid heavy logging/allocations.
                return;
            }

            int inTaxi = 0;
            int examples = 0;

            foreach ((RefRO<Resident> resident, RefRO<CurrentVehicle> currentVehicle, Entity passengerEntity) in SystemAPI
                         .Query<RefRO<Resident>, RefRO<CurrentVehicle>>()
                         .WithEntityAccess())
            {
                Entity vehicle = currentVehicle.ValueRO.m_Vehicle;
                if (vehicle == Entity.Null || !SystemAPI.HasComponent<Taxi>(vehicle))
                {
                    continue;
                }

                inTaxi++;

                if (examples >= kDebugPassengerDetailMax)
                {
                    continue;
                }

                examples++;

                ResidentFlags rf = resident.ValueRO.m_Flags;
                bool ignoreTaxi = (rf & ResidentFlags.IgnoreTaxi) != 0;
                bool forced = SystemAPI.HasComponent<IgnoreTaxiMark>(passengerEntity);

                // Citizen flags + household classification
                Entity citizenEntity = resident.ValueRO.m_Citizen;
                CitizenFlags citizenFlags = 0;
                bool hhCommuter = false;
                bool hhTourist = false;
                if (citizenEntity != Entity.Null)
                {
                    if (SystemAPI.HasComponent<Citizen>(citizenEntity))
                    {
                        citizenFlags = SystemAPI.GetComponentRO<Citizen>(citizenEntity).ValueRO.m_State;
                    }

                    if (SystemAPI.HasComponent<HouseholdMember>(citizenEntity))
                    {
                        Entity household = SystemAPI.GetComponentRO<HouseholdMember>(citizenEntity).ValueRO.m_Household;
                        if (household != Entity.Null)
                        {
                            hhCommuter = SystemAPI.HasComponent<CommuterHousehold>(household);
                            hhTourist = SystemAPI.HasComponent<TouristHousehold>(household);
                        }
                    }
                }

                // Travel context hints
                bool hasResourceBuyer = SystemAPI.HasComponent<ResourceBuyer>(passengerEntity);
                bool hasAttendingMeeting = SystemAPI.HasComponent<AttendingMeeting>(passengerEntity);
                bool hasTripNeeded = SystemAPI.HasBuffer<TripNeeded>(passengerEntity);
                string purpose = "none";
                if (SystemAPI.HasComponent<TravelPurpose>(passengerEntity))
                {
                    TravelPurpose tp = SystemAPI.GetComponentRO<TravelPurpose>(passengerEntity).ValueRO;
                    purpose = tp.m_Purpose.ToString();
                }

                TaxiFlags taxiFlags = SystemAPI.GetComponentRO<Taxi>(vehicle).ValueRO.m_State;

                Mod.s_Log.Debug(
                    $"{Mod.ModTag} TaxiPassengerNow: passenger={passengerEntity.Index}:{passengerEntity.Version} " +
                    $"vehicle={vehicle.Index}:{vehicle.Version} taxiFlags={taxiFlags} " +
                    $"ignoreTaxi={ignoreTaxi} forcedMarker={forced} " +
                    $"citizenFlags={citizenFlags} hhCommuter={hhCommuter} hhTourist={hhTourist} " +
                    $"purpose={purpose} tripNeeded={hasTripNeeded} resourceBuyer={hasResourceBuyer} attendingMeeting={hasAttendingMeeting}");
            }

            if (inTaxi > 0)
            {
                Mod.s_Log.Debug($"{Mod.ModTag} TaxiPassengerNow: totalResidentsInTaxi={inTaxi} (examplesShown={examples}/{kDebugPassengerDetailMax})");
            }
        }
    }
}
