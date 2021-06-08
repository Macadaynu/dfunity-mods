using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.MacadaynuMods.HotkeyBar;
using Assets.Scripts.MyMods;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using Wenzil.Console;
using static DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallInventoryWindow;

public class HotkeysMod : MonoBehaviour, IHasModSaveData
{
    //assigns serializer version to ensures mod data continuaty / debugging.
    //sets up the save data class for the seralizer to read and save to text file.
    #region Types
    [FullSerializer.fsObject("v1")]
    public class HotkeysSaveData
    {
        public Dictionary<KeyCode, Hotkey> HotkeysSerialized;
    }
    #endregion

    GameObject console;
    ConsoleController consoleController;
    static Mod mod;
    public static HotkeysMod instance;
    static ModSettings settings;

    public DaggerfallUnityItem hoveredItem;
    public int? hotkeySelectionId;
    public string spellSelectionName;

    public Dictionary<KeyCode, Hotkey> hotkeys;

    public Type SaveDataType { get { return typeof(HotkeysSaveData); } }

    public event EventHandler OnHotkeyPressed;
    public event EventHandler OnHotkeyAssigned;

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
        //loads mods settings.
        //settings = mod.GetSettings();
        //initiates save paramaters for class/script.
        mod.SaveDataInterface = instance;
        //after finishing, set the mod's IsReady flag to true.
        mod.IsReady = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        // get the console controller so we can check if the user has the console open
        console = GameObject.Find("Console");
        consoleController = console.GetComponent<ConsoleController>();

        // assign empty hotkeys if none exist
        if (hotkeys == null)
        {
            hotkeys = GetBlankHotkeys();
        }

        //UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Inventory, typeof(HotkeysInventoryWindow));
        UIWindowFactory.RegisterCustomUIWindow(UIWindowType.SpellBook, typeof(HotkeysSpellBookWindow));

        var daggerfallUI = GameObject.Find("DaggerfallUI").GetComponent<DaggerfallUI>();

        var hotkeysPanel = new HotkeysPanel
        {
            Size = daggerfallUI.DaggerfallHUD.NativePanel.Size,
            HorizontalAlignment = HorizontalAlignment.Center,
            Enabled = true
        };

        daggerfallUI.DaggerfallHUD.NativePanel.Components.Add(hotkeysPanel);

        //DaggerfallUI.Instance.InventoryWindow.OnItemHover += ItemHoverHandler;
        //DaggerfallUI.Instance.InventoryWindow.OnClose += OnInventoryClose;

