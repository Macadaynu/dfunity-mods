using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.MacadaynuMods.HotkeyBar;
using Assets.Scripts.Game.MacadaynuMods.HotkeyBar;
using Assets.Scripts.MyMods;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using Newtonsoft.Json;
using UnityEngine;
using Wenzil.Console;
using static DaggerfallWorkshop.Game.InputManager;
using static DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallInventoryWindow;

public class HotkeysMod : MonoBehaviour, IHasModSaveData
{
    #region Types

    //assigns serializer version to ensures mod data continuaty / debugging.
    //sets up the save data class for the seralizer to read and save to text file.
    [FullSerializer.fsObject("v1")]
    public class HotkeysSaveData
    {
        public List<Dictionary<KeyCode, Hotkey>> HotKeyBanksSerialized;
        public HotkeySettings HotKeySettings;
    }

    #endregion

    #region Fields

    GameObject console;
    ConsoleController consoleController;
    public static Mod mod;
    public static HotkeysMod instance;

    public DaggerfallUnityItem hoveredItem;
    public int? selectionId;

    public Dictionary<KeyCode, Hotkey> hotkeys;

    public Type SaveDataType { get { return typeof(HotkeysSaveData); } }

    public event EventHandler OnHotkeyPressed;
    public event EventHandler OnCloseSettings;

    public EntityEffectBundle spellToRearm;

    public int MaxHotkeyBarSize = 9;
    public int MaxHotkeyBanks = 1;
    public bool InvertScrollOrder;

    public List<Dictionary<KeyCode, Hotkey>> hotkeyBanks;
    public int CurrentHotkeyBank = 1;

    public HotkeysPanel panel;
    public GameObject panelController;

    bool checkingForPause;

    #endregion

    #region Intialisation

    //starts mod manager on game begin. Grabs mod initializing paramaters.
    //ensures SateTypes is set to .Start for proper save data restore values.
    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        //sets up instance of class/script/mod.
        GameObject go = new GameObject("HotkeysMod");
        instance = go.AddComponent<HotkeysMod>();
        //initiates mod paramaters for class/script.
        mod = initParams.Mod;
        //initiates save paramaters for class/script.
        mod.SaveDataInterface = instance;
        // initiates mod message handler
        mod.MessageReceiver = DFModMessageReceiver;
        //after finishing, set the mod's IsReady flag to true.
        mod.IsReady = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        // get the console controller so we can check if the user has the console open
        console = GameObject.Find("Console");
        consoleController = console.GetComponent<ConsoleController>();

        // assign empty hotkey banks if none exist
        if (hotkeyBanks == null)
        {
            hotkeyBanks = GetBlankHotkeyBanks();
        }

        hotkeys = hotkeyBanks.ElementAt(CurrentHotkeyBank - 1);

        UIWindowFactory.RegisterCustomUIWindow(UIWindowType.SpellBook, typeof(HotkeysSpellBookWindow));

        var daggerfallUI = GameObject.Find("DaggerfallUI").GetComponent<DaggerfallUI>();

        gameObject.AddComponent<HotkeySettingsGUI>();

        panel = new HotkeysPanel
        {
            Size = daggerfallUI.DaggerfallHUD.NativePanel.Size,
            HorizontalAlignment = HorizontalAlignment.Center,
            Enabled = true
        };

        daggerfallUI.DaggerfallHUD.NativePanel.Components.Add(panel);

        DaggerfallUI.Instance.OnInstantiatePersistentWindowInstances += OnInstantiatePersistentWindowInstances_ItemHoverHandler;
        DaggerfallUI.Instance.OnInstantiatePersistentWindowInstances += OnInstantiatePersistentWindowInstances_OnInventoryClose;

        GameManager.Instance.PlayerEffectManager.OnCastReadySpell += OnCastReadySpell;

        StartGameBehaviour.OnStartGame += StartGameBehaviour_OnStartGame;

