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
            // TODO: be more selective with quest resource
            QuestResource[] clockResources = quest.GetAllResources(typeof(Clock));
            foreach (QuestResource clockResource in clockResources)
            {
                Clock clock = (Clock)clockResource;

                //DFLocation questLocation = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(questPlace.SiteDetails.regionName, questPlace.SiteDetails.locationName);

                //AddMapSummaryFromLocation(questLocation);
            }

            var siteDetails = QuestMachine.Instance.GetAllActiveQuestSites().ToList();

            // if the quest has a dungeon location, make sure the dungeon has a target resource
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

        public void UIManager_OnWindowChangeHandler(object sender, EventArgs e)
        {
            SiteDetails[] siteDetails = QuestMachine.Instance.GetAllActiveQuestSites();
        }

        // TODO: be more selective with quest resource
        //QuestResource[] placeResources = quest.GetAllResources(typeof(Place));
        //foreach (QuestResource placeResource in placeResources)
        //{
        //    Place questPlace = (Place)placeResource;

        //    DFLocation questLocation = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(questPlace.SiteDetails.regionName, questPlace.SiteDetails.locationName);

        //    AddMapSummaryFromLocation(questLocation);
        //}

        //if (sender.GetType() == typeof(UserInterfaceManager))
        //{
        //    var uiManager = (UserInterfaceManager)sender;

        //    if (uiManager.TopWindow.GetType() == typeof(DaggerfallMessageBox))
        //    {
        //        var messageBox = (DaggerfallMessageBox)uiManager.TopWindow;

        //        messageBox.
        //    }

        //    var text = uiManager.TopWindow.Value
        //}

        //if (!GameInProgress) //don't override non-game state when UI push
        //    return;
        //else if (DaggerfallUI.UIManager.WindowCount > 0)
        //    ChangeState(StateTypes.UI);
        //else if (!GameManager.IsGamePaused)
        //    ChangeState(LastState);
        //else
        //{
        //    ChangeState(StateTypes.None);
        //}

        //void RecordLocationFromMap(DaggerfallUnityItem item)
        //{
        //    const int mapTextId = 499;
        //    PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

        //    try
        //    {
        //        DFLocation revealedLocation = playerGPS.DiscoverRandomLocation();

        //        if (string.IsNullOrEmpty(revealedLocation.Name))
        //            throw new Exception();

        //        playerGPS.LocationRevealedByMapItem = revealedLocation.Name;
        //        GameManager.Instance.PlayerEntity.Notebook.AddNote(
        //            TextManager.Instance.GetLocalizedText("readMap").Replace("%map", revealedLocation.Name));

        //        DaggerfallMessageBox mapText = new DaggerfallMessageBox(uiManager, this);
        //        mapText.SetTextTokens(DaggerfallUnity.Instance.TextProvider.GetRandomTokens(mapTextId));
        //        mapText.ClickAnywhereToClose = true;
        //        mapText.Show();
        //    }
        //    catch (Exception)
        //    {
        //        // Player has already descovered all valid locations in this region!
        //        DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("readMapFail"));
        //    }
        //}

        //public void AddQuestTopicWithInfoAndRumors(Quest quest)
        //{
        //    // Add RumorsDuringQuest rumor to rumor mill
        //    Message message = quest.GetMessage((int)QuestMachine.QuestMessages.RumorsDuringQuest);
        //    if (message != null)
        //        AddOrReplaceQuestProgressRumor(quest.UID, message);

        //    // Add topics for the places to see, people to meet and items to handle.
        //    foreach (QuestResource resource in quest.GetAllResources())
        //    {
        //        QuestInfoResourceType type = GetQuestInfoResourceType(resource);
        //        List<TextFile.Token[]> anyInfoAnswers = resource.GetMessage(resource.InfoMessageID);
        //        List<TextFile.Token[]> rumorsAnswers = resource.GetMessage(resource.RumorsMessageID);

        //        AddQuestTopicWithInfoAndRumors(quest.UID, resource, resource.Symbol.Name, type, anyInfoAnswers, rumorsAnswers);
        //    }
        //}
    }
}