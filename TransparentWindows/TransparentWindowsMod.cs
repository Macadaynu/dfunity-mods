using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using System.Collections.Generic;
using System.Linq;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Serialization;
using Assets.MacadaynuMods.Transparent_Windows;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Utility;

namespace TransparentWindowsModMod
{
    public class TransparentWindowsMod : MonoBehaviour
    {
        static ModSettings settings;
        private static Mod mod;
        public int DelayTime = 500;
        public float ClipBoxPad = 1.15f;
        bool RealGrassModEnabled;
        bool DistantTerrainModEnabled;
        bool TavernsRedoneModEnabled;
        bool SplatTerrainTextureModEnabled;
        bool DreamTexturesEnabled;
        bool TemplesRedoneModEnabled;

        GameObject ClipBox;
        GameObject TerrainClipBox;
        Shader ClipBoxShader;
        Shader OriginalTerrainShader;
        PlayerWeather PlayerWeather;
        AudioClip FireplaceSFX;
        AudioClip CampfireSFX;
        AudioClip RainSFX;
        AudioClip ThunderstormSFX;
        Shader CutOutShader;
        Shader TransparentShader;
        Shader OpaqueShader;
        Shader TerrainClipShader;
        
        static bool EnableFireplaceSFX;
        static bool EnableRainSFX;
        static float FireplaceSFXVolume;
        static float RainSFXVolume;

        DFBlock blockData;
        DFBlock.RmbSubRecord recordData;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<TransparentWindowsMod>();

            mod.IsReady = true;

            settings = mod.GetSettings();
        }

        private void Awake()
        {
            PlayerWeather = GameManager.Instance.PlayerObject.GetComponent<PlayerWeather>();

            PlayerEnterExit.OnTransitionInterior += OnTransitionToInterior;
            PlayerEnterExit.OnTransitionExterior += OnTransitionToExterior;
            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;
            WorldTime.OnNewHour += OnNewHour;
            WeatherManager.OnWeatherChange += WeatherManager_OnWeatherChange;
            PlayerEnterExit.OnRespawnerComplete += OnRespawnerComplete;
        }

        private void Start()
        {
            Mod realGrassMod = ModManager.Instance.GetMod("Real Grass");
            RealGrassModEnabled = realGrassMod != null && realGrassMod.Enabled;

            Mod distantTerrainMod = ModManager.Instance.GetMod("Distant Terrain");
            DistantTerrainModEnabled = distantTerrainMod != null && distantTerrainMod.Enabled;

            Mod tavernsRedoneMod = ModManager.Instance.GetMod("Taverns Redone");
            TavernsRedoneModEnabled = tavernsRedoneMod != null && tavernsRedoneMod.Enabled;

            Mod splatTerrainMod = ModManager.Instance.GetMod("Splat Terrain Texturing");
            SplatTerrainTextureModEnabled = splatTerrainMod != null && splatTerrainMod.Enabled;

            Mod dreamTexturesMod = ModManager.Instance.GetModFromGUID("4b3e27d3-f622-4b7c-9f47-a9ded24956c2");
            DreamTexturesEnabled = dreamTexturesMod != null && dreamTexturesMod.Enabled;

            Mod templesRedoneMod = ModManager.Instance.GetMod("Finding My Religion");
            TemplesRedoneModEnabled = templesRedoneMod != null && templesRedoneMod.Enabled;

            EnableFireplaceSFX = settings.GetBool("GeneralSettings", "EnableFireplaceSFX");
            EnableRainSFX = settings.GetBool("GeneralSettings", "EnableRainSFX");
            RainSFXVolume = settings.GetFloat("GeneralSettings", "RainSFXVolume");
            FireplaceSFXVolume = settings.GetFloat("GeneralSettings", "FireplaceSFXVolume");

            FireplaceSFX = mod.GetAsset<AudioClip>("fire-1", false);
            CampfireSFX = mod.GetAsset<AudioClip>("campfire-1", false);
            RainSFX = mod.GetAsset<AudioClip>("rain-on-windows", false);
            ThunderstormSFX = mod.GetAsset<AudioClip>("thunderstorm", false);
            ClipBoxShader = mod.GetAsset<Shader>("ClipBoxShader");
            OriginalTerrainShader = Shader.Find("Daggerfall/TilemapTextureArray");//mod.GetAsset<Shader>("TerrainShader");
            CutOutShader = mod.GetAsset<Shader>("CS_Lite Standard (Specular setup) Cutout");
            TransparentShader = mod.GetAsset<Shader>("CS_Lite Standard (Specular setup) Transparent");
            OpaqueShader = mod.GetAsset<Shader>("CS_Lite Standard (Specular setup) Opaque");
            TerrainClipShader = mod.GetAsset<Shader>("TerrainClipShader");
        }

