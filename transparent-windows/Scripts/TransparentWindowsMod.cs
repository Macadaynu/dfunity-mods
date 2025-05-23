using Assets.MacadaynuMods.Transparent_Windows;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelOptions;
using UnityEngine;
using static DaggerfallWorkshop.Utility.ModelCombiner;

namespace TransparentWindowsModMod
{
    public class TransparentWindowsMod : MonoBehaviour
    {
        #region Properties

        private static ModSettings settings;
        private static Mod mod;
        public int DelayTime = 500;
        public float ClipBoxPad = 1.15f;
        private bool RealGrassModEnabled;
        private bool DistantTerrainModEnabled;
        private bool TavernsRedoneModEnabled;
        private bool SplatTerrainTextureModEnabled;
        private bool TemplesRedoneModEnabled;
        private bool DynamicSkiesModEnabled;
        //private bool HandPaintedModelsModEnabled;
        private bool isFoggyOutside;
        private bool IsDreamVersion;

        private GameObject ClipBox;
        private GameObject SnowParticles;
        private GameObject RainParticles;
        private Shader ClipBoxShader;
        private Shader OriginalTerrainShader;
        private Shader DaggerfallDefaultShader;
        private Shader ExteriorClipShader;
        private Shader StandardShader;
        private PlayerWeather PlayerWeather;
        private AudioClip FireplaceSFX;
        private AudioClip CampfireSFX;
        private AudioClip RainSFX;
        private AudioClip ThunderstormSFX;
        private Shader TerrainClipShader;
        private Shader ModShader;
        private GameObject[] TerrainClippers = new GameObject[0];
        private static Material Glass;
        private static Material Hidden;
        private Color GlassColour;
        private int GlassAlpha;

        private static bool EnableFireplaceSFX;
        private static bool EnableRainSFX;
        private static float FireplaceSFXVolume;
        private static float RainSFXVolume;

        public float TerrrainClipHeight = 5.0f;

        private DFBlock blockData;
        private DFBlock.RmbSubRecord recordData;

        private HashSet<GameObject> ObjectsToReEnable = new HashSet<GameObject>();

        public static TransparentWindowsMod Instance { get; private set; }

        #endregion

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            Instance = go.AddComponent<TransparentWindowsMod>();

            mod.IsReady = true;

            settings = mod.GetSettings();

            mod.LoadSettingsCallback = Instance.LoadSettings;
        }

        private void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            EnableFireplaceSFX = settings.GetBool("GeneralSettings", "EnableFireplaceSFX");
            EnableRainSFX = settings.GetBool("GeneralSettings", "EnableRainSFX");
            RainSFXVolume = settings.GetFloat("GeneralSettings", "RainSFXVolume");
            FireplaceSFXVolume = settings.GetFloat("GeneralSettings", "FireplaceSFXVolume");
            GlassColour = settings.GetColor("GeneralSettings", "GlassColour");
            GlassAlpha = settings.GetInt("GeneralSettings", "GlassOpacity");

            float alpha = GlassAlpha / 255f;
            Glass.color = new Color(GlassColour.r, GlassColour.g, GlassColour.b, alpha);

            if (GameManager.Instance.IsPlayerInside)
            {
                UpdateFireplaceSFX();
                UpdateRainSFX(PlayerWeather.WeatherType);
            }
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
            Mod mod1 = ModManager.Instance.GetMod("Real Grass");
            RealGrassModEnabled = mod1 != null && mod1.Enabled;

            Mod mod2 = ModManager.Instance.GetMod("Distant Terrain");
            DistantTerrainModEnabled = mod2 != null && mod2.Enabled;

            Mod mod3 = ModManager.Instance.GetMod("Taverns Redone");
            TavernsRedoneModEnabled = mod3 != null && mod3.Enabled;

            Mod mod4 = ModManager.Instance.GetMod("Splat Terrain Texturing");
            SplatTerrainTextureModEnabled = mod4 != null && mod4.Enabled;

            Mod mod5 = ModManager.Instance.GetMod("Finding My Religion");
            TemplesRedoneModEnabled = mod5 != null && mod5.Enabled;

            Mod dynamicSkies = ModManager.Instance.GetMod("Dynamic Skies");
            DynamicSkiesModEnabled = dynamicSkies != null && dynamicSkies.Enabled;

            //Mod handPaintedModels = ModManager.Instance.GetMod("Hand Painted Models - Main");
            //HandPaintedModelsModEnabled = handPaintedModels != null && handPaintedModels.Enabled;
            //HandPaintedModelsModEnabled = true;

            FireplaceSFX = mod.GetAsset<AudioClip>("fire-1", false);
            CampfireSFX = mod.GetAsset<AudioClip>("campfire-1", false);
            RainSFX = mod.GetAsset<AudioClip>("rain-on-windows", false);
            ThunderstormSFX = mod.GetAsset<AudioClip>("thunderstorm", false);

            IsDreamVersion = mod.GUID == "3ee77927-fdc5-4dbb-9723-bc0e3300cf9d";

