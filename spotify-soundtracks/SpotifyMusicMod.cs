using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using System.Linq;
using DaggerfallConnect.Arena2;
using System.Collections.Generic;

namespace Assets.Scripts.Game.MacadaynuMods
{
    public class SpotifyMusicMod : MonoBehaviour
    {
        static Mod mod;
        static ModSettings settings;
        static bool useTownMusic;
        static bool useNightExplorationMusic;
        static bool useShopMusic;
        static bool useTempleMusic;
        static bool useMagesGuildMusic;
        static bool useFightersGuildMusic;
        static bool isInsideTavern;
        static bool isInsideShop;
        static bool isInsideTemple;
        static bool isInsideMagesGuild;
        static bool isInsideFightersGuild;

        public static string explorationPlaylistId;
        public static string explorationNightPlaylistId;
        public static string dungeonPlaylistId;
        public static string tavernPlaylistId;
        public static string townPlaylistId;
        public static string shopsPlaylistId;
        public static string templePlaylistId;
        public static string magesGuildPlaylistId;
        public static string fightersGuildPlaylistId;
        public static string clientId;
        public static string clientSecret;
        public static string refreshToken;
        public static string computerName;

        List<FactionFile.FactionIDs> templeFactions;
        List<FactionFile.FactionIDs> magesGuildFactions;
        List<FactionFile.FactionIDs> fightersGuildFactions;

        public void Awake()
        {
            mod.IsReady = true;

            PlayerEnterExit.OnTransitionExterior += OnTransitionToExterior;
            PlayerEnterExit.OnTransitionInterior += OnEnterInterior;
            PlayerEnterExit.OnTransitionDungeonExterior += OnExitDungeon;
            PlayerEnterExit.OnTransitionDungeonInterior += OnEnterDungeon;
            PlayerEnterExit.OnFailedTransition += OnFailedTransition;
            SaveLoadManager.OnLoad += OnLoad;
            GameManager.Instance.PlayerEntity.OnDeath += OnDeath;
            PlayerGPS.OnEnterLocationRect += OnEnterLocation;
            PlayerGPS.OnExitLocationRect += OnExitLocation;
            WorldTime.OnDawn += OnDawn;
            WorldTime.OnDusk += OnDusk;

            DaggerfallUnity.Settings.MusicVolume = 0;

            magesGuildFactions = new List<FactionFile.FactionIDs> { FactionFile.FactionIDs.The_Mages_Guild };
            fightersGuildFactions = new List<FactionFile.FactionIDs> { FactionFile.FactionIDs.The_Fighters_Guild };
            templeFactions = new List<FactionFile.FactionIDs>
            {
                FactionFile.FactionIDs.The_Akatosh_Chantry,
                FactionFile.FactionIDs.Akatosh,
                FactionFile.FactionIDs.The_Order_of_Arkay,
                FactionFile.FactionIDs.Arkay,
                FactionFile.FactionIDs.The_House_of_Dibella,
                FactionFile.FactionIDs.Dibella,
                FactionFile.FactionIDs.The_Schools_of_Julianos,
                FactionFile.FactionIDs.Julianos,
                FactionFile.FactionIDs.The_Temple_of_Kynareth,
                FactionFile.FactionIDs.Kynareth,
                FactionFile.FactionIDs.The_Benevolence_of_Mara,
                FactionFile.FactionIDs.Mara,
                FactionFile.FactionIDs.The_Temple_of_Stendarr,
                FactionFile.FactionIDs.Stendarr,
                FactionFile.FactionIDs.The_Resolution_of_Zen,
                FactionFile.FactionIDs.Zen
            };
        }

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            Debug.Log("SPOTIFY MOD INIT");

            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<SpotifyMusicMod>();

            settings = mod.GetSettings();

            clientId = settings.GetString("SpotifyAppCredentials", "ClientId");
            clientSecret = settings.GetString("SpotifyAppCredentials", "ClientSecret");
            refreshToken = settings.GetString("SpotifyAppCredentials", "RefreshToken");
            computerName = settings.GetString("SpotifyAppCredentials", "ComputerName");
            useTownMusic = settings.GetBool("PlaylistIds", "UseTownMusic");
            useNightExplorationMusic = settings.GetBool("PlaylistIds", "UseNightExplorationMusic");
            useShopMusic = settings.GetBool("PlaylistIds", "UseOpenShopMusic");
            useTempleMusic = settings.GetBool("PlaylistIds", "UseTempleMusic");
            useMagesGuildMusic = settings.GetBool("PlaylistIds", "UseMagesGuildMusic");
            useFightersGuildMusic = settings.GetBool("PlaylistIds", "UseFightersGuildMusic");

