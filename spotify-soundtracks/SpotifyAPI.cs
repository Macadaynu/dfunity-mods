using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;

namespace Assets.Scripts.Game.MacadaynuMods
{
    public static class SpotifyAPI
    {
        private static readonly HttpClient client = new HttpClient();
        private static string accessToken;
        private static string currentPlaylistId;
        private static string deviceId;
        private static int unauthorisedCount;

        public static void PlayPlaylist(string playlistId)
        {
            Debug.Log($"SPOTIFY SOUNDTRACKS: Play playlist id: {playlistId}");

            currentPlaylistId = playlistId;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                accessToken = GetAuthToken()?.access_token;
            }

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                var device = GetAvailableSpotifyDevices()?.devices?.FirstOrDefault(x => x.type == "Computer");
                if (device == null || string.IsNullOrWhiteSpace(device.id))
                {
                    Debug.Log("SPOTIFY SOUNDTRACKS: No computer device found");
                    return;
                }

                deviceId = device.id;
            }

            var playlistItems = GetPlaylistItems(playlistId);
            if (playlistItems == null)
            {
                Debug.Log($"SPOTIFY SOUNDTRACKS: No playlist items found for playlist id: {playlistId}");
                return;
            }

            UnityEngine.Random.InitState(DateTime.Now.Millisecond);
            var trackNumberToPlay = UnityEngine.Random.Range(0, playlistItems.total);

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"https://api.spotify.com/v1/me/player/play?device_id={deviceId}"),
                Method = HttpMethod.Put,
                Content = new StringContent(
                    $"{{\"context_uri\":\"spotify:playlist:{playlistId}\",\"offset\":{{\"position\":{trackNumberToPlay}}},\"position_ms\":0}}",
                    Encoding.UTF8,
                    "application/json")
            };

            Debug.Log($"SPOTIFY SOUNDTRACKS: Playing playlist for device: {deviceId}");

            SendHttpRequest<string>(request, new AuthenticationHeaderValue("Bearer", $"{accessToken}"), new MediaTypeWithQualityHeaderValue("application/json"));
        }        

        private static SpotifyToken GetAuthToken()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://accounts.spotify.com/api/token"),
                Method = HttpMethod.Post,
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", SpotifyMusicMod.refreshToken }
                })
            };

            var encodedClientidClientsecret = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{SpotifyMusicMod.clientId}:{SpotifyMusicMod.clientSecret}")).TrimEnd(new[] {'='});

            return SendHttpRequest<SpotifyToken>(request, new AuthenticationHeaderValue("Basic", encodedClientidClientsecret));
        }

        public static AvailableSpotifyDevices GetAvailableSpotifyDevices()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.spotify.com/v1/me/player/devices"),
                Method = HttpMethod.Get
            };

            return SendHttpRequest<AvailableSpotifyDevices>(request, new AuthenticationHeaderValue("Bearer", $"{accessToken}"));
        }

        public static SpotifyPlaylistItems GetPlaylistItems(string playlistId)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"https://api.spotify.com/v1/playlists/{playlistId}/tracks"),
                Method = HttpMethod.Get
            };

            return SendHttpRequest<SpotifyPlaylistItems>(request, new AuthenticationHeaderValue("Bearer", $"{accessToken}"));
        }

        public static SpotifyPlayerInfo GetCurrentlyPlayingInfo()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"https://api.spotify.com/v1/me/player"),
                Method = HttpMethod.Get
            };

            return SendHttpRequest<SpotifyPlayerInfo>(request, new AuthenticationHeaderValue("Bearer", $"{accessToken}"));
        }

        public static void PausePlayback()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"https://api.spotify.com/v1/me/player/pause?device_id={deviceId}"),
                Method = HttpMethod.Put
            };

            SendHttpRequest<string>(request, new AuthenticationHeaderValue("Bearer", $"{accessToken}"));
        }

        static T SendHttpRequest<T>(HttpRequestMessage request, AuthenticationHeaderValue authorisationHeader, MediaTypeWithQualityHeaderValue acceptHeader = null)
        {
            PrepareHeaders(authorisationHeader, acceptHeader);

            string webResponse = string.Empty;
            bool unauthorised = false;
            try
            {
                var task = client.SendAsync(request)
                    .ContinueWith((taskwithmsg) =>
                    {
                        var response = taskwithmsg.Result;
                        unauthorised = response.StatusCode == HttpStatusCode.Unauthorized;

                        var jsonTask = response.Content.ReadAsStringAsync();
                        webResponse = jsonTask.Result;
                    });
                task.Wait();
            }
            catch (WebException ex)
            {
                Debug.LogError($"SPOTIFY API EXCEPTION: {ex.Message} {ex.Status}");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"SPOTIFY API EXCEPTION: {e.InnerException.Message}");
                throw;
            }

            if (unauthorised)
            {
                unauthorisedCount += 1;
                accessToken = null;

                if (unauthorisedCount <= 5)
                {
                    PlayPlaylist(currentPlaylistId);
                }
                else
                {
                    unauthorisedCount = 0;
                    accessToken = null;
                    Debug.LogError($"Spotify Access Token Invalid");
                    throw new WebException();
                }

                unauthorisedCount = 0;                
            }            

            return JsonConvert.DeserializeObject<T>(webResponse);
        }

        static void PrepareHeaders(AuthenticationHeaderValue authorisationHeader, MediaTypeWithQualityHeaderValue acceptHeader = null)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = authorisationHeader;

            if (acceptHeader != null)
            {
                client.DefaultRequestHeaders.Accept.Add(acceptHeader);
            }
        }
    }
}