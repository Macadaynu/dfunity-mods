using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallConnect.Arena2;
using Assets.Scripts.Game.MacadaynuMods.QuestOfferLocations;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Guilds;
using Assets.Scripts.Game.UserInterfaceWindows;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public class QuestOfferLocationGuildServicePopUpWindow : DaggerfallGuildServicePopupWindow
    {
        public int SelectedIndex;
        public NearestQuest NearestQuest;

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
                if (QuestOfferLocationsMod.preferNearbyQuests)
                {
                    NearestQuest = null;

                    IGuild guild = guildManager.GetGuild(guildGroup);
                    int rep = guild.GetReputation(playerEntity);
                    int factionId = guild.GetFactionId();

                    var timeStart = System.DateTime.Now;
                    bool found = false;
                    while (!found)
                    {
                        // if nearby quest still not found after enough time has passed, stick with the current offered quest
                        if (System.DateTime.Now.Subtract(timeStart).TotalSeconds >= QuestOfferLocationsMod.maxSearchTimeInSeconds)
                        {
                            found = true;
                            offeredQuest = NearestQuest?.Quest;
                            break;
                        }

                        // null check for safety
                        if (offeredQuest == null)
                        {
                            continue;
                        }

                        var farthestTravelTimeInDays = 0.0f;
                        var questPlaces = offeredQuest.GetAllResources(typeof(Questing.Place));
                        foreach (Questing.Place questPlace in questPlaces)
                        {
                            DFLocation location;
                            DaggerfallUnity.Instance.ContentReader.GetLocation(questPlace.SiteDetails.regionName, questPlace.SiteDetails.locationName, out location);

                            var travelTimeDays = QuestOfferMessageHelper.GetTravelTimeToLocation(location);

                            if (travelTimeDays > farthestTravelTimeInDays)
                            {
                                farthestTravelTimeInDays = travelTimeDays;
                            }
                        }

                        // if the farthest quest location is too far, get another quest and try again
                        if (farthestTravelTimeInDays > QuestOfferLocationsMod.maxTravelDistanceInDays)
                        {
                            // store the nearest quest in the loop, as a fallback
                            if (NearestQuest == null || NearestQuest.TimeToTravelToQuestInDays > farthestTravelTimeInDays)
                            {
                                NearestQuest = new NearestQuest
                                {
                                    Quest = offeredQuest,
                                    TimeToTravelToQuestInDays = farthestTravelTimeInDays
                                };
                            }

                            if (DaggerfallUnity.Settings.GuildQuestListBox)
                            {
                                offeredQuest = GameManager.Instance.QuestListsManager.LoadQuest(questPool[SelectedIndex], factionId);
                            }
                            else
                            {
                                offeredQuest = GameManager.Instance.QuestListsManager.GetGuildQuest(guildGroup, guild.IsMember() ? Questing.MembershipStatus.Member : Questing.MembershipStatus.Nonmember, guild.GetFactionId(), rep, guild.Rank);
                            }
                        }
                        else
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (offeredQuest == null)
                {
                    ShowFailGetQuestMessage();
                }
                else
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
            }
            else
            {
                ShowFailGetQuestMessage();
            }
            questPool.Clear();            
        }

        protected override void QuestPicker_OnItemPicked(int index, string name)
        {
            SelectedIndex = index;

            base.QuestPicker_OnItemPicked(index, name);
        }
    }
}
