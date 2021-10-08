using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System.Linq;

namespace Assets.MacadaynuMods.HiddenMapLocations
{
    public class HiddenMapLocationsTalkWindow : DaggerfallTalkWindow
    {
        public HiddenMapLocationsTalkWindow(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null)
            : base(uiManager, previous)
        {
        }

        protected override void SelectTopicFromTopicList(int index, bool forceExecution = false)
        {
            TalkManager.ListItem listItem = listCurrentTopics[index];

            base.SelectTopicFromTopicList(index, forceExecution);

            if (listItem.type == TalkManager.ListItemType.Item && listItem.questionType == TalkManager.QuestionType.Regional)
            {
                var locationAnswer = listboxConversation.ListItems.Last().textLabel.Text;

                var locationSplit = "I think they have one in ";

                if (locationAnswer.Contains(locationSplit))
                {
                    var locationName = locationAnswer.Remove(0, locationSplit.Length).TrimEnd('.');

                    var location = DaggerfallUnity.ContentReader.MapFileReader.GetLocation(GameManager.Instance.PlayerGPS.CurrentRegionName, locationName);

                    HiddenMapLocationsMod.AddMapSummaryFromLocation(location);
                }
            }
        }
    }
}