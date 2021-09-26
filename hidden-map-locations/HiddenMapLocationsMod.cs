using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;
using System.Collections.Generic;
using UnityEngine;
using static DaggerfallWorkshop.Utility.ContentReader;

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
        public static Mod mod;
        public static HiddenMapLocationsMod instance;
        public Type SaveDataType { get { return typeof(HiddenMapLocationsSaveData); } }

        //starts mod manager on game begin. Grabs mod initializing paramaters.
        //ensures SateTypes is set to .Start for proper save data restore values.
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            //sets up instance of class/script/mod.
            GameObject go = new GameObject("HiddenMapLocationsMod");
            instance = go.AddComponent<HiddenMapLocationsMod>();
            //initiates mod paramaters for class/script.
            mod = initParams.Mod;
            ////initiates save paramaters for class/script.
            mod.SaveDataInterface = instance;
            mod.IsReady = true;
        }

        private void Start()
        {
            // assign empty map summaries if none exist
            if (discoveredMapSummaries == null)
            {
                discoveredMapSummaries = new HashSet<MapSummary>();
            }

            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelMap, typeof(HiddenMapLocationsWindow));           
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