        DaggerfallUI.Instance.OnInstantiatePersistentWindowInstances += OnInstantiatePersistentWindowInstances_ItemHoverHandler;
        DaggerfallUI.Instance.OnInstantiatePersistentWindowInstances += OnInstantiatePersistentWindowInstances_OnInventoryClose;
    }

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

    void OnInstantiatePersistentWindowInstances_OnInventoryClose()
    {
        DaggerfallUI.Instance.InventoryWindow.OnClose += OnInventoryClose;
    }

    public void OnInventoryClose()
    {
        hotkeySelectionId = null;
        hoveredItem = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (consoleController.ui.isConsoleOpen || SaveLoadManager.Instance.LoadInProgress)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            HandleInput(KeyCode.Alpha1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            HandleInput(KeyCode.Alpha2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            HandleInput(KeyCode.Alpha3);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            HandleInput(KeyCode.Alpha4);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            HandleInput(KeyCode.Alpha5);
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            HandleInput(KeyCode.Alpha6);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            HandleInput(KeyCode.Alpha7);
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            HandleInput(KeyCode.Alpha8);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            HandleInput(KeyCode.Alpha9);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            // Raise event
            if (OnHotkeyPressed != null)
            {
                OnHotkeyPressed(this, new HotkeyEventArgs(KeyCode.Alpha0));
            }
        }
    }

    private void HandleInput(KeyCode keyCode)
    {
        if (!GameManager.IsGamePaused && DaggerfallUI.UIManager.WindowCount == 0)
        {
            ActivateHotKey(keyCode);
        }
        else if (hotkeySelectionId.HasValue)
        {
            HotkeyType? hotkeyType = null;
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
                else if (hoveredItem.IsPotion)
                {
                    hotkeyType = HotkeyType.Potion;
                }

                var tokens = new List<TextFile.Token>();

                if (hotkeyType.HasValue)
                {
                    tokens.Add(new TextFile.Token { formatting = TextFile.Formatting.Text, text = $"{hoveredItem.LongName} has been assigned to Hotkey: {keyCode.ToString().Last()}" });
                }
                else
                {
                    tokens.Add(new TextFile.Token { formatting = TextFile.Formatting.Text, text = $"You can only map weapons, potions or spells to hotkeys" });
                }

                tokens.Add(new TextFile.Token { formatting = TextFile.Formatting.JustifyCenter });

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens.ToArray(), DaggerfallUI.Instance.InventoryWindow);
                messageBox.ClickAnywhereToClose = true;
                messageBox.AllowCancel = false;
                messageBox.ParentPanel.BackgroundColor = Color.clear;
                DaggerfallUI.UIManager.PushWindow(messageBox);
            }

            if (hotkeyType.HasValue)
            {
                AssignHotkey(keyCode, hotkeySelectionId.Value, hotkeyType.Value, spellSelectionName);
            }
        }

        // Raise event
        if (OnHotkeyPressed != null)
        {
            OnHotkeyPressed(this, new HotkeyEventArgs(keyCode));
        }
    }

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

    private void ActivateHotKey(KeyCode keyCode)
    {
        var hotKey = hotkeys[keyCode];

        if (hotKey == null)
        {
            //TODO: Handle error
        }
        else
        {
            switch (hotKey.Type)
            {
                case HotkeyType.Weapon:
                    EquipWeapon((ulong)hotKey.Id);
                    break;
                case HotkeyType.Potion:
                    UsePotion((ulong)hotKey.Id);
                    break;
                case HotkeyType.Spell:
                    SetSpellReady(hotKey.SpellName);
                    break;
            }
        }
    }

    private void AssignHotkey(KeyCode keyCode, int selectionId, HotkeyType hotkeyType, string spellSelectionName)
    {
        //TODO: Do this for spells as well
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
            SpellName = spellSelectionName
        };

        // Raise event
        if (OnHotkeyAssigned != null)
        {
            OnHotkeyAssigned(this, new HotkeyEventArgs(keyCode));
        }
    }

    private void EquipWeapon(ulong itemId)
    {
        //TODO: handle this being null
        var weapon = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        GameManager.Instance.PlayerEntity.ItemEquipTable.EquipItem(weapon);

        if (GameManager.Instance.WeaponManager.Sheathed)
        {
            GameManager.Instance.WeaponManager.ToggleSheath();
        }
    }

    private void UsePotion(ulong itemId)
    {
        //TODO: handle this being null
        var potion = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (potion != null)
        {
            GameManager.Instance.PlayerEffectManager.DrinkPotion(potion);

            GameManager.Instance.PlayerEntity.Items.RemoveOne(potion);
        }
    }

    private void SetSpellReady(string spellName)
    {
        // TODO: Can this be done with an id of the actual spell, rather than name?

        var spellSettings = new EffectBundleSettings();
        EffectBundleSettings[] spellbook = GameManager.Instance.PlayerEntity.GetSpells();

        for (int i = 0; i < spellbook.Length; i++)
        {
            if (spellbook[i].Name == spellName)
            {
                if (!GameManager.Instance.PlayerEntity.GetSpell(i, out spellSettings))
                {
                    return;
                }
                else
                {
                    break;
                }
            }
        }

        EntityEffectManager playerEffectManager = GameManager.Instance.PlayerEffectManager;
        if (playerEffectManager)
        {
            playerEffectManager.SetReadySpell(new EntityEffectBundle(spellSettings,
                GameManager.Instance.PlayerEntityBehaviour));
        }
    }

    public object NewSaveData()
    {
        return new HotkeysSaveData
        {
            HotkeysSerialized = GetBlankHotkeys()
        };
    }

    public object GetSaveData()
    {
        return new HotkeysSaveData
        {
            HotkeysSerialized = hotkeys
        };
    }

    public void RestoreSaveData(object saveData)
    {
        var hotkeysSaveData = (HotkeysSaveData)saveData;
        hotkeys = hotkeysSaveData.HotkeysSerialized;
    }

    Dictionary<KeyCode, Hotkey> GetBlankHotkeys()
    {
        return new Dictionary<KeyCode, Hotkey>
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
    }
}