            AssignPlaylistId("Exploration", "3duUM5NBqB8OqIZNWt6W4Q", ref explorationPlaylistId);
            AssignPlaylistId("Dungeons", "5SN1OWk8oQ7KNDp5ZiUWql", ref dungeonPlaylistId);
            AssignPlaylistId("Taverns", "0M23q20iFObvPzht43bSmK", ref tavernPlaylistId);
            AssignPlaylistId("Towns", "2uCzDvmy1yvKbtua6QWEwG", ref townPlaylistId);
            AssignPlaylistId("NightExploration", "7ieTrY5xbsjDJ8ze63Pk1Z", ref explorationNightPlaylistId);
            AssignPlaylistId("OpenShops", "3duUM5NBqB8OqIZNWt6W4Q", ref shopsPlaylistId);
            AssignPlaylistId("Temples", "3duUM5NBqB8OqIZNWt6W4Q", ref templePlaylistId);
            AssignPlaylistId("MagesGuilds", "3duUM5NBqB8OqIZNWt6W4Q", ref magesGuildPlaylistId);
            AssignPlaylistId("FightersGuilds", "3duUM5NBqB8OqIZNWt6W4Q", ref fightersGuildPlaylistId);
        }

        private static void AssignPlaylistId(string area, string defaultPlaylistId, ref string playlistId)
        {
            var settingId = settings.GetString("PlaylistIds", area);
            playlistId = !string.IsNullOrWhiteSpace(settingId) ? settingId : defaultPlaylistId;
        }

        private void OnLoad(SaveData_v1 saveData)
        {
            var playerInfo = SpotifyAPI.GetCurrentlyPlayingInfo();
            if (!playerInfo?.is_playing ?? true)
            {
                PlayPlaylistForCurrentLocation();
            }
        }

        private void OnEnterDungeon(PlayerEnterExit.TransitionEventArgs args)
        {
            SpotifyAPI.PlayPlaylist(dungeonPlaylistId);
        }

        private void OnExitDungeon(PlayerEnterExit.TransitionEventArgs args)
        {
            PlayExplorationPlaylistForTimeOfDay();
        }

