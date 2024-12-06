using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility;
using LScript;

namespace ClassicUO.LegionScripting
{
    internal static class LegionScripting
    {
        private const uint MAX_SERIAL = 2147483647;
        private static bool _enabled, _loaded;
        public static string ScriptPath;

        private static List<ScriptFile> runningScripts = new List<ScriptFile>();
        private static List<ScriptFile> removeRunningScripts = new List<ScriptFile>();

        public static List<ScriptFile> LoadedScripts = new List<ScriptFile>();

        private static LScriptSettings lScriptSettings;

        public static void Init()
        {
            ScriptPath = Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts");

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
            LoadLScriptSettings();
            AutoPlayGlobal();
            AutoPlayChar();
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
            if (!Directory.Exists(ScriptPath))
                Directory.CreateDirectory(ScriptPath);

            string[] loadedScripts = new string[LoadedScripts.Count];
            int i = 0;

            foreach (ScriptFile script in LoadedScripts)
            {
                loadedScripts[i] = script.FullPath;
                i++;
            }

            foreach (string file in Directory.EnumerateFiles(ScriptPath))
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

        public static void SetAutoPlay(ScriptFile script, bool global, bool enabled)
        {
            if (global)
            {
                if (enabled)
                {
                    if (!lScriptSettings.GlobalAutoStartScripts.Contains(script.FileName))
                        lScriptSettings.GlobalAutoStartScripts.Add(script.FileName);
                }
                else
                {
                    lScriptSettings.GlobalAutoStartScripts.Remove(script.FileName);
                }

            }
            else
            {
                if (lScriptSettings.CharAutoStartScripts.ContainsKey(GetAccountCharName()))
                {
                    if (enabled)
                    {
                        if (!lScriptSettings.CharAutoStartScripts[GetAccountCharName()].Contains(script.FileName))
                            lScriptSettings.CharAutoStartScripts[GetAccountCharName()].Add(script.FileName);
                    }
                    else
                        lScriptSettings.CharAutoStartScripts[GetAccountCharName()].Remove(script.FileName);
                }
                else
                {
                    if (enabled)
                        lScriptSettings.CharAutoStartScripts.Add(GetAccountCharName(), new List<string> { script.FileName });
                }
            }
        }

        public static bool AutoLoadEnabled(ScriptFile script, bool global)
        {
            if (!_enabled)
                return false;

            if (global)
                return lScriptSettings.GlobalAutoStartScripts.Contains(script.FileName);
            else
            {
                if (lScriptSettings.CharAutoStartScripts.TryGetValue(GetAccountCharName(), out var scripts))
                {
                    return scripts.Contains(script.FileName);
                }
            }

            return false;
        }

        private static void AutoPlayGlobal()
        {
            foreach (string script in lScriptSettings.GlobalAutoStartScripts)
            {
                foreach (ScriptFile f in LoadedScripts)
                    if (f.FileName == script)
                        PlayScript(f);
            }
        }

        private static string GetAccountCharName()
        {
            return ProfileManager.CurrentProfile.Username + ProfileManager.CurrentProfile.CharacterName;
        }

        private static void AutoPlayChar()
        {
            if (World.Player == null)
                return;

            if (lScriptSettings.CharAutoStartScripts.TryGetValue(GetAccountCharName(), out var scripts))
                foreach (ScriptFile f in LoadedScripts)
                    if (scripts.Contains(f.FileName))
                        PlayScript(f);

        }

        private static void LoadLScriptSettings()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "lscript.json");

            try
            {
                if (File.Exists(path))
                {
                    lScriptSettings = JsonSerializer.Deserialize<LScriptSettings>(File.ReadAllText(path));
                    return;
                }
            }
            catch (Exception e) { }

            lScriptSettings = new LScriptSettings();
        }

