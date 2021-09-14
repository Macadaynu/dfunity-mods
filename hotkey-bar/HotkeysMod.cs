using System;
using System.Collections.Generic;
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
    public int? hotkeySelectionId;

    public Dictionary<KeyCode, Hotkey> hotkeys;

    public Type SaveDataType { get { return typeof(HotkeysSaveData); } }

    public event EventHandler OnHotkeyPressed;

    public EntityEffectBundle spellToRearm;

    public int MaxHotkeyBarSize = 9;
    public int MaxHotkeyBanks = 1;
    public bool InvertScrollOrder;

    public List<Dictionary<KeyCode, Hotkey>> hotkeyBanks;
    public int CurrentHotkeyBank = 1;

    public HotkeysPanel panel;
    public GameObject panelController;

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
    }

    #endregion

    #region Detect Hotkey Input

    private void HandleInput(KeyCode keyCode)
    {
        HotkeyType? hotkeyType = null;
        if (DaggerfallUI.UIManager.TopWindow.GetType() == typeof(DaggerfallInputMessageBox))
        {
            return;
        }

        if (!GameManager.IsGamePaused && DaggerfallUI.UIManager.WindowCount == 0)
        {
            HotkeyActivator.ActivateHotKey(keyCode);
        }
        else if (hotkeySelectionId.HasValue)
        {
            var uiType = DaggerfallUI.UIManager.TopWindow.GetType();
            if (uiType == typeof(HotkeysSpellBookWindow))
            {
                hotkeyType = HotkeyType.Spell;
            }
            else if (uiType == typeof(DaggerfallInventoryWindow) || (uiType?.IsSubclassOf(typeof(DaggerfallInventoryWindow)) ?? false))
            {
                if (hoveredItem.ItemGroup == ItemGroups.Weapons && hoveredItem.GroupIndex != 18) // group index 18 is for arrows
                {
                    hotkeyType = HotkeyType.Weapon;
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
                // generic item should be last
                else
                {
                    hotkeyType = HotkeyType.GenericItem;
                }
            }

            if (hotkeyType.HasValue)
            {
                AssignHotkey(keyCode, hotkeySelectionId.Value, hotkeyType.Value, hoveredItem?.TemplateIndex);
            }
            else
            {
                var tokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                    $"You cannot map the {hoveredItem.LongName}"
                );

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens.ToArray(), DaggerfallUI.Instance.InventoryWindow);
                messageBox.ClickAnywhereToClose = true;
                messageBox.AllowCancel = false;
                messageBox.ParentPanel.BackgroundColor = Color.clear;
                DaggerfallUI.UIManager.PushWindow(messageBox);
            }
        }

        // Raise event
        if (OnHotkeyPressed != null)
        {
            OnHotkeyPressed(this, new HotkeyEventArgs(keyCode));
        }
    }

    private void AssignHotkey(KeyCode keyCode, int selectionId, HotkeyType hotkeyType, int? templateId = null)
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
                $"{spellSettings.Name} has been assigned to Hotkey: {keyCode.ToString().Last()}"
                );

            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens, DaggerfallUI.UIManager.TopWindow);
            messageBox.ClickAnywhereToClose = true;
            messageBox.AllowCancel = false;
            messageBox.ParentPanel.BackgroundColor = Color.clear;
            DaggerfallUI.UIManager.PushWindow(messageBox);
        }
        else
        {
            var tokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                $"{hoveredItem.LongName} has been assigned to Hotkey: {keyCode.ToString().Last()}"
                );

            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens.ToArray(), DaggerfallUI.UIManager.TopWindow);
            messageBox.ClickAnywhereToClose = true;
            messageBox.AllowCancel = false;
            messageBox.ParentPanel.BackgroundColor = Color.clear;
            DaggerfallUI.UIManager.PushWindow(messageBox);
        }

        // remove item from already existing hotkey assignment
        var assignedHotkey = hotkeys.FirstOrDefault(x => x.Value?.Id == selectionId && x.Value?.Type == hotkeyType);
        if (assignedHotkey.Value != null)
        {
            hotkeys[assignedHotkey.Key] = null;
        }

        // assign the new hotkey
        hotkeys[keyCode] = new Hotkey
        {
            Id = selectionId,
            Type = hotkeyType,
            Spell = spellSettings,
            TemplateId = templateId
        };
    }

    void Update()
    {
        if (!GameManager.Instance.PlayerSpellCasting.IsPlayingAnim)
        {
            if (consoleController.ui.isConsoleOpen || SaveLoadManager.Instance.LoadInProgress)
                return;

            if (!GameManager.IsGamePaused && DaggerfallUI.UIManager.WindowCount == 0)
            {
                if (MaxHotkeyBanks > 1)
                {
                    // TODO: fast switch to default hotkey bar
                    //if (Input.GetMouseButtonDown(2))
                    //{
                    //    hotkeys = hotkeyBanks.ElementAt(0);

                    //    if (OnHotkeyPressed != null)
                    //    {
                    //        OnHotkeyPressed(this, new HotkeyEventArgs(KeyCode.Minus));
                    //    }

                    //    return;
                    //}

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

                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    Instance.AddAction(Actions.ActivateCursor);

                    HotkeySettingsGUI.hotkeySettingsEnabled = !HotkeySettingsGUI.hotkeySettingsEnabled;
                }
            }

            if (MaxHotkeyBarSize > 0)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    HandleInput(KeyCode.Alpha1);
                }
            }

            if (MaxHotkeyBarSize > 1)
            {
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    HandleInput(KeyCode.Alpha2);
                }
            }

            if (MaxHotkeyBarSize > 2)
            {
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    HandleInput(KeyCode.Alpha3);
                }
            }

            if (MaxHotkeyBarSize > 3)
            {
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    HandleInput(KeyCode.Alpha4);
                }
            }

            if (MaxHotkeyBarSize > 4)
            {
                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    HandleInput(KeyCode.Alpha5);
                }
            }

            if (MaxHotkeyBarSize > 5)
            {
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    HandleInput(KeyCode.Alpha6);
                }
            }

            if (MaxHotkeyBarSize > 6)
            {
                if (Input.GetKeyDown(KeyCode.Alpha7))
                {
                    HandleInput(KeyCode.Alpha7);
                }
            }

            if (MaxHotkeyBarSize > 7)
            {
                if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    HandleInput(KeyCode.Alpha8);
                }
            }

            if (MaxHotkeyBarSize > 8)
            {
                if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    HandleInput(KeyCode.Alpha9);
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
            hotkeySelectionId = (int)item.UID;
            hoveredItem = item;
        }
        else
        {
            hotkeySelectionId = null;
            hoveredItem = null;
        }
    }

    void OnCastReadySpell(EntityEffectBundle spell)
    {
        if (spellToRearm == null && spell.Settings.TargetType != TargetTypes.None && spell.Settings.TargetType != TargetTypes.CasterOnly)
        {
            spellToRearm = spell;
        }
    }

    void OnInstantiatePersistentWindowInstances_OnInventoryClose()
    {
        DaggerfallUI.Instance.InventoryWindow.OnClose += OnInventoryClose;
    }

    public void OnInventoryClose()
    {
        hotkeySelectionId = null;
        hoveredItem = null;
    }

    public void StartGameBehaviour_OnStartGame(object sender, EventArgs e)
    {
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
            // Parameter: Tuple<KeyCode, int, string>
            // - KeyCode: key the selected item is assigned to
            // - int: selection id. For spells, this is the position in the spellbook. For items, this is the item's UID
            // - string: HotkeyType enum value
            case "RegisterHotkey":
                Tuple<KeyCode, int, string> args = data as Tuple<KeyCode, int, string>;
                if (args == null)
                {
                    callBack?.Invoke(message, "Invalid arguments, expected Tuple<KeyCode, int, string>");
                    return;
                }

                var (keyCode, selectionId, hotkeyType) = args.ToValueTuple();
                if (!Enum.TryParse(hotkeyType, out HotkeyType parsedValue))
                {
                    callBack?.Invoke(message, $"Invalid hotkey type '{hotkeyType}'");
                    return;
                }

                instance.AssignHotkey(keyCode, selectionId, parsedValue);

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