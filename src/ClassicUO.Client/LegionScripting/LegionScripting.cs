using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using ClassicUO.Utility;
using LScript;

namespace ClassicUO.LegionScripting
{
    internal static class LegionScripting
    {
        private const uint MAX_SERIAL = 2147483647;
        private static bool _enabled, _loaded;
        private static string scriptPath;

        private static List<ScriptFile> runningScripts = new List<ScriptFile>();
        private static List<ScriptFile> removeRunningScripts = new List<ScriptFile>();

        public static List<ScriptFile> LoadedScripts = new List<ScriptFile>();

        public static void Init()
        {
            scriptPath = Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts");

            CommandManager.Register("lscript", (args) =>
            {
                if (args.Length == 1)
                {
                    UIManager.Add(new ScriptManagerGump());
                }
            });


            CommandManager.Register("lscriptfile", (args) =>
            {
                if (args.Length < 2)
                    return;

                string file = args[1];

                if (!file.EndsWith(".lscript"))
                    file += ".lscript";

                foreach (ScriptFile script in LoadedScripts)
                {
                    if (script.FileName == file && script.GetScript != null)
                    {
                        PlayScript(script);
                        break;
                    }
                }
            });


            if (!_loaded)
            {
                RegisterCommands();

                EventSink.JournalEntryAdded += EventSink_JournalEntryAdded;
                _loaded = true;
            }

            LoadScriptsFromFile();
            _enabled = true;
        }

        private static void EventSink_JournalEntryAdded(object sender, JournalEntry e)
        {
            foreach (ScriptFile script in runningScripts)
            {
                script.GetScript.JournalEntryAdded(e);
            }
        }

        public static void LoadScriptsFromFile()
        {
            if (!Directory.Exists(scriptPath))
                Directory.CreateDirectory(scriptPath);

            string[] loadedScripts = new string[LoadedScripts.Count];
            int i = 0;

            foreach (ScriptFile script in LoadedScripts)
            {
                loadedScripts[i] = script.FullPath;
                i++;
            }

            foreach (string file in Directory.EnumerateFiles(scriptPath))
            {
                if (file.EndsWith(".lscript"))
                {
                    string p = Path.GetDirectoryName(file);
                    string fname = Path.GetFileName(file);

                    if (!loadedScripts.Contains(file)) //Only add files not already loaded
                        LoadedScripts.Add(new ScriptFile(p, fname));
                }
            }
        }

        public static void Unload()
        {
            runningScripts.Clear();
            Interpreter.ClearAllLists();
            _enabled = false;
        }

        public static void OnUpdate()
        {
            if (!_enabled)
                return;

            foreach (ScriptFile script in runningScripts)
            {
                try
                {
                    if (!Interpreter.ExecuteScript(script.GetScript))
                    {
                        removeRunningScripts.Add(script);
                    }
                }
                catch (Exception e)
                {
                    removeRunningScripts.Add(script);
                    LScriptError($"Execution of script failed. -> [{e.Message}]");
                }
            }

            if (removeRunningScripts.Count > 0)
            {
                foreach (ScriptFile script in removeRunningScripts)
                    StopScript(script);

                removeRunningScripts.Clear();
            }
        }

        public static void PlayScript(ScriptFile script)
        {
            if (script != null)
            {
                script.GenerateScript();
                runningScripts.Add(script);
                script.GetScript.IsPlaying = true;
            }
        }

        public static void StopScript(ScriptFile script)
        {
            if (runningScripts.Contains(script))
                runningScripts.Remove(script);

            script.GetScript.IsPlaying = false;
        }

        private static IComparable DummyExpression(string expression, Argument[] args, bool quiet)
        {
            Console.WriteLine("Executing expression {0} {1}", expression, args);

            return 0;
        }

        private static bool DummyCommand(string command, Argument[] args, bool quiet, bool force)
        {
            Console.WriteLine("Executing command {0} {1}", command, args);

            return true;
        }

        private static bool MsgCommand(string command, Argument[] args, bool quiet, bool force)
        {
            string msg = "";

            foreach (Argument arg in args)
            {
                msg += " " + arg.AsString();
            }

            GameActions.Say(msg, ProfileManager.CurrentProfile.SpeechHue);

            return true;
        }

        private static bool CreateList(string command, Argument[] args, bool quiet, bool force)
        {
            Console.WriteLine("Creating list {0}", args[0].AsString());

            Interpreter.CreateList(args[0].AsString());

            return true;
        }

