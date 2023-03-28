using System;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using GameEngine;
using GameEngine.GUIControls;
using static System.Net.WebRequestMethods;
//using static System.Net.Mime.MediaTypeNames;

namespace NEStefan.Core;

public class NESSystem
{
    // color swatch
    private ScreenColor c_white = new ScreenColor(255, 255, 255, 255);
    private ScreenColor c_black = new ScreenColor(0, 0, 0, 255);
    private ScreenColor c_red = new ScreenColor(255, 0, 0, 255);
    private ScreenColor c_green = new ScreenColor(0, 255, 0, 255);
    private ScreenColor c_blue = new ScreenColor(0, 0, 255, 255);
    private ScreenColor c_dgray = new ScreenColor(40, 40, 40, 255);
    private ScreenColor c_gray = new ScreenColor(170, 170, 170, 255);

    // Engine
    public Settings settings = new Settings();
    public Engine engine;

    // Windows
    public IntPtr winMainRenderer;
    public IntPtr winNESRenderer;
    public IntPtr winPatternTableRenderer;
    public IntPtr winRAMInfoRenderer;
    public IntPtr winAudioRenderer;
    public IntPtr winASMRenderer;
    public IntPtr winROMInfoRenderer;

    //public Button NESButton;
    //public Button PatternButton;
    //public Button ROMInfoButton;
    //public Button ASMButton;

    public bool userQuit = false;

    public bool RunEmulation = false;

    public uint samplesCount = 0;

    private bool a_pressed = false;
    private bool b_pressed = false;
    private bool sel_pressed = false;
    private bool st_pressed = false;
    private bool up_pressed = false;
    private bool down_pressed = false;
    private bool left_pressed = false;
    private bool right_pressed = false;

    private bool gamecontroller_a_pressed = false;
    private bool gamecontroller_b_pressed = false;
    private bool gamecontroller_sel_pressed = false;
    private bool gamecontroller_st_pressed = false;
    private bool gamecontroller_up_pressed = false;
    private bool gamecontroller_down_pressed = false;
    private bool gamecontroller_left_pressed = false;
    private bool gamecontroller_right_pressed = false;

    public byte SelectedPalette = 0x00;

    public float[] chan1Buffer = new float[300];
    public float[] chan2Buffer = new float[300];
    public float[] triangleBuffer = new float[300];
    public float[] noiseBuffer = new float[300];
    public float[] mixBuffer = new float[300];

    // Disassembled program
    public Dictionary<ushort, string> asm = new Dictionary<ushort, string>();

    // NES SYSTEM COMPONENTS
    public Bus nes;
    public Cartridge cart;

    public float ResidualTime = 0f;
    
