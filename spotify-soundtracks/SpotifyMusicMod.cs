using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using System.Linq;

namespace Assets.Scripts.Game.MacadaynuMods
{
    public class SpotifyMusicMod : MonoBehaviour
    {
        static Mod mod;
        static ModSettings settings;
        static bool useTownMusic;
        static bool useNightExplorationMusic;
        static bool isInsideTavern;

        public static string explorationPlaylistId;
        public static string explorationNightPlaylistId;
        public static string dungeonPlaylistId;
        public static string tavernPlaylistId;
        public static string townPlaylistId;
        public static string clientId;
        public static string clientSecret;
        public static string refreshToken;

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
            useTownMusic = settings.GetBool("PlaylistIds", "UseTownMusic");
            useNightExplorationMusic = settings.GetBool("PlaylistIds", "UseNightExplorationMusic");

            AssignPlaylistId("Exploration", "3duUM5NBqB8OqIZNWt6W4Q", ref explorationPlaylistId);
            AssignPlaylistId("Dungeons", "5SN1OWk8oQ7KNDp5ZiUWql", ref dungeonPlaylistId);
            AssignPlaylistId("Taverns", "0M23q20iFObvPzht43bSmK", ref tavernPlaylistId);
            AssignPlaylistId("Towns", "2uCzDvmy1yvKbtua6QWEwG", ref townPlaylistId);
            AssignPlaylistId("NightExploration", "7ieTrY5xbsjDJ8ze63Pk1Z", ref explorationNightPlaylistId);
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
        }

        private void OnTransitionToExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            // if exiting a tavern
            if (isInsideTavern)
            {
                PlayPlaylistForCurrentLocation(true);
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
                && !GameManager.Instance.IsPlayerInsideDungeon)
            {
                PlayPlaylistForCurrentLocation(true);
            }
        }

        private void OnExitLocation()
        {            
            if (useTownMusic)
            {
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
            if (useTownMusic)
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

        private void PlayPlaylistForCurrentLocation(bool exitingTavern = false)
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