        private static bool PushList(string command, Argument[] args, bool quiet, bool force)
        {
            Console.WriteLine("Pushing {0} to list {1}", args[1].AsString(), args[0].AsString());

            Interpreter.PushList(args[0].AsString(), args[1], true, false);

            return true;
        }

        private static uint DefaultAlias(string alias)
        {
            if (World.InGame)
                switch (alias)
                {
                    case "backpack": return World.Player.FindItemByLayer(Layer.Backpack);
                    case "bank": return World.Player.FindItemByLayer(Layer.Bank);
                    case "lastobject": return World.LastObject;
                    case "lasttarget": return TargetManager.LastTargetInfo.Serial;
                    case "lefthand": return World.Player.FindItemByLayer(Layer.OneHanded);
                    case "righthand": return World.Player.FindItemByLayer(Layer.TwoHanded);
                    case "self": return World.Player;
                    case "mount": return World.Player.FindItemByLayer(Layer.Mount);
                    case "bandage": return World.Player.FindBandage();
                    case "any": return MAX_SERIAL;
                    case "anycolor": return ushort.MaxValue;
                }

            return 0;
        }

        private static void RegisterCommands()
        {
            #region Commands
            //Finished
            Interpreter.RegisterCommandHandler("togglefly", CommandFly);
            Interpreter.RegisterCommandHandler("useprimaryability", UsePrimaryAbility);
            Interpreter.RegisterCommandHandler("usesecondaryability", UseSecondaryAbility);
            Interpreter.RegisterCommandHandler("attack", CommandAttack);
            Interpreter.RegisterCommandHandler("clickobject", ClickObject);
            Interpreter.RegisterCommandHandler("bandageself", BandageSelf);
            Interpreter.RegisterCommandHandler("useobject", UseObject);
            Interpreter.RegisterCommandHandler("target", TargetSerial);
            Interpreter.RegisterCommandHandler("waitfortarget", WaitForTarget);
            Interpreter.RegisterCommandHandler("usetype", UseType);
            Interpreter.RegisterCommandHandler("pause", PauseCommand);
            Interpreter.RegisterCommandHandler("useskill", UseSkill);
            Interpreter.RegisterCommandHandler("walk", CommandWalk);
            Interpreter.RegisterCommandHandler("run", CommandRun);
            Interpreter.RegisterCommandHandler("canceltarget", CancelTarget);
            Interpreter.RegisterCommandHandler("sysmsg", SystemMessage);
            Interpreter.RegisterCommandHandler("moveitem", MoveItem);
            Interpreter.RegisterCommandHandler("moveitemoffset", MoveItemOffset);
            Interpreter.RegisterCommandHandler("cast", CastSpell);
            Interpreter.RegisterCommandHandler("waitforjournal", WaitForJournal);
            Interpreter.RegisterCommandHandler("settimer", SetTimer);
            Interpreter.RegisterCommandHandler("setalias", SetAlias);
            Interpreter.RegisterCommandHandler("unsetalias", UnsetAlias);
            Interpreter.RegisterCommandHandler("movetype", MoveType);
            Interpreter.RegisterCommandHandler("removetimer", RemoveTimer);
            Interpreter.RegisterCommandHandler("msg", MsgCommand);
            Interpreter.RegisterCommandHandler("toggleautoloot", ToggleAutoLoot);



            //Unfinished below
            Interpreter.RegisterCommandHandler("movetypeoffset", DummyCommand);
            Interpreter.RegisterCommandHandler("turn", DummyCommand);
            Interpreter.RegisterCommandHandler("feed", DummyCommand);
            Interpreter.RegisterCommandHandler("rename", DummyCommand);
            Interpreter.RegisterCommandHandler("shownames", DummyCommand);
            Interpreter.RegisterCommandHandler("togglehands", DummyCommand);
            Interpreter.RegisterCommandHandler("equipitem", DummyCommand);
            Interpreter.RegisterCommandHandler("togglemounted", DummyCommand);
            Interpreter.RegisterCommandHandler("equipwand", DummyCommand);
            Interpreter.RegisterCommandHandler("buy", DummyCommand);
            Interpreter.RegisterCommandHandler("sell", DummyCommand);
            Interpreter.RegisterCommandHandler("clearbuy", DummyCommand);
            Interpreter.RegisterCommandHandler("clearsell", DummyCommand);
            Interpreter.RegisterCommandHandler("organizer", DummyCommand);
            Interpreter.RegisterCommandHandler("dress", DummyCommand);
            Interpreter.RegisterCommandHandler("undress", DummyCommand);
            Interpreter.RegisterCommandHandler("dressconfig", DummyCommand);
            Interpreter.RegisterCommandHandler("toggleautoloot", DummyCommand);
            Interpreter.RegisterCommandHandler("togglescavenger", DummyCommand);
            Interpreter.RegisterCommandHandler("counter", DummyCommand);
            Interpreter.RegisterCommandHandler("promptalias", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforgump", DummyCommand);
            Interpreter.RegisterCommandHandler("replygump", DummyCommand);
            Interpreter.RegisterCommandHandler("closegump", DummyCommand);
            Interpreter.RegisterCommandHandler("clearjournal", DummyCommand);
            Interpreter.RegisterCommandHandler("poplist", DummyCommand);
            Interpreter.RegisterCommandHandler("pushlist", PushList);
            Interpreter.RegisterCommandHandler("removelist", DummyCommand);
            Interpreter.RegisterCommandHandler("createlist", CreateList);
            Interpreter.RegisterCommandHandler("clearlist", DummyCommand);
            Interpreter.RegisterCommandHandler("info", DummyCommand);
            Interpreter.RegisterCommandHandler("ping", DummyCommand);
            Interpreter.RegisterCommandHandler("playmacro", DummyCommand);
            Interpreter.RegisterCommandHandler("playsound", DummyCommand);
            Interpreter.RegisterCommandHandler("resync", DummyCommand);
            Interpreter.RegisterCommandHandler("snapshot", DummyCommand);
            Interpreter.RegisterCommandHandler("hotkeys", DummyCommand);
            Interpreter.RegisterCommandHandler("where", DummyCommand);
            Interpreter.RegisterCommandHandler("clickscreen", DummyCommand);
            Interpreter.RegisterCommandHandler("paperdoll", DummyCommand);
            Interpreter.RegisterCommandHandler("helpbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("guildbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("questsbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("logoutbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("virtue", DummyCommand);
            Interpreter.RegisterCommandHandler("headmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("partymsg", DummyCommand);
            Interpreter.RegisterCommandHandler("guildmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("allymsg", DummyCommand);
            Interpreter.RegisterCommandHandler("whispermsg", DummyCommand);
            Interpreter.RegisterCommandHandler("yellmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("chatmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("emotemsg", DummyCommand);
            Interpreter.RegisterCommandHandler("promptmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("timermsg", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforprompt", DummyCommand);
            Interpreter.RegisterCommandHandler("cancelprompt", DummyCommand);
            Interpreter.RegisterCommandHandler("addfriend", DummyCommand);
            Interpreter.RegisterCommandHandler("removefriend", DummyCommand);
            Interpreter.RegisterCommandHandler("contextmenu", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforcontext", DummyCommand);
            Interpreter.RegisterCommandHandler("ignoreobject", DummyCommand);
            Interpreter.RegisterCommandHandler("clearignorelist", DummyCommand);
            Interpreter.RegisterCommandHandler("setskill", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforproperties", DummyCommand);
            Interpreter.RegisterCommandHandler("autocolorpick", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforcontents", DummyCommand);
            Interpreter.RegisterCommandHandler("targettype", DummyCommand);
            Interpreter.RegisterCommandHandler("targetground", DummyCommand);
            Interpreter.RegisterCommandHandler("targettile", DummyCommand);
            Interpreter.RegisterCommandHandler("targettileoffset", DummyCommand);
            Interpreter.RegisterCommandHandler("targettilerelative", DummyCommand);
            Interpreter.RegisterCommandHandler("cleartargetqueue", DummyCommand);
            #endregion

            #region Expressions
            //Finished
            Interpreter.RegisterExpressionHandler("timerexists", TimerExists);
            Interpreter.RegisterExpressionHandler("timerexpired", TimerExpired);
            Interpreter.RegisterExpressionHandler("findtype", FindType);
            Interpreter.RegisterExpressionHandler("findalias", FindAlias);
            Interpreter.RegisterExpressionHandler("skill", SkillValue);
            Interpreter.RegisterExpressionHandler("poisoned", PoisonedStatus);
            Interpreter.RegisterExpressionHandler("war", CheckWar);


            //Unfinished
            Interpreter.RegisterExpressionHandler("contents", DummyExpression);
            Interpreter.RegisterExpressionHandler("findobject", DummyExpression);
            Interpreter.RegisterExpressionHandler("distance", DummyExpression);
            Interpreter.RegisterExpressionHandler("inrange", DummyExpression);
            Interpreter.RegisterExpressionHandler("buffexists", DummyExpression);
            Interpreter.RegisterExpressionHandler("property", DummyExpression);
            Interpreter.RegisterExpressionHandler("findlayer", DummyExpression);
            Interpreter.RegisterExpressionHandler("skillstate", DummyExpression);
            Interpreter.RegisterExpressionHandler("counttype", DummyExpression);
            Interpreter.RegisterExpressionHandler("counttypeground", DummyExpression);
            Interpreter.RegisterExpressionHandler("findwand", DummyExpression);
            Interpreter.RegisterExpressionHandler("inparty", DummyExpression);
            Interpreter.RegisterExpressionHandler("infriendslist", DummyExpression);
            Interpreter.RegisterExpressionHandler("ingump", DummyExpression);
            Interpreter.RegisterExpressionHandler("gumpexists", DummyExpression);
            Interpreter.RegisterExpressionHandler("injournal", DummyExpression);
            Interpreter.RegisterExpressionHandler("listexists", DummyExpression);
            Interpreter.RegisterExpressionHandler("list", DummyExpression);
            Interpreter.RegisterExpressionHandler("inlist", DummyExpression);
            #endregion

            #region Player Values
            //Finished
            Interpreter.RegisterExpressionHandler("mana", GetPlayerMana);
            Interpreter.RegisterExpressionHandler("maxmana", GetPlayerMaxMana);
            Interpreter.RegisterExpressionHandler("hits", GetPlayerHits);
            Interpreter.RegisterExpressionHandler("maxhits", GetPlayerMaxHits);
            Interpreter.RegisterExpressionHandler("stam", GetPlayerStam);
            Interpreter.RegisterExpressionHandler("maxstam", GetPlayerMaxStam);
            Interpreter.RegisterExpressionHandler("x", GetPosX);
            Interpreter.RegisterExpressionHandler("y", GetPosY);
            Interpreter.RegisterExpressionHandler("z", GetPosZ);
            Interpreter.RegisterExpressionHandler("name", GetPlayerName);
            Interpreter.RegisterExpressionHandler("true", GetTrue);
            Interpreter.RegisterExpressionHandler("false", GetFalse);
            #endregion

            #region Default aliases
            //Finished
            Interpreter.RegisterAliasHandler("backpack", DefaultAlias);
            Interpreter.RegisterAliasHandler("bank", DefaultAlias);
            Interpreter.RegisterAliasHandler("lastobject", DefaultAlias);
            Interpreter.RegisterAliasHandler("lasttarget", DefaultAlias);
            Interpreter.RegisterAliasHandler("lefthand", DefaultAlias);
            Interpreter.RegisterAliasHandler("righthand", DefaultAlias);
            Interpreter.RegisterAliasHandler("self", DefaultAlias);
            Interpreter.RegisterAliasHandler("mount", DefaultAlias);
            Interpreter.RegisterAliasHandler("bandage", DefaultAlias);
            Interpreter.RegisterAliasHandler("any", DefaultAlias);
            Interpreter.RegisterAliasHandler("anycolor", DefaultAlias);
            #endregion
        }

        private static bool ToggleAutoLoot(string command, Argument[] args, bool quiet, bool force)
        {
            ProfileManager.CurrentProfile.EnableAutoLoot = !ProfileManager.CurrentProfile.EnableAutoLoot;
            return true;
        }

        private static bool CheckWar(string expression, Argument[] args, bool quiet)
        {
            return World.Player.InWarMode;
        }

        private static bool RemoveTimer(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: removetimer 'timer name'");

            Interpreter.RemoveTimer(args[0].AsString());

            return true;
        }

        private static bool PoisonedStatus(string expression, Argument[] args, bool quiet)
        {
            uint serial = args.Length > 0 ? args[0].AsSerial() : World.Player;

            if (World.Mobiles.TryGetValue(serial, out var m))
            {
                return m.IsPoisoned;
            }

            return false;
        }

        private static double SkillValue(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: skill 'name' [true/false]");

            bool force = args.Length > 1 ? args[1].AsBool() : false;

            for (int i = 0; i < World.Player.Skills.Length; i++)
            {
                if (World.Player.Skills[i].Name.ToLower().Contains(args[0].AsString()))
                {
                    return force ? World.Player.Skills[World.Player.Skills[i].Index].Base : World.Player.Skills[World.Player.Skills[i].Index].Value;
                }
            }

            if (!quiet) LScriptError($"Skill {args[0].AsString()} not found!");
            return 0;
        }

        private static bool FindAlias(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: findalias 'name'");

            uint foundVal = Interpreter.GetAlias(args[0].AsString());

            if (foundVal != uint.MaxValue)
            {
                Interpreter.SetAlias("found", foundVal);
                return true;
            }
            try
            {
                if (World.Items.TryGetValue(args[0].AsSerial(), out Item i))
                {
                    Interpreter.SetAlias("found", i);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return false;
        }

        private static uint FindType(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: findtype 'graphic' 'source' [color] [range]");

            uint gfx = args[0].AsUInt();
            uint source = args[1].AsSerial();

            if (source == MAX_SERIAL) source = uint.MaxValue;

            ushort hue = args.Length >= 3 ? args[2].AsUShort() : ushort.MaxValue;
            int range = args.Length >= 4 ? args[3].AsInt() : int.MaxValue;


            List<Item> items = Utility.FindItems(gfx, parOrRootContainer: source, hue: hue, groundRange: range);

            if (items.Count > 0)            
                Interpreter.SetAlias("found", items[0]);                         

            return (uint)items.Count;
        }

        private static bool MoveType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 3)
                throw new RunTimeError(null, "Usage: movetype 'graphic' 'source' 'destination'  [amount] [color] [range]");

            uint gfx = args[0].AsUInt();
            uint source = args[1].AsSerial();
            uint target = args[2].AsSerial();

            int amount = args.Length >= 4 ? args[3].AsInt() : -1;
            ushort hue = args.Length >= 5 ? args[4].AsUShort() : ushort.MaxValue;
            int range = args.Length >= 6 ? args[5].AsInt() : 2;

            foreach (Item item in World.Items.Values)
            {
                if (source == MAX_SERIAL || item.Container == source || item.RootContainer == source)
                {
                    if (item.Graphic != gfx || item.Container == target || item.RootContainer == target)
                        continue;

                    if (source == MAX_SERIAL && item.Distance > range)
                        continue;

                    if (hue != ushort.MaxValue)
                    {
                        if (item.Hue == hue && GameActions.PickUp(item, 0, 0, amount < 1 ? item.Amount : amount))
                        {
                            GameActions.DropItem(item, 0xFFFF, 0xFFFF, 0, target);
                            return true;
                        }
                    }
                    else
                    {
                        if (GameActions.PickUp(item, 0, 0, amount < 1 ? item.Amount : amount))
                            GameActions.DropItem(item, 0xFFFF, 0xFFFF, 0, target);
                        return true;
                    }
                }
            }

            return true;
        }

        private static bool UnsetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: unsetalias 'name'");

            Interpreter.RemoveAlias(args[0].AsString());

            return true;
        }

        private static bool SetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: setalias 'name' 'serial'");

            string name = args[0].AsString();
            uint val = args[1].AsSerial();

            Interpreter.SetAlias(name, val);

            return true;
        }

        private static bool TimerExpired(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: timerexpired 'timer name'");

            return Interpreter.TimerExpired(args[0].AsString());
        }

        private static bool TimerExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: timerexists 'timer name'");

            return Interpreter.TimerExists(args[0].AsString());
        }

        private static bool SetTimer(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: settimer 'timer name' 'duration'");

            Interpreter.SetTimer(args[0].AsString(), args[1].AsInt());

            return true;
        }

        public static bool ReturnTrue() //Avoids creating a bunch of functions that need to be GC'ed
        {
            return true;
        }

        private static bool WaitForJournal(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: waitforjournal 'search text' 'duration'");

            if (Interpreter.ActiveScript.SearchJournalEntries(args[0].AsString()))
                return true;

            Interpreter.Timeout(args.Length >= 2 ? args[1].AsInt() : 10000, ReturnTrue);

            return false;
        }

        private static bool CastSpell(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: cast 'spell name'");

            GameActions.CastSpellByName(args[0].AsString());

            return true;
        }

        private static bool MoveItemOffset(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 5)
                throw new RunTimeError(null, "Usage: moveitemoffset 'item' 'amt' 'x' 'y' 'z'");

            uint item = args[0].AsSerial();
            int amt = args[1].AsInt();
            int x = args[2].AsInt();
            int y = args[3].AsInt();
            int z = args[4].AsInt();


            GameActions.PickUp(item, 0, 0, amt);
            GameActions.DropItem
            (
                item,
                World.Player.X + x,
                World.Player.Y + y,
                World.Player.Z + z,
                0
            );

            return true;
        }

        private static bool MoveItem(string command, Argument[] args, bool quiet, bool force)
        {

            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: moveitem 'item' 'container'");

            uint item = args[0].AsSerial();

            uint bag = args[1].AsSerial();

            ushort amt = 0;
            if (args.Length > 2)
                amt = args[2].AsUShort();

            GameActions.GrabItem(item, amt, bag, !force);
            return true;
        }

        private static bool SystemMessage(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: sysmsg 'message text' 'hue'");

            string msg = args[0].AsString();

            ushort hue = 946;

            if (args.Length > 1)
                hue = args[1].AsUShort();


            GameActions.Print(msg, hue);
            return true;
        }

        private static bool CancelTarget(string command, Argument[] args, bool quiet, bool force)
        {
            if (TargetManager.IsTargeting)
                TargetManager.CancelTarget();

            return true;
        }

        private static bool CommandRun(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: run 'direction'");

            string dir = args[0].AsString().ToLower();
            Direction d = Direction.North;

            switch (dir)
            {
                case "north": d = Direction.North; break;
                case "right": d = Direction.Right; break;
                case "east": d = Direction.East; break;
                case "down": d = Direction.Down; break;
                case "south": d = Direction.South; break;
                case "left": d = Direction.Left; break;
                case "west": d = Direction.West; break;
                case "up": d = Direction.Up; break;
            }

            World.Player.Walk(d, true);

            return true;
        }

        private static bool CommandWalk(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: walk 'direction'");

            string dir = args[0].AsString().ToLower();
            Direction d = Direction.North;

            switch (dir)
            {
                case "north": d = Direction.North; break;
                case "right": d = Direction.Right; break;
                case "east": d = Direction.East; break;
                case "down": d = Direction.Down; break;
                case "south": d = Direction.South; break;
                case "left": d = Direction.Left; break;
                case "west": d = Direction.West; break;
                case "up": d = Direction.Up; break;
            }

            World.Player.Walk(d, false);

            return true;
        }

        private static bool UseSkill(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: useskill 'skill name'");

            string skill = args[0].AsString().Trim().ToLower();

            if (skill.Length > 0)
            {
                for (int i = 0; i < World.Player.Skills.Length; i++)
                {
                    if (World.Player.Skills[i].Name.ToLower().Contains(skill))
                    {
                        GameActions.UseSkill(World.Player.Skills[i].Index);
                        break;
                    }
                }
            }

            return true;
        }

        private static bool PauseCommand(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: pause 'durtion'");

            int ms = args[0].AsInt();

            Interpreter.Pause(ms);
            return true;
        }

        private static bool UseType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: usetype 'container' 'graphic' 'hue'");

            Item container = World.Items.Get(args[0].AsSerial());

            if (container == null && args[0].AsSerial() != MAX_SERIAL) return true;

            uint objType = args[1].AsUInt();

            uint hue = uint.MaxValue;

            if (args.Length >= 3)
                hue = args[2].AsUInt();

            if (container == null)
                foreach (Item it in World.Items.Values)
                {
                    if (it == null) continue;

                    if (it.Graphic == objType || it.DisplayedGraphic == objType)
                    {
                        if (hue != uint.MaxValue)
                            if (it.Hue == hue)
                                GameActions.DoubleClickQueued(it);
                            else
                                GameActions.DoubleClickQueued(it);
                    }
                }
            else
                for (LinkedObject i = container.Items; i != null; i = i.Next)
                {
                    Item it = (Item)i;
                    if (it == null) continue;

                    if (it.Graphic == objType || it.DisplayedGraphic == objType)
                    {
                        if (hue != uint.MaxValue)
                            if (it.Hue == hue)
                                GameActions.DoubleClickQueued(it);
                            else
                                GameActions.DoubleClickQueued(it);
                    }
                }

            return true;
        }

        private static bool WaitForTarget(string command, Argument[] args, bool quiet, bool force)
        {
            TargetType type = TargetType.Neutral;

            if (args.Length >= 1)
            {
                type = (TargetType)args[0].AsInt();
            }

            Interpreter.Timeout(args.Length >= 2 ? args[1].AsInt() : 10000, ReturnTrue);

            if (TargetManager.IsTargeting)
            {
                if (type == TargetType.Neutral)
                    return true;

                if (TargetManager.TargetingType == type)
                    return true;
            }

            return false;
        }

        private static bool TargetSerial(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: target 'serial'");

            TargetManager.Target(args[0].AsSerial());

            return true;
        }

        private static bool UseObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: useobject 'serial' 'true/false'");

            bool useQueue = true;

            if (args.Length >= 2)
                if (args[1].AsBool())
                    useQueue = true;
                else
                    useQueue = false;

            if (useQueue)
                GameActions.DoubleClickQueued(args[0].AsSerial());
            else
                GameActions.DoubleClick(args[0].AsSerial());

            return true;
        }

        private static bool BandageSelf(string command, Argument[] args, bool quiet, bool force)
        {
            if (Client.Version < ClientVersion.CV_5020 || ProfileManager.CurrentProfile.BandageSelfOld)
            {
                Item band = World.Player.FindBandage();

                if (band != null)
                {
                    GameActions.DoubleClickQueued(band);
                }
            }
            else
            {
                GameActions.BandageSelf();
            }

            return true;
        }

        private static bool ClickObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: clickobject 'serial'");

            GameActions.SingleClick(args[0].AsSerial());
            return true;
        }

        private static bool CommandAttack(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: attack 'serial'");

            GameActions.Attack(args[0].AsSerial());
            return true;
        }

        private static bool UseSecondaryAbility(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.UsePrimaryAbility();
            return true;
        }

        private static bool UsePrimaryAbility(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.UseSecondaryAbility();
            return true;
        }

        private static void LScriptError(string msg)
        {
            GameActions.Print("[LScript Error]" + msg);
        }

        private static bool CommandFly(string command, Argument[] args, bool quiet, bool force)
        {
            if (World.Player.Race == RaceType.GARGOYLE)
            {
                NetClient.Socket.Send_ToggleGargoyleFlying();
                return true;
            }

            if (!quiet)
                LScriptError("Player is not a gargoyle, cannot fly.");

            return true;
        }

        private static int GetPlayerMaxStam(string expression, Argument[] args, bool quiet) => World.Player.StaminaMax;
        private static int GetPlayerStam(string expression, Argument[] args, bool quiet) => World.Player.Stamina;
        private static int GetPlayerMaxHits(string expression, Argument[] args, bool quiet) => World.Player.HitsMax;
        private static int GetPlayerHits(string expression, Argument[] args, bool quiet) => World.Player.Hits;
        private static int GetPlayerMaxMana(string expression, Argument[] args, bool quiet) => World.Player.ManaMax;
        private static int GetPlayerMana(string expression, Argument[] args, bool quiet) => World.Player.Mana;
        private static int GetPosX(string expression, Argument[] args, bool quiet) => World.Player.X;
        private static int GetPosY(string expression, Argument[] args, bool quiet) => World.Player.Y;
        private static int GetPosZ(string expression, Argument[] args, bool quiet) => World.Player.Z;
        private static string GetPlayerName(string expression, Argument[] args, bool quiet) => World.Player.Name;
        private static bool GetFalse(string expression, Argument[] args, bool quiet) => false;
        private static bool GetTrue(string expression, Argument[] args, bool quiet) => true;
    }

    internal class ScriptFile
    {
        public string Path;
        public string FileName;
        public string FullPath;
        public Script GetScript;

        public ScriptFile(string path, string fileName)
        {
            Path = path;
            FileName = fileName;
            FullPath = System.IO.Path.Combine(Path, FileName);
            GenerateScript();
        }

        public void GenerateScript()
        {
            try
            {
                if (GetScript == null)
                    GetScript = new Script(Lexer.Lex(FullPath));
                else
                    GetScript.UpdateScript(Lexer.Lex(FullPath));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
