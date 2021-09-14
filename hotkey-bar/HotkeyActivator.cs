using Assets.Scripts.Game.MacadaynuMods.HotkeyBar;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Linq;
using UnityEngine;

namespace Assets.MacadaynuMods.HotkeyBar
{
    public static class HotkeyActivator
    {
        public static void ActivateHotKey(KeyCode keyCode)
        {
            var hotKey = HotkeysMod.instance.hotkeys[keyCode];

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
                        UseLightSource((ulong)hotKey.Id, hotKey.TemplateId);
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

        private static void EquipWeapon(ulong itemId)
        {
            HotkeysMod.instance.spellToRearm = null;

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

        private static void UsePotion(ulong itemId)
        {
            var potion = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

            if (potion != null)
            {
                GameManager.Instance.PlayerEffectManager.DrinkPotion(potion);

                GameManager.Instance.PlayerEntity.Items.RemoveOne(potion);
            }
        }

        private static void UseEnchantedItem(ulong itemId)
        {
            var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

            if (item != null)
            {
                GameManager.Instance.PlayerEffectManager.DoItemEnchantmentPayloads(EnchantmentPayloadFlags.Used, item, GameManager.Instance.PlayerEntity.Items);
            }
        }

        private static void UseBook(ulong itemId)
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

        private static void UseSpellbook(ulong itemId)
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

        private static void UseDrug(ulong itemId)
        {
            var item = GameManager.Instance.PlayerEntity.Items.GetItem(itemId);

            if (item != null)
            {
                // Drug poison IDs are 136 through 139. Template indexes are 78 through 81, so add to that.
                FormulaHelper.InflictPoison(GameManager.Instance.PlayerEntity, GameManager.Instance.PlayerEntity, (Poisons)item.TemplateIndex + 66, true);
                GameManager.Instance.PlayerEntity.Items.RemoveItem(item);
            }
        }

        private static void UseLightSource(ulong itemId, int? templateId)
        {
            if (templateId.HasValue)
            {
                var lightSources = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.UselessItems2, templateId.Value);

                if (lightSources.Any())
                {
                    // use weakest light source first
                    var item = lightSources.OrderBy(x => x.ConditionPercentage).First();
                    if (item != null)
                    {
                        if (item.currentCondition > 0)
                        {
                            var playerEntity = GameManager.Instance.PlayerEntity;

                            if (playerEntity.LightSource == item)
                            {
                                playerEntity.LightSource = null;
                            }
                            else
                            {
                                playerEntity.LightSource = item;
                            }
                        }
                        else
                            DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightEmpty"), false, item);
                    }
                }
                else
                {
                    DaggerfallUI.MessageBox($"You have none left in your inventory");
                }
            }
        }

        private static void UseItem(ulong itemId)
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
            else
            {
                DaggerfallUI.MessageBox($"You have none left in your inventory");
            }
        }

        private static void UseItemByTemplateIndex(ItemGroups itemGroup, int templateIndex, bool useBestConditionFirst)
        {
            var item = GetItemByTemplateIndex(itemGroup, templateIndex, useBestConditionFirst);

            if (item != null)
            {
                UseItem(item.UID);
            }
            else
            {
                DaggerfallUI.MessageBox($"You have none left in your inventory");
            }
        }        

        private static void SetSpellReady(EffectBundleSettings spellSettings)
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

        private static void ToggleMount(HotkeyType hotkeyType)
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
       
        private static DaggerfallUnityItem GetItemByTemplateIndex(ItemGroups itemGroup, int templateIndex, bool useBestConditionFirst)
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
    }
}