        private void OnEnterInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            // if entering a tavern
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideTavern)
            {
                PlayPlaylistForCurrentLocation();
            }
            // if entering an open shop
            else if (useShopMusic && GameManager.Instance.PlayerEnterExit.IsPlayerInsideOpenShop)
            {
                PlayPlaylistForCurrentLocation();
            }
            // if entering a temple
            else if (useTempleMusic && PlayerIsInFactionBuilding(templeFactions))//, args.DaggerfallInterior))
            {
                PlayPlaylistForCurrentLocation();
            }
            // if entering a mages guild
            else if (useMagesGuildMusic && PlayerIsInFactionBuilding(magesGuildFactions))//, args.DaggerfallInterior))
            {
                PlayPlaylistForCurrentLocation();
            }
            // if entering a fighters guild
            else if (useMagesGuildMusic && PlayerIsInFactionBuilding(fightersGuildFactions))//, args.DaggerfallInterior))
            {
                PlayPlaylistForCurrentLocation();
            }
        }

        bool PlayerIsInFactionBuilding(List<FactionFile.FactionIDs> factionIds)//, DaggerfallInterior interior)
        {
            var interior = GameManager.Instance.PlayerEnterExit.Interior;

            if (interior == null)
            {
                return false;
            }

            return factionIds.Contains((FactionFile.FactionIDs)interior.BuildingData.FactionId);
        }

        private void OnTransitionToExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            // if exiting a tavern
            if (isInsideTavern)
            {
                PlayPlaylistForCurrentLocation(exitingTavern: true);
            }
            // if exiting an open shop
            else if (useShopMusic && isInsideShop)
            {
                PlayPlaylistForCurrentLocation(exitingShop: true);
            }
            // if exiting a temple
            else if (useTempleMusic && isInsideTemple)
            {
                PlayPlaylistForCurrentLocation(exitingTemple: true);
            }
            // if exiting a mages guild
            else if (useMagesGuildMusic && isInsideMagesGuild)
            {
                PlayPlaylistForCurrentLocation(exitingMagesGuild: true);//, transition: args);
            }
            // if exiting a fighters guild
            else if (useFightersGuildMusic && isInsideFightersGuild)
            {
                PlayPlaylistForCurrentLocation(exitingFightersGuild: true);//, transition: args);
            }
        }

        private void OnDeath(DaggerfallEntity entity)
        {
            SpotifyAPI.PausePlayback();
        }

        private void OnDusk()
        {
            PlayTimeAppropriateExplorationPlaylist();
        }

        private void OnDawn()
        {
            PlayTimeAppropriateExplorationPlaylist();
        }

        void PlayTimeAppropriateExplorationPlaylist()
        {
            if (useNightExplorationMusic
                && !isInsideTavern
                && !isInsideShop
                && !isInsideTemple
                && !isInsideMagesGuild
                && !isInsideFightersGuild
                && !GameManager.Instance.IsPlayerInsideDungeon)
            {
                PlayPlaylistForCurrentLocation(true, true, true, true, true);
            }
        }

        private void OnExitLocation()
        {            
            if (useTownMusic)
            {
                // if exiting a location and the current playlist is Town
                var playerInfo = SpotifyAPI.GetCurrentlyPlayingInfo();
                if (!string.IsNullOrWhiteSpace(playerInfo?.context?.external_urls?.spotify)
                    && playerInfo.context.external_urls.spotify.Split('/').Last() == townPlaylistId)
                {
                    PlayExplorationPlaylistForTimeOfDay();
                }
            }
        }

        private void OnEnterLocation(DaggerfallConnect.DFLocation location)
        {
            // have to check not in certain locations as this event fires when loading a game
            if (useTownMusic
                && !isInsideTavern
                && !isInsideShop
                && !isInsideTemple
                && !isInsideMagesGuild
                && !isInsideFightersGuild)
            {
                if (GameManager.Instance.PlayerGPS.IsPlayerInTown(true))
                {
                    SpotifyAPI.PlayPlaylist(townPlaylistId);
                }
                else
                {
                    var playerInfo = SpotifyAPI.GetCurrentlyPlayingInfo();

                    var currentlyPlayingPlaylist = playerInfo?.context?.external_urls?.spotify;

                    if (!string.IsNullOrWhiteSpace(currentlyPlayingPlaylist))
                    {
                        currentlyPlayingPlaylist = currentlyPlayingPlaylist.Split('/').Last();
                    }

                    if (currentlyPlayingPlaylist != explorationPlaylistId
                        && currentlyPlayingPlaylist != explorationNightPlaylistId)
                    {
                        PlayExplorationPlaylistForTimeOfDay();
                    }
                }
            }
        }

        private void OnFailedTransition(PlayerEnterExit.TransitionEventArgs obj)
        {
            PlayPlaylistForCurrentLocation();
        }

        private void PlayPlaylistForCurrentLocation(bool exitingTavern = false, bool exitingShop = false, bool exitingTemple = false,
            bool exitingMagesGuild = false, bool exitingFightersGuild = false, PlayerEnterExit.TransitionEventArgs transition = null)
        {
            if (GameManager.Instance.IsPlayerInsideDungeon)
            {
                SpotifyAPI.PlayPlaylist(dungeonPlaylistId);
                return;
            }

            isInsideTavern = !exitingTavern && GameManager.Instance.PlayerEnterExit.IsPlayerInsideTavern;
            if (isInsideTavern)
            {
                SpotifyAPI.PlayPlaylist(tavernPlaylistId);
                return;
            }

            if (useShopMusic)
            {
                isInsideShop = !exitingShop && GameManager.Instance.PlayerEnterExit.IsPlayerInsideOpenShop;
                if (isInsideShop)
                {
                    SpotifyAPI.PlayPlaylist(shopsPlaylistId);
                    return;
                }
            }

            if (useTempleMusic)
            {
                isInsideTemple = !exitingTemple && PlayerIsInFactionBuilding(templeFactions);//, transition.DaggerfallInterior);
                if (isInsideTemple)
                {
                    SpotifyAPI.PlayPlaylist(templePlaylistId);
                    return;
                }
            }

            if (useMagesGuildMusic)
            {
                isInsideMagesGuild = !exitingMagesGuild && PlayerIsInFactionBuilding(magesGuildFactions);//, transition.DaggerfallInterior);
                if (isInsideMagesGuild)
                {
                    SpotifyAPI.PlayPlaylist(magesGuildPlaylistId);
                    return;
                }
            }

            if (useFightersGuildMusic)
            {
                isInsideFightersGuild = !exitingFightersGuild && PlayerIsInFactionBuilding(fightersGuildFactions);//, transition.DaggerfallInterior);
                if (isInsideFightersGuild)
                {
                    SpotifyAPI.PlayPlaylist(fightersGuildPlaylistId);
                    return;
                }
            }            

            if (useTownMusic && GameManager.Instance.PlayerGPS.IsPlayerInTown(true))
            {
                SpotifyAPI.PlayPlaylist(townPlaylistId);
                return;
            }

            PlayExplorationPlaylistForTimeOfDay();
        }

        private void PlayExplorationPlaylistForTimeOfDay()
        {
            if (useNightExplorationMusic && DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                SpotifyAPI.PlayPlaylist(explorationNightPlaylistId);
                return;
            }

            SpotifyAPI.PlayPlaylist(explorationPlaylistId);
        }

        private void OnApplicationQuit()
        {
            SpotifyAPI.PausePlayback();
        }
    }
}