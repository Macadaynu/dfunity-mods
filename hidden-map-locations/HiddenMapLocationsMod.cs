using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static DaggerfallWorkshop.Utility.ContentReader;
using static DaggerfallConnect.DFRegion;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Serialization;

namespace Assets.MacadaynuMods.HiddenMapLocations
{
    public class HiddenMapLocationsMod : MonoBehaviour, IHasModSaveData
    {
        #region Types

        //assigns serializer version to ensures mod data continuity / debugging.
        //sets up the save data class for the seralizer to read and save to text file.
        [FullSerializer.fsObject("v1")]
        public class HiddenMapLocationsSaveData
        {
            public HashSet<MapSummary> DiscoveredMapSummaries;
        }

        #endregion

        public static HashSet<MapSummary> discoveredMapSummaries;
        public static HashSet<LocationTypes> revealedLocationTypes;
        public static Mod mod;
        public static HiddenMapLocationsMod instance;
        public Type SaveDataType { get { return typeof(HiddenMapLocationsSaveData); } }

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            //sets up instance of class/script/mod.
            GameObject go = new GameObject("HiddenMapLocationsMod");
            instance = go.AddComponent<HiddenMapLocationsMod>();
            //initiates mod paramaters for class/script.
            mod = initParams.Mod;
            //initiates save paramaters for class/script.
            mod.SaveDataInterface = instance;
            mod.IsReady = true;

            // initiates mod message handler
            mod.MessageReceiver = DFModMessageReceiver;

            DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHandler((int)MiscItems.Map, UseDungeonMap);

            //TODO: Need to add start/load game events to add current location to dicovered locations
            QuestMachine.OnQuestStarted += QuestMachine_OnQuestStarted;
            PlayerGPS.OnEnterLocationRect += PlayerGPS_OnEnterLocationRect;
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
            StartGameBehaviour.OnNewGame += OnNewGame;
            SaveLoadManager.OnLoad += OnLoadEvent;
        }

