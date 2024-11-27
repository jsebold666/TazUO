using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using LScript;

namespace ClassicUO.LegionScripting
{
    internal static class LegionScripting
    {
        private static bool _enabled;
        private static string scriptPath;

        private static List<Script> runningScripts = new List<Script>();
        private static List<Script> removeRunningScripts = new List<Script>();

        public static List<ScriptFile> LoadedScripts = new List<ScriptFile>();

        public static void Init()
        {
            scriptPath = Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts");

            if (!_enabled)
            {
                RegisterDummyCommands();

                CommandManager.Register("lscript", (args) =>
                {
                    string code = string.Join(" ", args.Skip(1));

                    Script s = new Script(Lexer.Lex([code]));

                    PlayScript(s);
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
                        if(script.FileName == file && script.FileAsScript != null)
                        {
                            PlayScript(script.FileAsScript);
                            break;
                        }
                    }
                });

                _enabled = true;
            }

            LoadScriptsFromFile();
        }

        private static void LoadScriptsFromFile()
        {
            if (!Directory.Exists(scriptPath))
                Directory.CreateDirectory(scriptPath);

            LoadedScripts.Clear();

            foreach (string file in Directory.EnumerateFiles(scriptPath))
            {
                if (file.EndsWith(".lscript"))
                {
                    string p = Path.GetDirectoryName(file);
                    string fname = Path.GetFileName(file);
                    LoadedScripts.Add(new ScriptFile(p, fname));
                }
            }
        }

        public static void Unload()
        {
            runningScripts.Clear();
            Interpreter.ClearAllLists();
        }

