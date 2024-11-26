using System;
using UOScript;

namespace ClassicUO.LegionScripting
{
    internal class LegionScripting
    {
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
            Console.WriteLine("Msg {0}", args[0].AsString());

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
            switch (alias)
            {
                case "backpack": break;
                case "bank": break;
                case "lastobject": break;
                case "lasttarget": break;
                case "lefthand": break;
                case "righthand": break;
                case "self": break;
                case "mount": break;
            }

            return 0;
        }

        public static void RegisterDummyCommands()
        {
            #region Commands
            Interpreter.RegisterCommandHandler("fly", DummyCommand);
            Interpreter.RegisterCommandHandler("land", DummyCommand);
            Interpreter.RegisterCommandHandler("setability", DummyCommand);
            Interpreter.RegisterCommandHandler("attack", DummyCommand);
            Interpreter.RegisterCommandHandler("clearhands", DummyCommand);
            Interpreter.RegisterCommandHandler("clickobject", DummyCommand);
            Interpreter.RegisterCommandHandler("bandageself", DummyCommand);
            Interpreter.RegisterCommandHandler("usetype", DummyCommand);
            Interpreter.RegisterCommandHandler("useobject", DummyCommand);
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

            Interpreter.RegisterExpressionHandler("mana", DummyExpression);
            Interpreter.RegisterExpressionHandler("x", DummyIntExpression);
            Interpreter.RegisterExpressionHandler("y", DummyExpression);
            Interpreter.RegisterExpressionHandler("z", DummyExpression);
            Interpreter.RegisterExpressionHandler("name", DummyStringExpression);


            Interpreter.RegisterAliasHandler("backpack", DefaultAlias);
            Interpreter.RegisterAliasHandler("bank", DefaultAlias);
            Interpreter.RegisterAliasHandler("lastobject", DefaultAlias);
            Interpreter.RegisterAliasHandler("lasttarget", DefaultAlias);
            Interpreter.RegisterAliasHandler("lefthand", DefaultAlias);
            Interpreter.RegisterAliasHandler("righthand", DefaultAlias);
            Interpreter.RegisterAliasHandler("self", DefaultAlias);
            Interpreter.RegisterAliasHandler("mount", DefaultAlias);
        }
    }
}
