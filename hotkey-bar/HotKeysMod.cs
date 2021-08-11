using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.MacadaynuMods.HotkeyBar;
using Assets.Scripts.MyMods;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Formulas;
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
        public List<Dictionary<KeyCode, Hotkey>> HotKeyBanksSerialized;
    }
    #endregion

    GameObject console;
    ConsoleController consoleController;
    static Mod mod;
    public static HotkeysMod instance;
    static ModSettings settings;

    public DaggerfallUnityItem hoveredItem;
    public int? hotkeySelectionId;

    public Dictionary<KeyCode, Hotkey> hotkeys;

    public Type SaveDataType { get { return typeof(HotkeysSaveData); } }

    public event EventHandler OnHotkeyPressed;

    EntityEffectBundle spellToRearm;

    public int MaxHotkeyBarSize { get; private set; }
    public int MaxHotkeyBanks = 1;

    public List<Dictionary<KeyCode, Hotkey>> hotkeyBanks;
    public int CurrentHotkeyBank = 1; 

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
        //loads mod settings.
        settings = mod.GetSettings();

        MaxHotkeyBarSize = settings.GetValue<int>("HotkeyOptions", "MaxHotkeyBarSize");
        MaxHotkeyBanks = settings.GetValue<int>("HotkeyOptions", "MaxHotkeyRows");

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

        var hotkeysPanel = new HotkeysPanel
        {
            Size = daggerfallUI.DaggerfallHUD.NativePanel.Size,
            HorizontalAlignment = HorizontalAlignment.Center,
            Enabled = true
        };

        daggerfallUI.DaggerfallHUD.NativePanel.Components.Add(hotkeysPanel);

        DaggerfallUI.Instance.OnInstantiatePersistentWindowInstances += OnInstantiatePersistentWindowInstances_ItemHoverHandler;
        DaggerfallUI.Instance.OnInstantiatePersistentWindowInstances += OnInstantiatePersistentWindowInstances_OnInventoryClose;

        GameManager.Instance.PlayerEffectManager.OnCastReadySpell += OnCastReadySpell;
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

    // Update is called once per frame
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
                    var mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");

                    if (Input.GetKeyDown(KeyCode.Minus) || mouseScrollWheel < 0f)
                    {
                        if (CurrentHotkeyBank == 1)
                        {
                            CurrentHotkeyBank = MaxHotkeyBanks;
                        }
                        else
                        {
                            CurrentHotkeyBank -= 1;
                        }

                        hotkeys = hotkeyBanks.ElementAt(CurrentHotkeyBank - 1);

                        if (OnHotkeyPressed != null)
                        {
                            OnHotkeyPressed(this, new HotkeyEventArgs(KeyCode.Minus));
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.Equals) || mouseScrollWheel > 0f)
                    {
                        if (CurrentHotkeyBank == MaxHotkeyBanks)
                        {
                            CurrentHotkeyBank = 1;
                        }
                        else
                        {
                            CurrentHotkeyBank += 1;
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
                    // Raise event
                    if (OnHotkeyPressed != null)
                    {
                        OnHotkeyPressed(this, new HotkeyEventArgs(KeyCode.Alpha0));
                    }
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

    private void HandleInput(KeyCode keyCode)
    {
        HotkeyType? hotkeyType = null;
        if (!GameManager.IsGamePaused && DaggerfallUI.UIManager.WindowCount == 0)
        {
            ActivateHotKey(keyCode);
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
                AssignHotkey(keyCode, hotkeySelectionId.Value, hotkeyType.Value);
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

        
    private void AssignHotkey(KeyCode keyCode, int selectionId, HotkeyType hotkeyType)
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
            Spell = spellSettings
        };
    }

    private void ActivateHotKey(KeyCode keyCode)
    {
        var hotKey = hotkeys[keyCode];

        if (hotKey != null)
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
                    SetSpellReady(hotKey.Spell);
                    break;
                case HotkeyType.Horse:
                case HotkeyType.Cart:
                    ToggleMount(hotKey.Type);
                    break;
                case HotkeyType.EnchantedItem:
                    UseEnchantedItem((ulong)hotKey.Id);
                    break;
                case HotkeyType.LightSource:
                    UseLightSource((ulong)hotKey.Id);
                    break;
                case HotkeyType.Book:
                    UseBook((ulong)hotKey.Id);
                    break;
                case HotkeyType.Spellbook:
                    UseSpellbook((ulong)hotKey.Id);
                    break;
                case HotkeyType.Drug:
                    UseDrug((ulong)hotKey.Id);
                    break;
                case HotkeyType.CampingEquipment:
                    UseItemByTemplateIndex(ItemGroups.UselessItems2, 530, false);
                    break;
                case HotkeyType.Meat:
                    UseItemByTemplateIndex(ItemGroups.UselessItems2, 537, true);
                    break;
                case HotkeyType.GenericItem:
                    UseItem((ulong)hotKey.Id);
                    break;
            }
        }
    }

    private void EquipWeapon(ulong itemId)
    {
        spellToRearm = null;

        var weapon = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (weapon != null)
        {
            var prohibited = false;

            // Check for prohibited weapon type
            if ((weapon.GetWeaponSkillUsed() & (int)GameManager.Instance.PlayerEntity.Career.ForbiddenProficiencies) != 0)
                prohibited = true;
            // Check for prohibited material
            else if ((1 << weapon.NativeMaterialValue & (int)GameManager.Instance.PlayerEntity.Career.ForbiddenMaterials) != 0)
                prohibited = true;

            if (prohibited)
            {
                DaggerfallUI.MessageBox($"Your class prohibits you from equipping this weapon.");
                return;
            }

            var hasArmedSpell = GameManager.Instance.PlayerEffectManager.HasReadySpell;

            // cancel armed missile spell
            GameManager.Instance.PlayerEffectManager.AbortReadySpell();
            
            if (GameManager.Instance.PlayerEntity.ItemEquipTable.IsEquipped(weapon))
            {
                // if switching from spell, unsheathe the weapon if sheathed
                if (hasArmedSpell)
                {
                    if (GameManager.Instance.WeaponManager.Sheathed)
                    {
                        GameManager.Instance.WeaponManager.ToggleSheath();
                    }
                }
                else
                {
                    // just sheathe/unsheathe the weapon if you are pressing a hotkey weapon already equipped
                    GameManager.Instance.WeaponManager.ToggleSheath();
                }
            }
            else
            {
                GameManager.Instance.PlayerEntity.ItemEquipTable.EquipItem(weapon);

                // unsheathe weapon if sheathed
                if (GameManager.Instance.WeaponManager.Sheathed)
                {
                    GameManager.Instance.WeaponManager.ToggleSheath();
                }
            }
        }
    }

    private void UsePotion(ulong itemId)
    {
        var potion = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (potion != null)
        {
            GameManager.Instance.PlayerEffectManager.DrinkPotion(potion);

            GameManager.Instance.PlayerEntity.Items.RemoveOne(potion);
        }
    }

    private void UseEnchantedItem(ulong itemId)
    {
        var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (item != null)
        {
            GameManager.Instance.PlayerEffectManager.DoItemEnchantmentPayloads(EnchantmentPayloadFlags.Used, item, GameManager.Instance.PlayerEntity.Items);
        }
    }

    private void UseBook(ulong itemId)
    {
        var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (item != null)
        {
            DaggerfallUI.Instance.BookReaderWindow.OpenBook(item);
            if (DaggerfallUI.Instance.BookReaderWindow.IsBookOpen)
            {
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenBookReaderWindow);
            }
            else
            {
                var messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager);
                messageBox.SetText(TextManager.Instance.GetLocalizedText("bookUnavailable"));
                messageBox.ClickAnywhereToClose = true;
                DaggerfallUI.UIManager.PushWindow(messageBox);
            }
        }
    }

    private void UseSpellbook(ulong itemId)
    {
        var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (item != null)
        {
            if (GameManager.Instance.PlayerEntity.SpellbookCount() == 0)
            {
                // Player has no spells
                TextFile.Token[] textTokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(12); // no spells text id
                DaggerfallMessageBox noSpells = new DaggerfallMessageBox(DaggerfallUI.UIManager);
                noSpells.SetTextTokens(textTokens);
                noSpells.ClickAnywhereToClose = true;
                noSpells.Show();
            }
            else
            {
                // Show spellbook
                DaggerfallUI.UIManager.PostMessage(DaggerfallUIMessages.dfuiOpenSpellBookWindow);
            }
        }
    }

    private void UseDrug(ulong itemId)
    {
        var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (item != null)
        {
            // Drug poison IDs are 136 through 139. Template indexes are 78 through 81, so add to that.
            FormulaHelper.InflictPoison(GameManager.Instance.PlayerEntity, GameManager.Instance.PlayerEntity, (Poisons)item.TemplateIndex + 66, true);
            GameManager.Instance.PlayerEntity.Items.RemoveItem(item);
        }
    }

    private void UseItemByTemplateIndex(ItemGroups itemGroup, int templateIndex, bool useBestConditionFirst)
    {
        var item = GetItemByTemplateIndex(itemGroup, templateIndex, useBestConditionFirst);

        if (item != null)
        {
            UseItem(item.UID);
        }
    }

    public DaggerfallUnityItem GetItemByTemplateIndex(ItemGroups itemGroup, int templateIndex, bool useBestConditionFirst)
    {
        var itemsInInventory = GameManager.Instance.PlayerEntity.Items.SearchItems(itemGroup, templateIndex);

        if (itemsInInventory.Any())
        {
            var orderedItems = itemsInInventory.OrderByDescending(x => x.currentCondition);

            if (useBestConditionFirst)
            {
                return orderedItems.First();
            }
            else
            {
                return orderedItems.Last();
            }
        }
        else
        {
            Debug.Log("None found in your inventory");
        }

        return null;
    }

    private void UseItem(ulong itemId)
    {
        var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (item != null)
        {
            // Try to handle use with a registered delegate
            ItemHelper.ItemUseHandler itemUseHandler;
            if (DaggerfallUnity.Instance.ItemHelper.GetItemUseHandler(item.TemplateIndex, out itemUseHandler))
            {
                if (itemUseHandler(item, GameManager.Instance.PlayerEntity.Items))
                    return;
            }

            if (!item.UseItem(GameManager.Instance.PlayerEntity.Items))
            {
                DaggerfallUI.MessageBox($"You cannot use the {item.LongName}");
            }
        }
    }

    private void UseLightSource(ulong itemId)
    {
        var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

        if (item != null)
        {
            if (item.currentCondition > 0)
            {
                var playerEntity = GameManager.Instance.PlayerEntity;

                if (playerEntity.LightSource == item)
                {
                    //DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightDouse"), false, item);
                    playerEntity.LightSource = null;
                }
                else
                {
                    //DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightLight"), false, item);
                    playerEntity.LightSource = item;
                }
            }
            else
                DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightEmpty"), false, item);
        }
    }

    private void SetSpellReady(EffectBundleSettings spellSettings)
    {
        if (GameManager.Instance.PlayerEffectManager.HasReadySpell &&
            GameManager.Instance.PlayerEffectManager.ReadySpell.Settings.Equals(spellSettings))
        {
            // cancel missile spell if already armed
            GameManager.Instance.PlayerEffectManager.AbortReadySpell();
            return;
        }

        EntityEffectManager playerEffectManager = GameManager.Instance.PlayerEffectManager;
        if (playerEffectManager)
        {
            playerEffectManager.SetReadySpell(new EntityEffectBundle(spellSettings,
                GameManager.Instance.PlayerEntityBehaviour));
        }
    }

    private void ToggleMount(HotkeyType hotkeyType)
    {
        if (GameManager.Instance.IsPlayerInside)
        {
            DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("cannotChangeTransportationIndoors"));
            return;
        }

        var transportMode = GameManager.Instance.TransportManager.TransportMode;

        if (hotkeyType == HotkeyType.Horse)
        {
            if (transportMode == TransportModes.Horse)
            {
                GameManager.Instance.TransportManager.TransportMode = TransportModes.Foot;
            }
            else
            {
                GameManager.Instance.TransportManager.TransportMode = TransportModes.Horse;
            }
        }

        if (hotkeyType == HotkeyType.Cart)
        {
            if (transportMode == TransportModes.Cart)
            {
                GameManager.Instance.TransportManager.TransportMode = TransportModes.Foot;
            }
            else
            {
                GameManager.Instance.TransportManager.TransportMode = TransportModes.Cart;
            }
        }
    }

    public object NewSaveData()
    {
        var hotkeyBanksSerialized = new List<Dictionary<KeyCode, Hotkey>>();

        for (int i = 0; i < MaxHotkeyBanks; i++)
        {
            hotkeyBanksSerialized.Add(GetBlankHotkeys());
        }

        return new HotkeysSaveData
        {
            HotKeyBanksSerialized = hotkeyBanksSerialized
        };
    }

    public object GetSaveData()
    {
        return new HotkeysSaveData
        {
            HotKeyBanksSerialized = hotkeyBanks
        };
    }

    public void RestoreSaveData(object saveData)
    {
        var hotkeysSaveData = (HotkeysSaveData)saveData;

        if (hotkeysSaveData.HotKeyBanksSerialized != null)
        {
            hotkeyBanks = hotkeysSaveData.HotKeyBanksSerialized.Take(MaxHotkeyBanks).ToList();

            // add empty banks if there are more banks than previous save
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

            hotkeys = hotkeyBanks.First();
        }
        else
        {
            hotkeyBanks = GetBlankHotkeyBanks();
            hotkeys = GetBlankHotkeys();
        }
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
}