        public static void OnUpdate()
        {
            removeRunningScripts.Clear();


            foreach (Script script in runningScripts)
            {
                try
                {
                    if (!Interpreter.ExecuteScript(script))
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

            foreach (Script script in removeRunningScripts)
                StopScript(script);
        }

        public static void PlayScript(Script script)
        {
            if (script != null)
            {
                runningScripts.Add(script);
            }
        }

        public static void StopScript(Script script)
        {
            if (runningScripts.Contains(script))
                runningScripts.Remove(script);
        }

        private static IComparable DummyExpression(string expression, Argument[] args, bool quiet)
        {
            Console.WriteLine("Executing expression {0} {1}", expression, args);

            return 0;
        }

        private static int DummyIntExpression(string expression, Argument[] args, bool quiet)
        {
            Console.WriteLine("Executing expression {0} {1}", expression, args);

            return 3;
        }

        private static string DummyStringExpression(string expression, Argument[] args, bool quiet)
        {
            Console.WriteLine("Executing expression {0} {1}", expression, args);

            return "test";
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
                    case "backpack": return World.Player.FindItemByLayer(Game.Data.Layer.Backpack);
                    case "bank": return World.Player.FindItemByLayer(Game.Data.Layer.Bank);
                    case "lastobject": return World.LastObject;
                    case "lasttarget": return TargetManager.LastTargetInfo.Serial;
                    case "lefthand": return World.Player.FindItemByLayer(Game.Data.Layer.OneHanded);
                    case "righthand": return World.Player.FindItemByLayer(Game.Data.Layer.TwoHanded);
                    case "self": return World.Player;
                    case "mount": return World.Player.FindItemByLayer(Game.Data.Layer.Mount);
                }

            return 0;
        }

        private static void RegisterDummyCommands()
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


            //Unfinished below
            Interpreter.RegisterCommandHandler("usetype", DummyCommand);
            Interpreter.RegisterCommandHandler("useonce", DummyCommand);
            Interpreter.RegisterCommandHandler("cleanusequeue", DummyCommand);
            Interpreter.RegisterCommandHandler("moveitem", DummyCommand);
            Interpreter.RegisterCommandHandler("moveitemoffset", DummyCommand);
            Interpreter.RegisterCommandHandler("movetype", DummyCommand);
            Interpreter.RegisterCommandHandler("movetypeoffset", DummyCommand);
            Interpreter.RegisterCommandHandler("walk", DummyCommand);
            Interpreter.RegisterCommandHandler("turn", DummyCommand);
            Interpreter.RegisterCommandHandler("run", DummyCommand);
            Interpreter.RegisterCommandHandler("useskill", DummyCommand);
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
            Interpreter.RegisterCommandHandler("autoloot", DummyCommand);
            Interpreter.RegisterCommandHandler("dress", DummyCommand);
            Interpreter.RegisterCommandHandler("undress", DummyCommand);
            Interpreter.RegisterCommandHandler("dressconfig", DummyCommand);
            Interpreter.RegisterCommandHandler("toggleautoloot", DummyCommand);
            Interpreter.RegisterCommandHandler("togglescavenger", DummyCommand);
            Interpreter.RegisterCommandHandler("counter", DummyCommand);
            Interpreter.RegisterCommandHandler("unsetalias", DummyCommand);
            Interpreter.RegisterCommandHandler("setalias", DummyCommand);
            Interpreter.RegisterCommandHandler("promptalias", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforgump", DummyCommand);
            Interpreter.RegisterCommandHandler("replygump", DummyCommand);
            Interpreter.RegisterCommandHandler("closegump", DummyCommand);
            Interpreter.RegisterCommandHandler("clearjournal", DummyCommand);
            Interpreter.RegisterCommandHandler("waitforjournal", DummyCommand);
            Interpreter.RegisterCommandHandler("poplist", DummyCommand);
            Interpreter.RegisterCommandHandler("pushlist", PushList);
            Interpreter.RegisterCommandHandler("removelist", DummyCommand);
            Interpreter.RegisterCommandHandler("createlist", CreateList);
            Interpreter.RegisterCommandHandler("clearlist", DummyCommand);
            Interpreter.RegisterCommandHandler("info", DummyCommand);
            Interpreter.RegisterCommandHandler("pause", DummyCommand);
            Interpreter.RegisterCommandHandler("ping", DummyCommand);
            Interpreter.RegisterCommandHandler("playmacro", DummyCommand);
            Interpreter.RegisterCommandHandler("playsound", DummyCommand);
            Interpreter.RegisterCommandHandler("resync", DummyCommand);
            Interpreter.RegisterCommandHandler("snapshot", DummyCommand);
            Interpreter.RegisterCommandHandler("hotkeys", DummyCommand);
            Interpreter.RegisterCommandHandler("where", DummyCommand);
            Interpreter.RegisterCommandHandler("messagebox", DummyCommand);
            Interpreter.RegisterCommandHandler("mapuo", DummyCommand);
            Interpreter.RegisterCommandHandler("clickscreen", DummyCommand);
            Interpreter.RegisterCommandHandler("paperdoll", DummyCommand);
            Interpreter.RegisterCommandHandler("helpbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("guildbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("questsbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("logoutbutton", DummyCommand);
            Interpreter.RegisterCommandHandler("virtue", DummyCommand);
            Interpreter.RegisterCommandHandler("msg", MsgCommand);
            Interpreter.RegisterCommandHandler("headmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("partymsg", DummyCommand);
            Interpreter.RegisterCommandHandler("guildmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("allymsg", DummyCommand);
            Interpreter.RegisterCommandHandler("whispermsg", DummyCommand);
            Interpreter.RegisterCommandHandler("yellmsg", DummyCommand);
            Interpreter.RegisterCommandHandler("sysmsg", DummyCommand);
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
            Interpreter.RegisterCommandHandler("miniheal", DummyCommand);
            Interpreter.RegisterCommandHandler("bigheal", DummyCommand);
            Interpreter.RegisterCommandHandler("cast", DummyCommand);
            Interpreter.RegisterCommandHandler("chivalryheal", DummyCommand);
            Interpreter.RegisterCommandHandler("waitfortarget", DummyCommand);
            Interpreter.RegisterCommandHandler("canceltarget", DummyCommand);
            Interpreter.RegisterCommandHandler("target", DummyCommand);
            Interpreter.RegisterCommandHandler("targettype", DummyCommand);
            Interpreter.RegisterCommandHandler("targetground", DummyCommand);
            Interpreter.RegisterCommandHandler("targettile", DummyCommand);
            Interpreter.RegisterCommandHandler("targettileoffset", DummyCommand);
            Interpreter.RegisterCommandHandler("targettilerelative", DummyCommand);
            Interpreter.RegisterCommandHandler("cleartargetqueue", DummyCommand);
            Interpreter.RegisterCommandHandler("settimer", DummyCommand);
            Interpreter.RegisterCommandHandler("removetimer", DummyCommand);
            Interpreter.RegisterCommandHandler("createtimer", DummyCommand);
            #endregion

            #region Expressions
            Interpreter.RegisterExpressionHandler("findalias", DummyExpression);
            Interpreter.RegisterExpressionHandler("contents", DummyExpression);
            Interpreter.RegisterExpressionHandler("inregion", DummyExpression);
            Interpreter.RegisterExpressionHandler("skill", DummyExpression);
            Interpreter.RegisterExpressionHandler("findobject", DummyExpression);
            Interpreter.RegisterExpressionHandler("distance", DummyExpression);
            Interpreter.RegisterExpressionHandler("inrange", DummyExpression);
            Interpreter.RegisterExpressionHandler("buffexists", DummyExpression);
            Interpreter.RegisterExpressionHandler("property", DummyExpression);
            Interpreter.RegisterExpressionHandler("findtype", DummyExpression);
            Interpreter.RegisterExpressionHandler("findlayer", DummyExpression);
            Interpreter.RegisterExpressionHandler("skillstate", DummyExpression);
            Interpreter.RegisterExpressionHandler("counttype", DummyExpression);
            Interpreter.RegisterExpressionHandler("counttypeground", DummyExpression);
            Interpreter.RegisterExpressionHandler("findwand", DummyExpression);
            Interpreter.RegisterExpressionHandler("inparty", DummyExpression);
            Interpreter.RegisterExpressionHandler("infriendslist", DummyExpression);
            Interpreter.RegisterExpressionHandler("war", DummyExpression);
            Interpreter.RegisterExpressionHandler("ingump", DummyExpression);
            Interpreter.RegisterExpressionHandler("gumpexists", DummyExpression);
            Interpreter.RegisterExpressionHandler("injournal", DummyExpression);
            Interpreter.RegisterExpressionHandler("listexists", DummyExpression);
            Interpreter.RegisterExpressionHandler("list", DummyExpression);
            Interpreter.RegisterExpressionHandler("inlist", DummyExpression);
            Interpreter.RegisterExpressionHandler("timer", DummyExpression);
            Interpreter.RegisterExpressionHandler("timerexists", DummyExpression);
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
            #endregion
        }

        private static bool UseObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0) return true;

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
            GameActions.BandageSelf();
            return true;
        }

        private static bool ClickObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0) return true;

            GameActions.SingleClick(args[0].AsSerial());
            return true;
        }

        private static bool CommandAttack(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0) return true;

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
    }

    internal class ScriptFile
    {
        public string Path;
        public string FileName;
        public string FullPath;
        public Script FileAsScript;

        public ScriptFile(string path, string fileName)
        {
            Path = path;
            FileName = fileName;
            FullPath = System.IO.Path.Combine(Path, FileName);
            ConvertToScript();
        }

        public string ReadFile()
        {
            try
            {
                return File.ReadAllText(FullPath);
            }
            catch { }

            return string.Empty;
        }

        public void ConvertToScript()
        {
            try
            {
                FileAsScript = new Script(Lexer.Lex(FullPath));
            }
            catch { }
        }
    }
}
