using Assets.Scripts.MacadaynuMods;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Game.MacadaynuMods
{
    public class LanguageSkillsOverhaulMod : MonoBehaviour, IHasModSaveData
    {
        #region Types

        //assigns serializer version to ensures mod data continuaty / debugging.
        //sets up the save data class for the seralizer to read and save to text file.
        [FullSerializer.fsObject("v1")]
        public class LanguageSkillsOverhaulSaveData
        {
            public List<EnemyToLoad> EnemiesToLoad;
        }

        public struct EnemyToTransition
        {
            public MobileTypes Type;
            public MobileGender Gender;
            public int CurrentHealth;
            public int CurrentMagicka;
            public int MaxHealth;
            public int MaxMagicka;
            public string Name;

            public EnemyToTransition(MobileTypes type, MobileGender gender, int currentHealth, int currentMagicka, int maxHealth, int maxMagicka, string name)
            {
                Type = type;
                Gender = gender;
                CurrentHealth = currentHealth;
                CurrentMagicka = currentMagicka;
                MaxHealth = maxHealth;
                MaxMagicka = maxMagicka;
                Name = name;
            }
        }

        public struct EnemyToLoad
        {
            public ulong LoadID;
            public int QueuePosition;
            public string Name;

            public EnemyToLoad(ulong uid, int queuePosition, string name)
            {
                LoadID = uid;
                QueuePosition = queuePosition;
                Name = name;
            }
        }

        public static class Questions
        {
            public const string HowAreYou = "How Are You?";
            public const string WhatIsYourName = "What Is Your Name?";
            public const string WhereAmI = "Where Am I?";
            public const string TradeBuy = "Do You Have Anything For Sale?";
            public const string TradeSell = "Take A Look At My Wares";
            public const string KhajiitTradeSell = "Khajiit Has Wares, If You Have Coin";
            public const string KhajiitTradeBuy = "Khajiit Has Coin, If You Have Wares";
            public const string PartyJoin = "Follow Me";
            public const string PartyLeave = "Part Ways";
            public const string Goodbye = "Goodbye";
        }

        #endregion

        #region Properties

        static Mod mod;
        static ModSettings settings;
        public static LanguageSkillsOverhaulMod instance;

        public static DaggerfallEntityBehaviour lastEnemyClicked;

        Camera mainCamera;

        int playerLayerMask = 0;
        public static int ignoreMaskForShooting;
        public static int ignoreMaskForObstacles;

        public static List<GameObject> enemyFollowers = new List<GameObject>();
        public static List<EnemyToTransition> enemyTypesToTransition = new List<EnemyToTransition>();

        public static float RayDistance = 3072 * MeshReader.GlobalScale;
        public static float doorCrouchingHeight = 1.65f;
        public static float predictionInterval = 0.0625f;
        public static float stopDistance = 2.3f;
        public const float pacifyActivationDistance = 300 * MeshReader.GlobalScale;

        bool showTradeSellWindow;

        static bool EnableStreetwiseAndEtiquettePacification;
        static bool DisplaySkillRequirements;
        static bool DeadFollowerNotifications;
        static bool LevelStreetwiseAndEtiquetteOnPacifyAttempt;
        static bool PlaySoundFXOnJoinParty;

        public Type SaveDataType { get { return typeof(LanguageSkillsOverhaulSaveData); } }

        #endregion

        public void Awake()
        {
            mod.IsReady = true;
        }

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            instance = go.AddComponent<LanguageSkillsOverhaulMod>();

            //initiates save paramaters for class/script.
            mod.SaveDataInterface = instance;

            settings = mod.GetSettings();
        }

        void Start()
        {
            mainCamera = GameManager.Instance.MainCamera;
            playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));

            ignoreMaskForShooting = ~(1 << LayerMask.NameToLayer("SpellMissiles") | 1 << LayerMask.NameToLayer("Ignore Raycast"));
            ignoreMaskForObstacles = ~(1 << LayerMask.NameToLayer("SpellMissiles") | 1 << LayerMask.NameToLayer("Ignore Raycast"));

            PlayerEnterExit.OnTransitionExterior += OnTransitionToExterior;
            PlayerEnterExit.OnTransitionInterior += OnTransitionInterior;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionDungeonExterior;
            PlayerEnterExit.OnTransitionDungeonInterior += OnTransitionDungeonInterior;
            PlayerEnterExit.OnPreTransition += OnPreTransistion;
            PlayerGPS.OnEnterLocationRect += OnEnterLocation;
            DaggerfallTravelPopUp.OnPreFastTravel += DaggerfallTravelPopUp_OnPreFastTravel;
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
            DaggerfallRestWindow.OnSleepEnd += DaggerfallRestWindow_OnSleepEnd;
            GameManager.OnEnemySpawn += OnEnemySpawn;

            LanguageSkillCommands.RegisterCommands();

            EnableStreetwiseAndEtiquettePacification = settings.GetBool("GeneralSettings", "EnableStreetwiseAndEtiquettePacification");
            DisplaySkillRequirements = settings.GetBool("GeneralSettings", "DisplaySkillRequirements");
            DeadFollowerNotifications = settings.GetBool("GeneralSettings", "DeadFollowerNotifications");
            LevelStreetwiseAndEtiquetteOnPacifyAttempt = settings.GetBool("GeneralSettings", "LevelStreetwiseAndEtiquetteOnPacifyAttempt");
            PlaySoundFXOnJoinParty = settings.GetBool("GeneralSettings", "PlaySoundFXOnJoinParty");
        }

        void Update()
        {
            if (!InputManager.Instance.IsPaused)
            {
                RemoveDeadFollowers();

                if (InputManager.Instance.ActionComplete(InputManager.Actions.ActivateCenterObject))
                {
                    lastEnemyClicked = null;

                    if (!GameManager.Instance.PlayerEffectManager.HasReadySpell)
                    {
                        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

                        // Test ray against scene
                        RaycastHit hit;
                        bool hitSomething = Physics.Raycast(ray, out hit, RayDistance, playerLayerMask);
                        if (hitSomething)
                        {
                            DaggerfallEntityBehaviour mobileEnemyBehaviour = null;
                            if (MobileEnemyCheck(hit, out mobileEnemyBehaviour))
                            {
                                lastEnemyClicked = mobileEnemyBehaviour;

                                var enemyEntity = mobileEnemyBehaviour.Entity as EnemyEntity;
                                DFCareer.Skills languageSkill = enemyEntity.GetLanguageSkill();

                                if (EnableStreetwiseAndEtiquettePacification || (languageSkill != DFCareer.Skills.Streetwise && languageSkill != DFCareer.Skills.Etiquette))
                                {
                                    var enemyMotor = mobileEnemyBehaviour.transform.GetComponent<EnemyMotor>();
                                    if (enemyMotor.IsHostile)
                                    {
                                        if (languageSkill != DFCareer.Skills.None && hit.distance > pacifyActivationDistance)
                                        {
                                            DaggerfallUI.AddHUDText($"{enemyEntity.Name} can't hear what you're saying");
                                            return;
                                        }

                                        AttemptPacify(enemyEntity, enemyMotor);
                                    }
                                    else
                                    {
                                        var senses = mobileEnemyBehaviour.GetComponent<EnemySenses>();
                                        if (!senses.TargetInSight)
                                        {
                                            DisplayQuestionsBox(mobileEnemyBehaviour);
                                        }
                                        else
                                        {
                                            DaggerfallUI.Instance.PopupMessage($"{mobileEnemyBehaviour.Entity.Name} is in combat");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void DisplayQuestionsBox(DaggerfallEntityBehaviour mobileEnemyBehaviour)
        {
            var skillID = (short)(mobileEnemyBehaviour.Entity as EnemyEntity).GetLanguageSkill();
            if (skillID != (short)DFCareer.Skills.None)
            {
                DaggerfallListPickerWindow topicSelection = new DaggerfallListPickerWindow(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
                topicSelection.OnItemPicked += QuestionPicker_OnItemPicked;

                topicSelection.ListBox.AddItem(Questions.HowAreYou);
                topicSelection.ListBox.AddItem(Questions.WhereAmI);
                topicSelection.ListBox.AddItem(Questions.WhatIsYourName);
                topicSelection.ListBox.AddItem(GameManager.Instance.PlayerEntity.Race == Races.Khajiit ? Questions.KhajiitTradeBuy : Questions.TradeBuy);
                topicSelection.ListBox.AddItem(GameManager.Instance.PlayerEntity.Race == Races.Khajiit ? Questions.KhajiitTradeSell : Questions.TradeSell);

                var isAlreadyFollowing = enemyFollowers.Where(x => x == mobileEnemyBehaviour.gameObject).Any();
                topicSelection.ListBox.AddItem(isAlreadyFollowing ? Questions.PartyLeave : Questions.PartyJoin);

                var questItemQuestion = GetQuestItemQuestion();
                if (questItemQuestion != null)
                {
                    topicSelection.ListBox.AddItem(questItemQuestion);
                }

                topicSelection.ListBox.AddItem(Questions.Goodbye);

                DaggerfallUI.UIManager.PushWindow(topicSelection);
            }
        }

        void RemoveDeadFollowers()
        {
            List<GameObject> deadEnemies = new List<GameObject>();

            foreach (var follower in enemyFollowers)
            {
                if (follower == null)
                {
                    deadEnemies.Add(follower);
                }
                else if (follower.transform.GetComponent<DaggerfallEntityBehaviour>().Entity.CurrentHealth == 0)
                {
                    deadEnemies.Add(follower);

                    if (DeadFollowerNotifications)
                    { 
                        DaggerfallUI.MessageBox($"{follower.transform.GetComponent<DaggerfallEntityBehaviour>().Entity.Name} has fallen in battle");
                    }
                }
                else if (follower.transform.GetComponent<EnemyMotor>().IsHostile)
                {
                    //TODO: This is a workaround for a bug in DFU, can remove this when the fix goes in
                    int id = (follower.transform.GetComponent<DaggerfallEntityBehaviour>().Entity as EnemyEntity).MobileEnemy.ID;
                    follower.transform.GetComponent<DaggerfallEntityBehaviour>().Entity.Team = EnemyBasics.Enemies.First(x => x.ID == id).Team;

                    deadEnemies.Add(follower);
                }
            }

            enemyFollowers = enemyFollowers.Except(deadEnemies).ToList();
        }

        protected virtual void QuestionPicker_OnItemPicked(int index, string name)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.UIManager.PopWindow();
            var personality = GameManager.Instance.PlayerEntityBehaviour.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Personality);
            var entity = lastEnemyClicked.Entity as EnemyEntity;

            var skillID = (short)entity.GetLanguageSkill();
            var languageScore = GameManager.Instance.PlayerEntityBehaviour.Entity.Skills.GetLiveSkillValue(skillID);

            ComprehendLanguages languagesEffect = (ComprehendLanguages)GameManager.Instance.PlayerEffectManager.FindIncumbentEffect<ComprehendLanguages>();
            if (languagesEffect != null)
            {
                languageScore += 20;
            }

            switch (name)
            {
                case Questions.HowAreYou:

                    if (languageScore < 20)
                    {
                        ShowLevelRequiredText(20, entity);
                    }
                    else if (personality < 40)
                    {
                        ShowPersonalityFailMessage(personality, 40);
                    }
                    else
                    {
                        var lastEntity = lastEnemyClicked.Entity;

                        var tokens = new List<TextFile.Token>
                        {
                            new TextFile.Token(TextFile.Formatting.JustifyCenter, null),
                            new TextFile.Token(TextFile.Formatting.Text, $"Health: {lastEntity.CurrentHealth}/{lastEntity.MaxHealth}"),
                            new TextFile.Token(TextFile.Formatting.JustifyCenter, null)
                        };

                        if (lastEntity.MaxMagicka > 0)
                        {
                            tokens.Add(new TextFile.Token(TextFile.Formatting.NewLine));
                            tokens.Add(new TextFile.Token(TextFile.Formatting.Text, $"Magicka: {lastEntity.CurrentMagicka}/{lastEntity.MaxMagicka}"));
                            tokens.Add(new TextFile.Token(TextFile.Formatting.JustifyCenter, null));
                        }

                        DaggerfallUI.MessageBox(tokens.ToArray());
                    }
                    break;

                case Questions.WhatIsYourName:

                    if (languageScore < 20)
                    {
                        ShowLevelRequiredText(20, entity);
                    }
                    else if (personality < 40)
                    {
                        ShowPersonalityFailMessage(personality, 40);
                    }
                    else
                    {
                        var lastEntity = lastEnemyClicked.Entity;

                        var messageboxUserNote = new DaggerfallInputMessageBox(DaggerfallUI.UIManager);
                        messageboxUserNote.SetTextBoxLabel($"Choose a name: ");
                        messageboxUserNote.TextPanelDistanceX = 5;
                        messageboxUserNote.TextPanelDistanceY = 8;
                        messageboxUserNote.TextBox.Numeric = false;
                        messageboxUserNote.TextBox.MaxCharacters = 50;
                        messageboxUserNote.TextBox.WidthOverride = 306;
                        messageboxUserNote.OnGotUserInput += ChooseName_OnGotUserInput;
                        messageboxUserNote.Show();
                    }
                    break;

                case Questions.WhereAmI:

                    if (languageScore < 20)
                    {
                        ShowLevelRequiredText(20, entity);
                    }
                    else if (personality < 40)
                    {
                        ShowPersonalityFailMessage(personality, 40);
                    }
                    else
                    {
                        DaggerfallUI.MessageBox($"You are in {GameManager.Instance.PlayerGPS.CurrentLocation.Name} " +
                            $"in the {GameManager.Instance.PlayerGPS.CurrentLocation.RegionName} region");
                    }
                    break;

                case Questions.TradeBuy:
                case Questions.TradeSell:
                case Questions.KhajiitTradeBuy:
                case Questions.KhajiitTradeSell:

                    if (languageScore < 30)
                    {
                        ShowLevelRequiredText(30, entity);
                    }
                    else if (personality < 45)
                    {
                        ShowPersonalityFailMessage(personality, 45);
                    }
                    else
                    {
                        if (name == Questions.KhajiitTradeSell || name == Questions.TradeSell || lastEnemyClicked.Entity.Items.Count > 0)
                        {
                            // TODO: Maybe this should also be in buildings? Would have to revert to previous building discovery data if so
                            if (!GameManager.Instance.IsPlayerInsideBuilding)
                            {
                                GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData =
                                    new PlayerGPS.DiscoveredBuilding
                                    {
                                        buildingKey = 1,
                                        buildingType = DFLocation.BuildingTypes.GeneralStore,
                                        quality = 1
                                    };
                            }

                            var windowMode = name == Questions.KhajiitTradeBuy || name == Questions.TradeBuy
                                ? DaggerfallTradeWindow.WindowModes.Buy
                                : DaggerfallTradeWindow.WindowModes.Sell;

                            UserInterfaceManager uiManager = DaggerfallUI.UIManager as UserInterfaceManager;
                            DaggerfallTradeWindow tradeWindow = (DaggerfallTradeWindow)UIWindowFactory.GetInstanceWithArgs(
                                UIWindowType.Trade,
                                new object[] { uiManager, null, windowMode, null });

                            if (windowMode == DaggerfallTradeWindow.WindowModes.Buy)
                            {
                                // TODO: Omit gold as a buyable item, and only show trade menu if enemy has items
                                tradeWindow.MerchantItems = lastEnemyClicked.Entity.Items;
                            }

                            showTradeSellWindow = windowMode == DaggerfallTradeWindow.WindowModes.Sell;

                            uiManager.PushWindow(tradeWindow);
                        }
                        else if (name == Questions.KhajiitTradeBuy || name == Questions.TradeBuy)
                        {
                            DaggerfallUI.MessageBox($"{lastEnemyClicked.Entity.Name} has no items to sell");
                        }
                    }
                    break;

                case Questions.PartyJoin:
                case Questions.PartyLeave:

                    if (languageScore < 40)
                    {
                        ShowLevelRequiredText(40, entity);
                    }
                    else if (personality < 50)
                    {
                        ShowPersonalityFailMessage(personality, 50);
                    }
                    else
                    {
                        // Part Ways
                        var enemyFollower = enemyFollowers.Where(x => x == lastEnemyClicked.gameObject).FirstOrDefault();
                        if (enemyFollower != null)
                        {
                            enemyFollower.transform.GetComponent<EnemyFollower>().isFollowing = false;
                            Physics.IgnoreCollision(enemyFollower.transform.GetComponent<CharacterController>(), GameManager.Instance.PlayerEntityBehaviour.transform.GetComponent<CharacterController>(), false);

                            DaggerfallUI.MessageBox($"You part with {lastEnemyClicked.Entity.Name}");

                            enemyFollowers.Remove(enemyFollower);
                            return;
                        }

                        if (enemyFollowers.Count > 3)
                        {
                            DaggerfallUI.MessageBox("Your party is already full");
                            return;
                        }

                        if (enemyFollowers.Count == 3)
                        {
                            if (languageScore < 80)
                            {
                                ShowLevelRequiredText(80, entity);
                                return;
                            }
                            else if (personality < 70)
                            {
                                ShowPersonalityFailMessage(personality, 70);
                                return;
                            }
                        }

                        if (enemyFollowers.Count == 2)
                        {
                            if (languageScore < 70)
                            {
                                ShowLevelRequiredText(70, entity);
                                return;
                            }
                            else if (personality < 65)
                            {
                                ShowPersonalityFailMessage(personality, 65);
                                return;
                            }
                        }

                        if (enemyFollowers.Count == 1)
                        {
                            if (languageScore < 60)
                            {
                                ShowLevelRequiredText(60, entity);
                                return;
                            }
                            else if (personality < 60)
                            {
                                ShowPersonalityFailMessage(personality, 60);
                                return;
                            }
                        }

                        //Join Party
                        DaggerfallUI.MessageBox($"{lastEnemyClicked.Entity.Name} joins your party");

                        var enemyController = lastEnemyClicked.transform.GetComponent<CharacterController>();
                        var playerController = GameManager.Instance.PlayerEntityBehaviour.transform.GetComponent<CharacterController>();

                        Physics.IgnoreCollision(enemyController, playerController, true);

                        if (PlaySoundFXOnJoinParty)
                        {
                            DaggerfallAudioSource dfAudioSource = GameManager.Instance.PlayerActivate.GetComponent<DaggerfallAudioSource>();
                            if (dfAudioSource != null)
                            {
                                dfAudioSource.PlayOneShot(SoundClips.SelectClassDrums);
                            }
                        }

                        FollowMe();
                    }
                    break;
                case Questions.Goodbye:

                    DaggerfallUI.UIManager.PopWindow();

                    break;
                default:
                    break;
            }

            if (name == GetQuestItemQuestion())
            {
                if (languageScore < 50)
                {
                    ShowLevelRequiredText(50, entity);
                }
                else if (personality < 55)
                {
                    ShowPersonalityFailMessage(personality, 55);
                }
                else
                {
                    Item questItem;
                    bool result = GetCurrentLocationQuestItem(out questItem);
                    if (!result)
                    {
                        DaggerfallUI.MessageBox($"There is no Quest Item here");
                        return;
                    }
                    else
                    {
                        if (GameManager.Instance.PlayerEntity.Items.Contains(questItem))
                        {
                            DaggerfallUI.MessageBox($"You already have the {GetQuestItemName(questItem)}");
                        }
                        else
                        {
                            var playerLocation = GameManager.Instance.PlayerEntityBehaviour.transform.position;

                            var questItemLocation = GetQuestTargetLocation();

                            string heightDifference = "";
                            var yDiff = questItemLocation.y - playerLocation.y;
                            if (yDiff > 3)
                            {
                                heightDifference = ", above us";
                            }
                            else if (yDiff < -3)
                            {
                                heightDifference = ", below us";
                            }

                            if (questItemLocation == Vector3.zero)
                            {
                                DaggerfallUI.MessageBox($"There is no quest item here");
                            }
                            else
                            {
                                DaggerfallUI.MessageBox($"It's to the {GetCompassDirection(questItemLocation)} of here{heightDifference}");
                            }
                        }
                    }
                }
            }
        }

        private async void ChooseName_OnGotUserInput(DaggerfallInputMessageBox sender, string input)
        {
            lastEnemyClicked.transform.GetComponent<DaggerfallEntityBehaviour>().Entity.Name = input;

            // Workaround for World Tooltips mod, have to create another gameobject so the tooltip name updates
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = lastEnemyClicked.transform.position;
            sphere.transform.localScale = new Vector3(2, 2, 2);
            sphere.transform.GetComponent<Renderer>().enabled = false;

            await System.Threading.Tasks.Task.Delay(100);

            Destroy(sphere);
        }

        private void OnEnterLocation(DFLocation location)
        {
            SpawnParty();
        }

        private void OnTransitionDungeonInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            SpawnParty();
        }

        private void OnTransitionInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            SpawnParty();
        }

        private async void OnTransitionDungeonExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            // Have to wait as the Player has not been repositioned when exiting dungeons
            await System.Threading.Tasks.Task.Delay(500);

            SpawnParty();
        }

        private void OnTransitionToExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            SpawnParty();
        }

        private void DaggerfallTravelPopUp_OnPreFastTravel(DaggerfallTravelPopUp obj)
        {
            StorePartyForTransition();
        }

        public void UIManager_OnWindowChangeHandler(object sender, EventArgs e)
        {
            if (showTradeSellWindow)
            {
                if (!GameManager.Instance.IsPlayerInsideBuilding)
                {
                    GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData =
                        new PlayerGPS.DiscoveredBuilding
                        {
                            buildingKey = 0,
                            buildingType = 0,
                            quality = 0
                        };
                }

                showTradeSellWindow = false;
            }
        }

        private void OnPreTransistion(PlayerEnterExit.TransitionEventArgs args)
        {
            StorePartyForTransition();
        }

        private void DaggerfallRestWindow_OnSleepEnd()
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideTavern)
            {
                foreach (var follower in enemyFollowers)
                {
                    var followerEntity = follower.GetComponent<DaggerfallEntityBehaviour>().Entity;
                    followerEntity.SetHealth(followerEntity.MaxHealth);
                    followerEntity.SetMagicka(followerEntity.MaxMagicka);
                }
            }
        }

        private async void OnEnemySpawn(GameObject enemyObject)
        {
            await System.Threading.Tasks.Task.Delay(1000);

            QuestResourceBehaviour qrb = enemyObject.GetComponent<QuestResourceBehaviour>();
            if (qrb)
            {
                qrb.IsAttackableByAI = true;
            }                        
        }

        void StorePartyForTransition()
        {
            enemyTypesToTransition.Clear();

            foreach (var follower in enemyFollowers.Where(x => x != null))
            {
                var enemy = follower.GetComponent<DaggerfallEntityBehaviour>().Entity as EnemyEntity;

                enemyTypesToTransition.Add(
                    new EnemyToTransition(
                        (MobileTypes)enemy.MobileEnemy.ID,
                        enemy.MobileEnemy.Gender,
                        enemy.CurrentHealth,
                        enemy.CurrentMagicka,
                        enemy.MaxHealth,
                        enemy.MaxMagicka,
                        enemy.Name));
            }

            foreach (var follower in enemyFollowers.Where(x => x != null))
            {
                Destroy(follower);
            }

            enemyFollowers.Clear();
        }

        async void SpawnParty()
        {
            await System.Threading.Tasks.Task.Delay(1000);

            foreach (var enemyType in enemyTypesToTransition)
            {
                // TODO: Must be able to find a surface below
                var createdFoe = GameObjectHelper.CreateEnemy(
                    "Enemy",
                    enemyType.Type,
                    GameManager.Instance.PlayerEnterExit.transform.position - GameManager.Instance.PlayerEnterExit.transform.forward, //+ (GameManager.Instance.PlayerEnterExit.transform.forward * count),
                    enemyType.Gender,
                    null,
                    mobileReaction: MobileReactions.Passive);

                createdFoe.transform.SetParent(GameObjectHelper.GetBestParent());

                DaggerfallEnemy enemy = createdFoe.GetComponent<DaggerfallEnemy>();
                if (enemy)
                {
                    enemy.LoadID = DaggerfallUnity.NextUID;
                }

                createdFoe.GetComponent<EnemyMotor>().enabled = false;

                var foeEntity = createdFoe.GetComponent<DaggerfallEntityBehaviour>().Entity;
                foeEntity.Team = MobileTeams.PlayerAlly;
                foeEntity.MaxHealth = enemyType.MaxHealth;
                foeEntity.MaxMagicka = enemyType.MaxMagicka;
                foeEntity.SetHealth(enemyType.CurrentHealth);
                foeEntity.SetMagicka(enemyType.CurrentMagicka);
                foeEntity.Name = enemyType.Name;

                createdFoe.gameObject.SetActive(true);

                enemyFollowers.Add(createdFoe);

                Physics.IgnoreCollision(createdFoe.transform.GetComponent<CharacterController>(), GameManager.Instance.PlayerEntityBehaviour.transform.GetComponent<CharacterController>(), true);
            }

            enemyTypesToTransition.Clear();

            foreach (var follower in enemyFollowers)
            {
                follower.GetComponent<EnemyMotor>().enabled = true;

                follower.transform.gameObject.AddComponent<EnemyFollower>();
                follower.transform.GetComponent<EnemyFollower>().SetupEnemy(follower.GetComponent<DaggerfallEntityBehaviour>());
                follower.transform.GetComponent<EnemyFollower>().isFollowing = true; // is this needed?

                follower.gameObject.SetActive(true);
            }
        }

        public bool GetCurrentLocationQuestItem(out Item questItem)
        {
            questItem = new Item(new Quest());

            // Get PlayerEnterExit for world context
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            if (!playerEnterExit)
                return false;

            // Get SiteLinks for player's current location
            SiteLink[] siteLinks = null;
            if (playerEnterExit.IsPlayerInsideBuilding)
            {
                StaticDoor[] exteriorDoors = playerEnterExit.ExteriorDoors;
                if (exteriorDoors == null || exteriorDoors.Length < 1)
                    return false;

                siteLinks = QuestMachine.Instance.GetSiteLinks(SiteTypes.Building, GameManager.Instance.PlayerGPS.CurrentMapID, exteriorDoors[0].buildingKey);
                if (siteLinks == null || siteLinks.Length == 0)
                    return false;
            }
            else if (playerEnterExit.IsPlayerInsideDungeon)
            {
                siteLinks = QuestMachine.Instance.GetSiteLinks(SiteTypes.Dungeon, GameManager.Instance.PlayerGPS.CurrentMapID);
            }
            else
            {
                return false;
            }

            // Exit if no links found
            if (siteLinks == null || siteLinks.Length == 0)
                return false;

            // Walk through all found SiteLinks
            foreach (SiteLink link in siteLinks)
            {
                // Get the Quest object referenced by this link
                Quest quest = QuestMachine.Instance.GetQuest(link.questUID);
                if (quest == null)
                    return false;

                var resources = quest.GetAllResources();
                if (resources == null || resources.Length == 0)
                    return false;

                foreach (QuestResource resource in resources)
                {
                    if (resource is Item)
                    {
                        questItem = (Item)resource;
                        return true;
                    }
                }
            }

            return false;
        }

        private Vector3 GetQuestTargetLocation()
        {
            QuestMarker targetMarker;
            Vector3 buildingOrigin;
            bool result = QuestMachine.Instance.GetCurrentLocationQuestMarker(out targetMarker, out buildingOrigin);
            if (!result)
            {
                Debug.Log("Problem getting quest marker.");
                return Vector3.zero;
            }
            Vector3 dungeonBlockPosition = new Vector3(targetMarker.dungeonX * RDBLayout.RDBSide, 0, targetMarker.dungeonZ * RDBLayout.RDBSide);
            return dungeonBlockPosition + targetMarker.flatPosition + buildingOrigin;
        }

        string GetQuestItemName(Item questItem)
        {
            return questItem.DaggerfallUnityItem.LongName.Contains("Letter:") ? "Letter" : questItem.DaggerfallUnityItem.LongName;
        }

        string GetCompassDirection(Vector3 questItemPosition)
        {
            var playerPosition = new Vector3(GameManager.Instance.PlayerEntityBehaviour.transform.position.x, 0, GameManager.Instance.PlayerEntityBehaviour.transform.position.z);
            var newQuestItem = new Vector3(questItemPosition.x, 0, questItemPosition.z);

            Vector3 dir = (newQuestItem - playerPosition).normalized;

            if (Vector3.Angle(dir, Vector3.forward) <= 45.0)
            {
                return "North";
            }
            else if (Vector3.Angle(dir, Vector3.right) <= 45.0)
            {
                return "East";
            }
            else if (Vector3.Angle(dir, Vector3.back) <= 45.0)
            {
                return "South";
            }
            else
            {
                return "West";
            }
        }

        void ShowPersonalityFailMessage(int personality, int personalityRequired)
        {
            var statRequirement = DisplaySkillRequirements ? $" (PER: {personality}/{personalityRequired})" : "";
            DaggerfallUI.MessageBox($"{lastEnemyClicked.Entity.Name} doesn't like you enough.{statRequirement}");
        }

        private void ShowLevelRequiredText(int levelRequired, EnemyEntity entity)
        {
            var entityLanguage = entity.GetLanguageSkill();
            var currentPlayerLanguageSkill = GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(entityLanguage);

            string text;
            if (entityLanguage == DFCareer.Skills.Streetwise)
            {
                text = "sneers at";
            }
            else if (entityLanguage == DFCareer.Skills.Etiquette)
            {
                text = "scoffs at";
            }
            else
            {
                text = "doesn't understand";
            }

            var statRequirement = DisplaySkillRequirements ? $" ({Enum.GetName(typeof(DFCareer.Skills), entityLanguage)}: {currentPlayerLanguageSkill}/{levelRequired})" : "";
            DaggerfallUI.MessageBox($"{entity.Name} {text} you{statRequirement}");
        }

        void AttemptPacify(EnemyEntity enemyEntity, EnemyMotor enemyMotor)
        {
            if (!GameManager.Instance.WeaponManager.Sheathed)
            {
                return;
            }

            DFCareer.Skills languageSkill = enemyEntity.GetLanguageSkill();
            if (languageSkill != DFCareer.Skills.None)
            {
                PlayerEntity player = GameManager.Instance.PlayerEntity;
                if (player.Skills.GetLiveSkillValue(languageSkill) < 20)
                {
                    DaggerfallUI.AddHUDText($"{enemyEntity.Name} can't understand you");
                    return;
                }

                if (CalculateEnemyPacification(player, languageSkill))
                {
                    DaggerfallUI.AddHUDText(enemyEntity.Name + " pacified using " + languageSkill.ToString() + " skill.");
                    enemyMotor.IsHostile = false;

                    foreach (var follower in enemyFollowers)
                    {
                        (follower.GetComponent<DaggerfallEntityBehaviour>().Entity as EnemyEntity).SuppressInfighting = true;
                        follower.GetComponent<EnemySenses>().Target = null;
                        follower.GetComponent<EnemySenses>().SecondaryTarget = null;
                    }

                    enemyEntity.Team = MobileTeams.PlayerAlly;
                    enemyMotor.GetComponent<EnemySenses>().Target = null;
                    enemyMotor.GetComponent<EnemySenses>().SecondaryTarget = null;

                    foreach (var follower in enemyFollowers)
                    {
                        (follower.GetComponent<DaggerfallEntityBehaviour>().Entity as EnemyEntity).SuppressInfighting = false;
                    }

                    if (LevelStreetwiseAndEtiquetteOnPacifyAttempt || (languageSkill != DFCareer.Skills.Etiquette && languageSkill != DFCareer.Skills.Streetwise))
                    {
                        player.TallySkill(languageSkill, 3);
                    }
                    return;
                }

                if (LevelStreetwiseAndEtiquetteOnPacifyAttempt || (languageSkill != DFCareer.Skills.Etiquette && languageSkill != DFCareer.Skills.Streetwise))
                {
                    player.TallySkill(languageSkill, 1);
                }

                DaggerfallUI.AddHUDText($"{enemyEntity.Name} isn't impressed by your {languageSkill}");
            }
        }

        public static bool CalculateEnemyPacification(PlayerEntity player, DFCareer.Skills languageSkill)
        {
            double chance = 0;
            if (languageSkill == DFCareer.Skills.Etiquette ||
                languageSkill == DFCareer.Skills.Streetwise)
            {
                chance += player.Skills.GetLiveSkillValue(languageSkill) / 10;
                chance += player.Stats.LivePersonality / 5;
            }
            else
            {
                chance += player.Skills.GetLiveSkillValue(languageSkill);
                chance += player.Stats.LivePersonality / 10;
            }
            chance += 10;

            // Add chance from Comprehend Languages effect if present
            ComprehendLanguages languagesEffect = (ComprehendLanguages)GameManager.Instance.PlayerEffectManager.FindIncumbentEffect<ComprehendLanguages>();
            if (languagesEffect != null)
                chance += languagesEffect.ChanceValue();

            int roll = UnityEngine.Random.Range(0, 200);
            Debug.Log($"LANGUAGE ROLL: {roll}");
            bool success = (roll < chance);

            Debug.LogFormat("Pacification {3} using {0} skill: chance= {1}  roll= {2}", languageSkill, chance, roll, success ? "success" : "failure");
            return success;
        }

        void FollowMe()
        {
            enemyFollowers.Add(lastEnemyClicked.gameObject);

            foreach (var follower in enemyFollowers)
            {
                if (follower != null)
                {
                    follower.transform.GetComponent<EnemySenses>().Target = null;
                }
            }

            if (lastEnemyClicked.transform.GetComponent<EnemyFollower>() == null)
            {
                lastEnemyClicked.transform.gameObject.AddComponent<EnemyFollower>();

                var enemyFollower = lastEnemyClicked.transform.gameObject.GetComponent<EnemyFollower>();
                enemyFollower.SetupEnemy(lastEnemyClicked);
            }
            else
            {
                lastEnemyClicked.transform.GetComponent<EnemyFollower>().isFollowing = true;
            }
        }

        bool MobileEnemyCheck(RaycastHit hitInfo, out DaggerfallEntityBehaviour mobileEnemy)
        {
            mobileEnemy = hitInfo.transform.GetComponent<DaggerfallEntityBehaviour>();

            return mobileEnemy != null && (mobileEnemy.EntityType == EntityTypes.EnemyMonster || mobileEnemy.EntityType == EntityTypes.EnemyClass);
        }

        static ItemCollection GetItemsForSale()
        {
            ItemCollection items = new ItemCollection();
            items.AddItem(ItemBuilder.CreateRandomIngredient(ItemGroups.CreatureIngredients1));
            items.AddItem(ItemBuilder.CreateRandomIngredient(ItemGroups.PlantIngredients1));
            return items;
        }

        string GetQuestItemQuestion()
        {
            Item questItem;
            var result = GetCurrentLocationQuestItem(out questItem);
            if (result)
            {
                return $"Where is the {GetQuestItemName(questItem)}?";
            }

            return null;
        }

        public void TeleportParty()
        {
            foreach (var follower in enemyFollowers)
            {
                var enemyFollower = follower.GetComponent<EnemyFollower>();

                enemyFollower.TeleportBehindPlayer(enemyFollower.GetTargetTransform());
            }
        }

        public object NewSaveData()
        {
            return new LanguageSkillsOverhaulSaveData
            {
                EnemiesToLoad = new List<EnemyToLoad>()
            };
        }

        public object GetSaveData()
        {
            var enemiesToLoad = new List<EnemyToLoad>();

            foreach (var item in enemyFollowers.Select((value, i) => new { i, value }))
            {
                var value = item.value;
                var index = item.i;

                enemiesToLoad.Add(new EnemyToLoad(value.GetComponent<DaggerfallEnemy>().LoadID, index, value.GetComponent<DaggerfallEntityBehaviour>().Entity.Name));
            }

            return new LanguageSkillsOverhaulSaveData
            {
                EnemiesToLoad = enemiesToLoad
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var languageSkillsOverhaulSaveData = (LanguageSkillsOverhaulSaveData)saveData;

            if (languageSkillsOverhaulSaveData.EnemiesToLoad != null)
            {
                enemyFollowers.Clear();

                DaggerfallEnemy[] enemies = FindObjectsOfType<DaggerfallEnemy>();

                foreach (var enemy in languageSkillsOverhaulSaveData.EnemiesToLoad.OrderBy(x => x.QueuePosition))
                {
                    var loadedEnemy = enemies.FirstOrDefault(x => x.LoadID == enemy.LoadID);

                    if (loadedEnemy != null)
                    {
                        loadedEnemy.GetComponent<DaggerfallEntityBehaviour>().Entity.Name = enemy.Name;
                        enemyFollowers.Add(loadedEnemy.gameObject);
                        loadedEnemy.transform.gameObject.AddComponent<EnemyFollower>();
                        loadedEnemy.transform.GetComponent<EnemyFollower>().SetupEnemy(loadedEnemy.GetComponent<DaggerfallEntityBehaviour>());

                        Physics.IgnoreCollision(loadedEnemy.transform.GetComponent<CharacterController>(), GameManager.Instance.PlayerEntityBehaviour.transform.GetComponent<CharacterController>(), true);
                    }
                }
            }
        }
    }
}
