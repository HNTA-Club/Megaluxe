#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsGraphics : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideTextureQuality = "SelectSlideTextureQuality";
        private const string _SelectSlideCoverSize = "SelectSlideCoverSize";
        private const string _SelectSlideFullScreen = "SelectSlideFullScreen";
        private const string _SelectSlideStretch = "SelectSlideStretch";
        private const string _TextWarningRestart = "TextWarningRestart";
        private const string _StaticWarningRestart = "StaticWarningRestart";
        private const string _ButtonExit = "ButtonExit";
        private static readonly string[] CoverSizes = { "32", "64", "128", "256", "512", "1024" };

        private int _WarningStream = -1;
        private bool _HasPlayedWarningSound = false;

        private static int PlaySound(ESounds sound, int volume)
        {
            var streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);

            return streamId;
        }

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { _ButtonExit };
            _ThemeSelectSlides = new string[] { _SelectSlideTextureQuality, _SelectSlideCoverSize, _SelectSlideFullScreen, _SelectSlideStretch };
            _ThemeTexts = new string[] { _TextWarningRestart };
            _ThemeStatics = new string[] { _StaticWarningRestart };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            if (_SelectSlides.ContainsKey(_SelectSlideTextureQuality))
            {
                _SelectSlides[_SelectSlideTextureQuality].SetValues<ETextureQuality>((int)CConfig.Config.Graphics.TextureQuality);
            }

            if (_SelectSlides.ContainsKey(_SelectSlideCoverSize))
            {
                _SelectSlides[_SelectSlideCoverSize].AddValues(CoverSizes);
                var currentCoverSize = CConfig.Config.Graphics.CoverSize;
                var index = Array.IndexOf(CoverSizes, currentCoverSize.ToString());
                _SelectSlides[_SelectSlideCoverSize].Selection = index;
            }

            if (_SelectSlides.ContainsKey(_SelectSlideFullScreen))
            {
                _SelectSlides[_SelectSlideFullScreen].SetValues<EOffOn>((int)CConfig.Config.Graphics.FullScreen);
                _SelectSlides[_SelectSlideFullScreen].Selection = (int)CConfig.Config.Graphics.FullScreen;
            }

            if (_SelectSlides.ContainsKey(_SelectSlideStretch))
            {
                _SelectSlides[_SelectSlideStretch].SetValues<EOffOn>((int)CConfig.Config.Graphics.Stretch);
                _SelectSlides[_SelectSlideStretch].Selection = (int)CConfig.Config.Graphics.Stretch;
            }

            if (_Texts.ContainsKey(_TextWarningRestart))
            {
                _Texts[_TextWarningRestart].Visible = false;
            }

            if (_Statics.ContainsKey(_StaticWarningRestart))
            {
                _Statics[_StaticWarningRestart].Visible = false;
            }
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            switch (keyEvent.Key)
            {
                case Keys.Escape:
                case Keys.Back:
                    _SaveConfig();
                    CGraphics.FadeTo(EScreen.Options);
                    _LeaveScreen();
                    break;

                case Keys.S:
                    CParty.SetNormalGameMode();
                    _SaveConfig();
                    CGraphics.FadeTo(EScreen.Song);
                    _LeaveScreen();
                    break;

                case Keys.Enter:
                    if (_Buttons.ContainsKey(_ButtonExit) && _Buttons[_ButtonExit].Selected)
                    {
                        _SaveConfig();
                        CGraphics.FadeTo(EScreen.Options);
                        _LeaveScreen();
                    }

                    break;

                case Keys.Left:
                    _SaveConfig();
                    break;

                case Keys.Right:
                    _SaveConfig();
                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                _SaveConfig();
                CGraphics.FadeTo(EScreen.Options);
                _LeaveScreen();
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                _SaveConfig();
                if (_Buttons.ContainsKey(_ButtonExit) && _Buttons[_ButtonExit].Selected)
                {
                    CGraphics.FadeTo(EScreen.Options);
                    _LeaveScreen();
                }
            }

            return true;
        }

        public override bool UpdateGame()
        {
            if (_Texts.ContainsKey(_TextWarningRestart) && _Texts[_TextWarningRestart].Visible && !_HasPlayedWarningSound)
            {
                _WarningStream = CScreenOptionsGraphics.PlaySound(ESounds.Warning, CConfig.SoundEffectVolume);
                _HasPlayedWarningSound = true;
            }

            return true;
        }

        private void _SaveConfig()
        {
            // Detect Texture quality change
            if (_SelectSlides.ContainsKey(_SelectSlideTextureQuality))
            {
                var currentTextureQuality = CConfig.Config.Graphics.TextureQuality;
                var newTextureQuality = (ETextureQuality)_SelectSlides[_SelectSlideTextureQuality].Selection;
                if (currentTextureQuality != newTextureQuality)
                {
                    if (_Texts.ContainsKey(_TextWarningRestart))
                    {
                        _Texts[_TextWarningRestart].Visible = true;
                    }

                    if (_Statics.ContainsKey(_StaticWarningRestart))
                    {
                        _Statics[_StaticWarningRestart].Visible = true;
                    }

                    CConfig.Config.Graphics.TextureQuality = newTextureQuality;
                }
            }

            // Detect Cover size change
            if (_SelectSlides.ContainsKey(_SelectSlideCoverSize) && _SelectSlides[_SelectSlideCoverSize].Selection >= 0 && _SelectSlides[_SelectSlideCoverSize].Selection < CoverSizes.Length)
            {
                var selectedValue = CoverSizes[_SelectSlides[_SelectSlideCoverSize].Selection];
                var currentCoverSize = CConfig.Config.Graphics.CoverSize;
                if (int.TryParse(selectedValue, out var newCoverSize) && currentCoverSize != newCoverSize)
                {
                    if (_Texts.ContainsKey(_TextWarningRestart))
                    {
                        _Texts[_TextWarningRestart].Visible = true;
                    }

                    if (_Statics.ContainsKey(_StaticWarningRestart))
                    {
                        _Statics[_StaticWarningRestart].Visible = true;
                    }

                    CConfig.Config.Graphics.CoverSize = newCoverSize;
                }
            }

            if (_SelectSlides.ContainsKey(_SelectSlideFullScreen))
            {
                CConfig.Config.Graphics.FullScreen = (EOffOn)_SelectSlides[_SelectSlideFullScreen].Selection;
            }

            if (_SelectSlides.ContainsKey(_SelectSlideStretch))
            {
                CConfig.Config.Graphics.Stretch = (EOffOn)_SelectSlides[_SelectSlideStretch].Selection;
            }

            CConfig.SaveConfig();
        }

        private void _LeaveScreen()
        {
            if (_WarningStream != -1)
            {
                CSound.Close(_WarningStream);
                _WarningStream = -1;
            }
        }
    }
}
