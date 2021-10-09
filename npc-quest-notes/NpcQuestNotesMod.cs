using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Questing.Actions;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.MacadaynuMods.QuestInfoRecorder
{
    public class NpcQuestNotesMod : MonoBehaviour
    {
        public static Mod mod;
        public static NpcQuestNotesMod instance;
        public HashSet<DaggerfallWorkshop.Utility.Tuple<ulong, Symbol>> recordedQuestNotes;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            GameObject go = new GameObject("NpcQuestNotesMod");
            instance = go.AddComponent<NpcQuestNotesMod>();
            mod = initParams.Mod;
            mod.IsReady = true;

            //initiates save paramaters for class/script.
            //mod.SaveDataInterface = instance;
        }

        public void Start()
        {
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChangeHandler;
            QuestMachine.OnQuestEnded += QuestMachine_OnQuestEnded;
            recordedQuestNotes = new HashSet<DaggerfallWorkshop.Utility.Tuple<ulong, Symbol>>();
        }

        // TODO: Merge GetSayId and GetTotingItemAndClickedNPCId into one method?
        int GetSayId(Quest quest, string source)
        {
            int sayId;

            // Factory new say
            Say say = new Say(quest);

            // Source must match pattern
            Match match = Regex.Match(source, say.Pattern);
            if (!match.Success)
                return 0; // return null?

            sayId = Parser.ParseInt(match.Groups["id"].Value);

            // Resolve static message back to ID
            string idName = match.Groups["idName"].Value;
            if (sayId == 0 && !string.IsNullOrEmpty(idName))
            {
                Table table = QuestMachine.Instance.StaticMessagesTable;
                sayId = Parser.ParseInt(table.GetValue("id", idName));
            }

            return sayId;
        }

        int GetEndQuestId(Quest quest, string source)
        {
            // Factory new action
            EndQuest endQuest = new EndQuest(quest);

            // Source must match pattern
            Match match = Regex.Match(source, endQuest.Pattern);
            if (!match.Success)
                return 0; //return null?

            return Parser.ParseInt(match.Groups["id"].Value);
        }

        int GetTotingItemAndClickedNPCId(Quest quest, string source)
        {
            int totingItemAndClickedNpcId;

            // Factory new action
            TotingItemAndClickedNpc totingItemAndClickedNpc = new TotingItemAndClickedNpc(quest);

            // Source must match pattern
            Match match = Regex.Match(source, totingItemAndClickedNpc.Pattern);
            if (!match.Success)
                return 0; //return null?

            //action.itemSymbol = new Symbol(match.Groups["anItem"].Value);
            //action.npcSymbol = new Symbol(match.Groups["anNPC"].Value);
            totingItemAndClickedNpcId = Parser.ParseInt(match.Groups["id"].Value);

            // Resolve static message back to ID
            string idName = match.Groups["idName"].Value;
            if (totingItemAndClickedNpcId == 0 && !string.IsNullOrEmpty(idName))
            {
                Table table = QuestMachine.Instance.StaticMessagesTable;
                totingItemAndClickedNpcId = Parser.ParseInt(table.GetValue("id", idName));
            }

            return totingItemAndClickedNpcId;
        }

        int GetGivePcId(Quest quest, string source)
        {
            // Factory new action
            GivePc givePc = new GivePc(quest);

            // Source must match pattern
            Match match = Regex.Match(source, givePc.Pattern);
            if (!match.Success)
                return 0;

            return Parser.ParseInt(match.Groups["id"].Value);
        }

        void AddQuestTextToNotebook(Message questMessage, ulong questUID, Symbol symbol, string questName)
        {
            // TODO: Find out how to add the NPC name?

            questName = questName.Replace("Main Quest Backbone", "Main Quest");

            var tokens = questMessage.GetTextTokens();

            tokens[0].text = $"{questName}: {tokens[0].text}";

            var tokensList = tokens.ToList();

            tokensList.RemoveAll(x => string.IsNullOrWhiteSpace(x.text));

            GameManager.Instance.PlayerEntity.Notebook.AddNote(tokensList);

            recordedQuestNotes.Add(new DaggerfallWorkshop.Utility.Tuple<ulong, Symbol>(questUID, symbol));
        }

        // Triggered when a UI window is opened/closed
        public void UIManager_OnWindowChangeHandler(object sender, EventArgs e)
        {
            // TODO: not great for performance, maybe move this check as an override of the note log?
            // and then insert the notes when the user opens the notebook? (location on the note then won't work)
            if (DaggerfallUI.UIManager.WindowCount > 0
                && DaggerfallUI.UIManager.TopWindow.GetType() == typeof(DaggerfallMessageBox))
            {
                ulong[] uids = QuestMachine.Instance.GetAllActiveQuests();
                foreach (ulong questUID in uids)
                {
                    Quest quest = QuestMachine.Instance.GetQuest(questUID);
                    // if quest is a main quest
                    //TODO: Make the Brisiena quest optional
                    if (quest != null && (quest.QuestName.StartsWith("S0000") || quest.QuestName.StartsWith("_BRISIEN")))
                    {
                        // get all triggered tasks for this quest
                        var triggeredTaskStates = quest.GetTaskStates().Where(x => x.set).ToList(); // remove ToList after debugging

                        foreach (var taskState in triggeredTaskStates)
                        {
                            // if the task note has not already been recorded
                            if (!recordedQuestNotes.Where(x => x.First == questUID && x.Second.Equals(taskState.symbol)).Any())
                            {
                                var task = quest.GetTask(taskState.symbol);
                                foreach (var action in task.Actions)
                                {
                                    Message questMessage;

                                    switch (action)
                                    {
                                        case Say say:
                                            questMessage = quest.GetMessage(GetSayId(quest, action.DebugSource));
                                            break;
                                        //case EndQuest endQuest:
                                        //    questMessage = quest.GetMessage(GetEndQuestId(quest, action.DebugSource));
                                        //    break;
                                        case TotingItemAndClickedNpc totingItemAndClickedNpc:
                                            questMessage = quest.GetMessage(GetTotingItemAndClickedNPCId(quest, action.DebugSource));
                                            break;
                                        //case GivePc givePc:
                                        //    questMessage = quest.GetMessage(GetGivePcId(quest, action.DebugSource));
                                        //    break;
                                        default:
                                            continue;
                                            //break;
                                    }

                                    AddQuestTextToNotebook(questMessage, questUID, taskState.symbol, quest.DisplayName);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void QuestMachine_OnQuestEnded(Quest quest)
        {
            // TODO: check the quest note isn't already in the collection
            if (quest.QuestSuccess && quest.QuestName.StartsWith("S0000"))
            {
                Message message = quest.GetMessage((int)QuestMachine.QuestMessages.QuestComplete);

                AddQuestTextToNotebook(message, quest.UID, new Symbol { Original = "QuestComplete" }, quest.DisplayName);
            }
        }
    }
}