        private void Awake()
        {
            // assign empty map summaries if none exist
            if (discoveredMapSummaries == null)
            {
                discoveredMapSummaries = new HashSet<MapSummary>();
            }

            // TODO: get revealed locations from mod settings (no need for null check)
            if (revealedLocationTypes == null)
            {
                revealedLocationTypes = new HashSet<LocationTypes> { LocationTypes.TownCity };
            }

            // Only override the Travel Map if Travel Options is not enabled
            Mod travelOptionsMod = ModManager.Instance.GetMod("Travel Options");
            bool travelOptionsModEnabled = travelOptionsMod != null && travelOptionsMod.Enabled;
            if (!travelOptionsModEnabled)
            {
                UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelMap, typeof(HiddenMapLocationsWindow));
            }

            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Talk, typeof(HiddenMapLocationsTalkWindow));
        }

        public static bool UseDungeonMap(DaggerfallUnityItem item, ItemCollection collection)
        {
            if ((item.IsOfTemplate(ItemGroups.MiscItems, (int)MiscItems.Map) ||
                      item.IsOfTemplate(ItemGroups.Maps, (int)Maps.Map)) && collection != null)
            {
                const int mapTextId = 499;
                PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

                try
                {
                    DFLocation revealedLocation = DiscoverRandomLocation();

                    if (string.IsNullOrEmpty(revealedLocation.Name))
                        throw new Exception();

                    playerGPS.LocationRevealedByMapItem = revealedLocation.Name;
                    GameManager.Instance.PlayerEntity.Notebook.AddNote(
                        TextManager.Instance.GetLocalizedText("readMap").Replace("%map", revealedLocation.Name));

                    DaggerfallMessageBox mapText = new DaggerfallMessageBox(DaggerfallUI.Instance.UserInterfaceManager, DaggerfallUI.Instance.InventoryWindow);
                    mapText.SetTextTokens(DaggerfallUnity.Instance.TextProvider.GetRandomTokens(mapTextId));
                    mapText.ClickAnywhereToClose = true;
                    mapText.Show();

                    collection.RemoveItem(item);
                    DaggerfallUI.Instance.InventoryWindow.Refresh(false);

                    return true;
                }
                catch (Exception)
                {
                    // Player has already descovered all valid locations in this region!
                    //DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("readMapFail"));
                }
            }

            return false;
        }

        public static DFLocation DiscoverRandomLocation()
        {
            var currentRegion = GameManager.Instance.PlayerGPS.CurrentRegion;

            // Get all undiscovered locations that exist in the current region
            List<int> undiscoveredLocIdxs = new List<int>();
            for (int i = 0; i < currentRegion.LocationCount; i++)
            {
                if (currentRegion.MapTable[i].Discovered == false
                    && !GameManager.Instance.PlayerGPS.HasDiscoveredLocation(currentRegion.MapTable[i].MapId & 0x000fffff)
                    && !discoveredMapSummaries.Where(x => x.MapIndex == currentRegion.MapTable[i].MapId).Any())                    
                {
                    undiscoveredLocIdxs.Add(i);
                }
            }

            // If there aren't any left, there's nothing to find. Classic will just keep returning a particular location over and over if this happens.
            if (undiscoveredLocIdxs.Count == 0)
            {
                return new DFLocation();
            }

            // Choose a random location and discover it
            int locIdx = UnityEngine.Random.Range(0, undiscoveredLocIdxs.Count);
            DFLocation location = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(GameManager.Instance.PlayerGPS.CurrentRegionIndex, undiscoveredLocIdxs[locIdx]);
            AddMapSummaryFromLocation(location);

            return location;
        }

        public static void AddMapSummaryFromLocation(DFLocation location)
        {
            var dFPosition = MapsFile.LongitudeLatitudeToMapPixel(location.MapTableData.Longitude, location.MapTableData.Latitude);

            MapSummary mapSummary;
            if (DaggerfallUnity.Instance.ContentReader.HasLocation(dFPosition.X, dFPosition.Y, out mapSummary))
            {
                discoveredMapSummaries.Add(mapSummary);
            }
        }

        //// Go through every undiscovered location, and add it to the discovered locations if its a dungeon
        //List<int> undiscoveredLocIdxs = new List<int>();
        //for (int i = 0; i < currentRegion.LocationCount; i++)
        //{
        //    var mapId = currentRegion.MapTable[i].MapId;

        //    if (!discoveredMapSummaries.Where(x => x.MapIndex == mapId).Any())
        //    {
        //        DFLocation location = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(GameManager.Instance.PlayerGPS.CurrentRegionIndex, i);
        //        var summary = GetMapSummaryFromLocation(location);
        //        if (summary.HasValue && summary.Value.LocationType == DFRegion.LocationTypes.DungeonRuin)
        //        {

        //        }
        //    }
        //}

        static void QuestMachine_OnQuestStarted(Quest quest)
        {
            var siteDetails = QuestMachine.Instance.GetAllActiveQuestSites().ToList();

            foreach (var questSiteDetails in siteDetails.Where(x => x.questUID == quest.UID))
            {
                // TODO: Maybe better if these checks are specifically for Grab an Ingredient?
                var questItemSiteIsNewDungeon = questSiteDetails.questItemMarkers?.Where(x => x.placeSymbol != null && x.placeSymbol.Original == "_newdung_").Any() ?? false;
                var questSpawnSiteIsNewDungeon = questSiteDetails.questSpawnMarkers?.Where(x => x.placeSymbol != null && x.placeSymbol.Original == "_newdung_").Any() ?? false;
                if ((!questItemSiteIsNewDungeon && !questSpawnSiteIsNewDungeon)) //&& quest.QuestName.StartsWith("S0000"))
                {
                    DFLocation questLocation = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(questSiteDetails.regionName, questSiteDetails.locationName);

                    AddMapSummaryFromLocation(questLocation);
                }
            }
        }

        static void PlayerGPS_OnEnterLocationRect(DFLocation location)
        {
            AddMapSummaryFromLocation(location);
        }

        static void UIManager_OnWindowChangeHandler(object sender, EventArgs e)
        {
            // Could use the actions list on the Task class?
            SiteDetails[] siteDetails = QuestMachine.Instance.GetAllActiveQuestSites();
        }

        static void OnNewGame()
        {
            AddCurrentLocationMapSummary();
        }

        static void OnLoadEvent(SaveData_v1 saveData)
        {
            AddCurrentLocationMapSummary();
        }

        static void AddCurrentLocationMapSummary()
        {
            AddMapSummaryFromLocation(GameManager.Instance.PlayerGPS.CurrentLocation);

            //MapSummary currentLocationMapSummary;
            //DFPosition mapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
            //if (DaggerfallUnity.Instance.ContentReader.HasLocation(mapPixel.X, mapPixel.Y, out currentLocationMapSummary))
            //{
            //    discoveredMapSummaries.Add(currentLocationMapSummary);
            //}
        }

        static void DFModMessageReceiver(string message, object data, DFModMessageCallback callBack)
        {
            switch (message)
            {
                case "getDiscoveredMapSummaries":
                    callBack?.Invoke(message, discoveredMapSummaries);
                    break;
                case "getRevealedLocationTypes":
                    callBack?.Invoke(message, revealedLocationTypes);
                    break;
                default:
                    callBack?.Invoke(message, "Unknown message");
                    break;
            }
        }

        public object NewSaveData()
        {
            return new HiddenMapLocationsSaveData
            {
                DiscoveredMapSummaries = new HashSet<MapSummary>()
            };
        }

        public object GetSaveData()
        {
            return new HiddenMapLocationsSaveData
            {
                DiscoveredMapSummaries = discoveredMapSummaries
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var hiddenMapLocationsSaveData = (HiddenMapLocationsSaveData)saveData;

            discoveredMapSummaries = hiddenMapLocationsSaveData.DiscoveredMapSummaries;
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