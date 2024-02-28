using System;
using System.Threading;
using ClassicUO.Dust765.Shared;
using ClassicUO.Dust765.Lobby.Networking;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;

namespace ClassicUO.Dust765.Autos
{
    internal class AutoLobbyStealthPosition
    {
        public static bool IsEnabled { get; set; }
        public static Thread a_AutoLobbyStealthPositionThread;

        //##AutoLobbyStealthPosition Toggle##//
        public static void Toggle()
        {
            GameActions.Print(String.Format("Auto LobbyStealthPosition:{0}abled", (IsEnabled = !IsEnabled) == true ? "En" : "Dis"), 70);
        }

        //##Register Command and Perform Checks##//
        public static void Initialize()
        {
            CommandManager.Register("autohid", args => Toggle());

            a_AutoLobbyStealthPositionThread = new Thread(new ThreadStart(DoAutoLobbyStealthPosition))
            {
                IsBackground = true
            };
        }

        //##Default AutoLobbyStealthPosition Status on GameLoad##//
        static AutoLobbyStealthPosition()
        {
            IsEnabled = false;
        }

        //##Perform AutoLobbyStealthPosition Update on Toggle/Player Death##//
        public static void Update()
        {
            if (!IsEnabled || World.Player.IsDead)
                DisableAutoLobbyStealthPosition();

            if (IsEnabled)
                EnableAutoLobbyStealthPosition();
        }

        //##Enable AutoLobbyStealthPosition##//
        private static void EnableAutoLobbyStealthPosition()
        {
            if (!a_AutoLobbyStealthPositionThread.IsAlive)
            {
                a_AutoLobbyStealthPositionThread = new Thread(new ThreadStart(DoAutoLobbyStealthPosition))
                {
                    IsBackground = true
                };
                a_AutoLobbyStealthPositionThread.Start();
            }
        }

        //##Disable AutoLobbyStealthPosition##//
        private static void DisableAutoLobbyStealthPosition()
        {
            if (a_AutoLobbyStealthPositionThread.IsAlive)
            {
                a_AutoLobbyStealthPositionThread.Abort();
            }
        }

        //##Perform AutoLobbyStealthPosition Checks##//
        private static void DoAutoLobbyStealthPosition()
        {
            DateTime dateTime = DateTime.Now;
            while (true)
            {

                if (World.Player == null || World.Player.IsDead)
                    DisableAutoLobbyStealthPosition();

                if (!World.Player.IsDead && World.Player != null && ((DateTime.Now - dateTime) > TimeSpan.FromSeconds(2.5)))
                {
                    
                    if (Lobby.Lobby._netState == null || !Lobby.Lobby._netState.IsOpen)
                    {

                    }
                    else if (World.Player.IsHidden)
                    {
                        SendPacketToLobby();
                    }

                    dateTime = DateTime.Now;
                    Thread.Sleep(250);
                }
                Thread.Sleep(250);
            }
        }

        //##Perform SendPacketToLobby##//
        private static void SendPacketToLobby()
        {
            string posXY = (World.Player.X.ToString() + "," + World.Player.Y.ToString() + "," + World.Player.Z.ToString());
            Lobby.Lobby._netState.Send(new PHiddenPosition((posXY)));
        }

        //##Perform Message##//
        public static void Print(string message, MessageColor color = MessageColor.Default)
        {
            GameActions.Print(message, (ushort)color, MessageType.System, 1);
        }
    }
}