        private void Update()
        {
            if (GameManager.Instance.WeatherManager.UpdateWeatherFromClimateArray
                && GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                if (PlayerWeather.PlayerGps.ReadyCheck())
                {
                    GameManager.Instance.WeatherManager.SetWeatherFromWeatherClimateArray();
                    GameManager.Instance.WeatherManager.UpdateWeatherFromClimateArray = false;
                }                
            }
        }

        private async void OnRespawnerComplete()
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                await System.Threading.Tasks.Task.Delay(DelayTime);
                SetupExterior();
            }
        }

        private async void SaveLoadManager_OnLoad(SaveData_v1 saveData)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                await System.Threading.Tasks.Task.Delay(DelayTime);
                SetupExterior();
            }
        }

        private void OnTransitionToInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            if (args.TransitionType == PlayerEnterExit.TransitionType.ToBuildingInterior || GameManager.Instance.PlayerGPS.IsPlayerInTown())
            {
                SetupInterior();
                SetupExterior();
            }            
        }

        private void OnTransitionToExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            if (TavernsRedoneModEnabled || TemplesRedoneModEnabled)
            {
                SetupTerrainMask(OriginalTerrainShader, ignoreTerrainCollision: false, updateShader: true);
            }

            if (ClipBox != null)
            {
                ClipBox.SetActive(false);
            }

            if (args.TransitionType == PlayerEnterExit.TransitionType.ToBuildingExterior)
            {
                DrawTerrainDetails(true);

                GameManager.Instance.MainCamera.farClipPlane = DistantTerrainModEnabled ? 15000.0f : 2600f;

                ActivateExteriorAmbience(true);

                EnableExteriorEnemies(true);

                var combinedModels = GameManager.Instance.ExteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name == "CombinedModels");

                foreach (var combinedModel in combinedModels)
                {
                    combinedModel.GetComponent<MeshCollider>().enabled = true;

                    List<Material> materials = combinedModel.GetComponent<Renderer>().materials.ToList();

                    foreach (var material in materials)
                    {
                        material.shader = Shader.Find("Standard");
                        material.SetFloat("_Glossiness", 0f);
                    }
                }                
            }
        }

        private async void OnNewHour()
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                await System.Threading.Tasks.Task.Delay(DelayTime);
                SetupExterior();
            }
        }

        private void WeatherManager_OnWeatherChange(WeatherType weather)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                UpdateSnowAndRainParticles(weather);

                if (EnableRainSFX)
                {
                    UpdateRainSFX(weather);
                }
            }
        }

        private void UpdateRainSFX(WeatherType weatherType)
        {
            var interiorSFX = GameManager.Instance.InteriorParent.transform.Find("RainSFX");
            if (interiorSFX == null)
            {
                if (weatherType == WeatherType.Rain || weatherType == WeatherType.Thunder)
                {
                    var rainSFX = new GameObject("RainSFX");

                    rainSFX.transform.parent = GameManager.Instance.InteriorParent.transform;

                    AudioSource audioSource = rainSFX.gameObject.AddComponent<AudioSource>();

                    audioSource.clip = weatherType == WeatherType.Thunder ? ThunderstormSFX : RainSFX;
                    audioSource.loop = true;
                    audioSource.volume = RainSFXVolume;
                    audioSource.Play();
                }
            }
            else
            {
                var audioSource = interiorSFX.GetComponent<AudioSource>();

                if (audioSource == null)
                {
                    audioSource = interiorSFX.gameObject.AddComponent<AudioSource>();
                }

                audioSource.timeSamples = 0;
                audioSource.loop = true;                

                if (weatherType != WeatherType.Rain && weatherType != WeatherType.Thunder)
                {
                    audioSource.Stop();
                    return;
                }

                audioSource.volume = RainSFXVolume;

                if (weatherType == WeatherType.Thunder)
                {
                    audioSource.clip = ThunderstormSFX;
                    int randomStartTime = Random.Range(0, ThunderstormSFX.samples - 1);
                    audioSource.timeSamples = randomStartTime;
                }
                else if (weatherType == WeatherType.Rain)
                {                    
                    audioSource.clip = RainSFX;
                }

                audioSource.Play();
            }
        }

        private async void SetupExterior()
        {
            var interiorModel = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                .Where(c => c.gameObject.name.StartsWith("CombinedModels")).FirstOrDefault();

            if (interiorModel != null)
            {
                List<string> materialNames = interiorModel.GetComponent<Renderer>().materials.Select(x => x.name.Replace(" (Instance)", "")).ToList();

                // check if the interior actually has window textures before applying the clipbox shader
                if (materialNames.Intersect(WindowTextureNames.WindowNames).Any())
                {
                    DrawTerrainDetails(false);

                    if (!TemplesRedoneModEnabled || GameManager.Instance.PlayerEnterExit.BuildingType != DFLocation.BuildingTypes.Temple)
                    {
                        ApplyClipBox(interiorModel);
                    }

                    ActivateExteriorAmbience(false);

                    EnableExteriorEnemies(false);

                    // activate the exterior
                    GameManager.Instance.ExteriorParent.SetActive(true);

                    if (TavernsRedoneModEnabled || TemplesRedoneModEnabled)
                    {
                        PositionTerrainSlicer();

                        SetupTerrainMask(TerrainClipShader, true);
                    }

                    await System.Threading.Tasks.Task.Delay(DelayTime);

                    UpdateSnowAndRainParticles(PlayerWeather.WeatherType);

                    // fix to make sky render properly
                    var camera = GameManager.Instance.MainCamera;
                    camera.farClipPlane = 500;
                    camera.clearFlags = CameraClearFlags.Depth;
                }
            }
        }        

        private void SetupInterior()
        {
            var interiorModel = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                .Where(c => c.gameObject.name.StartsWith("CombinedModels")).FirstOrDefault();

            List<string> materialNames = interiorModel.GetComponent<Renderer>().materials.Select(x => x.name.Replace(" (Instance)", "")).ToList();

            // check if the interior actually has window textures before applying the double sided shader
            if (materialNames.Intersect(WindowTextureNames.WindowNames).Any())
            {
                DrawTerrainDetails(false);

                SetupInteriorColliders(interiorModel);

                foreach (var material in interiorModel.GetComponent<Renderer>().materials)
                {
                    // fix for temples as you are able to see windows on top of windows
                    if (!TemplesRedoneModEnabled && (material.name.Contains("063_2-0") || material.name.Contains("363_2-0")))
                    {
                        material.shader = CutOutShader;
                        material.SetFloat("_ZWrite", 1f);
                    }
                    // if the texture is a window make it transparent
                    else if (WindowTextureNames.WindowNames.Any(x => x.Contains(material.name.Replace(" (Instance)", ""))))
                    {
                        material.shader = TransparentShader;
                        material.SetFloat("_ZWrite", 1f);
                    }
                    // make every other indoor material double-sided opaque (except taverns redone and temples redone textures)
                    else if (!(TavernsRedoneModEnabled && GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Tavern)
                        && !(TemplesRedoneModEnabled && GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Temple && material.name.Contains("TEXTURE.363 [Index=4]"))
                        && !material.name.StartsWith("TEXTURE.449")
                        && !material.name.StartsWith("TEXTURE.450")
                        && !material.name.StartsWith("TEXTURE.049"))
                    {
                        material.shader = OpaqueShader;
                    }

                    material.SetFloat("_Glossiness", 0f);
                    material.SetFloat("_SpecularIntensity", 0f);
                }
            }

            if (EnableRainSFX)
            {
                UpdateRainSFX(PlayerWeather.WeatherType);
            }

            if (EnableFireplaceSFX)
            {
                var models = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name == "Models").FirstOrDefault();

                foreach (Transform model in models.GetComponentsInChildren<Transform>())
                {
                    if ((model.gameObject.name.Contains("ID=41116") || model.gameObject.name.Contains("ID=41117")) && model.gameObject.GetComponent<AudioSource>() == null)
                    {
                        AudioSource audioSource = model.gameObject.AddComponent<AudioSource>();

                        audioSource.clip = FireplaceSFX;
                        audioSource.loop = true;
                        audioSource.spatialBlend = 1.0f;
                        audioSource.volume = FireplaceSFXVolume;
                        audioSource.Play();
                    }
                }

                var interiorFlats = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name == "Interior Flats").FirstOrDefault();

                foreach (var item in interiorFlats.GetComponentsInChildren<Transform>())
                {
                    if ((item.gameObject.name == "DaggerfallBillboard [TEXTURE.210, Index=0]" || item.gameObject.name.Contains("210_0"))
                        && item.gameObject.GetComponent<AudioSource>() == null)
                    {
                        AudioSource audioSource = item.gameObject.AddComponent<AudioSource>();

                        audioSource.clip = CampfireSFX;
                        audioSource.loop = true;
                        audioSource.spatialBlend = 1.0f;
                        audioSource.volume = FireplaceSFXVolume;
                        audioSource.Play();
                    }
                }
            }
        }

        private void UpdateSnowAndRainParticles(WeatherType weatherType)
        {
            switch (weatherType)
            {
                case WeatherType.Snow:
                    PlayerWeather.SnowParticles.SetActive(true);
                    PlayerWeather.RainParticles.SetActive(false);
                    break;
                case WeatherType.Thunder:
                case WeatherType.Rain:
                    PlayerWeather.RainParticles.SetActive(true);
                    PlayerWeather.SnowParticles.SetActive(false);
                    break;
                default:
                    PlayerWeather.SnowParticles.SetActive(false);
                    PlayerWeather.RainParticles.SetActive(false);
                    break;
            }
        }

        private void SetupTerrainMask(Shader terrainShader, bool ignoreTerrainCollision, bool updateShader = true)
        {
            var playerTerrain = GameManager.Instance.StreamingWorld.PlayerTerrainTransform?.GetComponentInChildren<DaggerfallTerrain>();
            if (playerTerrain != null)
            {
                // only update the terrain shader if it is changing
                if (playerTerrain.GetComponent<Terrain>().materialTemplate.shader != terrainShader
                    && updateShader
                    && !SplatTerrainTextureModEnabled)
                {
                    playerTerrain.GetComponent<Terrain>().materialTemplate.shader = terrainShader;
                }

                Physics.IgnoreCollision(playerTerrain.GetComponent<TerrainCollider>(), GameManager.Instance.PlayerEntityBehaviour.transform.GetComponent<CharacterController>(), ignoreTerrainCollision);
            }
        }

        private void DrawTerrainDetails(bool drawTerrainDetails)
        {
            if (!RealGrassModEnabled)
            {
                return;
            }

            var playerTerrain = GameManager.Instance?.StreamingWorld?.PlayerTerrainTransform?.GetComponentInChildren<DaggerfallTerrain>();
            if (playerTerrain != null)
            {
                var terrain = playerTerrain.gameObject?.GetComponent<Terrain>();

                if (terrain != null)
                {
                    terrain.drawTreesAndFoliage = drawTerrainDetails;
                }
            }
        }

        private void PositionTerrainSlicer()
        {
            if (TerrainClipBox == null)
            {
                AssignBlockData(GameManager.Instance.PlayerEnterExit.ExteriorDoors[0]);

                var obj = recordData.Interior.Block3dObjectRecords.FirstOrDefault(x => x.ModelIdNum == 31622);

                if (obj.ModelIdNum == 0)
                {
                    obj = recordData.Interior.Block3dObjectRecords.FirstOrDefault(x => x.ModelIdNum == 31623);
                }

                if (obj.ModelIdNum == 0)
                {
                    obj = recordData.Interior.Block3dObjectRecords.FirstOrDefault(x => x.ModelIdNum == 31522);
                }

                if (obj.ModelIdNum == 0)
                {
                    obj = recordData.Interior.Block3dObjectRecords.FirstOrDefault(x => x.ModelIdNum == 31523);
                }

                if (obj.ModelIdNum == 0)
                {
                    return;
                }

                Vector3 modelPosition = new Vector3(obj.XPos, -obj.YPos, obj.ZPos) * MeshReader.GlobalScale;

                TerrainClipBox = mod.GetAsset<GameObject>("TerrainClipBox", true);

                TerrainClipBox.transform.parent = GameManager.Instance.PlayerEnterExit.Interior.transform;

                TerrainClipBox.transform.localPosition = new Vector3(modelPosition.x, modelPosition.y, modelPosition.z);

                TerrainClipBox.transform.localScale = new Vector3(3.6f, 10f, 3.5f);

                TerrainClipBox.AddComponent<TerrainClipBox>();
            }
        }

        private void AssignBlockData(StaticDoor door)
        {
            // Get block data
            DFLocation location = GameManager.Instance.PlayerGPS.CurrentLocation;

            Debug.Log($"LOCATION: {location}");

            DFBlock[] blocks = RMBLayout.GetLocationBuildingData(location);
            bool foundBlock = false;
            for (int index = 0; index < blocks.Length && !foundBlock; ++index)
            {
                if (blocks[index].Index == door.blockIndex)
                {
                    this.blockData = blocks[index];
                    foundBlock = true;
                }
            }

            // Get record data
            recordData = blockData.RmbBlock.SubRecords[door.recordIndex];
        }

        private void ApplyClipBox(Transform interiorModel)
        {
            if (ClipBox == null)
            {
                ClipBox = mod.GetAsset<GameObject>("ClipBox", true);
                ClipBox.AddComponent<ClipBox>();
            }

            Bounds interiorBounds = interiorModel.GetComponent<Renderer>().bounds;

            ClipBox.transform.localScale = new Vector3(interiorBounds.size.x * ClipBoxPad, interiorBounds.size.y * ClipBoxPad, interiorBounds.size.z * ClipBoxPad);
            ClipBox.transform.position = new Vector3(interiorBounds.center.x, interiorBounds.center.y, interiorBounds.center.z);

            //TODO: Does this have to be all combined models?
            var combinedModels = GameManager.Instance.ExteriorParent.GetComponentsInChildren<Transform>()
                .Where(c => c.gameObject.name == "CombinedModels");

            // set the exterior combined meshes to use the clipbox shader
            foreach (var combinedModel in combinedModels)
            {
                combinedModel.GetComponent<MeshCollider>().enabled = false;

                Material[] materials = combinedModel.GetComponent<Renderer>().materials;
                foreach (var material in materials)
                {
                    var shaderMaterial = new Material(ClipBoxShader);

                    shaderMaterial.CopyPropertiesFromMaterial(material);

                    material.shader = ClipBoxShader;

                    if (!DreamTexturesEnabled)
                    {
                        material.SetFloat("_Emission", 1f);
                    }

                    material.SetFloat("_Glossiness", 0f);
                }
            }

            // activate the Clip Box around the Interior
            if (ClipBox != null)
            {
                ClipBox.SetActive(true);
            }
        }

        void ActivateExteriorAmbience(bool activate)
        {
            var songPlayer = GameManager.Instance.ExteriorParent.transform.Find("SongPlayer");
            songPlayer.gameObject.SetActive(activate);

            var ambientEffects = GameManager.Instance.ExteriorParent.transform.Find("WeatherAmbientEffects");
            ambientEffects.gameObject.SetActive(activate);
        }

        void EnableExteriorEnemies(bool enable)
        {
            var enemies = GameManager.Instance.ExteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name.StartsWith("DaggerfallEnemy"));

            foreach (var enemy in enemies)
            {
                enemy.gameObject.GetComponent<CharacterController>().enabled = enable;
                enemy.gameObject.GetComponent<EnemyAttack>().enabled = enable;
            }
        }

        void SetupInteriorColliders(Transform interiorModel)
        {
            if (interiorModel.GetComponent<MeshCollider>() == null)
            {
                interiorModel.gameObject.AddComponent<MeshCollider>();
            }

            if (interiorModel.GetComponent<Rigidbody>() == null)
            {
                var rigidBody = interiorModel.gameObject.AddComponent<Rigidbody>();
                rigidBody.isKinematic = true;
            }

            if (interiorModel.GetComponent<InteriorCollisionHandler>() == null)
            {
                interiorModel.gameObject.AddComponent<InteriorCollisionHandler>();
            }
        }
    }
}
