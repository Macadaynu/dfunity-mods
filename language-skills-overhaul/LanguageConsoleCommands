using Assets.Scripts.Game.MacadaynuMods;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using System;
using Wenzil.Console;
using Wenzil.Console.Commands;

namespace Assets.Scripts.MacadaynuMods
{
    public static class LanguageSkillCommands
    {
        public static void RegisterCommands()
        {
            try
            {
                ConsoleCommandsDatabase.RegisterCommand(SkillLevel.name, SkillLevel.description, SkillLevel.usage, SkillLevel.Execute);
                ConsoleCommandsDatabase.RegisterCommand(StatLevel.name, StatLevel.description, StatLevel.usage, StatLevel.Execute);
                ConsoleCommandsDatabase.RegisterCommand(TeleportParty.name, TeleportParty.description, TeleportParty.usage, TeleportParty.Execute);
            }
            catch (Exception e)
            {
                DaggerfallUnity.LogMessage(string.Format("Error registering LanguageSkill console commands: {0}", e.Message), true);
            }
        }

        private static class TeleportParty
        {
            public static readonly string name = "teleparty";
            public static readonly string description = "Teleport your party behind you";
            public static readonly string usage = "teleparty";

            public static string Execute(params string[] args)
            {
                LanguageSkillsOverhaulMod.instance.TeleportParty();
                return "Party Teleported";
            }
        }

        private static class SkillLevel
        {
            public static readonly string name = "setskill";
            public static readonly string description = "Set a skill";
            public static readonly string usage = "setskill skillName [skillLevel]";

            public static string Execute(params string[] args)
            {
                if (args.Length == 0)
                {
                    return HelpCommand.Execute(name);
                }
                else
                {
                    // Is the input language a skill?
                    if (Enum.IsDefined(typeof(DFCareer.Skills), args[0]))
                    {
                        DFCareer.Skills skill = (DFCareer.Skills)Enum.Parse(typeof(DFCareer.Skills), args[0]);

                        if (args.Length > 1)
                        {
                            int skillLevel = int.Parse(args[1]);

                            GameManager.Instance.PlayerEntity.Skills.SetPermanentSkillValue(skill, (short)skillLevel);
                            return "Skill level set.";
                        }
                        else
                        {
                            return "Need skill level to set to.";
                        }
                    }
                    else
                    {
                        return "Not a recognised skill, see DFCareer.Skills enum.";
                    }
                }
            }
        }

        private static class StatLevel
        {
            public static readonly string name = "setstat";
            public static readonly string description = "Set a stat";
            public static readonly string usage = "setstat statName [statLevel]";

            public static string Execute(params string[] args)
            {
                if (args.Length == 0)
                {
                    return HelpCommand.Execute(name);
                }
                else
                {
                    // Is the input an attribute?
                    if (Enum.IsDefined(typeof(DFCareer.Stats), args[0]))
                    {
                        DFCareer.Stats stat = (DFCareer.Stats)Enum.Parse(typeof(DFCareer.Stats), args[0]);

                        if (args.Length > 1)
                        {
                            int skillLevel = int.Parse(args[1]);

                            GameManager.Instance.PlayerEntity.Stats.SetPermanentStatValue(stat, (short)skillLevel);
                            return "Stat level set.";
                        }
                        else
                        {
                            return "Need stat level to set to.";
                        }
                    }
                    else
                    {
                        return "Not a recognised stat, see DFCareer.Stats enum.";
                    }
                }
            }
        }
    }
}
