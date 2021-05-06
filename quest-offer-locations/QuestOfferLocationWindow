using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Entity;
using Assets.Scripts.Game.MacadaynuMods.QuestOfferLocations;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public class QuestOfferLocationWindow : DaggerfallQuestOfferWindow
    {
        #region Constructors

        public QuestOfferLocationWindow(IUserInterfaceManager uiManager, StaticNPC.NPCData npc, FactionFile.SocialGroups socialGroup, bool menu)
            : base(uiManager, npc, socialGroup, menu)
        {
        }

        #endregion

        protected override void Setup()
        {
            CloseWindow();
            GetQuest();
        }

        #region Quest handling

        protected override void GetQuest()
        {
            // Just exit if this NPC already involved in an active quest
            // If quest conditions are complete the quest system should pickup ending
            if (QuestMachine.Instance.IsLastNPCClickedAnActiveQuestor())
            {
                CloseWindow();
                return;
            }

            // Get the faction id for affecting reputation on success/failure, and current rep
            int factionId = questorNPC.factionID;
            Genders gender = questorNPC.gender;
            int reputation = GameManager.Instance.PlayerEntity.FactionData.GetReputation(factionId);
            int level = GameManager.Instance.PlayerEntity.Level;

            // Select a quest at random from appropriate pool
            offeredQuest = GameManager.Instance.QuestListsManager.GetSocialQuest(socialGroup, factionId, gender, reputation, level);
            if (offeredQuest != null)
            {
                // Log offered quest
                Debug.LogFormat("Offering quest {0} from Social group {1} affecting factionId {2}", offeredQuest.QuestName, socialGroup, offeredQuest.FactionId);

                // Offer the quest to player
                var messageBox = QuestOfferMessageHelper.CreateQuestOffer(offeredQuest);
                if (messageBox != null)
                {
                    messageBox.OnButtonClick += OfferQuest_OnButtonClick;
                    messageBox.Show();
                }
            }
            else if (!GameManager.Instance.IsPlayerInsideCastle) // Failed get quest messages do not appear inside castles in classic.
            {
                ShowFailGetQuestMessage();
            }
        }

        #endregion
    }
}
