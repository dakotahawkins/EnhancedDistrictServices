﻿using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedDistrictServices
{
    /// <summary>
    /// Misc helper methods for classifying buildings and offers.
    /// </summary>
    public static class TransferManagerInfo
    {
        /// <summary>
        /// Returns the building id associated with the offer, if specified.
        /// If a citizen is associated with the offer, returns the citizen's home building id.
        /// If a service vehicle is associated with the offer, returns that vehicle's service building.
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public static ushort GetHomeBuilding(ref TransferManager.TransferOffer offer)
        {
            if (offer.Building != 0)
            {
                return offer.Building;
            }

            if (offer.Citizen != 0)
            {
                return Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
            }

            if (offer.Vehicle != 0)
            {
                return VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].m_sourceBuilding;
            }

            return 0;
        }

        /// <summary>
        /// Returns the district of the offer's home building or segment.
        /// Should return 0 if the offer does not originate from a district.
        /// </summary>
        /// <returns></returns>
        public static byte GetDistrict(ref TransferManager.TransferOffer offer)
        {
            if (offer.NetSegment != 0)
            {
                var position = NetManager.instance.m_segments.m_buffer[offer.NetSegment].m_middlePosition;
                return DistrictManager.instance.GetDistrict(position);
            }
            else
            {
                return GetDistrict(GetHomeBuilding(ref offer));
            }
        }

        /// <summary>
        /// Returns the name of the building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetBuildingName(int building)
        {
            return Singleton<BuildingManager>.instance.GetBuildingName((ushort)building, InstanceID.Empty);
        }

        /// <summary>
        /// Returns the district of the building.
        /// Should return 0 if the building is not in a district.
        /// </summary>
        /// <returns></returns>
        public static byte GetDistrict(int building)
        {
            if (building != 0)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[building].m_position;
                return DistrictManager.instance.GetDistrict(position);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the home district of the specified building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetDistrictText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var district = GetDistrict(building);
            if (district != 0)
            {
                var districtName = DistrictManager.instance.GetDistrictName((int)district);
                return $"Home district: {districtName}";
            }
            else
            {
                return $"Home district: (Not in a district)";
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the districts that are served by the specified building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetDistrictsServedText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<DistrictsServed>>");

            bool addedText = false;
            if (Constraints.AllLocalAreas(building))
            {
                txtItems.Add($"All local areas served");
                addedText = true;
            }

            if (Constraints.OutsideConnections(building))
            {
                txtItems.Add($"All outside connections served");
                addedText = true;
            }

            if (Constraints.AllLocalAreas(building))
            {
                return string.Join("\n", txtItems.ToArray());
            }

            if (Constraints.SupplyDestinations(building)?.Count > 0)
            {
                txtItems.Add($"Supply chain restricted, only serves specified Supply Chain Out buildings!");
                return string.Join("\n", txtItems.ToArray());
            }
            else if (Constraints.DistrictServiced(building)?.Count > 0)
            {
                var districtNames = Constraints.DistrictServiced(building)
                    .Select(d => DistrictManager.instance.GetDistrictName(d))
                    .OrderBy(s => s);

                foreach (var districtName in districtNames)
                {
                    txtItems.Add(districtName);
                }

                addedText = true;
            }

            if (!addedText)
            {
                txtItems.Add($"No districts served!");
            }

            return string.Join("\n", txtItems.ToArray());
        }

        /// <summary>
        /// Returns a descriptive text about the type of service provided by the building.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetServicesText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[building].Info;
            var service = buildingInfo.GetService();
            var subService = buildingInfo.GetSubService();
            if (buildingInfo.GetAI() is OutsideConnectionAI)
            {
                if (buildingInfo.GetService() == ItemClass.Service.Road)
                {
                    return $"Service: OutsideConnection (Road)";
                }
                else
                {
                    return $"Service: OutsideConnection ({subService})";
                }
            }
            else if (service == ItemClass.Service.PlayerIndustry)
            {
                if (buildingInfo.GetAI() is ExtractingFacilityAI extractingFacilityAI)
                {
                    return $"Service: {service} ({extractingFacilityAI.m_outputResource})";
                }
                else if (buildingInfo.GetAI() is ProcessingFacilityAI processingFacilityAI)
                {
                    return $"Service: {service} ({processingFacilityAI.m_outputResource})";
                }
                else if (buildingInfo.GetAI() is WarehouseAI warehouseAI)
                {
                    return $"Service: {service} ({warehouseAI.m_storageType})";
                }
                else
                {
                    return $"Service: {service}";
                }
            }
            else
            {
                return $"Service: {service}";
            }
        }

        /// <summary>
        /// Returns a descriptive text indicating the supply chain destination buildings that the given building
        /// will ship to.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplyDestinationsText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments Only To>>");

            var buildingNames = Constraints.SupplyDestinations(building)
                .Select(b => $"{GetBuildingName(b)} ({b})")
                .OrderBy(s => s);

            foreach (var buildingName in buildingNames)
            {
                txtItems.Add(buildingName);
            }

            return string.Join("\n", txtItems.ToArray());
        }

        /// <summary>
        /// Returns a descriptive text indicating the supply chain source buildings that the given building
        /// will receive shipments from.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static string GetSupplySourcesText(ushort building)
        {
            if (building == 0)
            {
                return string.Empty;
            }

            var txtItems = new List<string>();
            txtItems.Add($"<<Supply Chain Shipments Only From>>");

            var buildingNames = Constraints.SupplySources(building)
                .Select(b => $"{GetBuildingName(b)} ({b})")
                .OrderBy(s => s);

            foreach (var buildingName in buildingNames)
            {
                txtItems.Add(buildingName);
            }

            return string.Join("\n", txtItems.ToArray());
        }

        /// <summary>
        /// Returns true if the building's service is a supported district-only service.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsDistrictServicesBuilding(int building)
        {
            if (building == 0)
            {
                return false;
            }

            var instance = Singleton<BuildingManager>.instance;

            if ((instance.m_buildings.m_buffer[building].m_flags & Building.Flags.Created) != Building.Flags.None)
            {
                var info = instance.m_buildings.m_buffer[building].Info;
                switch (info?.GetService())
                {
                    case ItemClass.Service.Disaster:
                    case ItemClass.Service.Education:
                    case ItemClass.Service.FireDepartment:
                    case ItemClass.Service.Garbage:
                    case ItemClass.Service.HealthCare:
                    case ItemClass.Service.PoliceDepartment:
                        return !(
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is LibraryAI ||
                            info.GetAI() is SaunaAI);

                    case ItemClass.Service.PlayerEducation:
                        return !(
                            info.GetSubService() == ItemClass.SubService.PlayerEducationLiberalArts ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationTradeSchool ||
                            info.GetSubService() == ItemClass.SubService.PlayerEducationUniversity);

                    case ItemClass.Service.PublicTransport:
                        return (
                            info.GetSubService() == ItemClass.SubService.PublicTransportPost ||
                            info.GetAI() is OutsideConnectionAI);

                    case ItemClass.Service.Road:
                        return (
                            info.GetAI() is MaintenanceDepotAI ||
                            info.GetAI() is OutsideConnectionAI);

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is AuxiliaryBuildingAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is MainIndustryBuildingAI);

                    case ItemClass.Service.Water:
                        return (
                            info.GetAI() is WaterFacilityAI waterFacilityAI &&
                            waterFacilityAI.m_pumpingVehicles > 0);

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the building corresponds to an outside connection.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsOutsideBuilding(int building)
        {
            return building != 0 && Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info.m_buildingAI is OutsideConnectionAI;
        }

        /// <summary>
        /// Returns true if the building's service is a supported supply chain service.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static bool IsSupplyChainBuilding(int building)
        {
            if (building == 0)
            {
                return false;
            }

            var instance = Singleton<BuildingManager>.instance;

            if ((instance.m_buildings.m_buffer[building].m_flags & Building.Flags.Created) != Building.Flags.None)
            {
                var info = instance.m_buildings.m_buffer[building].Info;
                switch (info?.GetService())
                {
                    case ItemClass.Service.PublicTransport:
                    case ItemClass.Service.Road:
                        return (
                            info.GetAI() is OutsideConnectionAI ||
                            info.GetSubService() == ItemClass.SubService.PublicTransportPost);

                    case ItemClass.Service.PlayerIndustry:
                        return !(
                            info.GetAI() is AuxiliaryBuildingAI ||
                            info.GetAI() is DummyBuildingAI ||
                            info.GetAI() is MainIndustryBuildingAI);

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the offer concerns a city service that should be restricted within a district.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool IsDistrictOffer(TransferManager.TransferReason material)
        {
            return
                material == TransferManager.TransferReason.Garbage ||
                material == TransferManager.TransferReason.Crime ||
                material == TransferManager.TransferReason.CriminalMove ||
                material == TransferManager.TransferReason.Sick ||
                material == TransferManager.TransferReason.Dead ||
                material == TransferManager.TransferReason.Fire ||
                material == TransferManager.TransferReason.Mail ||

                material == TransferManager.TransferReason.RoadMaintenance ||

                material == TransferManager.TransferReason.ForestFire ||
                material == TransferManager.TransferReason.Collapsed ||
                material == TransferManager.TransferReason.Collapsed2 ||
                material == TransferManager.TransferReason.Fire2 ||
                material == TransferManager.TransferReason.Sick2 ||
                material == TransferManager.TransferReason.FloodWater ||
                material == TransferManager.TransferReason.EvacuateA ||
                material == TransferManager.TransferReason.EvacuateB ||
                material == TransferManager.TransferReason.EvacuateC ||
                material == TransferManager.TransferReason.EvacuateD ||
                material == TransferManager.TransferReason.EvacuateVipA ||
                material == TransferManager.TransferReason.EvacuateVipB ||
                material == TransferManager.TransferReason.EvacuateVipC ||
                material == TransferManager.TransferReason.EvacuateVipD ||

                material == TransferManager.TransferReason.Student1 ||
                material == TransferManager.TransferReason.Student2 ||

                material == TransferManager.TransferReason.UnsortedMail;
        }

        /// <summary>
        /// Returns true if the offer concerns a supported supply chain material.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool IsSupplyChainOffer(TransferManager.TransferReason material)
        {
            return
                material == TransferManager.TransferReason.Coal ||
                material == TransferManager.TransferReason.Food ||
                material == TransferManager.TransferReason.Petrol ||
                material == TransferManager.TransferReason.Lumber ||

                material == TransferManager.TransferReason.Logs ||
                material == TransferManager.TransferReason.Paper ||
                material == TransferManager.TransferReason.PlanedTimber ||

                material == TransferManager.TransferReason.Grain ||
                material == TransferManager.TransferReason.Flours ||
                material == TransferManager.TransferReason.AnimalProducts ||

                material == TransferManager.TransferReason.Oil ||
                material == TransferManager.TransferReason.Petroleum ||
                material == TransferManager.TransferReason.Plastics ||

                material == TransferManager.TransferReason.Ore ||
                material == TransferManager.TransferReason.Glass ||
                material == TransferManager.TransferReason.Metals ||

                material == TransferManager.TransferReason.LuxuryProducts ||
                material == TransferManager.TransferReason.SortedMail;
        }
        
        /// <summary>
        /// Returns true if the offer was given from an outside connection.
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public static bool IsOutsideOffer(ref TransferManager.TransferOffer offer)
        {
            return IsOutsideBuilding(GetHomeBuilding(ref offer));
        }

        /// <summary>
        /// Helper method for dumping the contents of an offer, for debugging purposes.
        /// </summary>
        /// <param name="offer"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        public static string ToString(ref TransferManager.TransferOffer offer, TransferManager.TransferReason material)
        {
            if (offer.Building != 0)
            {
                return $"Id=B{offer.Building}, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
            }

            if (offer.Citizen != 0)
            {
                var homeBuilding = Singleton<CitizenManager>.instance.m_citizens.m_buffer[offer.Citizen].m_homeBuilding;
                return $"Id=C{offer.Citizen}, Home=B{homeBuilding}, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
            }

            if (offer.Vehicle != 0)
            {
                return $"Id=V{offer.Vehicle}, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
            }

            if (offer.NetSegment != 0)
            {
                return $"Id=S{offer.NetSegment}, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
            }

            return $"Id=0, (Amt,Mat,Pri,Exc)=({offer.Amount},{material},{offer.Priority},{offer.Exclude})";
        }
    }
}