        if (!Directory.Exists(mod.PersistentDataDirectory))
        {
            Directory.CreateDirectory(mod.PersistentDataDirectory);
        }

    }

    #endregion

    #region Detect Hotkey Input

    void Update()
    {
        if (checkingForPause && GameManager.IsGamePaused && DaggerfallUI.UIManager.WindowCount > 0)//DaggerfallUI.UIManager.TopWindow.GetType() == (typeof(DaggerfallPauseOptionsWindow)))
        {
            DaggerfallUI.UIManager.PopWindow();
            checkingForPause = false;
            Instance.AddAction(Actions.ActivateCursor);
        }

        if (!GameManager.Instance.PlayerSpellCasting.IsPlayingAnim)
        {
            if (consoleController.ui.isConsoleOpen
                || SaveLoadManager.Instance.LoadInProgress)
                return;

            if (!GameManager.IsGamePaused && DaggerfallUI.UIManager.WindowCount == 0)
            {
                if (Input.GetKeyDown(KeyCode.Escape) && HotkeySettingsGUI.hotkeySettingsEnabled)
                {
                    checkingForPause = true;
                    HotkeySettingsGUI.hotkeySettingsEnabled = false;
                }

                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    Instance.AddAction(Actions.ActivateCursor);

                    HotkeySettingsGUI.hotkeySettingsEnabled = !HotkeySettingsGUI.hotkeySettingsEnabled;
                }

                if (MaxHotkeyBanks > 1)
                {
                    var mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");

                    if (Input.GetKeyDown(KeyCode.Minus) || mouseScrollWheel < 0f)
                    {
                        if (InvertScrollOrder)
                        {
                            IncrementHotkeyBank();
                        }
                        else
                        {
                            DecrementHotKeyBank();
                        }

                        hotkeys = hotkeyBanks.ElementAt(CurrentHotkeyBank - 1);

                        if (OnHotkeyPressed != null)
                        {
                            OnHotkeyPressed(this, new HotkeyEventArgs(KeyCode.Minus));
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.Equals) || mouseScrollWheel > 0f)
                    {
                        if (InvertScrollOrder)
                        {
                            DecrementHotKeyBank();
                        }
                        else
                        {
                            IncrementHotkeyBank();
                        }

                        hotkeys = hotkeyBanks.ElementAt(CurrentHotkeyBank - 1);

                        if (OnHotkeyPressed != null)
                        {
                            OnHotkeyPressed(this, new HotkeyEventArgs(KeyCode.Equals));
                        }
                    }
                }
            }

            for (int i = 0; i < 9; i++)
            {
                if (MaxHotkeyBarSize > i)
                {
                    if (Input.GetKeyDown((KeyCode)(49 + i)))
                    {
                        HandleInput((KeyCode)(49 + i), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                    }
                }
            }

            if (spellToRearm != null)
            {
                EntityEffectManager playerEffectManager = GameManager.Instance.PlayerEffectManager;
                if (playerEffectManager && !playerEffectManager.HasReadySpell)
                {
                    playerEffectManager.SetReadySpell(spellToRearm);
                    spellToRearm = null;
                }
            }
        }
    }

    #endregion

    #region Handle Hotkey Input

    private void HandleInput(KeyCode keyCode, bool leftHandEquip)
    {
        HotkeyType? hotkeyType = null;
        if (DaggerfallUI.UIManager.TopWindow.GetType() == typeof(DaggerfallInputMessageBox))
        {
            return;
        }

        var isEquipping = HotkeySettingsGUI.useEquipDelayTimes && (GameManager.Instance.WeaponManager.EquipCountdownRightHand > 0f || GameManager.Instance.WeaponManager.EquipCountdownLeftHand > 0f);

        // activate hotkeys in game
        if (!GameManager.IsGamePaused
            && DaggerfallUI.UIManager.WindowCount == 0
            && !isEquipping)
        {
            HotkeyActivator.ActivateHotKey(keyCode);
        }
        // map hotkeys in menus
        else if (selectionId.HasValue)
        {
            string warningMessage = null;
            var uiType = DaggerfallUI.UIManager.TopWindow.GetType();
            if (uiType == typeof(HotkeysSpellBookWindow))
            {
                hotkeyType = HotkeyType.Spell;
            }
            else if (uiType == typeof(DaggerfallInventoryWindow) || (uiType?.IsSubclassOf(typeof(DaggerfallInventoryWindow)) ?? false))
            {
                if (leftHandEquip)
                {
                    var hoveredItemHands = ItemEquipTable.GetItemHands(hoveredItem);

                    if (hotkeys[keyCode] != null && (hoveredItemHands == ItemHands.LeftOnly || hoveredItemHands == ItemHands.Either))
                    {
                        var rightItem = GameManager.Instance.PlayerEntity.Items.GetItem((ulong)hotkeys[keyCode].Id);

                        if (rightItem != null && rightItem.UID == hoveredItem.UID)
                        {
                            warningMessage = $"{hoveredItem.LongName} has already been mapped to the Right Hand of this Hotkey";
                        }
                        else if (rightItem != null && ItemEquipTable.GetItemHands(rightItem) == ItemHands.Both)
                        {
                            warningMessage = $"{hoveredItem.LongName} cannot be mapped as {rightItem.LongName} requires both hands for this Hotkey";
                        }
                        else if (hoveredItemHands == ItemHands.RightOnly)
                        {
                            warningMessage = $"{hoveredItem.LongName} is right handed only";
                        }
                        else if (hoveredItem.ItemGroup == ItemGroups.Armor && (hoveredItemHands == ItemHands.LeftOnly || hoveredItemHands == ItemHands.Either))
                        {
                            hotkeyType = HotkeyType.Armor;
                        }
                        else if (hoveredItem.ItemGroup == ItemGroups.Weapons && hoveredItem.GroupIndex != 18 && (hoveredItemHands == ItemHands.LeftOnly || hoveredItemHands == ItemHands.Either))
                        {
                            hotkeyType = HotkeyType.Weapon;
                        }
                    }
                }
                else if (hoveredItem.UID == hotkeys[keyCode]?.LeftHandItemId)
                {
                    warningMessage = $"{hoveredItem.LongName} has already been mapped to the Left Hand of this Hotkey";
                }
                else if (hoveredItem.ItemGroup == ItemGroups.Weapons)
                {
                    if (hoveredItem.GroupIndex != 18)
                    {
                        hotkeyType = HotkeyType.Weapon;
                    }
                }
                else if (hoveredItem.ItemGroup == ItemGroups.Armor && ItemEquipTable.GetItemHands(hoveredItem) == ItemHands.LeftOnly)
                {
                    hotkeyType = null;
                }
                else if (hoveredItem.ItemGroup == ItemGroups.Books && !hoveredItem.IsArtifact)
                {
                    hotkeyType = HotkeyType.Book;
                }
                else if (hoveredItem.TemplateIndex == (int)MiscItems.Spellbook)
                {
                    hotkeyType = HotkeyType.Spellbook;
                }
                else if (hoveredItem.ItemGroup == ItemGroups.Drugs)
                {
                    hotkeyType = HotkeyType.Drug;
                }
                else if (hoveredItem.IsPotion)
                {
                    hotkeyType = HotkeyType.Potion;
                }
                else if (hoveredItem.ItemGroup == ItemGroups.Transportation)
                {
                    // TODO: change these to template indexes (add ship?)
                    if (hoveredItem.ItemName == "Horse")
                    {
                        hotkeyType = HotkeyType.Horse;
                    }
                    if (hoveredItem.ItemName == "Small Cart")
                    {
                        hotkeyType = HotkeyType.Cart;
                    }
                }
                else if (hoveredItem.IsLightSource)
                {
                    hotkeyType = HotkeyType.LightSource;
                }
                else if (hoveredItem.IsEnchanted)
                {
                    hotkeyType = HotkeyType.EnchantedItem;
                }
                else if (hoveredItem.ItemGroup == ItemGroups.UselessItems2 && hoveredItem.GroupIndex == 530) // check for calories and climate tents
                {
                    hotkeyType = HotkeyType.CampingEquipment;
                }
                else if (hoveredItem.ItemGroup == ItemGroups.UselessItems2 && hoveredItem.GroupIndex == 537) // check for calories and climate meat
                {
                    hotkeyType = HotkeyType.Meat;
                }
                // generic item should be last check, the remaining items will attempt to be used as normal items
                else
                {
                    hotkeyType = HotkeyType.GenericItem;
                }
            }

            if (hotkeyType.HasValue)
            {
                AssignHotkey(keyCode, selectionId.Value, hotkeyType.Value, leftHandEquip, hoveredItem?.TemplateIndex);
            }
            else
            {
                if (leftHandEquip && hotkeys[keyCode] == null)
                {
                    warningMessage = $"You must first map a Right Handed weapon to this Hotkey";
                }
                else if (string.IsNullOrWhiteSpace(warningMessage))
                {
                    warningMessage = $"You cannot map the {hoveredItem.LongName + (leftHandEquip ? " to your Left Hand" : "")}";
                }

                var tokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    warningMessage);

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(
                    DaggerfallUI.UIManager,
                    DaggerfallMessageBox.CommonMessageBoxButtons.Nothing,
                    tokens.ToArray(),
                    DaggerfallUI.Instance.InventoryWindow);

                messageBox.ClickAnywhereToClose = true;
                messageBox.AllowCancel = false;
                messageBox.ParentPanel.BackgroundColor = Color.clear;
                DaggerfallUI.UIManager.PushWindow(messageBox);
            }
        }

        // Raise event
        if (OnHotkeyPressed != null && !isEquipping)
        {
            OnHotkeyPressed(this, new HotkeyEventArgs(keyCode));
        }
    }

    #endregion

    #region Assign Hotkey Input

    private void AssignHotkey(KeyCode keyCode, int selectionId, HotkeyType hotkeyType, bool leftHandEquip = false, int? templateId = null)
    {
        var spellSettings = new EffectBundleSettings();

        // Show the "hotkey assigned" dialog
        if (hotkeyType == HotkeyType.Spell)
        {
            if (!GameManager.Instance.PlayerEntity.GetSpell(selectionId, out spellSettings))
            {
                Debug.Log($"Unable to find spell with index: {selectionId}");
                return;
            }

            var tokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                $"{spellSettings.Name} has been assigned to Hotkey: {keyCode.ToString().Last()}");

            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(
                DaggerfallUI.UIManager,
                DaggerfallMessageBox.CommonMessageBoxButtons.Nothing,
                tokens,
                DaggerfallUI.UIManager.TopWindow);

            messageBox.ClickAnywhereToClose = true;
            messageBox.AllowCancel = false;
            messageBox.ParentPanel.BackgroundColor = Color.clear;
            DaggerfallUI.UIManager.PushWindow(messageBox);
        }
        else
        {
            // check what's currently mapped for both hands
            DaggerfallUnityItem rightItem = null;
            DaggerfallUnityItem leftItem = null;
            if (hotkeys[keyCode] != null)
            {
                rightItem = leftHandEquip ? GameManager.Instance.PlayerEntity.Items.GetItem((ulong)hotkeys[keyCode].Id) : hoveredItem;

                if (hotkeys[keyCode].LeftHandItemId.HasValue)
                {
                    // if equipped Right Hand item is 2-Handed, unmap the Left Hand
                    if (rightItem != null && ItemEquipTable.GetItemHands(rightItem) == ItemHands.Both)
                    {
                        leftItem = null;
                        hotkeys[keyCode].LeftHandItemId = null;
                    }
                    else
                    {
                        // get the currently mapped left hand info
                        leftItem = GameManager.Instance.PlayerEntity.Items.GetItem(hotkeys[keyCode].LeftHandItemId.Value);

                        // if this Left Hand item is already equipped, toggle it off
                        if (leftHandEquip && leftItem.UID == hoveredItem.UID)
                        {
                            leftItem = null;
                            hotkeys[keyCode].LeftHandItemId = null;
                        }
                    }
                }
            }

            List<string> hotkeyMappingInfo;
            if ((hotkeys[keyCode] == null && ItemEquipTable.GetItemHands(hoveredItem) == ItemHands.Both)
                || (rightItem != null && ItemEquipTable.GetItemHands(rightItem) == ItemHands.Both))
            {
                hotkeyMappingInfo = new List<string>
                {
                    $"Hotkey {keyCode.ToString().Last()} has the following mapping:",
                    string.Empty,
                    $"Both Hands: {(leftHandEquip ? $"{hoveredItem.LongName}" : $"{hoveredItem.LongName}")}"
                };
            }
            else if (hotkeyType == HotkeyType.Armor || hotkeyType == HotkeyType.Weapon)
            {
                hotkeyMappingInfo = new List<string>
                {
                    $"Hotkey {keyCode.ToString().Last()} has the following mapping:",
                    string.Empty,
                    $"Right Hand: {(leftHandEquip ? $"{rightItem?.LongName ?? "Empty"}" : $"{hoveredItem.LongName}")}",
                    $"Left Hand: {(leftHandEquip ? $"{hoveredItem.LongName}" : $"{leftItem?.LongName ?? "Empty"}")}"
                };
            }
            else
            {
                hotkeyMappingInfo = new List<string>
                {
                    $"{hoveredItem.LongName} has been assigned to Hotkey {keyCode.ToString().Last()}"
                };
            }

            var tokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter, hotkeyMappingInfo.ToArray());

            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(
                DaggerfallUI.UIManager,
                DaggerfallMessageBox.CommonMessageBoxButtons.Nothing,
                tokens.ToArray(),
                DaggerfallUI.UIManager.TopWindow);

            messageBox.ClickAnywhereToClose = true;
            messageBox.AllowCancel = false;
            messageBox.ParentPanel.BackgroundColor = Color.clear;
            DaggerfallUI.UIManager.PushWindow(messageBox);
        }

        if (leftHandEquip)
        {
            hotkeys[keyCode].LeftHandItemId = (ulong?)selectionId;
        }
        else
        {
            // remove item from already existing item hotkey assignments, that don't have LH mapped
            var assignedHotkey = hotkeys.FirstOrDefault(x => x.Value?.Id == selectionId && x.Value?.Type == hotkeyType && (!x.Value?.LeftHandItemId.HasValue ?? true));
            if (assignedHotkey.Value != null)
            {
                hotkeys[assignedHotkey.Key] = null;
            }

            // assign the new hotkey
            hotkeys[keyCode] = new Hotkey
            {
                Id = selectionId,
                Spell = spellSettings,
                Type = hotkeyType,
                TemplateId = templateId,
                LeftHandItemId = hotkeys[keyCode]?.LeftHandItemId
            };
        }
    }

    #endregion

    #region Hotkey Helper Methods

    public void ResizeHotkeyBar()
    {
        // add empty banks if there are more banks than last save
        if (hotkeyBanks.Count < MaxHotkeyBanks)
        {
            var hotkeyDifference = MaxHotkeyBanks - hotkeyBanks.Count;
            for (int i = 0; i < hotkeyDifference; i++)
            {
                hotkeyBanks.Add(GetBlankHotkeys());
            }
        }

        for (int i = 0; i < hotkeyBanks.Count; i++)
        {
            hotkeyBanks[i] = hotkeyBanks[i].Take(MaxHotkeyBarSize).ToDictionary(x => x.Key, x => x.Value);

            // need to add empty hotkeys if MaxHotkeyBarSize is bigger than last save
            if (MaxHotkeyBarSize > hotkeyBanks[i].Count)
            {
                hotkeyBanks[i] = GetBlankHotkeys().Concat(hotkeyBanks[i])
                  .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
                  .ToDictionary(g => g.Key, g => g.Last());
            }
        }

        CurrentHotkeyBank = 1;
        hotkeys = hotkeyBanks.First();
    }    

    List<Dictionary<KeyCode, Hotkey>> GetBlankHotkeyBanks()
    {
        var banks = new List<Dictionary<KeyCode, Hotkey>>();

        for (int i = 0; i < MaxHotkeyBanks; i++)
        {
            banks.Add(GetBlankHotkeys());
        }

        return banks;
    }

    Dictionary<KeyCode, Hotkey> GetBlankHotkeys()
    {
        var hotkeys = new Dictionary<KeyCode, Hotkey>
            {
                {KeyCode.Alpha1, null},
                {KeyCode.Alpha2, null},
                {KeyCode.Alpha3, null},
                {KeyCode.Alpha4, null},
                {KeyCode.Alpha5, null},
                {KeyCode.Alpha6, null},
                {KeyCode.Alpha7, null},
                {KeyCode.Alpha8, null},
                {KeyCode.Alpha9, null}
            };

        return hotkeys.Take(MaxHotkeyBarSize).ToDictionary(x => x.Key, x => x.Value);
    }

    void IncrementHotkeyBank()
    {
        if (CurrentHotkeyBank == MaxHotkeyBanks)
        {
            CurrentHotkeyBank = 1;
        }
        else
        {
            CurrentHotkeyBank += 1;
        }
    }

    void DecrementHotKeyBank()
    {
        if (CurrentHotkeyBank == 1)
        {
            CurrentHotkeyBank = MaxHotkeyBanks;
        }
        else
        {
            CurrentHotkeyBank -= 1;
        }
    }

    #endregion

    #region Event Arguments

    public class HotkeyEventArgs : EventArgs
    {
        public KeyCode HotKeyCode { get; set; }

        public HotkeyEventArgs(KeyCode hotKeyCode)
        {
            HotKeyCode = hotKeyCode;
        }
    }

    #endregion

    #region Events

    void OnInstantiatePersistentWindowInstances_ItemHoverHandler()
    {
        DaggerfallUI.Instance.InventoryWindow.OnItemHover += ItemHoverHandler;
    }

    void ItemHoverHandler(DaggerfallUnityItem item, ItemHoverLocation loc)
    {
        if (loc == ItemHoverLocation.LocalList || loc == ItemHoverLocation.Paperdoll)
        {
            selectionId = (int)item.UID;
            hoveredItem = item;
        }
        else
        {
            selectionId = null;
            hoveredItem = null;
        }
    }

    void OnInstantiatePersistentWindowInstances_OnInventoryClose()
    {
        DaggerfallUI.Instance.InventoryWindow.OnClose += OnInventoryClose;
    }

    public void OnInventoryClose()
    {
        selectionId = null;
        hoveredItem = null;
    }

    void OnCastReadySpell(EntityEffectBundle spell)
    {
        if (spellToRearm == null && spell.Settings.TargetType != TargetTypes.None && spell.Settings.TargetType != TargetTypes.CasterOnly)
        {
            spellToRearm = spell;
        }
    }
    
    public void StartGameBehaviour_OnStartGame(object sender, EventArgs e)
    {
        HotkeySettings hotkeyBarSettings = null;
        var modSettingsDataFile = Directory.GetFiles(mod.PersistentDataDirectory, "HotkeySettingsData.json").FirstOrDefault();

        if (modSettingsDataFile != null)
        {
            hotkeyBarSettings = JsonConvert.DeserializeObject<HotkeySettings>(File.ReadAllText(Path.Combine(mod.PersistentDataDirectory, "HotkeySettingsData.json")));
        }

        if (hotkeyBarSettings != null)
        {
            HotkeySettingsGUI.LoadHotkeySettings(hotkeyBarSettings);
        }

        panel.InitIcons();
        panel.UpdateIcons();
    }

    #endregion

    #region Mod Message Recievers

    static void DFModMessageReceiver(string message, object data, DFModMessageCallback callBack)
    {
        switch (message)
        {
            // Other mod sends hotkey parameters, and it gets registered as a new hotkey at the selected keycode
            // Parameter: Tuple<KeyCode, int, string, bool>
            // - KeyCode: key the selected item is assigned to
            // - int: selection id. For spells, this is the position in the spellbook. For items, this is the item's UID
            // - string: HotkeyType enum value
            // - bool: whether or not the item is to be mapped to the left hand
            case "RegisterHotkey":
                Tuple<KeyCode, int, string, bool> args = data as Tuple<KeyCode, int, string, bool>;
                if (args == null)
                {
                    callBack?.Invoke(message, "Invalid arguments, expected Tuple<KeyCode, int, string>");
                    return;
                }

                var (keyCode, selectionId, hotkeyType, leftHandEquip) = args.ToValueTuple();
                if (!Enum.TryParse(hotkeyType, out HotkeyType parsedValue))
                {
                    callBack?.Invoke(message, $"Invalid hotkey type '{hotkeyType}'");
                    return;
                }

                instance.AssignHotkey(keyCode, selectionId, parsedValue, leftHandEquip);

                callBack?.Invoke(message, null);
                break;

            case "GetMaxHotkeyBarSize":
                callBack?.Invoke(message, instance.MaxHotkeyBarSize);
                break;

            default:
                callBack?.Invoke(message, "Unknown message");
                break;
        }
    }

    #endregion

    #region Save Data

    public object NewSaveData()
    {
        var hotkeyBanksSerialized = new List<Dictionary<KeyCode, Hotkey>>();

        for (int i = 0; i < MaxHotkeyBanks; i++)
        {
            hotkeyBanksSerialized.Add(GetBlankHotkeys());
        }

        return new HotkeysSaveData
        {
            HotKeyBanksSerialized = hotkeyBanksSerialized,
            HotKeySettings = HotkeySettingsGUI.GetHotkeySettings()
        };
    }

    public object GetSaveData()
    {
        // whenever you save a game, save the mod settings to persistent save data so it can be loaded for new games
        string filePath = Path.Combine(mod.PersistentDataDirectory, "HotkeySettingsData.json");
        var json = JsonConvert.SerializeObject(HotkeySettingsGUI.GetHotkeySettings());
        File.WriteAllText(filePath, json);

        return new HotkeysSaveData
        {
            HotKeyBanksSerialized = hotkeyBanks,
            HotKeySettings = HotkeySettingsGUI.GetHotkeySettings()
        };
    }

    public void RestoreSaveData(object saveData)
    {
        var hotkeysSaveData = (HotkeysSaveData)saveData;

        if (hotkeysSaveData.HotKeyBanksSerialized != null)
        {
            MaxHotkeyBanks = hotkeysSaveData.HotKeyBanksSerialized.Count;
            MaxHotkeyBarSize = hotkeysSaveData.HotKeyBanksSerialized.First().Count;

            hotkeyBanks = hotkeysSaveData.HotKeyBanksSerialized.Take(MaxHotkeyBanks).ToList();

            if (hotkeysSaveData.HotKeySettings != null)
            {
                HotkeySettingsGUI.LoadHotkeySettings(hotkeysSaveData.HotKeySettings);
            }
            else
            {
                HotkeySettingsGUI.LoadDefaultHotkeySettings(MaxHotkeyBarSize, MaxHotkeyBanks);

                ResizeHotkeyBar();
                panel.InitIcons();
                panel.UpdateIcons();
            }
        }
        else
        {
            hotkeyBanks = GetBlankHotkeyBanks();
            hotkeys = GetBlankHotkeys();
        }
    }

    #endregion
}