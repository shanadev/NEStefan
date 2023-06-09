﻿using System;
namespace GameEngine
{
    public class Settings
    {
        private Dictionary<WindowSettingTypes, WindowSize> _windowSettings = new Dictionary<WindowSettingTypes, WindowSize>();

        public Dictionary<WindowSettingTypes, WindowSize> WindowSettings
        {
            get { return _windowSettings; }
        }

        public Settings()
        {
            initWindowSettings();
        }

        private void initWindowSettings()
        {
            _windowSettings.Add(WindowSettingTypes.NES, new WindowSize(width: 254, height: 240, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.NES_Double, new WindowSize(width: 254 * 2, height: 240 * 2, pixelSize: 2));
            _windowSettings.Add(WindowSettingTypes.NES_Triple, new WindowSize(width: 254 * 3, height: 240 * 3, pixelSize: 3));
            _windowSettings.Add(WindowSettingTypes.NES_Quad, new WindowSize(width: 254 * 4, height: 240 * 4, pixelSize: 4));
            _windowSettings.Add(WindowSettingTypes.SD, new WindowSize(width: 640, height: 480, pixelSize: 2));
            _windowSettings.Add(WindowSettingTypes.SD_Double, new WindowSize(width: 640, height: 480, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.HD, new WindowSize(width: 1280, height: 720, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.HD_Double, new WindowSize(width: 1280, height: 720, pixelSize: 2));
            _windowSettings.Add(WindowSettingTypes.FullHD, new WindowSize(width: 1920, height: 1080, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.QuadHD, new WindowSize(width: 2560, height: 1440, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.UHD, new WindowSize(width: 3840, height: 2160, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.FullUHD, new WindowSize(width: 7680, height: 4320, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.CPUView, new WindowSize(width: 1400, height: 900, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.PatternView, new WindowSize(width: 260*2, height: 130*2, pixelSize: 2));
            _windowSettings.Add(WindowSettingTypes.AudioView, new WindowSize(width: 400, height: 700, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.RAMView, new WindowSize(width: 840, height: 665, pixelSize: 1));
            _windowSettings.Add(WindowSettingTypes.MainWin, new WindowSize(width: 750, height: 480, pixelSize: 1));

        }
    }
}