            ClipBoxShader = mod.GetAsset<Shader>("ClipBoxShader");
            OriginalTerrainShader = Shader.Find("Daggerfall/TilemapTextureArray");
            DaggerfallDefaultShader = Shader.Find("Daggerfall/Default");
            TerrainClipShader = mod.GetAsset<Shader>("TerrainClipShader");
            ExteriorClipShader = mod.GetAsset<Shader>("ExteriorClipShader");
            ModShader = mod.GetAsset<Shader>("Double Sided Specular");
            Glass = mod.GetAsset<Material>("Glass");
            Hidden = mod.GetAsset<Material>("Hidden");
            StandardShader = Shader.Find("Standard");

            EnableFireplaceSFX = settings.GetBool("GeneralSettings", "EnableFireplaceSFX");
            EnableRainSFX = settings.GetBool("GeneralSettings", "EnableRainSFX");
            RainSFXVolume = settings.GetFloat("GeneralSettings", "RainSFXVolume");
            FireplaceSFXVolume = settings.GetFloat("GeneralSettings", "FireplaceSFXVolume");
            GlassColour = settings.GetColor("GeneralSettings", "GlassColour");
            GlassAlpha = settings.GetInt("GeneralSettings", "GlassOpacity");            

            float alpha = GlassAlpha / 255f;
            Glass.color = new Color(GlassColour.r, GlassColour.g, GlassColour.b, alpha);

            // Load initial dynamic settings.
            LoadSettings(settings, new ModSettingsChange());

            AssignParticleSystems();

