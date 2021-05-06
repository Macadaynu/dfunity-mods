using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallConnect.Arena2;
using Assets.Scripts.Game.MacadaynuMods.QuestOfferLocations;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public class QuestOfferLocationGuildServicePopUpWindow : DaggerfallGuildServicePopupWindow
    {
        #region Constructors

        public QuestOfferLocationGuildServicePopUpWindow(IUserInterfaceManager uiManager, StaticNPC npc, FactionFile.GuildGroups guildGroup, int buildingFactionId)
            : base(uiManager, npc, guildGroup, buildingFactionId)
        {
        }

        #endregion

        protected override void OfferQuest()
        {
            if (offeredQuest != null)
            {
                // Offer the quest to player, setting external context provider to guild if a member
                if (guild.IsMember())
                    offeredQuest.ExternalMCP = guild;
                DaggerfallMessageBox messageBox = QuestOfferMessageHelper.CreateQuestOffer(offeredQuest);
                if (messageBox != null)
                {
                    messageBox.OnButtonClick += OfferQuest_OnButtonClick;
                    messageBox.Show();
                }
            }
            else
            {
                ShowFailGetQuestMessage();
            }
            questPool.Clear();
        }
    }
}
