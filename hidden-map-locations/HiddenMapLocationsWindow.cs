using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;
using System;
using System.Linq;

namespace Assets.MacadaynuMods.HiddenMapLocations
{
    public class HiddenMapLocationsWindow : DaggerfallTravelMapWindow
    {
        public HiddenMapLocationsWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
            QuestMachine.OnQuestStarted += QuestMachine_OnQuestStarted;
            PlayerGPS.OnEnterLocationRect += PlayerGPS_OnEnterLocationRect;
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
        }

        protected override bool checkLocationDiscovered(ContentReader.MapSummary summary)
        {
            //TODO: probably pointless checking that the players current location exists every time
            ContentReader.MapSummary currentLocationMapSummary;
            DFPosition mapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
            if (DaggerfallUnity.Instance.ContentReader.HasLocation(mapPixel.X, mapPixel.Y, out currentLocationMapSummary))
            {
                // The map summary has previosuly been discovered, or it's the player's current location, or it's a city
                var hasLocation = HiddenMapLocationsMod.discoveredMapSummaries.Contains(summary) || currentLocationMapSummary.ID == summary.ID || summary.LocationType == DFRegion.LocationTypes.TownCity;

                if (hasLocation && !HiddenMapLocationsMod.discoveredMapSummaries.Contains(summary))
                {
                    //add to storage
                    HiddenMapLocationsMod.discoveredMapSummaries.Add(summary);
                }

                return hasLocation;
            }

            // any cities remaining are deemed discovered
            return HiddenMapLocationsMod.discoveredMapSummaries.Contains(summary) || summary.LocationType == DFRegion.LocationTypes.TownCity;
        }

        void QuestMachine_OnQuestStarted(Quest quest)
        {
            var siteDetails = QuestMachine.Instance.GetAllActiveQuestSites().ToList();
            foreach (var questSiteDetails in siteDetails.Where(x => x.questUID == quest.UID))
            {
                QuestMarker? newDungeonQuestMarker = questSiteDetails.questItemMarkers?.FirstOrDefault(x => x.placeSymbol?.Original == "_newdung_");
                QuestMarker? newDungeonSpawnMarker = questSiteDetails.questSpawnMarkers?.FirstOrDefault(x => x.placeSymbol?.Original == "_newdung_");

                if (newDungeonQuestMarker == null && newDungeonSpawnMarker == null)
                {
                    DFLocation questLocation = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(questSiteDetails.regionName, questSiteDetails.locationName);

                    AddMapSummaryFromLocation(questLocation);
                }
            }
        }

        void PlayerGPS_OnEnterLocationRect(DFLocation location)
        {
            AddMapSummaryFromLocation(location);
        }

        void AddMapSummaryFromLocation(DFLocation location)
        {
            var dFPosition = MapsFile.LongitudeLatitudeToMapPixel(location.MapTableData.Longitude, location.MapTableData.Latitude);

            ContentReader.MapSummary mapSummary;
            if (DaggerfallUnity.Instance.ContentReader.HasLocation(dFPosition.X, dFPosition.Y, out mapSummary))
            {
                HiddenMapLocationsMod.discoveredMapSummaries.Add(mapSummary);
            }
        }
    }
}
