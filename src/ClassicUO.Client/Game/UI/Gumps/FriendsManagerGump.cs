using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class FriendsManagerGump : Gump
    {
        private const ushort HUE_FONT = 0xFFFF;
        private const ushort BACKGROUND_COLOR = 999;
        private const ushort GUMP_WIDTH = 300;
        private const ushort GUMP_HEIGHT = 400;

        private readonly int _gumpPosX = Client.Game.Scene.Camera.Bounds.Width / 2 - 125;
        private readonly int _gumpPosY = 100;

        private static ScrollArea _scrollArea;

        private bool _isListModified;

        private enum ButtonsId
        {
            ADD_NEW_IGNORE,
        }

        public FriendsManagerGump() : base(0, 0)
        {
            CanMove = true;

            Add
            (
                new AlphaBlendControl(0.95f)
                {
                    X = _gumpPosX,
                    Y = _gumpPosY,
                    Width = GUMP_WIDTH,
                    Height = GUMP_HEIGHT,
                    Hue = BACKGROUND_COLOR,
                    AcceptMouseInput = true,
                    CanMove = true,
                    CanCloseWithRightClick = true,
                }
            );

            #region Boarder
            Add
            (
                new Line
                (
                    _gumpPosX,
                    _gumpPosY,
                    GUMP_WIDTH,
                    1,
                    Color.Gray.PackedValue
                )
            );

            Add
            (
                new Line
                (
                    _gumpPosX,
                    _gumpPosY,
                    1,
                    GUMP_HEIGHT,
                    Color.Gray.PackedValue
                )
            );

            Add
            (
                new Line
                (
                    _gumpPosX,
                    GUMP_HEIGHT + _gumpPosY,
                    GUMP_WIDTH,
                    1,
                    Color.Gray.PackedValue
                )
            );

            Add
            (
                new Line
                (
                    GUMP_WIDTH + _gumpPosX,
                    _gumpPosY,
                    1,
                    GUMP_HEIGHT,
                    Color.Gray.PackedValue
                )
            );
            #endregion

            var initY = _gumpPosY + 10;

            #region Legend

            Add(new Label(ResGumps.IgnoreListName, true, HUE_FONT, 185, 255, FontStyle.BlackBorder) { X = _gumpPosX + 10, Y = initY });

            Add(new Label(ResGumps.Remove, true, HUE_FONT, 185, 255, FontStyle.BlackBorder) { X = _gumpPosX + 210, Y = initY });

            Add
            (
                new Line
                (
                    _gumpPosX,
                    initY + 20,
                    GUMP_WIDTH,
                    1,
                    Color.Gray.PackedValue
                )
            );

            #endregion

            Add
            (
                new NiceButton
                (
                    _gumpPosX + 20, _gumpPosY + GUMP_HEIGHT - 30, GUMP_WIDTH - 40, 25,
                    ButtonAction.Activate, "Add Person to Friend List"
                )
            );

            DrawArea();
            SetInScreen();
        }

        /// <summary>
        /// On Dispose save XML File
        /// </summary>
        public override void Dispose()
        {
            if (_isListModified)
                FriendManager.SaveFriendList();

            if (TargetManager.IsTargeting)
                TargetManager.CancelTarget();

            base.Dispose();
        }

        /// <summary>
        /// Draw Scroll Area
        /// </summary>
        private void DrawArea()
        {
            _scrollArea = new ScrollArea
            (
                _gumpPosX + 10, _gumpPosY + 40, GUMP_WIDTH - 20, GUMP_HEIGHT - 80,
                true
            );

            var y = 0;
            foreach (FriendListControl element in FriendManager.FriendsList.Select(m => new FriendListControl(m) { Y = y }))
            {
                element.RemoveMarkerEvent += MarkerRemoveEventHandler;

                _scrollArea.Add(element);
                y += 25;
            }

            Add(_scrollArea);
        }

        /// <summary>
        /// Handle Remove from ignored list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkerRemoveEventHandler(object sender, EventArgs e)
        {
            Redraw();
        }

        /// <summary>
        /// Redraw ignored list
        /// </summary>
        public void Redraw()
        {
            // If we need to redraw we assume that something changed in list
            _isListModified = true;
            Remove(_scrollArea);
            DrawArea();
        }

        /// <summary>
        /// On Button Click handler
        /// </summary>
        /// <param name="buttonId">Button Id</param>
        public override void OnButtonClick(int buttonId)
        {
            switch (buttonId)
            {
                case (int)ButtonsId.ADD_NEW_IGNORE:
                    TargetManager.SetTargeting(CursorTarget.FriendPlayerTarget, CursorType.Target, TargetType.Neutral);
                    break;
            }
        }

        private sealed class FriendListControl : Control
        {
            private readonly KeyValuePair<uint, string> _friend;

            public event EventHandler RemoveMarkerEvent;

            public FriendListControl(KeyValuePair<uint, string> friend)
            {
                CanMove = true;
                AcceptMouseInput = false;
                CanCloseWithRightClick = true;
                _friend = friend;

                Add(new Label(friend.Value, true, HUE_FONT, 290) { X = 10 });

                Add(new Button(1, 0xFAB, 0xFAC) { X = 220, ButtonAction = ButtonAction.Activate });
            }

            public override void OnButtonClick(int buttonId)
            {
                FriendManager.RemoveFriend(_friend.Key);
                RemoveMarkerEvent.Raise();
            }
        }
    }
}
