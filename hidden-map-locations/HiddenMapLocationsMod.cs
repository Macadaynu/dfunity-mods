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
using DaggerfallWorkshop.Game.Questing.Actions;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;
using System.Text.RegularExpressions;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

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
        static ModSettings settings;
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

            settings = mod.GetSettings();

            revealedLocationTypes = new HashSet<LocationTypes>();

            AddRevealedLocationTypeFromSettings("Cities");
            AddRevealedLocationTypeFromSettings("Taverns");
            AddRevealedLocationTypeFromSettings("WealthyHomes");
            AddRevealedLocationTypeFromSettings("PoorHomes");
            AddRevealedLocationTypeFromSettings("Hamlets");
            AddRevealedLocationTypeFromSettings("Villages");
            AddRevealedLocationTypeFromSettings("Farms");
            AddRevealedLocationTypeFromSettings("Temples");

            DaggerfallUnity.Instance.ItemHelper.RegisterItemUseHandler((int)MiscItems.Map, UseDungeonMap);

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

            AddDefaultLocation("Betony", "Whitefort");
            AddDefaultLocation("Isle of Balfiera", "Singbrugh");
            AddDefaultLocation("Isle of Balfiera", "Blackhead");

            // Only override the Travel Map if Travel Options is not enabled
            Mod travelOptionsMod = ModManager.Instance.GetMod("Travel Options");
            bool travelOptionsModEnabled = travelOptionsMod != null && travelOptionsMod.Enabled;
            if (!travelOptionsModEnabled)
            {
                UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelMap, typeof(HiddenMapLocationsWindow));
            }

            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Talk, typeof(HiddenMapLocationsTalkWindow));
        }

        public void AddDefaultLocation(string regionName, string locationName)
        {
            var location = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(regionName, locationName);
            AddMapSummaryFromLocation(location);
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

        static void QuestMachine_OnQuestStarted(Quest quest)
        {
            // story quests will be handled through quest actions (as some quests start silently)
            if (!quest.QuestName.StartsWith("S0000") && !quest.QuestName.StartsWith("_BRISIEN"))
            {
                var siteDetails = QuestMachine.Instance.GetAllActiveQuestSites().ToList();

                foreach (var questSiteDetails in siteDetails.Where(x => x.questUID == quest.UID))
                {
                    // TODO: Maybe better if these checks are specifically for Grab an Ingredient?
                    var questItemSiteIsNewDungeon = questSiteDetails.questItemMarkers?.Where(x => x.placeSymbol != null && x.placeSymbol.Original == "_newdung_").Any() ?? false;
                    var questSpawnSiteIsNewDungeon = questSiteDetails.questSpawnMarkers?.Where(x => x.placeSymbol != null && x.placeSymbol.Original == "_newdung_").Any() ?? false;
                    if (!questItemSiteIsNewDungeon && !questSpawnSiteIsNewDungeon)
                    {
                        DFLocation questLocation = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(questSiteDetails.regionName, questSiteDetails.locationName);

                        AddMapSummaryFromLocation(questLocation);
                    }
                }
            }
        }

        static void PlayerGPS_OnEnterLocationRect(DFLocation location)
        {
            AddMapSummaryFromLocation(location);
        }

        static void OnNewGame()
        {
            AddCurrentLocationMapSummary();
        }

        static void OnLoadEvent(SaveData_v1 saveData)
        {
            AddCurrentLocationMapSummary();
        }

        static int GetSayId(Quest quest, string source)
        {
            int sayId;

            // Factory new say
            Say say = new Say(quest);

            // Source must match pattern
            Match match = Regex.Match(source, say.Pattern);
            if (!match.Success)
                return 0; // return null?

            sayId = Parser.ParseInt(match.Groups["id"].Value);

            // Resolve static message back to ID
            string idName = match.Groups["idName"].Value;
            if (sayId == 0 && !string.IsNullOrEmpty(idName))
            {
                Table table = QuestMachine.Instance.StaticMessagesTable;
                sayId = Parser.ParseInt(table.GetValue("id", idName));
            }

            return sayId;
        }

        static DFLocation? GetLocationFromPlaceNPC(string source, Quest quest)
        {
            // Factory new action
            PlaceNpc placeNpc = new PlaceNpc(quest);

            // Source must match pattern
            Match match = Regex.Match(source, placeNpc.Pattern);
            if (!match.Success)
                return null;

            var placeSymbol = new Symbol(match.Groups["aPlace"].Value);

            Place place = quest.GetPlace(placeSymbol);

            return DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(
                place.SiteDetails.regionName, place.SiteDetails.locationName);
        }

        static DFLocation? GetLocationFromRevealLocation(string source, Quest quest)
        {
            // Factory new reveal location
            RevealLocation revealLocation = new RevealLocation(quest);

            // Source must match pattern
            Match match = Regex.Match(source, revealLocation.Pattern);
            if (!match.Success)
                return null;

            var placeSymbol = new Symbol(match.Groups["aPlace"].Value);

            Place place = quest.GetPlace(placeSymbol);

            return DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(
                    place.SiteDetails.regionName, place.SiteDetails.locationName);
        }

        static DFLocation? GetLocationFromDialogLink(string source, Quest quest)
        {
            // Factory new dialog link
            DialogLink dialogLink = new DialogLink(quest);

            // Source must match pattern
            Match match = Regex.Match(source, dialogLink.Pattern);
            if (!match.Success)
                return null;

            if (!string.IsNullOrEmpty(match.Groups["aSite"].Value))
            {
                Place place = quest.GetPlace(new Symbol(match.Groups["aSite"].Value));

                return DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(
                        place.SiteDetails.regionName, place.SiteDetails.locationName);
            }

            return null;
        }

        static void UIManager_OnWindowChangeHandler(object sender, EventArgs e)
        {
            if (DaggerfallUI.UIManager.WindowCount > 0
                && DaggerfallUI.UIManager.TopWindow.GetType() == typeof(DaggerfallMessageBox))
            {
                ulong[] uids = QuestMachine.Instance.GetAllActiveQuests();
                foreach (ulong questUID in uids)
                {
                    Quest quest = QuestMachine.Instance.GetQuest(questUID);
                    // if quest is a main quest
                    //TODO: Make the Brisiena quest optional
                    if (quest != null && (quest.QuestName.StartsWith("S0000") || quest.QuestName.StartsWith("_BRISIEN")))
                    {
                        // get all triggered tasks for this quest
                        var triggeredTaskStates = quest.GetTaskStates().Where(x => x.set).ToList(); // remove ToList after debugging

                        foreach (var taskState in triggeredTaskStates)
                        {
                            var task = quest.GetTask(taskState.symbol);
                            foreach (var action in task.Actions)
                            {
                                Message questMessage = null;
                                DFLocation? dfLocation = null;

                                switch (action)
                                {
                                    case Say say:
                                        questMessage = quest.GetMessage(GetSayId(quest, action.DebugSource));
                                        break;
                                    //case EndQuest endQuest:
                                    //    questMessage = quest.GetMessage(GetEndQuestId(quest, action.DebugSource));
                                    //    break;
                                    //case TotingItemAndClickedNpc totingItemAndClickedNpc:
                                    //    questMessage = quest.GetMessage(GetTotingItemAndClickedNPCId(quest, action.DebugSource));
                                    //    break;
                                    //case GivePc givePc:
                                    //    questMessage = quest.GetMessage(GetGivePcId(quest, action.DebugSource));
                                    //    break;
                                    case RevealLocation revealLocation:
                                        dfLocation = GetLocationFromRevealLocation(action.DebugSource, quest);
                                        break;
                                    case DialogLink dialogLink:
                                        dfLocation = GetLocationFromDialogLink(action.DebugSource, quest);
                                        break;
                                    case PlaceNpc placeNpc:
                                        dfLocation = GetLocationFromPlaceNPC(action.DebugSource, quest);
                                        break;
                                    default:
                                        continue;
                                }

                                if (questMessage != null)
                                {
                                    var messageLocations = GetLocationsMentionedInMessage(questMessage);

                                    foreach (var location in messageLocations)
                                    {
                                        AddMapSummaryFromLocation(location);
                                    }
                                }
                                if (dfLocation.HasValue)
                                {
                                    AddMapSummaryFromLocation(dfLocation.Value);
                                }
                            }                            
                        }
                    }
                }
            }
        }

        static List<DFLocation> GetLocationsMentionedInMessage(Message message)
        {
            var questLocations = new List<DFLocation>();

            QuestMacroHelper helper = new QuestMacroHelper();
            QuestResource[] resources = helper.GetMessageResources(message);
            
            foreach (Place place in resources?.OfType<Place>())
            {
                questLocations.Add(DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(
                    place.SiteDetails.regionName, place.SiteDetails.locationName));
            }

            return questLocations;
        }

        static void AddCurrentLocationMapSummary()
        {
            AddMapSummaryFromLocation(GameManager.Instance.PlayerGPS.CurrentLocation);
        }

        static void AddRevealedLocationTypeFromSettings(string locationType)
        {
            if (settings.GetBool("LocationTypesToReveal", locationType))
            {
                switch (locationType)
                {
                    case "Cities":
                        revealedLocationTypes.Add(LocationTypes.TownCity);
                        break;
                    case "Taverns":
                        revealedLocationTypes.Add(LocationTypes.Tavern);
                        break;
                    case "WealthyHomes":
                        revealedLocationTypes.Add(LocationTypes.HomeWealthy);
                        break;
                    case "PoorHomes":
                        revealedLocationTypes.Add(LocationTypes.HomePoor);
                        break;
                    case "Hamlets":
                        revealedLocationTypes.Add(LocationTypes.TownHamlet);
                        break;
                    case "Villages":
                        revealedLocationTypes.Add(LocationTypes.TownVillage);
                        break;
                    case "Farms":
                        revealedLocationTypes.Add(LocationTypes.HomeFarms);
                        break;
                    case "Temples":
                        revealedLocationTypes.Add(LocationTypes.ReligionTemple);
                        break;
                    default:
                        break;
                }
            }
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
    }
}