        private static void SaveScriptSettings()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "lscript.json");

            string json = JsonSerializer.Serialize(lScriptSettings);
            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception e) { }
        }

        public static void Unload()
        {
            while (runningScripts.Count > 0)
                StopScript(runningScripts[0]);

            Interpreter.ClearAllLists();

            SaveScriptSettings();

            _enabled = false;
        }

        public static void OnUpdate()
        {
            if (!_enabled || !World.InGame)
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
                if (runningScripts.Contains(script)) //Already playing
                    return;

                script.GenerateScript();
                runningScripts.Add(script);
                script.GetScript.IsPlaying = true;
            }
        }

        public static void StopScript(ScriptFile script)
        {
            //GameActions.Print($"STOPPING {script.FileName} on line {script.GetScript.CurrentLine}");

            if (runningScripts.Contains(script))
                runningScripts.Remove(script);

            script.GetScript.IsPlaying = false;
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
            Interpreter.RegisterCommandHandler("info", InfoGump);
            Interpreter.RegisterCommandHandler("setskill", SetSkillLock);
            Interpreter.RegisterCommandHandler("getproperties", GetProperties);
            Interpreter.RegisterCommandHandler("turn", TurnCommand);
            Interpreter.RegisterCommandHandler("createlist", CreateList);
            Interpreter.RegisterCommandHandler("pushlist", PushList);
            Interpreter.RegisterCommandHandler("rename", RenamePet);
            Interpreter.RegisterCommandHandler("logout", Logout);
            Interpreter.RegisterCommandHandler("shownames", ShowNames);
            Interpreter.RegisterCommandHandler("clearlist", ClearList);
            Interpreter.RegisterCommandHandler("removelist", RemoveList);
            Interpreter.RegisterCommandHandler("togglehands", ToggleHands);
            Interpreter.RegisterCommandHandler("equipitem", EquipItem);
            Interpreter.RegisterCommandHandler("togglemounted", ToggleMounted);
            Interpreter.RegisterCommandHandler("promptalias", PromptAlias);
            Interpreter.RegisterCommandHandler("waitforgump", WaitForGump);
            Interpreter.RegisterCommandHandler("replygump", ReplyGump);
            Interpreter.RegisterCommandHandler("closegump", CloseGump);
            Interpreter.RegisterCommandHandler("clearjournal", ClearJournal);
            Interpreter.RegisterCommandHandler("poplist", PopList);
            Interpreter.RegisterCommandHandler("targettilerel", TargetTileRel);



            //Unfinished below
            Interpreter.RegisterCommandHandler("dress", DummyCommand);
            Interpreter.RegisterCommandHandler("undress", DummyCommand);
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
            Interpreter.RegisterCommandHandler("contextmenu", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforcontext", DummyCommand);
            Interpreter.RegisterCommandHandler("ignoreobject", DummyCommand);
            Interpreter.RegisterCommandHandler("clearignorelist", DummyCommand);
            Interpreter.RegisterCommandHandler("autocolorpick", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforcontents", DummyCommand);
            Interpreter.RegisterCommandHandler("targettype", DummyCommand);
            Interpreter.RegisterCommandHandler("targetground", DummyCommand);
            Interpreter.RegisterCommandHandler("targettile", DummyCommand);
            Interpreter.RegisterCommandHandler("targettileoffset", DummyCommand);
            #endregion

            #region Expressions
            Interpreter.RegisterExpressionHandler("timerexists", TimerExists);
            Interpreter.RegisterExpressionHandler("timerexpired", TimerExpired);
            Interpreter.RegisterExpressionHandler("findtype", FindType);
            Interpreter.RegisterExpressionHandler("findalias", FindAlias);
            Interpreter.RegisterExpressionHandler("skill", SkillValue);
            Interpreter.RegisterExpressionHandler("poisoned", PoisonedStatus);
            Interpreter.RegisterExpressionHandler("war", CheckWar);
            Interpreter.RegisterExpressionHandler("contents", CountContents);
            Interpreter.RegisterExpressionHandler("findobject", FindObject);
            Interpreter.RegisterExpressionHandler("distance", DistanceCheck);
            Interpreter.RegisterExpressionHandler("injournal", InJournal);
            Interpreter.RegisterExpressionHandler("inparty", InParty);
            Interpreter.RegisterExpressionHandler("property", PropertySearch);
            Interpreter.RegisterExpressionHandler("buffexists", BuffExists);
            Interpreter.RegisterExpressionHandler("findlayer", FindLayer);
            Interpreter.RegisterExpressionHandler("gumpexists", GumpExists);
            Interpreter.RegisterExpressionHandler("listcount", ListCount);
            Interpreter.RegisterExpressionHandler("listexists", ListExists);
            Interpreter.RegisterExpressionHandler("inlist", InList);
            Interpreter.RegisterExpressionHandler("nearesthostile", NearestHostile);
            Interpreter.RegisterExpressionHandler("counttype", CountType);
            Interpreter.RegisterExpressionHandler("ping", Ping);
            Interpreter.RegisterExpressionHandler("itemamt", ItemAmt);

            #endregion

            #region Player Values
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
            Interpreter.RegisterExpressionHandler("dead", IsDead);
            #endregion

            #region Default aliases
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

        private static ushort ItemAmt(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: itemamt 'serial'");

            if (World.Items.TryGetValue(args[0].AsSerial(), out var item))
                return item.Amount < 1 ? (ushort)1 : item.Amount;

            return 0;
        }

        private static bool TargetTileRel(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: targettilerel 'x' 'y' ['graphic']");

            if (!TargetManager.IsTargeting)
                return true;

            ushort x = (ushort)(World.Player.X + args[0].AsInt());
            ushort y = (ushort)(World.Player.Y + args[1].AsInt());

            GameObject g = World.Map.GetTile(x, y);

            if (args.Length > 3)
            {
                ushort gfx = args[4].AsUShort();

                if (g.Graphic != gfx)
                    return true;
            }

            TargetManager.Target(g.Graphic, x, y, g.Z);
            return true;
        }

        private static uint Ping(string expression, Argument[] args, bool quiet)
        {
            return NetClient.Socket.Statistics.Ping;
        }

        private static bool PopList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: poplist 'name' 'value'");

            Interpreter.PopList(args[0].AsString(), args[1]);

            return true;
        }

        private static bool ClearJournal(string command, Argument[] args, bool quiet, bool force)
        {
            Interpreter.ClearJournal();
            return true;
        }

        private static bool CloseGump(string command, Argument[] args, bool quiet, bool force)
        {
            uint gump = args.Length > 0 ? args[0].AsUInt() : World.Player.LastGumpID;

            UIManager.GetGumpServer(gump)?.Dispose();

            return true;
        }

        private static bool ReplyGump(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: replygump 'buttonid' 'gumpid'");

            int buttonID = args[0].AsInt();
            uint gumpID = args.Length > 1 ? args[1].AsUInt() : World.Player.LastGumpID;

            Gump g = UIManager.GetGumpServer(gumpID);

            if (g != null)
                GameActions.ReplyGump(g.LocalSerial, gumpID, buttonID);

            return true;
        }

        private static bool WaitForGump(string command, Argument[] args, bool quiet, bool force)
        {
            uint gumpID = uint.MaxValue;
            int timeout = 5000;

            if (args.Length > 0) gumpID = args[0].AsUInt();
            if (args.Length > 1) timeout = args[1].AsInt();

            Interpreter.Timeout(timeout, ReturnTrue);

            if (World.Player.HasGump && (World.Player.LastGumpID == gumpID || gumpID == uint.MaxValue))
            {
                Interpreter.ClearTimeout();
                Interpreter.SetAlias("lastgump", World.Player.LastGumpID);
                return true;
            }

            return false;
        }

        private static bool PromptAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: promptalias 'name'");

            if (Interpreter.IsTargetRequested())
            {
                if (TargetManager.IsTargeting && TargetManager.TargetingState == CursorTarget.Internal)
                    return false;

                if (TargetManager.LastTargetInfo.IsEntity)
                {
                    Interpreter.SetAlias(args[0].AsString(), TargetManager.LastTargetInfo.Serial);
                    Interpreter.SetTargetRequested(false);
                    return true;
                }

                LScriptWarning("Warning: Targeted object only supports items and mobiles.");
                Interpreter.SetTargetRequested(false);
                return true;
            }

            TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral);
            Interpreter.SetTargetRequested(true);

            return false;
        }

        private static bool ToggleMounted(string command, Argument[] args, bool quiet, bool force)
        {
            //No params
            Item mount = World.Player.FindItemByLayer(Layer.Mount);

            if (mount != null)
            {
                Interpreter.SetAlias(Constants.LASTMOUNT + World.Player.Serial.ToString(), mount);
                GameActions.DoubleClick(World.Player.Serial);
                return true;
            }
            else
            {
                uint serial = Interpreter.GetAlias(Constants.LASTMOUNT + World.Player.Serial.ToString());
                if (serial != uint.MaxValue)
                {
                    GameActions.DoubleClick(serial);
                    return true;
                }
            }

            return true;
        }

        private static bool EquipItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: equipitem 'serial'");

            uint serial = args[0].AsSerial();

            if (SerialHelper.IsItem(serial))
            {
                GameActions.PickUp(serial, 0, 0, 1);
                GameActions.Equip(serial);
            }

            return true;
        }

        private static bool ToggleHands(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: togglehands 'left/right'");

            Layer hand = args[0].AsString() switch
            {
                "left" => Layer.OneHanded,
                "right" => Layer.TwoHanded,
                _ => Layer.Invalid
            };

            if (hand != Layer.Invalid)
            {
                Item i = World.Player.FindItemByLayer(hand);
                if (i != null) //Item is in hand, lets unequip and save it
                {
                    GameActions.GrabItem(i, 0, World.Player.FindItemByLayer(Layer.Backpack));
                    Interpreter.SetAlias(Constants.LASTITEMINHAND + hand.ToString(), i);
                    return true;
                }
                else //No item in hand, lets see if we have a saved item for this slot
                {
                    uint serial = Interpreter.GetAlias(Constants.LASTITEMINHAND + hand.ToString());
                    if (SerialHelper.IsItem(serial))
                    {
                        GameActions.PickUp(serial, 0, 0, 1);
                        GameActions.Equip();
                        return true;
                    }
                }
            }

            return true;
        }

        private static bool IsDead(string expression, Argument[] args, bool quiet)
        {
            Mobile m = World.Player;

            if (args.Length > 0)
                if (World.Mobiles.TryGetValue(args[0].AsSerial(), out m))                
                    return m.IsDead;                
                else
                    return true;

            return m.IsDead;
        }

        private static bool RemoveList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: removelist 'name'");

            Interpreter.DestroyList(args[0].AsString());

            return true;
        }

        private static bool ClearList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: clearlist 'name'");

            Interpreter.ClearList(args[0].AsString());

            return true;
        }

        private static bool ShowNames(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.AllNames();
            return true;
        }

        private static int CountType(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: counttype 'graphic' 'source' 'hue', 'ground range'");

            uint graphic = args[0].AsUInt();
            uint source = args[1].AsSerial();
            if (source == MAX_SERIAL) source = uint.MaxValue;

            ushort hue = args.Length > 2 ? args[2].AsUShort() : ushort.MaxValue;

            int ground = args.Length > 3 ? args[3].AsInt() : int.MaxValue;

            var items = Utility.FindItems(graphic, parOrRootContainer: source, hue: hue, groundRange: ground);

            int count = 0;

            foreach (var item in items)
                count += item.Amount == 0 ? 1 : item.Amount;

            return count;
        }

        private static bool NearestHostile(string expression, Argument[] args, bool quiet)
        {
            // nearesthostile 'distance'
            int maxDist = 10;

            if (args.Length > 0)
            {
                maxDist = args[0].AsInt();
            }

            uint m = World.FindNearest(ScanTypeObject.Hostile);

            if (SerialHelper.IsMobile(m) && World.Mobiles.TryGetValue(m, out var mobile))
            {
                if (mobile.Distance <= maxDist)
                {
                    Interpreter.SetAlias(Constants.FOUND, m);
                    return true;
                }
            }

            return false;
        }

        private static bool Logout(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.Logout();
            return true;
        }

        private static bool RenamePet(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: rename 'serial' 'name'");

            GameActions.Rename(args[0].AsSerial(), args[1].AsString());

            return true;
        }

        private static bool PushList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: pushlist 'name' 'value' [front]");

            bool front = false;

            if (args.Length > 2 && args[2].AsString().ToLower() == "front")
                front = true;

            Interpreter.PushList(args[0].AsString(), args[1], front, force);

            return true;
        }

        private static bool CreateList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: createlist 'name'");

            Interpreter.CreateList(args[0].AsString());

            return true;
        }

        private static bool TurnCommand(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: turn 'direction'");

            Direction d = Utility.GetDirection(args[0].AsString());

            if (d != Direction.NONE && World.Player.Direction != d)
                World.Player.Walk(d, false);

            return true;
        }

        private static bool InList(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: inlist 'name' 'value'");

            return Interpreter.ListContains(args[0].AsString(), args[1]);
        }

        private static bool ListExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: listexists 'name'");

            return Interpreter.ListExists(args[0].AsString());
        }

        private static int ListCount(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: listcount 'name'");

            return Interpreter.ListLength(args[0].AsString());
        }

        private static bool GumpExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: gumpexists 'gumpid'");

            uint gumpid = args[0].AsUInt();

            for (LinkedListNode<Gump> last = UIManager.Gumps.Last; last != null; last = last.Previous)
            {
                Control c = last.Value;
                if (last.Value != null && (last.Value.ServerSerial == gumpid || last.Value.LocalSerial == gumpid))
                    return true;
            }

            return false;
        }

        private static bool FindLayer(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: findlayer 'layer'");

            string layer = args[0].AsString();

            Layer finalLayer = Utility.GetItemLayer(layer);

            if (finalLayer != Layer.Invalid)
            {
                Item item = World.Player.FindItemByLayer(finalLayer);
                if (item != null)
                {
                    Interpreter.SetAlias(Constants.FOUND, item);
                    return true;
                }
            }

            return false;
        }

        private static bool BuffExists(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: buffexists 'name'");

            string bufftext = args[0].AsString().ToLower();

            foreach (BuffIcon buff in World.Player.BuffIcons.Values)
            {
                if (buff.Title.ToLower().Contains(bufftext))
                    return true;
            }

            return false;
        }

        private static bool GetProperties(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: getproperties 'serial'");

            bool hasProps = World.OPL.Contains(args[0].AsSerial()); //This will request properties if we don't already have them

            if (force)
                return true;

            return hasProps;
        }

        private static bool PropertySearch(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: property 'serial' 'text'");

            if (World.Items.TryGetValue(args[0].AsSerial(), out var item))
            {
                return Utility.SearchItemNameAndProps(args[1].AsString(), item);
            }

            return false;
        }

        private static bool InParty(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: inparty 'serial'");

            uint serial = args[0].AsSerial();

            foreach (var mem in World.Party.Members)
            {
                if (mem.Serial == serial)
                    return true;
            }

            return false;
        }

        private static bool InJournal(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: injournal 'search text'");

            return Interpreter.ActiveScript.SearchJournalEntries(args[0].AsString());
        }

        private static bool SetSkillLock(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: setskill 'skill' 'up/down/locked'");

            Lock status = Lock.Up;

            switch (args[1].AsString())
            {
                case "up":
                default:
                    status = Lock.Up;
                    break;
                case "down":
                    status = Lock.Down;
                    break;
                case "locked":
                    status = Lock.Locked;
                    break;
            }

            for (int i = 0; i < World.Player.Skills.Length; i++)
            {
                if (World.Player.Skills[i].Name.ToLower().Contains(args[0].AsString()))
                {
                    World.Player.Skills[i].Lock = status;
                    break;
                }
            }

            return true;
        }

        private static uint DistanceCheck(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: distance 'serial'");

            uint serial = args[0].AsSerial();

            if (SerialHelper.IsValid(serial))
            {
                if (SerialHelper.IsItem(serial))
                {
                    if (World.Items.TryGetValue(serial, out var item))
                        return (uint)item.Distance;
                }
                else if (SerialHelper.IsMobile(serial))
                {
                    if (World.Mobiles.TryGetValue(serial, out var mobile))
                        return (uint)mobile.Distance;
                }
            }

            return uint.MaxValue;
        }

        private static bool InfoGump(string command, Argument[] args, bool quiet, bool force)
        {
            TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
            return true;
        }

        private static bool FindObject(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: findobject 'serial' [container]");

            if (World.Items.TryGetValue(args[0].AsSerial(), out var obj))
            {
                if (args.Length > 1)
                {
                    uint source = args[1].AsSerial();

                    if (obj.Container == source || obj.RootContainer == source)
                        return true;
                }
                else
                    return true;
            }
            else
            if (World.Mobiles.TryGetValue(args[0].AsSerial(), out var m))
            {
                return true;
            }

            return false;
        }

        private static uint CountContents(string expression, Argument[] args, bool quiet)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: contents 'container'");

            if (World.Items.TryGetValue(args[0].AsSerial(), out var item))
            {
                return Utility.ContentsCount(item);
            }

            return 0;
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
            if (foundVal == uint.MaxValue)
                foundVal = args[0].AsSerial();

            try
            {
                if (World.Items.TryGetValue(foundVal, out Item i))
                {
                    Interpreter.SetAlias(Constants.FOUND, i);
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
            {
                Interpreter.SetAlias(Constants.FOUND, items[0]);
            }

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

            World.Player.Walk(Utility.GetDirection(dir), true);

            return true;
        }

        private static bool CommandWalk(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
                throw new RunTimeError(null, "Usage: walk 'direction'");

            string dir = args[0].AsString().ToLower();

            World.Player.Walk(Utility.GetDirection(dir), false);

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
                throw new RunTimeError(null, "Usage: pause 'duration'");

            int ms = args[0].AsInt();

            Interpreter.Pause(ms);
            return true;
        }

        private static bool UseType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
                throw new RunTimeError(null, "Usage: usetype 'container' 'graphic' 'hue'");

            uint source = args[0].AsSerial();
            uint gfx = args[1].AsUInt();
            ushort hue = args.Length > 2 ? args[2].AsUShort() : ushort.MaxValue;

            if (gfx == MAX_SERIAL) gfx = uint.MaxValue;

            var items = Utility.FindItems(gfx, parOrRootContainer: source, hue: hue);

            if (items.Count > 0)
            {
                GameActions.DoubleClick(items[0]);
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
            GameActions.Print($"[{Interpreter.ActiveScript.CurrentLine}][LScript Error]" + msg);
        }

        private static void LScriptWarning(string msg)
        {
            GameActions.Print($"[{Interpreter.ActiveScript.CurrentLine}][LScript Warning]" + msg);
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