            //DarkenNightInteriors();
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
                await Task.Delay(DelayTime);
                SetupExterior();
            }
        }

        private async void SaveLoadManager_OnLoad(SaveData_v1 saveData)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                await Task.Delay(DelayTime);
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
            if (isFoggyOutside)
            {
                GameManager.Instance.WeatherManager.SetWeather(WeatherType.Fog);
                isFoggyOutside = false;
            }

            //TODO: Work out if building has basement, rather than check if these mods are enabled
            //if (TavernsRedoneModEnabled || TemplesRedoneModEnabled)
            //{
            SetupTerrainMask(OriginalTerrainShader, ignoreTerrainCollision: false, updateShader: true);
            ClearTerrainSlicers();
            //}

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

                ReApplyExteriorShaders();
            }

            //if (HandPaintedModelsModEnabled)
            //{
                foreach (var item in ObjectsToReEnable)
                {
                    if (item != null)
                    {
                        item.SetActive(true);
                    }
                }

                ObjectsToReEnable.Clear();
            //}
        }

        private async void OnNewHour()
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                await Task.Delay(DelayTime);
                SetupExterior();
            }
        }

        private void WeatherManager_OnWeatherChange(WeatherType weather)
        {
            isFoggyOutside = false;

            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding)
            {
                var materialsToDouble = GetInteriorMaterialsToDoubleSide();

                if (materialsToDouble.Any())
                {
                    UpdateSnowAndRainParticles(weather);
                    GameManager.Instance.WeatherManager.SetFog(GameManager.Instance.WeatherManager.SunnyFogSettings, false);
                }

                if (EnableRainSFX)
                {
                    UpdateRainSFX(weather);
                }
            }
        }

        private void UpdateRainSFX(WeatherType weatherType)
        {
            var interior = GameManager.Instance.PlayerEnterExit.Interior.transform;

            var interiorSFX = interior.transform.Find("RainSFX");
            if (interiorSFX == null)
            {
                if (weatherType == WeatherType.Rain || weatherType == WeatherType.Thunder)
                {
                    var rainSFX = new GameObject("RainSFX");

                    rainSFX.transform.parent = interior.transform;

                    AudioSource audioSource = rainSFX.gameObject.AddComponent<AudioSource>();

                    audioSource.clip = weatherType == WeatherType.Thunder ? ThunderstormSFX : RainSFX;
                    audioSource.loop = true;
                    audioSource.volume = RainSFXVolume;
                    audioSource.Play();
                    audioSource.enabled = EnableRainSFX;
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
                audioSource.enabled = EnableRainSFX;

                if (weatherType == WeatherType.Thunder)
                {
                    audioSource.clip = ThunderstormSFX;
                    int randomStartTime = UnityEngine.Random.Range(0, ThunderstormSFX.samples - 1);
                    audioSource.timeSamples = randomStartTime;
                }
                else if (weatherType == WeatherType.Rain)
                {
                    audioSource.clip = RainSFX;
                }

                audioSource.Play();
            }
        }

        private void UpdateFireplaceSFX()
        {
            var models = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                .Where(c => c.gameObject.name == "Models").FirstOrDefault();

            foreach (Transform model in models.GetComponentsInChildren<Transform>())
            {
                if (model.gameObject.name.Contains("ID=41116") || model.gameObject.name.Contains("ID=41117"))
                {
                    var fireAudio = model.gameObject.GetComponent<AudioSource>();
                    if (fireAudio == null)
                    {
                        AudioSource audioSource = model.gameObject.AddComponent<AudioSource>();

                        audioSource.clip = FireplaceSFX;
                        audioSource.loop = true;
                        audioSource.spatialBlend = 1.0f;
                        audioSource.volume = FireplaceSFXVolume;
                        audioSource.Play();
                        audioSource.enabled = EnableFireplaceSFX;
                    }
                    else
                    {
                        fireAudio.volume = FireplaceSFXVolume;
                        fireAudio.enabled = EnableFireplaceSFX;
                    }
                }
            }

            var interiorFlats = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                .Where(c => c.gameObject.name == "Interior Flats").FirstOrDefault();

            foreach (var item in interiorFlats.GetComponentsInChildren<Transform>())
            {
                if (item.gameObject.name == "DaggerfallBillboard [TEXTURE.210, Index=0]" || item.gameObject.name.Contains("210_0"))
                {
                    var fireAudio = item.gameObject.GetComponent<AudioSource>();
                    if (fireAudio == null)
                    {
                        AudioSource audioSource = item.gameObject.AddComponent<AudioSource>();

                        audioSource.clip = CampfireSFX;
                        audioSource.loop = true;
                        audioSource.spatialBlend = 1.0f;
                        audioSource.volume = FireplaceSFXVolume;
                        audioSource.Play();
                    }
                    else
                    {
                        fireAudio.volume = FireplaceSFXVolume;
                        fireAudio.enabled = EnableFireplaceSFX;
                    }
                }
            }
        }

        private async void SetupExterior()
        {
            var materialsToDoubleSide = GetInteriorMaterialsToDoubleSide();
            if (materialsToDoubleSide.Any())
            {
                if (GameManager.Instance.WeatherManager.PlayerWeather.WeatherType == WeatherType.Fog)
                {
                    GameManager.Instance.WeatherManager.SetWeather(WeatherType.Overcast);
                    isFoggyOutside = true;
                }

                DrawTerrainDetails(false);

                ApplyClipBox();                

                ActivateExteriorAmbience(false);

                EnableExteriorEnemies(false);

                GameManager.Instance.ExteriorParent.SetActive(true);

                // TODO: Check if building has a basement before doing the terrain slicing
                PositionTerrainSlicers();
                SetupTerrainMask(TerrainClipShader, true);                

                if (GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Palace)
                {
                    SetupPalaceExteriorClip();
                }

                await Task.Delay(DelayTime);

                UpdateSnowAndRainParticles(PlayerWeather.WeatherType);

                Camera mainCamera = GameManager.Instance.MainCamera;

                //fix to make sky render properly
                if (!DynamicSkiesModEnabled)
                {                    
                    mainCamera.clearFlags = CameraClearFlags.Depth;
                }
                else if (DistantTerrainModEnabled)
                {
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                }

                //TODO: Is this needed for modless?
                mainCamera.farClipPlane = 500f;

            }
        }

        private void SetupInterior()
        {
            var materialsToDoubleSide = GetInteriorMaterialsToDoubleSide();

            // check if the interior actually has window textures before amending interior details
            if (materialsToDoubleSide.Any())
            {
                //DaggerfallUI.AddHUDText("This place has windows");

                if (GameManager.Instance.WeatherManager.PlayerWeather.WeatherType == WeatherType.Fog)
                {
                    GameManager.Instance.WeatherManager.SetWeather(WeatherType.Overcast);
                    isFoggyOutside = true;
                }

                //remove terrain details for real grass mod
                DrawTerrainDetails(false);

                if (DaggerfallUnity.Instance.Option_CombineRMB)
                {
                    SetupInteriorColliders(GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                        .Where(c => c.gameObject.name == "CombinedModels").FirstOrDefault());
                }

                if (GameManager.Instance.PlayerEnterExit.BuildingType != DFLocation.BuildingTypes.Palace)
                {
                    ApplyDoubleSidedInteriorTextures(materialsToDoubleSide);
                }                

                ApplyGlassPanes();
            }

            if (EnableRainSFX)
            {
                UpdateRainSFX(PlayerWeather.WeatherType);
            }

            if (EnableFireplaceSFX)
            {
                UpdateFireplaceSFX();
            }
        }

        private void ApplyDoubleSidedInteriorTextures(Material[] materialsToDoubleSide)
        {
            foreach (var material in materialsToDoubleSide)
            {
                //if it's a window texture
                if (material.shader.name == ModShader.name)
                {
                    material.renderQueue++;

                    //Taverns redone tavern windows do not need double faced textures
                    if (TavernsRedoneModEnabled && GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Tavern)
                    {
                        material.SetFloat("_Cull", 2f);
                    }
                }
                else if (!(TavernsRedoneModEnabled && GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Tavern) &&
                        !(TemplesRedoneModEnabled && GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Temple && material.name.Contains("TEXTURE.363 [Index=4]")))
                {
                    var textureGroups = material.name.Split(' ');
                    if (textureGroups?.Length > 0)
                    {
                        if (IsDreamVersion)
                        {
                            if (material.name.StartsWith("363_"))
                            {
                                continue;
                            }

                            if (ModdedInteriorTextureNames.InteriorTextures.Any(x => textureGroups[0].StartsWith(x)))
                            {
                                material.shader = ModShader;
                                material.SetColor("_SpecColor", Color.black);
                            }
                        }
                        // only double side walls and ceilings
                        else if (InteriorTextureNames.InteriorTextures.Contains(textureGroups[0]))
                        {
                            material.shader = ModShader;
                            material.SetColor("_SpecColor", Color.black);
                            //material.SetFloat("_Cull", 0f);
                        }
                    }
                }
            }
        }

        private Material[] GetInteriorMaterialsToDoubleSide()
        {
            if (DaggerfallUnity.Instance.Option_CombineRMB)
            {
                var interiorModel = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name.StartsWith("CombinedModels")).FirstOrDefault();

                List<string> materialNames = interiorModel.GetComponent<Renderer>().materials.Select(x => x.name.Replace(" (Instance)", "")).ToList();

                // check if the interior actually has window textures before applying the double sided shader
                if (materialNames.Intersect(WindowTextureNames.WindowNames).Any())
                {
                    return interiorModel.GetComponent<Renderer>().materials;
                }
            }
            else
            {
                //TODO: Check this is returning all materials we need to double side when RMB is not combined
                var modelParent = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name.StartsWith("Models")).FirstOrDefault();

                if (modelParent != null)
                {
                    var allChildRenderers = modelParent.GetComponentsInChildren<Renderer>();
                    var allMaterials = new List<Material>();
                    var materialNames = new List<string>();

                    foreach (var childRenderer in allChildRenderers)
                    {
                        if (!childRenderer.gameObject.name.Contains("ID=41116") && !childRenderer.gameObject.name.Contains("ID=41117"))
                        { 
                            materialNames.AddRange(childRenderer.materials.Select(x => x.name.Replace(" (Instance)", "")).ToList());
                            allMaterials.AddRange(childRenderer.materials);
                        }
                    }

                    if (materialNames.Intersect(WindowTextureNames.WindowNames).Any())
                    {
                        return allMaterials.ToArray();
                    }
                }
            }

            return new Material[0];
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

        private void SetupPalaceExteriorClip()
        {        
            var currentBlock = GetCurrentBlock();

            if (currentBlock != null)
            {
                Transform exterior;

                if (DaggerfallUnity.Instance.Option_CombineRMB)
                {
                    exterior = currentBlock.GetComponentsInChildren<Transform>()
                        .Where(c => c.gameObject.name == "CombinedModels").FirstOrDefault();
                }
                else
                {
                    exterior = currentBlock.GetChild(0).GetChild(0);
                }

                if (exterior != null)
                {
                    exterior.GetComponent<MeshCollider>().enabled = false;

                    Material[] materials = exterior.GetComponent<Renderer>().materials;
                    foreach (var material in materials)
                    {
                        var shaderMaterial = new Material(ExteriorClipShader);

                        shaderMaterial.CopyPropertiesFromMaterial(material);

                        material.shader = ExteriorClipShader;                         
                    }
                }
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

        private void PositionTerrainSlicers()
        {
            if (!TerrainClippers.Any())
            {
                AssignBlockData(GameManager.Instance.PlayerEnterExit.ExteriorDoors[0]);

                //TODO: Does this need to be limited to the size of the array in the shader?
                var matrices = new Matrix4x4[8];

                var stairwellsToHide = recordData.Interior.Block3dObjectRecords.Where(x =>
                    x.ModelIdNum == 5000 ||
                    x.ModelIdNum == 5100 ||
                    x.ModelIdNum == 5200 ||
                    x.ModelIdNum == 5300 ||
                    x.ModelIdNum == 5400 ||
                    x.ModelIdNum == 5500 ||
                    x.ModelIdNum == 5600 ||
                    x.ModelIdNum == 5700 ||
                    x.ModelIdNum == 5800 ||
                    x.ModelIdNum == 31022 ||
                    x.ModelIdNum == 31122 ||
                    x.ModelIdNum == 31622 ||
                    x.ModelIdNum == 31623 ||
                    x.ModelIdNum == 31522 ||
                    x.ModelIdNum == 31523).ToArray();

                TerrainClippers = new GameObject[stairwellsToHide.Count()];

                var yOffset = GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Palace ? 5f : 0f;

                var lastMatrix = new Matrix4x4();
                for (int i = 0; i < stairwellsToHide.Length; i++)
                {
                    var terrainClipBox = mod.GetAsset<GameObject>("TerrainClipBox", true);
                    Vector3 modelPosition = new Vector3(stairwellsToHide[i].XPos, -stairwellsToHide[i].YPos, stairwellsToHide[i].ZPos) * MeshReader.GlobalScale;
                    terrainClipBox.transform.parent = GameManager.Instance.PlayerEnterExit.Interior.transform;
                    terrainClipBox.transform.localPosition = new Vector3(modelPosition.x, modelPosition.y + yOffset, modelPosition.z);
                    terrainClipBox.transform.localScale = new Vector3(3.6f, 10f, 3.5f);
                    matrices[i] = terrainClipBox.transform.worldToLocalMatrix;
                    lastMatrix = terrainClipBox.transform.worldToLocalMatrix;
                }

                if (stairwellsToHide.Any())
                {
                    for (int i = stairwellsToHide.Length - 1; i < matrices.Length; i++)
                    {
                        matrices[i] = lastMatrix;
                    }

                    Shader.SetGlobalMatrixArray("_TerrainMatrices", matrices);
                }
            }
        }

        private void ClearTerrainSlicers()
        {
            TerrainClippers = new GameObject[0];
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

        public static Bounds GetInteriorCombinedModelBounds()
        {
            var models = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name == "Models").FirstOrDefault();

            Bounds combined = new Bounds();

            for (int i = 0; i < models.childCount; ++i)
            {
                GameObject model = models.GetChild(i).gameObject;
                var meshRenderer = model.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    Bounds bounds = meshRenderer.bounds;
                    if (combined.size == Vector3.zero)
                        combined = new Bounds(bounds.center, bounds.size);
                    else
                        combined.Encapsulate(bounds);
                }
            }

            return combined;
        }

        private void ApplyGlassPanes()
        {
            AssignBlockData(GameManager.Instance.PlayerEnterExit.ExteriorDoors[0]);

            GameObject node = new GameObject("GlassPanes");
            foreach (DFBlock.RmbBlock3dObjectRecord obj in recordData.Interior.Block3dObjectRecords)
            {
                if (obj.ModelIdNum == 41116 || obj.ModelIdNum == 41117)
                    continue;

                DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

                CachedMaterial[] cachedMaterials;
                int[] textureKeys;
                bool hasAnimations;
                Mesh mesh = dfUnity.MeshReader.GetMesh(
                    dfUnity,
                    obj.ModelIdNum,
                    out cachedMaterials,
                    out textureKeys,
                    out hasAnimations,
                    dfUnity.MeshReader.AddMeshTangents,
                    dfUnity.MeshReader.AddMeshLightmapUVs);

                if (cachedMaterials != null)
                {
                    List<string> materialNames = cachedMaterials.Select(x => x.material?.name?.Replace(" (Instance)", "")).ToList();

                    // check if the mesh actually has a window texture before applying the glass pane
                    if (materialNames.Intersect(WindowTextureNames.WindowNames).Any())
                    {
                        ModelData modelData;
                        dfUnity.MeshReader.GetModelData(obj.ModelIdNum, out modelData);

                        Vector3 modelPosition = new Vector3(obj.XPos, -obj.YPos, obj.ZPos) * MeshReader.GlobalScale;

                        // Get model transform
                        Vector3 modelRotation = new Vector3(-obj.XRotation / BlocksFile.RotationDivisor, -obj.YRotation / BlocksFile.RotationDivisor, -obj.ZRotation / BlocksFile.RotationDivisor);
                        Vector3 modelScale = RMBLayout.GetModelScaleVector(obj);
                        Matrix4x4 modelMatrix = Matrix4x4.TRS(modelPosition, Quaternion.Euler(modelRotation), modelScale);

                        // Add individual GameObject
                        var modelGO = GameObjectHelper.CreateDaggerfallMeshGameObject(obj.ModelIdNum, node.transform, dfUnity.Option_SetStaticFlags);
                        modelGO.transform.position = modelMatrix.GetColumn(3);
                        modelGO.transform.rotation = modelMatrix.rotation;
                        modelGO.transform.localScale = modelMatrix.lossyScale;

                        if (modelData.SubMeshes.Length > 3)
                        {
                            modelGO.transform.localScale = new Vector3(1.002f, 1.002f, 1.002f);
                        }
                        else
                        {
                            modelGO.transform.Translate(new Vector3(0.05f, 0, 0), Space.Self);
                        }

                        var meshRenderer = modelGO.GetComponent<MeshRenderer>();

                        var glassMaterials = new Material[meshRenderer.materials.Length];
                        for (int i = 0; i < glassMaterials.Length; i++)
                        {
                            var materialName = meshRenderer.materials[i].name.Replace(" (Instance)", "");

                            if (WindowTextureNames.WindowNames.Contains(materialName))
                            {
                                if (materialName == ("028_2-0") || materialName == ("066_2-0"))
                                {
                                    glassMaterials[i] = Hidden;
                                }
                                else
                                {
                                    glassMaterials[i] = Glass;
                                }
                            }
                            else
                            {
                                glassMaterials[i] = Hidden;
                            }
                        }

                        meshRenderer.materials = glassMaterials;
                    }
                }
            }

            StaticDoor door;
            Vector3 closestDoorPos = DaggerfallStaticDoors.FindClosestDoor(transform.position, GameManager.Instance.PlayerEnterExit.ExteriorDoors, out door);

            node.transform.position = door.ownerPosition + (Vector3)door.buildingMatrix.GetColumn(3);
            node.transform.rotation = GameObjectHelper.QuaternionFromMatrix(door.buildingMatrix);

            var models = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                .Where(c => c.gameObject.name == "Models").FirstOrDefault();

            node.transform.parent = models;
        }

        private void ApplyClipBox()
        {
            try
            {
                // Create the ClipBox if it doesn't exist yet
                if (ClipBox == null)
                {
                    ClipBox = mod.GetAsset<GameObject>("ClipBox", true);
                    ClipBox.AddComponent<ClipBox>();
                }

                var interiorBounds = new Bounds();
                var transformsToApplyShader = new List<Transform>();
                if (DaggerfallUnity.Instance.Option_CombineRMB)
                {
                    var interiorModel = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
                        .Where(c => c.gameObject.name == "CombinedModels").FirstOrDefault();

                    if (interiorModel != null)
                    {
                        // Get the interior bounds to determine the size of the ClipBox
                        interiorBounds = interiorModel.GetComponent<Renderer>().bounds;

                        transformsToApplyShader = GameManager.Instance.ExteriorParent.GetComponentsInChildren<Transform>()
                            .Where(c => c.gameObject.name == "CombinedModels").ToList();
                    }
                }
                else
                {
                    interiorBounds = GetInteriorCombinedModelBounds();

                    if (GameManager.Instance.PlayerEnterExit.BuildingType != DFLocation.BuildingTypes.Palace)
                    {
                        var location = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;
                        if (location != null)
                        {
                            var meshes = location.GetComponentsInChildren<DaggerfallMesh>();
                            foreach (var mesh in meshes)
                            {
                                transformsToApplyShader.Add(mesh.transform);
                            }
                        }
                    }

                    var interiorCollisions = ClipBox.GetComponent<InteriorCollisionHandler>();
                    if (interiorCollisions == null)
                    {
                        ClipBox.AddComponent<InteriorCollisionHandler>();
                    }

                    var npcs = FindObjectsOfType<MobilePersonNPC>();
                    foreach (var npc in npcs)
                    {
                        if (IsInside(npc.gameObject, ClipBox))
                        {
                            npc.gameObject.SetActive(false);
                        }
                    }
                }

                if (interiorBounds != null)
                {
                    ClipBox.transform.localScale = new Vector3(interiorBounds.size.x * ClipBoxPad, interiorBounds.size.y * ClipBoxPad, interiorBounds.size.z * ClipBoxPad);
                    ClipBox.transform.position = new Vector3(interiorBounds.center.x, interiorBounds.center.y, interiorBounds.center.z);
                }

                if (GameManager.Instance.PlayerEnterExit.BuildingType != DFLocation.BuildingTypes.Palace)
                {
                    if (!TemplesRedoneModEnabled || GameManager.Instance.PlayerEnterExit.BuildingType != DFLocation.BuildingTypes.Temple)
                    {
                        ApplyClipBoxShaderToExterior(transformsToApplyShader);
                    }
                }

                // Activate the ClipBox around the Interior
                if (ClipBox != null)
                {
                    ShieldSnowAndRainParticles();
                    ClipBox.SetActive(true);
                    //if (HandPaintedModelsModEnabled)
                    //{
                        AssignModelsToDisable(interiorBounds);
                    //}
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            //if (DaggerfallUnity.Instance.Option_CombineRMB)
            //{
            //    var interiorModel = GameManager.Instance.InteriorParent.GetComponentsInChildren<Transform>()
            //        .Where(c => c.gameObject.name == "CombinedModels").FirstOrDefault();

            //    // Get the interior bounds to determine the size of the ClipBox
            //    //Bounds interiorBounds = interiorModel.GetComponent<Renderer>().bounds;
            //    ClipBox.transform.localScale = new Vector3(interiorBounds.size.x * ClipBoxPad, interiorBounds.size.y * ClipBoxPad, interiorBounds.size.z * ClipBoxPad);
            //    ClipBox.transform.position = new Vector3(interiorBounds.center.x, interiorBounds.center.y, interiorBounds.center.z);

            //    //TODO: Does this have to be all combined models?
            //    var combinedModels = GameManager.Instance.ExteriorParent.GetComponentsInChildren<Transform>()
            //        .Where(c => c.gameObject.name == "CombinedModels").ToList();

            //    // set the exterior combined meshes to use the clipbox shader
            //    ApplyClipBoxShaderToExterior(combinedModels);
            //}
            //else if (ExteriorBuildingToReEnable == null)
            //{
            //    // we still need the clipbox for non combined rmb preventing weather indoors
            //    // Get the interior bounds to determine the size of the ClipBox
            //    //Bounds interiorBounds = GeInteriorCombinedModelBounds();
            //    //ClipBox.transform.localScale = new Vector3(interiorBounds.size.x + 1f, interiorBounds.size.y, interiorBounds.size.z + 1f);
            //    //ClipBox.transform.position = new Vector3(interiorBounds.center.x, interiorBounds.center.y, interiorBounds.center.z);

            //    //ClipBox.gameObject.layer = 2;

            //    if (ClipBox.GetComponent<InteriorCollisionHandler>() == null)
            //    {
            //        ClipBox.AddComponent<InteriorCollisionHandler>();
            //    }

            //    // we need to disable the current exterior building
            //    DFBlock blockData = DaggerfallUnity.Instance.ContentReader.BlockFileReader.GetBlock(GameManager.Instance.PlayerEnterExit.Interior.EntryDoor.blockIndex);

            //    StaticDoor closestDoor;
            //    Vector3 closestDoorPos = DaggerfallStaticDoors.FindClosestDoor(transform.position, GameManager.Instance.PlayerEnterExit.ExteriorDoors, out closestDoor);

            //    // Get building directory for location
            //    BuildingDirectory buildingDirectory = GameManager.Instance.StreamingWorld.GetCurrentBuildingDirectory();
            //    if (buildingDirectory != null)
            //    {
            //        // Get detailed building data from directory
            //        BuildingSummary buildingSummary;
            //        if (buildingDirectory.GetBuildingSummary(closestDoor.buildingKey, out buildingSummary))
            //        {
            //            // use buildingSummary.ModelID to find nearest exterior building?
            //            var location = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;

            //            var meshes = location.GetComponentsInChildren<DaggerfallMesh>();
            //            var namedMeshes = meshes.Where(x => x.gameObject.name.StartsWith($"DaggerfallMesh [ID={buildingSummary.ModelID}]")).ToList();
            //            var parentedMeshes = namedMeshes.Where(x => x.transform.parent?.parent?.name == $"DaggerfallBlock [{blockData.Name}]").ToList();

            //            //could just get nearest exterior building with a sphere collider
            //            GameObject bestTarget = null;
            //            if (parentedMeshes?.Count > 1)
            //            {                            
            //                float closestDistanceSqr = Mathf.Infinity;

            //                foreach (var mesh in parentedMeshes)
            //                {
            //                    var potentialTarget = mesh.gameObject;

            //                    Vector3 directionToTarget = potentialTarget.transform.position - GameObject.Find("PlayerAdvanced").transform.position;

            //                    float dSqrToTarget = directionToTarget.sqrMagnitude;

            //                    if (dSqrToTarget < closestDistanceSqr)
            //                    {
            //                        closestDistanceSqr = dSqrToTarget;
            //                        bestTarget = potentialTarget;
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                bestTarget = parentedMeshes?.FirstOrDefault()?.gameObject;
            //            }

            //            if (bestTarget != null)
            //            {
            //                ExteriorBuildingToReEnable = bestTarget;
            //                bestTarget.SetActive(false);

            //                //need to disable building columns
            //                //foreach (var item in collection)
            //                //{

            //                //}
            //            }

            //            //var meshes = children.Where(x => x.gameObject.name == $"DaggerfallMesh [{buildingSummary.ModelID}]" && x.parent?.parent?.name == blockData.Name);
            //        }
            //    }
            //}
        }

        bool IsInside(GameObject obj1, GameObject obj2)
        {
            Vector3 worldPos = obj1.transform.position;
            Bounds worldBounds = obj2.GetComponent<MeshRenderer>().bounds;
            return worldBounds.Contains(worldPos);
        }

        private void ApplyClipBoxShaderToExterior(List<Transform> combinedModels)
        {
            // set the exterior combined meshes to use the clipbox shader
            foreach (var combinedModel in combinedModels)
            {
                if (combinedModel != null)
                {
                    combinedModel.GetComponent<MeshCollider>().enabled = false;

                    Material[] materials = combinedModel.GetComponent<Renderer>().materials;
                    foreach (var material in materials)
                    {
                        var shaderMaterial = new Material(ClipBoxShader);

                        shaderMaterial.CopyPropertiesFromMaterial(material);

                        material.shader = ClipBoxShader;

                        //if (IsDreamVersion)
                        //{
                        //    material.SetFloat("_Emission", 0f);
                        //    //material.SetFloat("_Glossiness", 0.0f);
                        //}
                    }
                }
            }
        }

        private void ActivateExteriorAmbience(bool activate)
        {
            var songPlayer = GameManager.Instance.ExteriorParent.transform.Find("SongPlayer");
            songPlayer.gameObject.SetActive(activate);

            var ambientEffects = GameManager.Instance.ExteriorParent.transform.Find("WeatherAmbientEffects");
            ambientEffects.gameObject.SetActive(activate);
        }

        private void EnableExteriorEnemies(bool enable)
        {
            var enemies = GameManager.Instance.ExteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name.StartsWith("DaggerfallEnemy"));

            foreach (var enemy in enemies)
            {
                if (enemy.gameObject.transform.parent?.name != "LSO Party")
                {
                    enemy.gameObject.GetComponent<CharacterController>().enabled = enable;
                    enemy.gameObject.GetComponent<EnemyAttack>().enabled = enable;
                }
            }
        }

        private void SetupInteriorColliders(Transform interiorModel)
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

        private void ReApplyExteriorShaders()
        {
            var transformsToReapply = new List<Transform>();

            if (DaggerfallUnity.Instance.Option_CombineRMB)
            {
                transformsToReapply = GameManager.Instance.ExteriorParent.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name == "CombinedModels").ToList();
            }
            else
            {
                var location = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;
                var meshes = location.GetComponentsInChildren<DaggerfallMesh>();
                foreach (var mesh in meshes)
                {
                    transformsToReapply.Add(mesh.transform);
                }
            }

            var shaderToApply = IsDreamVersion ? StandardShader : DaggerfallDefaultShader;

            foreach (var combinedModel in transformsToReapply)
            {
                combinedModel.GetComponent<MeshCollider>().enabled = true;

                List<Material> materials = combinedModel.GetComponent<Renderer>().materials.ToList();                

                foreach (var material in materials)
                {
                    material.shader = shaderToApply;
                    material.SetFloat("_Glossiness", 0f);
                }
            }
        }

        private void ShieldSnowAndRainParticles()
        {
            if (SnowParticles == null || RainParticles == null)
            {
                AssignParticleSystems();
            }

            if (SnowParticles != null)
            {
                ApplyParticleSystemTriggers(SnowParticles.GetComponent<ParticleSystem>());
                SnowParticles.transform.GetChild(0)?.gameObject?.SetActive(false);
                //ApplyParticleSystemTriggers(SnowParticles.transform.GetChild(0).GetComponent<ParticleSystem>());
            }

            if (RainParticles != null)
            {
                ApplyParticleSystemTriggers(RainParticles.GetComponent<ParticleSystem>());
                RainParticles.transform.GetChild(0)?.gameObject?.SetActive(false);
                //ApplyParticleSystemTriggers(RainParticles.transform.GetChild(0).GetComponent<ParticleSystem>());
            }
        }

        private void ApplyParticleSystemTriggers(ParticleSystem system)
        {
            if (system != null)
            {
                var trigger = system.trigger;
                var collider = trigger.GetCollider(0);
                if (collider == null)
                {
                    trigger.enabled = true;
                    trigger.inside = ParticleSystemOverlapAction.Kill;
                    trigger.enter = ParticleSystemOverlapAction.Kill;
                    trigger.SetCollider(0, ClipBox.transform);
                    trigger.SetCollider(1, ClipBox.transform.GetChild(0));
                    trigger.SetCollider(2, ClipBox.transform.GetChild(1));
                    trigger.SetCollider(3, ClipBox.transform.GetChild(2));
                    trigger.SetCollider(4, ClipBox.transform.GetChild(3));
                    trigger.SetCollider(5, ClipBox.transform.GetChild(4));
                }
            }
        }

        private void AssignParticleSystems()
        {
            GameObject playerAdvanced = GameObject.Find("PlayerAdvanced");
            if (playerAdvanced != null)
            {
                GameObject smoothFollower = playerAdvanced.transform.Find("SmoothFollower").gameObject;
                if (smoothFollower != null)
                {
                    GameObject snowParticles = smoothFollower.transform.Find("Snow_Particles").gameObject;
                    if (snowParticles != null)
                    {
                        SnowParticles = snowParticles;
                    }

                    GameObject rainParticles = smoothFollower.transform.Find("Rain_Particles").gameObject;
                    if (rainParticles != null)
                    {
                        RainParticles = rainParticles;
                    }
                }
            }
        }

        private void DarkenNightInteriors()
        {
            GameObject playerAdvanced = GameObject.Find("PlayerAdvanced");
            if (playerAdvanced != null)
            {
                var ambientLight = playerAdvanced.GetComponent<PlayerAmbientLight>();

                if (ambientLight != null)
                {
                    ambientLight.InteriorNightAmbientLight = ambientLight.ExteriorNightAmbientLight;
                }
            }
        }

        private void AssignModelsToDisable(Bounds bounds)
        {
            var currentBlock = GetCurrentBlock();

            if (currentBlock != null)
            {
                var currentBlockModels = currentBlock.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name == "Models").FirstOrDefault();

                if (currentBlockModels != null)
                {
                    var blockRenderers = currentBlockModels.gameObject.GetComponentsInChildren<Renderer>();

                    foreach (var renderer in blockRenderers)
                    {
                        if (renderer.gameObject.name.Contains("[Replacement]") || renderer.gameObject.name.Contains("[ID=41902]"))
                        {
                            if (bounds.Intersects(renderer.bounds))
                            {
                                ObjectsToReEnable.Add(renderer.gameObject);
                                renderer.gameObject.SetActive(false);
                            }
                        }
                    }
                }

                var currentBlockFlats = currentBlock.GetComponentsInChildren<Transform>()
                    .Where(c => c.gameObject.name == "Flats").FirstOrDefault();

                if (currentBlockFlats != null)
                {
                    var flatRenderers = currentBlockFlats.gameObject.GetComponentsInChildren<Renderer>();

                    foreach (var renderer in flatRenderers)
                    {
                        if (renderer.transform.parent != null)
                        {
                            if (renderer.transform.parent.name.Contains("[Replacement]"))
                            {
                                if (bounds.Intersects(renderer.bounds))
                                {
                                    ObjectsToReEnable.Add(renderer.gameObject);
                                    renderer.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }
        }

        private Transform GetCurrentBlock()
        {
            DaggerfallInterior interior = GameManager.Instance.PlayerEnterExit.Interior;
            int blockIndex = interior.EntryDoor.blockIndex;

            DFBlock blockData = DaggerfallUnity.Instance.ContentReader.BlockFileReader.GetBlock(blockIndex);
            var location = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;
            if (location != null)
            {
                return location.transform.Find($"DaggerfallBlock [{blockData.Name}]");
            }

            return null;
        }
    }
}
