using UnityEngine;
using System.Collections.Generic;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallConnect;
using System.Linq;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public class ConvenientQuestLogWindow : DaggerfallQuestJournalWindow
    {
        static bool selectedQuestDisplayed = false;
        TravelTimeCalculator travelTimeCalculator = new TravelTimeCalculator();
        Message selectedQuestMessage;
        static bool travelOptionsModEnabled = false;
        static bool travelOptionsCautiousTravel = false;
        static bool travelOptionsStopAtInnsTravel = false;
        List<Message> groupedQuestMessages;
        static int defaultMessageCheckValue = -1;
        int currentMessageCheck = defaultMessageCheckValue;

        public ConvenientQuestLogWindow(IUserInterfaceManager uiManager) : base(uiManager)
        {
        }

        protected override void Setup()
        {
            base.Setup();

            Mod travelOptionsMod = ModManager.Instance.GetMod("TravelOptions");

            if (travelOptionsMod != null)
            {
                travelOptionsModEnabled = travelOptionsMod.Enabled;
                var travelOptionsSettings = travelOptionsMod.GetSettings();
                travelOptionsCautiousTravel = travelOptionsSettings.GetBool("CautiousTravel", "PlayerControlledCautiousTravel");
                travelOptionsStopAtInnsTravel = travelOptionsSettings.GetBool("StopAtInnsTravel", "PlayerControlledInnsTravel");
            }
        }

        public override void Update()
        {
            base.Update();

            if (DisplayMode == JournalDisplay.ActiveQuests && currentMessageCheck != currentMessageIndex)
            {
                currentMessageCheck = currentMessageIndex;

                questLogLabel.Clear();

                if (selectedQuestDisplayed)
                {
                    SetTextForSelectedQuest(selectedQuestMessage);
                }
                else
                {
                    SetTextActiveQuests();
                }
            }
        }

        public override void OnPush()
        {
            base.OnPush();

            currentMessageCheck = defaultMessageCheckValue;
        }

        public override void OnPop()
        {
            base.OnPop();

            currentMessageCheck = defaultMessageCheckValue;
        }

        protected override void HandleClick(Vector2 position, bool remove = false)
        {
            if (DisplayMode != JournalDisplay.ActiveQuests)
            {
                base.HandleClick(position, remove);
                return;
            }

            if (entryLineMap == null)
                return;

            int line = (int)(position.y / questLogLabel.LineHeight);

            if (line < entryLineMap.Count)
                selectedEntry = entryLineMap[line];
            else
                selectedEntry = entryLineMap[entryLineMap.Count - 1];
            Debug.LogFormat("line is: {0} entry: {1}", line, selectedEntry);

            if (selectedQuestDisplayed)
            {
                currentMessageIndex = 0;
                SetTextActiveQuests();
            }
            else
            {
                //ensure nothing happens when last empty line is clicked
                if (line + 1 < entryLineMap.Count)
                {
                    if (line == 0 || entryLineMap[line - 1] != selectedEntry)
                    {
                        currentMessageIndex = 0;
                        selectedQuestMessage = groupedQuestMessages[selectedEntry];

                        if (remove)
                        {
                            if (selectedQuestMessage.ParentQuest.QuestName.StartsWith("S0000"))
                            {
                                var tokens = new List<TextFile.Token> { new TextFile.Token(TextFile.Formatting.Text, $"You cannot cancel story quests.") };
                                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens.ToArray(), this);
                                messageBox.ClickAnywhereToClose = true;
                                messageBox.AllowCancel = false;
                                messageBox.ParentPanel.BackgroundColor = Color.clear;
                                DaggerfallUI.UIManager.PushWindow(messageBox);
                            }
                            else if (selectedQuestMessage.ParentQuest.OneTime)
                            {
                                var tokens = new List<TextFile.Token> { new TextFile.Token(TextFile.Formatting.Text, $"You cannot cancel one time quests.") };
                                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens.ToArray(), this);
                                messageBox.ClickAnywhereToClose = true;
                                messageBox.AllowCancel = false;
                                messageBox.ParentPanel.BackgroundColor = Color.clear;
                                DaggerfallUI.UIManager.PushWindow(messageBox);
                            }
                            else
                            {
                                var tokens = new List<TextFile.Token> { new TextFile.Token(TextFile.Formatting.Text, $"Are you sure you want to cancel {selectedQuestMessage.ParentQuest.DisplayName}?") };
                                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, tokens.ToArray(), this);
                                messageBox.ClickAnywhereToClose = true;
                                messageBox.AllowCancel = false;
                                messageBox.ParentPanel.BackgroundColor = Color.clear;
                                messageBox.OnButtonClick += CancelQuest_OnButtonClick;
                                DaggerfallUI.UIManager.PushWindow(messageBox);
                            }
                        }
                        else
                        {
                            SetTextForSelectedQuest(selectedQuestMessage);
                        }
                    }
                    else if (entryLineMap[line - 1] == selectedEntry && entryLineMap[line + 1] == selectedEntry)
                    {
                        HandleQuestClicks(groupedQuestMessages[selectedEntry]);
                    }
                }
            }
        }

        protected override void DialogButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            base.DialogButton_OnMouseClick(sender, position);
            currentMessageCheck = defaultMessageCheckValue;
        }

        private string GetTravelTime(Place place)
        {
            DFLocation location;
            DaggerfallUnity.Instance.ContentReader.GetLocation(place.SiteDetails.regionName, place.SiteDetails.locationName, out location);

            if (location.LocationIndex == GameManager.Instance.PlayerGPS.CurrentLocation.LocationIndex)
            {
                return "Current location";
            }

            DFPosition position = MapsFile.LongitudeLatitudeToMapPixel(location.MapTableData.Longitude, location.MapTableData.Latitude);

            if (travelOptionsModEnabled && travelOptionsCautiousTravel && travelOptionsStopAtInnsTravel)
            {
                TransportManager transportManager = GameManager.Instance.TransportManager;
                bool horse = transportManager.TransportMode == TransportModes.Horse;
                bool cart = transportManager.TransportMode == TransportModes.Cart;

                var travelTimeTotalMins = travelTimeCalculator.CalculateTravelTime(position,
                    speedCautious: !travelOptionsCautiousTravel,
                    sleepModeInn: !travelOptionsStopAtInnsTravel,
                    travelShip: false,
                    hasHorse: horse,
                    hasCart: cart);
                travelTimeTotalMins = GameManager.Instance.GuildManager.FastTravel(travelTimeTotalMins);

                //TODO: can make this calc dynamic based on mod settings
                float travelTimeMinsMult = (travelOptionsCautiousTravel ? 0.8f : 1.0f) * 2;
                travelTimeTotalMins = (int)(travelTimeTotalMins / travelTimeMinsMult);
                int travelTimeHours = travelTimeTotalMins / 60;
                int travelTimeMinutes = travelTimeTotalMins % 60;

                return string.Format("{0} hours {1} mins travel", travelTimeHours, travelTimeMinutes);
            }
            else
            {
                bool hasHorse = GameManager.Instance.TransportManager.HasHorse();
                bool hasCart = GameManager.Instance.TransportManager.HasCart();

                //TODO: can consider ship calcs
                //bool hasShip = GameManager.Instance.TransportManager.ShipAvailiable();

                var travelTimeTotalMins = travelTimeCalculator.CalculateTravelTime(position,
                    speedCautious: true,
                    sleepModeInn: true,
                    travelShip: false,
                    hasHorse: hasHorse,
                    hasCart: hasCart);

                // Players can have fast travel benefit from guild memberships
                travelTimeTotalMins = GameManager.Instance.GuildManager.FastTravel(travelTimeTotalMins);

                int travelTimeDaysTotal = (travelTimeTotalMins / 1440);

                // Classic always adds 1. For DF Unity, only add 1 if there is a remainder to round up.
                if ((travelTimeTotalMins % 1440) > 0)
                    travelTimeDaysTotal += 1;

                return $"{travelTimeDaysTotal} days travel";
            }
        }

        private void SetTextActiveQuests()
        {
            selectedQuestDisplayed = false;

            if (questMessages == null)
            {
                return;
            }

            messageCount = questMessages.Count;
            questLogLabel.TextScale = 1;
            titleLabel.Text = TextManager.Instance.GetLocalizedText("activeQuests");
            titleLabel.ToolTipText = "Click on a quest for quest information. Click on a location to travel there.";

            int totalLineCount = 0;
            entryLineMap = new List<int>(maxLinesQuests);
            List<TextFile.Token> textTokens = new List<TextFile.Token>();

            groupedQuestMessages = new List<Message>();
            var parents = questMessages.Select(x => x.ParentQuest).Distinct();

            foreach (var parent in parents)
            {
                var parentMessages = parent.GetLogMessages();
                var lastLogEntry = parentMessages.OrderBy(x => x.stepID).Last();
                var message = questMessages.Single(x => x.ParentQuest == parent && x.ID == lastLogEntry.messageID);
                groupedQuestMessages.Add(message);
            }

            for (int i = currentMessageIndex; i < groupedQuestMessages.Count; i++)
            {
                if (totalLineCount >= maxLinesQuests)
                    break;

                //Get the quest title
                var questTitle = groupedQuestMessages[i].ParentQuest.DisplayName;
                questTitle = FormatQuestTitle(questTitle);

                //Get the time left to complete the quest
                if (questMessages.Where(x => x.ParentQuest == groupedQuestMessages[i].ParentQuest).Any(y => y.Variants.Any(z => z.tokens.Any(a => a.text.Contains("_ days")))))
                {
                    var enabledClocks = new List<Clock>();
                    QuestResource[] clocks = groupedQuestMessages[i].ParentQuest.GetAllResources(typeof(Clock));
                    for (int x = 0; x < clocks.Length; x++)
                    {
                        Clock clock = (Clock)clocks[x];
                        if (clock.Enabled)
                        {
                            enabledClocks.Add(clock);
                        }
                    }

                    if (enabledClocks.Any())
                    {
                        var clockWithLeastTimeLeft = enabledClocks.OrderBy(x => x.RemainingTimeInSeconds).First();
                        var daysTotal = clockWithLeastTimeLeft.GetDaysString(clockWithLeastTimeLeft.RemainingTimeInSeconds);
                        var dayString = daysTotal == "1" ? "day" : "days";
                        questTitle += $" ({daysTotal} {dayString} left)";
                    }
                }

                var tokens = new List<TextFile.Token> { new TextFile.Token(TextFile.Formatting.Text, questTitle) };

                //Display the location and time to travel there if relevant                
                var lastPlaceMentioned = GetLastPlaceMentionedInMessage(groupedQuestMessages[i]);
                var locationAndTravelTime = string.Empty;

                if (!string.IsNullOrWhiteSpace(lastPlaceMentioned?.SiteDetails.locationName))
                {
                    locationAndTravelTime = $"{lastPlaceMentioned.SiteDetails.locationName} ({GetTravelTime(lastPlaceMentioned)})";

                    tokens.Add(TextFile.NewLineToken);
                    tokens.Add(new TextFile.Token(TextFile.Formatting.TextHighlight, locationAndTravelTime));
                }

                //Add a blank line
                tokens.Add(new TextFile.Token(TextFile.Formatting.Nothing, string.Empty));

                for (int j = 0; j < tokens.Count; j++)
                {
                    if (totalLineCount >= maxLinesQuests)
                        break;

                    var token = tokens[j];

                    if (token.formatting == TextFile.Formatting.Text || token.formatting == TextFile.Formatting.NewLine || token.formatting == TextFile.Formatting.TextHighlight)
                    {
                        if (!string.Equals(token.text, locationAndTravelTime))
                        {
                            totalLineCount++;
                            entryLineMap.Add(i);
                        }
                    }
                    else
                        token.formatting = TextFile.Formatting.JustifyLeft;

                    textTokens.Add(token);
                }
                textTokens.Add(TextFile.NewLineToken);
                totalLineCount++;
                entryLineMap.Add(i);
            }

            questLogLabel.SetText(textTokens.ToArray());
        }

        private void SetTextForSelectedQuest(Message message)
        {
            selectedQuestDisplayed = true;

            if (questMessages == null)
            {
                return;
            }

            messageCount = questMessages.Count;
            questLogLabel.TextScale = 1;

            var activeMessages = questMessages.Where(x => x.ParentQuest.UID == message.ParentQuest.UID).ToList();

            var questTitle = activeMessages.First().ParentQuest.DisplayName;
            questTitle = FormatQuestTitle(questTitle);

            titleLabel.Text = questTitle;
            titleLabel.ToolTipText = "Click on a quest mesage to go back to Active Quests.";

            int totalLineCount = 0;
            entryLineMap = new List<int>(maxLinesQuests);
            List<TextFile.Token> textTokens = new List<TextFile.Token>();

            for (int i = currentMessageIndex; i < activeMessages.Count; i++)
            {
                if (totalLineCount >= maxLinesQuests)
                    break;

                var tokens = activeMessages[i].GetTextTokens();

                for (int j = 0; j < tokens.Length; j++)
                {
                    if (totalLineCount >= maxLinesQuests)
                        break;

                    var token = tokens[j];

                    if (token.formatting == TextFile.Formatting.Text || token.formatting == TextFile.Formatting.NewLine)
                    {
                        totalLineCount++;
                        entryLineMap.Add(0);
                    }
                    else
                        token.formatting = TextFile.Formatting.JustifyLeft;

                    textTokens.Add(token);
                }
                textTokens.Add(TextFile.NewLineToken);
                totalLineCount++;
                entryLineMap.Add(0);
            }

            questLogLabel.SetText(textTokens.ToArray());
        }

        private string FormatQuestTitle(string questTitle)
        {
            if (questTitle == "Main Quest Backbone")
            {
                questTitle = questTitle.Replace(" Backbone", string.Empty);
            }

            return string.IsNullOrWhiteSpace(questTitle) ? "Untitled Quest" : questTitle;
        }

        private void CancelQuest_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                QuestMachine.Instance.TombstoneQuest(selectedQuestMessage.ParentQuest);
            }

            sender.CloseWindow();
            DisplayMode = JournalDisplay.ActiveQuests;
            uiManager.PushWindow(this);
        }
    }
}