    public NESSystem()
    {
        for (int i = 0; i < 300; i++)
        {
            chan1Buffer[i] = 0f;
        }

        engine = new Engine();
        engine.init();

        nes = new Bus(this);

        winMainRenderer = engine.CreateWindow(settings.WindowSettings[WindowSettingTypes.MainWin], "NEStefan Launch Window", RenderMain, "Main");

        // define button
        //engine.AddButton("Main", 10, 100, 100, 40, "Click Me", c_white, c_red, ButtonClick);

        engine.myWindows["Main"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Main"], "Main", new Rect(10, 30, 140, 40), "Emulation", c_white, c_dgray, NESButtonClick));
        engine.myWindows["Main"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Main"], "Pattern", new Rect(10, 90, 140, 40), "Pattern Table", c_white, c_dgray, PatternButtonClick));
        engine.myWindows["Main"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Main"], "RAM", new Rect(10, 150, 140, 40), "RAM Info", c_white, c_dgray, RAMInfoButtonClick));
        engine.myWindows["Main"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Main"], "Audio", new Rect(10, 210, 140, 40), "Audio Channels", c_white, c_dgray, AudioChannelsButtonClick));
        engine.myWindows["Main"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Main"], "ASM", new Rect(10, 270, 140, 40), "Disassembly", c_white, c_dgray, ASMButtonClick));
        engine.myWindows["Main"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Main"], "ROMInfo", new Rect(10, 330, 140, 40), "ROM Info", c_white, c_dgray, ROMInfoButtonClick));

        //engine.myWindows["Main"].ControlGUI.Controls["ASM"].Enabled = false;

        //NESButton = new Button(engine, engine.myWindows["Main"], "Main", new Rect(10, 200, 100, 40), "Emulation", c_white, c_dgray, NESButtonClick);
        //PatternButton = new Button(engine, engine.myWindows["Main"], "Pattern", new Rect(10, 260, 100, 40), "Pattern Table", c_white, c_dgray, PatternButtonClick);
        //ROMInfoButton = new Button(engine, engine.myWindows["Main"], "ROM", new Rect(10, 320, 100, 40), "ROM Info", c_white, c_dgray, ROMInfoButtonClick);

        List<string> listoffiles = new List<string>();
        DirectoryInfo di = new DirectoryInfo("/Users/shanamac/Projects/Programming/Romz/");
        List<Cartridge> romzlist = new List<Cartridge>();
        foreach (var file in di.GetFiles())
        {
            if (file.Extension == ".zip")
            {
                ZipArchive maybeNes = ZipFile.OpenRead(file.FullName);
                foreach (ZipArchiveEntry entry in maybeNes.Entries)
                {
                    if (entry.FullName.EndsWith(".nes", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(di.FullName + entry.FullName, true);
                        Cartridge newCart = new Cartridge(entry.FullName, nes, true);
                        romzlist.Add(newCart);
                    }
                }
            }
            if (file.Extension.ToLower() == ".nes")
            {
                Cartridge newCart = new Cartridge(file.FullName, nes, true);
                romzlist.Add(newCart);
                //listoffiles.Add($"{newCart.mapperID}~{file.Name}");
            }
        }

        //(string, string)[] newRomlist = romzlist.OrderBy(c => c.romFilename).Select(c => c.romFilename).ToArray();
        //romzlist.OrderBy(c => c.romFilename);
        
        foreach (Cartridge cart in romzlist.OrderBy(c => c.romFilename))
        { 
            string map = cart.mapperID.ToString();
            map = map.PadRight(3);
            listoffiles.Add($"{map}~{cart.romFilename}");
        }

        //foreach (var dir in di.EnumerateDirectories())
        //{
                       
        //    string dirname = dir.Name;            

        //    foreach (var files in dir.GetFiles())
        //    {

        //        listoffiles.Add($"{dirname}~{files.Name}");
        //    }

        //}                                                           

        engine.myWindows["Main"].ControlGUI.AddControl(new ListSelector(engine, engine.myWindows["Main"], "List", new Rect(160, 30, 575, 400), listoffiles, c_white));

        nes.SetSampleFrequency(44100);
        engine.InitAudio(44100, 1, 512, ReturnAudio);



        engine.Run();
    }

    
    public float ReturnAudio()
    {
        if (RunEmulation && !userQuit)
        {

            while (!nes.Clock()) { }
            //float newValue = (float)(Math.Sin((float)(2.0f * Math.PI * (samplesCount / 44100f) * 220f)));
            //samplesCount++;
            return nes.dAudioSample;
            //return newValue;
        }
        else
        {
            return 0.0f;
        }
    }

    public void ROMInfoButtonClick()
    {
        if (winROMInfoRenderer == IntPtr.Zero)
        {
            winROMInfoRenderer = engine.CreateWindow(settings.WindowSettings[WindowSettingTypes.SD_Double], "ROM Info", RenderROMWin, "ROM");
        }
    }

    public void ASMButtonClick()
    {
        if (winASMRenderer == IntPtr.Zero)
        {
            winASMRenderer = engine.CreateWindow(settings.WindowSettings[WindowSettingTypes.HD], "ASM", RenderASMWin, "ASM");

        }
        else
        {
            engine.myWindows["ASM"].Focus();
        }
    }

    public void AudioChannelsButtonClick()
    {
        if (winAudioRenderer == IntPtr.Zero)
        {
            winAudioRenderer = engine.CreateWindow(settings.WindowSettings[WindowSettingTypes.AudioView], "Audio", RenderAudioWin, "Audio");
            //engine.myWindows["Audio"].ControlGUI.AddControl(new Knob(engine, engine.myWindows["Audio"], "P1AudioAdjust", new Coord(10, 150), 0, 10f, 0));
            engine.myWindows["Audio"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Audio"], "P1Enable", new Rect(320, 80, 70, 20), "Enable", c_white, c_black, null, true, true));
            engine.myWindows["Audio"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Audio"], "P2Enable", new Rect(320, 180, 70, 20), "Enable", c_white, c_black, null, true, true));
            engine.myWindows["Audio"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Audio"], "TEnable", new Rect(320, 290, 70, 20), "Enable", c_white, c_black, null, true, true));
            engine.myWindows["Audio"].ControlGUI.AddControl(new Button(engine, engine.myWindows["Audio"], "NEnable", new Rect(320, 390, 70, 20), "Enable", c_white, c_black, null, true, true));

        }
        else
        {
            engine.myWindows["Audio"].Focus();
        }
    }


    public void NESButtonClick()
    {
        // launch a new window
        if (winNESRenderer == IntPtr.Zero)
        {
            winNESRenderer = engine.CreateWindow(settings.WindowSettings[WindowSettingTypes.NES_Triple], "Emulation", RenderNES, "NES");
            // start emulation
            // load cart
            ListSelector s = engine.myWindows["Main"].ControlGUI.Controls["List"] as ListSelector;

            string cartname = s.SValue;
            if (cartname != string.Empty)
            {
                string[] cartpieces = cartname.Split('~');
                //string fullcart = "/Users/shanamac/Projects/Programming/dotnet/SteveNES/NES/bin/ROMS/" + cartpieces[0] + "/" + cartpieces[1];
                string fullcart = "/Users/shanamac/Projects/Programming/Romz/" + cartpieces[1];
                this.cart = new Cartridge(fullcart, nes);

                nes.InsertCartridge(cart);
                //asm = nes.cpu.Disassemble(0x0000, 0xFFFF);
                nes.cpu.Reset();

                RunEmulation = true;
            }
        }
        else
        {
            ListSelector s = engine.myWindows["Main"].ControlGUI.Controls["List"] as ListSelector;
            engine.myWindows["NES"].Focus();
            string cartname = s.SValue;
            if (cartname != string.Empty)
            {
                string[] cartpieces = cartname.Split('~');
                //string fullcart = "/Users/shanamac/Projects/Programming/dotnet/SteveNES/NES/bin/ROMS/" + cartpieces[0] + "/" + cartpieces[1];
                string fullcart = "/Users/shanamac/Projects/Programming/Romz/" + cartpieces[1];
                this.cart = new Cartridge(fullcart, nes);

                nes.InsertCartridge(cart);
                //asm = nes.cpu.Disassemble(0x0000, 0xFFFF);

                //nes.cpu.Reset();
                nes.Reset();

                RunEmulation = true;
                userQuit = false;
            }

        }
    }

    public void PatternButtonClick()
    {
        if (winPatternTableRenderer == IntPtr.Zero)
        {
            winPatternTableRenderer = engine.CreateWindow(settings.WindowSettings[WindowSettingTypes.PatternView], "Pattern Table", RenderPattern, "Pattern");
        }
        else
        {
            engine.myWindows["Pattern"].Focus();
        }
    }

    public void RAMInfoButtonClick()
    {
        if (winRAMInfoRenderer == IntPtr.Zero && nes.cart != null)
        {
            winRAMInfoRenderer = engine.CreateWindow(settings.WindowSettings[WindowSettingTypes.RAMView], "RAM Info", RenderRAM, "RAM");
        }
        else
        {
            engine.myWindows["RAM"].Focus();
        }
    }


    public void RenderROMWin()
    {
        engine.ClearScreen(winROMInfoRenderer, c_dgray);

        // render rom information
        engine.DrawText(winROMInfoRenderer, 10, 10, $"ROM Name: {nes.cart.romFilename}", c_white);
        engine.DrawText(winROMInfoRenderer, 10, 20, $"Mapper: {nes.cart.mapperID}", c_white);
        engine.DrawText(winROMInfoRenderer, 10, 30, $"CHR Banks: {nes.cart.CHRbanks}", c_white);
        engine.DrawText(winROMInfoRenderer, 10, 40, $"PRG Banks: {nes.cart.PRGbanks}", c_white);
        engine.DrawText(winROMInfoRenderer, 10, 50, $"Has Battery: {nes.cart.hasSaveBattery}", c_white);
        engine.DrawText(winROMInfoRenderer, 10, 60, $"PRG Chunks: {nes.cart.prg_rom_chunks}", c_white);
        engine.DrawText(winROMInfoRenderer, 10, 70, $"CHR Chunks: {nes.cart.chr_rom_chunks}", c_white);
        
    }


    public void RenderASMWin()
    {
        engine.ClearScreen(winASMRenderer, c_dgray);

        DrawCode(winASMRenderer, 10, 10, 20);
    }

    public void DrawCode(IntPtr renderer, int x, int y, int lines)
    {
        int lineitem = asm.Keys.ToList().IndexOf(nes.cpu.pc);

        int lineY = (lines >> 1) * 10 + y;

        //if (lineitem > 0)
        //{
        try
        {
            engine.DrawText(renderer, x, lineY, asm.ElementAt(lineitem).Value, c_gray);
        }
        catch (Exception ex)
        {
            engine.DrawText(renderer, x, lineY, "NO DATA", c_white);
        }
        while (lineY < (lines * 10) + y)
        {
            lineY += 10;
            if (++lineitem > 0)
            {
                try
                {
                    engine.DrawText(renderer, x, lineY, asm.ElementAt(lineitem).Value, c_white);
                }
                catch (Exception ex)
                {
                    engine.DrawText(renderer, x, lineY, "NOT FOUND", c_white);
                }
            }
        }
        //}

        lineitem = Array.IndexOf(asm.Keys.ToArray(), nes.cpu.pc);
        lineY = (lines >> 1) * 10 + y;

        //if (lineitem > 0)
        //{
        while (lineY > y)
        {
            lineY -= 10;
            --lineitem;
            //if (--lineitem > 0)
            //{
            try
            {
                engine.DrawText(renderer, x, lineY, asm.ElementAt(lineitem).Value, c_gray);
            }
            catch (Exception ex)
            {
                engine.DrawText(renderer, x, lineY, "NOT FOUND", c_white);
            }
            //}
        }
        //}

    }


    public void RenderAudioWin()
    {
        engine.ClearScreen(winAudioRenderer, c_dgray);

        Button check1 = (Button)engine.myWindows["Audio"].ControlGUI.Controls["P1Enable"];
        nes.apu.Pulse1UserEnable = check1.Latched;
        Button check2 = (Button)engine.myWindows["Audio"].ControlGUI.Controls["P2Enable"];
        nes.apu.Pulse2UserEnable = check2.Latched;
        Button checkt = (Button)engine.myWindows["Audio"].ControlGUI.Controls["TEnable"];
        nes.apu.TriangleUserEnable = checkt.Latched;
        Button checkn = (Button)engine.myWindows["Audio"].ControlGUI.Controls["NEnable"];
        nes.apu.NoiseUserEnable = checkn.Latched;

        float[] drawbuffer1 = engine.FillBuffer(chan1Buffer, nes.apu.pulse1buffer.ToArray());
        nes.apu.pulse1buffer.Clear();

        float[] drawbuffer2 = engine.FillBuffer(chan2Buffer, nes.apu.pulse2buffer.ToArray());
        nes.apu.pulse2buffer.Clear();

        float[] drawtrianglebuffer = engine.FillBuffer(triangleBuffer, nes.apu.trianglebuffer.ToArray());
        nes.apu.trianglebuffer.Clear();

        float[] drawnoisebuffer = engine.FillBuffer(noiseBuffer, nes.apu.noisebuffer.ToArray());
        nes.apu.noisebuffer.Clear();

        float[] drawbuffermix = engine.FillBuffer(mixBuffer, engine.audioBuffer.ToArray());
        engine.audioBuffer.Clear();

        //float factor = .4f;

        engine.DrawText(winAudioRenderer, 12, 10, $"clock counter: {nes.apu.clock_counter}", c_white);
        engine.DrawText(winAudioRenderer, 12, 20, $"frame clock counter: {nes.apu.frame_clock_counter}", c_white);
        engine.DrawText(winAudioRenderer, 12, 30, $"dGlobalTime: {nes.apu.dGlobalTime}", c_white);

        //float adjustAmp = engine.myWindows["Audio"].ControlGUI.Controls["P1AudioAdjust"].Value;
        engine.DrawAudioBuffer(winAudioRenderer, 10, 40, 300, 100, c_white, drawbuffer1, 1);
        engine.DrawText(winAudioRenderer, 12, 45, $"Freq: {nes.apu.pulse1Osc.frequency}", c_white);
        engine.DrawText(winAudioRenderer, 12, 55, $"Duty: {nes.apu.pulse1Osc.dutycycle}", c_white);
        engine.DrawText(winAudioRenderer, 12, 65, $"Amp: {nes.apu.pulse1Osc.amplitude}", c_white);
        engine.DrawText(winAudioRenderer, 12, 75, $"Reload: {nes.apu.pulse1Seq.reload}", c_white);
        engine.DrawText(winAudioRenderer, 12, 85, $"Timer: {nes.apu.pulse1Seq.timer}", c_white);
        engine.DrawText(winAudioRenderer, 250, 45, $"Pulse 1", c_white);


        //float adjustAmp2 = engine.myWindows["Audio"].ControlGUI.Controls["P2AudioAdjust"].Value;
        engine.DrawAudioBuffer(winAudioRenderer, 10, 150, 300, 100, c_white, drawbuffer2, 1);
        engine.DrawText(winAudioRenderer, 12, 155, $"Freq: {nes.apu.pulse2Osc.frequency}", c_white);
        engine.DrawText(winAudioRenderer, 12, 165, $"Duty: {nes.apu.pulse2Osc.dutycycle}", c_white);
        engine.DrawText(winAudioRenderer, 12, 175, $"Amp: {nes.apu.pulse2Osc.amplitude}", c_white);
        engine.DrawText(winAudioRenderer, 12, 185, $"Reload: {nes.apu.pulse2Seq.reload}", c_white);
        engine.DrawText(winAudioRenderer, 12, 195, $"Timer: {nes.apu.pulse2Seq.timer}", c_white);
        engine.DrawText(winAudioRenderer, 250, 155, $"Pulse 2", c_white);


        engine.DrawAudioBuffer(winAudioRenderer, 10, 260, 300, 100, c_white, drawtrianglebuffer, 1);
        engine.DrawText(winAudioRenderer, 12, 265, $"Freq: {nes.apu.triangleChan.frequency}", c_white);
        engine.DrawText(winAudioRenderer, 12, 275, $"Reload: {nes.apu.triangleChan.reload}", c_white);

        engine.DrawText(winAudioRenderer, 12, 285, $"Linear Counter: {nes.apu.triangleChan.linear_timer}", c_white);
        engine.DrawText(winAudioRenderer, 12, 295, $"Length Counter: {nes.apu.triangleLC.counter}", c_white);
        //engine.DrawText(winAudioRenderer, 12, 305, $"Timer: {nes.apu.pulse1Seq.timer}", c_white);
        engine.DrawText(winAudioRenderer, 240, 265, $"Triangle", c_white);


        engine.DrawAudioBuffer(winAudioRenderer, 10, 370, 300, 100, c_white, drawnoisebuffer, 1);
        engine.DrawText(winAudioRenderer, 12, 375, $"Phase: {nes.apu.noiseChan.phase}", c_white);
        engine.DrawText(winAudioRenderer, 12, 385, $"ShiftReg: {nes.apu.noiseChan.shiftRegister} - {Convert.ToString(nes.apu.noiseChan.shiftRegister, toBase:2).PadLeft(15,'0')}", c_white);
        engine.DrawText(winAudioRenderer, 12, 395, $"Vol: {nes.apu.noiseEnv.volume}", c_white);
        engine.DrawText(winAudioRenderer, 12, 405, $"Reload: {nes.apu.noiseChan.reload}", c_white);
        engine.DrawText(winAudioRenderer, 12, 415, $"LC Counter: {nes.apu.noiseLC.counter}", c_white);
        engine.DrawText(winAudioRenderer, 250, 375, $"Noise", c_white);



        engine.DrawAudioBuffer(winAudioRenderer, 10, 520, 300, 100, c_white, drawbuffermix, 1);
        engine.DrawText(winAudioRenderer, 250, 525, $"Mix", c_white);

    }

    public void RenderPattern()
    {

        engine.ClearScreen(winPatternTableRenderer, c_dgray);

        DrawPatternAndPalette(winPatternTableRenderer, 3, 3);
        //Coord mousePos = engine.mouse.GetMousePos();
        //engine.DrawText(winPatternTableRenderer, 10, 10, $"X:{mousePos.x} Y:{mousePos.y} {engine.mouse.Button1Clicked}", c_white);

    }

    public void RenderRAM()
    {
        engine.ClearScreen(winRAMInfoRenderer, c_dgray);


        DrawRam(winRAMInfoRenderer, 10, 10, 0x0000, 64, 32);

        //Coord mousePos = engine.mouse.GetMousePos();
        //engine.DrawText(winROMInfoRenderer, 10, 10, $"X:{mousePos.x} Y:{mousePos.y} {engine.mouse.Button1Clicked}", c_white);
    }


    public void RenderMain()
    {

            engine.ClearScreen(winMainRenderer, c_dgray);
            Coord mousePos = engine.mouse.GetMousePos();
            engine.DrawText(winMainRenderer, 10, 10, $"X:{mousePos.x} Y:{mousePos.y} {engine.mouse.Button1Clicked}", c_white);
        

        
    }

    public void RenderNESWithAudio()
    {
        engine.ClearScreen(winNESRenderer, c_red);

        // get controls - check out keys first-
        // if we have a game controller hooked up, let's make the keyboard player 2

        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_x)) a_pressed = true;
        else a_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_z)) b_pressed = true;
        else b_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_a)) sel_pressed = true;
        else sel_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_s)) st_pressed = true;
        else st_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_i)) up_pressed = true;
        else up_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_k)) down_pressed = true;
        else down_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_j)) left_pressed = true;
        else left_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_l)) right_pressed = true;
        else right_pressed = false;

        gamecontroller_a_pressed = false;
        gamecontroller_b_pressed = false;
        gamecontroller_sel_pressed = false;
        gamecontroller_st_pressed = false;
        gamecontroller_up_pressed = false;
        gamecontroller_down_pressed = false;
        gamecontroller_left_pressed = false;
        gamecontroller_right_pressed = false;

        if (engine.gamepad.a_pressed) gamecontroller_a_pressed = true;
        if (engine.gamepad.b_pressed) gamecontroller_b_pressed = true;
        if (engine.gamepad.sel_pressed) gamecontroller_sel_pressed = true;
        if (engine.gamepad.st_pressed) gamecontroller_st_pressed = true;
        if (engine.gamepad.up_pressed) gamecontroller_up_pressed = true;
        if (engine.gamepad.down_pressed) gamecontroller_down_pressed = true;
        if (engine.gamepad.left_pressed) gamecontroller_left_pressed = true;
        if (engine.gamepad.right_pressed) gamecontroller_right_pressed = true;


        if (engine.gamepad.noGamepad)
        {
            nes.controller[0] = 0x00;
            nes.controller[0] |= (byte)(a_pressed ? 0x01 : 0x00);
            nes.controller[0] |= (byte)(b_pressed ? 0x02 : 0x00);
            nes.controller[0] |= (byte)(sel_pressed ? 0x04 : 0x00);
            nes.controller[0] |= (byte)(st_pressed ? 0x08 : 0x00);
            nes.controller[0] |= (byte)(up_pressed ? 0x10 : 0x00);
            nes.controller[0] |= (byte)(down_pressed ? 0x20 : 0x00);
            nes.controller[0] |= (byte)(left_pressed ? 0x40 : 0x00);
            nes.controller[0] |= (byte)(right_pressed ? 0x80 : 0x00);
        }
        else
        {
            nes.controller[0] = 0x00;
            nes.controller[0] |= (byte)(gamecontroller_a_pressed ? 0x01 : 0x00);
            nes.controller[0] |= (byte)(gamecontroller_b_pressed ? 0x02 : 0x00);
            nes.controller[0] |= (byte)(gamecontroller_sel_pressed ? 0x04 : 0x00);
            nes.controller[0] |= (byte)(gamecontroller_st_pressed ? 0x08 : 0x00);
            nes.controller[0] |= (byte)(gamecontroller_up_pressed ? 0x10 : 0x00);
            nes.controller[0] |= (byte)(gamecontroller_down_pressed ? 0x20 : 0x00);
            nes.controller[0] |= (byte)(gamecontroller_left_pressed ? 0x40 : 0x00);
            nes.controller[0] |= (byte)(gamecontroller_right_pressed ? 0x80 : 0x00);

            nes.controller[1] = 0x00;
            nes.controller[1] |= (byte)(a_pressed ? 0x01 : 0x00);
            nes.controller[1] |= (byte)(b_pressed ? 0x02 : 0x00);
            nes.controller[1] |= (byte)(sel_pressed ? 0x04 : 0x00);
            nes.controller[1] |= (byte)(st_pressed ? 0x08 : 0x00);
            nes.controller[1] |= (byte)(up_pressed ? 0x10 : 0x00);
            nes.controller[1] |= (byte)(down_pressed ? 0x20 : 0x00);
            nes.controller[1] |= (byte)(left_pressed ? 0x40 : 0x00);
            nes.controller[1] |= (byte)(right_pressed ? 0x80 : 0x00);
        }


        if (engine.keyboard.WasKeyPressed(SDL2.SDL.SDL_Keycode.SDLK_q) || engine.myWindows["NES"].isClosed())
        {
            userQuit = true;
            RunEmulation = false;
            engine.myWindows["NES"].Close();
            //engine.Close();
        }

        if (RunEmulation && !userQuit)
        {



            if (engine.keyboard.WasKeyPressed(SDL2.SDL.SDL_Keycode.SDLK_r))
            {
                nes.Reset();
            }

            //if (ResidualTime > 0f)
            //{
            //    ResidualTime -= engine.engineTime.TotalTime / 1000;
            //}
            //else
            //{
            //    ResidualTime += (1f / 60f) - (engine.engineTime.TotalTime / 1000);
            //    do
            //    {
            //        nes.Clock();
            //    } while (!nes.ppu.FrameComplete);
            //    nes.ppu.FrameComplete = false;
            //}

            engine.DrawSprite(engine.myWindows["NES"].Renderer, nes.ppu.GetScreen(), 0, 0, Flip.NONE);

        }


    }

    public void RenderNES()
    {
        RenderNESWithAudio();
        //RenderNESWithoutAudio();
    }

    public void RenderNESWithoutAudio()
    {
        engine.ClearScreen(winNESRenderer, c_red);

        // get controls - check out keys first
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_x)) a_pressed = true;
        else a_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_z)) b_pressed = true;
        else b_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_a)) sel_pressed = true;
        else sel_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_s)) st_pressed = true;
        else st_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_i)) up_pressed = true;
        else up_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_k)) down_pressed = true;
        else down_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_j)) left_pressed = true;
        else left_pressed = false;
        if (engine.keyboard.IsKeyDown(SDL2.SDL.SDL_Keycode.SDLK_l)) right_pressed = true;
        else right_pressed = false;

        if (engine.gamepad.a_pressed) a_pressed = true;
        if (engine.gamepad.b_pressed) b_pressed = true;
        if (engine.gamepad.sel_pressed) sel_pressed = true;
        if (engine.gamepad.st_pressed) st_pressed = true;
        if (engine.gamepad.up_pressed) up_pressed = true;
        if (engine.gamepad.down_pressed) down_pressed = true;
        if (engine.gamepad.left_pressed) left_pressed = true;
        if (engine.gamepad.right_pressed) right_pressed = true;

        nes.controller[0] = 0x00;
        nes.controller[0] |= (byte)(a_pressed ? 0x01 : 0x00);
        nes.controller[0] |= (byte)(b_pressed ? 0x02 : 0x00);
        nes.controller[0] |= (byte)(sel_pressed ? 0x04 : 0x00);
        nes.controller[0] |= (byte)(st_pressed ? 0x08 : 0x00);
        nes.controller[0] |= (byte)(up_pressed ? 0x10 : 0x00);
        nes.controller[0] |= (byte)(down_pressed ? 0x20 : 0x00);
        nes.controller[0] |= (byte)(left_pressed ? 0x40 : 0x00);
        nes.controller[0] |= (byte)(right_pressed ? 0x80 : 0x00);

        if (engine.keyboard.WasKeyPressed(SDL2.SDL.SDL_Keycode.SDLK_q) || engine.myWindows["NES"].isClosed())
        {
            userQuit = true;
            RunEmulation = false;
            engine.myWindows["NES"].Close();
            //engine.Close();
        }

        if (RunEmulation && !userQuit)
        {



            if (engine.keyboard.WasKeyPressed(SDL2.SDL.SDL_Keycode.SDLK_r))
            {
                nes.Reset();
            }

            if (ResidualTime > 0f)
            {
                ResidualTime -= engine.engineTime.TotalTime / 1000;
            }
            else
            {
                ResidualTime += (1f / 60f) - (engine.engineTime.TotalTime / 1000);
                do
                {
                    nes.Clock();
                } while (!nes.ppu.FrameComplete);
                nes.ppu.FrameComplete = false;
            }

            engine.DrawSprite(engine.myWindows["NES"].Renderer, nes.ppu.GetScreen(), 0, 0, Flip.NONE);

        }


        //engine.DrawText(winNESRenderer, 10, 10, "Emulation of NES GAME", c_white);
    }

    //public void Win2Render()
    //{
    //    engine.ClearScreen(win2renderer, c_black);
    //    engine.DrawText(win2renderer, 30, 30, "Not time", c_red);

    //}


    public void DrawPatternAndPalette(IntPtr renderer, int x, int y)
    {
        // Draw the palettes and pattern tables
        int margin = x;
        int starty = y;
        //int downfactor = 40;
        const int SwatchSize = 6;

        Rect[] palRects = new Rect[8];

        for (int p = 0; p < 8; p++)
        {
            for (int s = 0; s < 4; s++)
            {
                int factor = p * (SwatchSize * 5) + s * SwatchSize;
                engine.DrawQuadFilled(renderer, margin + factor, starty, margin + factor + SwatchSize, starty + SwatchSize, nes.ppu.GetColorFromPaletteRam((byte)p, (byte)s));
                // FillRect(265 + p * (SwatchSize * 5) + s * SwatchSize, 340, SwatchSize, SwatchSize, nes.ppu.GetColorFromPaletteRam(p, s));
            }
            //palRects[p] = new Rect(margin + factor, starty, margin + factor + SwatchSize, starty + SwatchSize);
            
        }

        engine.DrawQuad(renderer, margin + SelectedPalette * (SwatchSize * 5) - 1, starty, margin + SelectedPalette * (SwatchSize * 5) + (SwatchSize * 4), starty + SwatchSize, c_white);

        engine.DrawSprite(renderer, nes.ppu.GetPatternTable(0, SelectedPalette), margin, starty + 9, Flip.NONE);
        engine.DrawSprite(renderer, nes.ppu.GetPatternTable(1, SelectedPalette), margin + 132, starty + 9, Flip.NONE);
    }

    public void DrawRam(IntPtr renderer, int x, int y, ushort addr, int rows, int cols)
    {
        int cX = x;
        int cY = y;

        for (int row = 0; row < rows; row++)
        {
            string offset = "$" + Hex(addr, 4) + ": ";
            for (int col = 0; col < cols; col++)
            {
                offset += " " + Hex(nes.cpuRead(addr), 2);
                addr += 1;
            }
            engine.DrawText(renderer, cX, cY, offset, c_white);
            cY += 10;
        }
    }


    public void DrawNametables(IntPtr renderer, int x, int y)
    {
        byte[] nametable = new byte[4096];
        // get $2000 - 2FFF this is all of the nametable data. Each element
        for (int offset = 0; offset < 4096; offset++)
        {
            int addr = 0x2000 + offset;
            nametable[offset] = nes.cpuRead(addr, true);
        }

        //engine.DrawSprite(renderer, nes.ppu.GetPatternTable(nametable[0], ))
    }


    // Gimme hex
    public static string Hex(uint num, int pad)
    {
        return Convert.ToString(num, toBase: 16).ToUpper().PadLeft(pad, '0');
    }

}


