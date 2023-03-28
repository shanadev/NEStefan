using SDL2;

using System;
using GameEngine.Input;
using GameEngine.GUIControls;

namespace GameEngine;


/// <summary>
/// The Engine class is responsible for windows, graphics and sound
/// using SDL. This engine is agnostic of the application, it is there for
/// easily setting up a specific size and pixel size window for
/// display of any game or visualization application
/// * Time - offers game timers used for syncing
/// * Controls - capture key, mouse and joystick input and provide easy way to see what is being read
/// * Windows - methods for opening new windows, this class holds an array of windows, each window will have a refresh method that will be overridden
/// * Drawing - methods for drawing pixels, object, sprites, text, etc.
/// * Audio - methods for playing souds and mixing them
///
/// Usage:
/// First start the engine - do any initialization you want
/// var myEngine = new Engine();
/// 
/// Then open a window - pass in the
/// * window settings
/// * window title
/// * function for rendering a frame
/// * A Key for the dictionary that holds all windows in the Engine
/// var myMainWindow = myEngine.CreateWindow(WindowSettings, WindowTitle, renderFrameFunc);
///
/// Start the engine
/// myEngine.Run();
/// 
/// </summary>
public partial class Engine
{
    public Dictionary<string, GameWindow> myWindows = new Dictionary<string, GameWindow>();

    private IntPtr gameController;

    private bool quit = false;

    public Keyboard keyboard;
    public Mouse mouse;
    public Gamepad gamepad;

    public Time engineTime;


    // Mouse
    //public int mouseX;
    //public int mouseY;
    //public bool leftMouseDown = false;
    //public bool rightMouseDown = false;


    /// <summary>
    /// Get elapsed time in milliseconds
    /// </summary>
    /// <returns></returns>
    //public ulong GetElapsedTime()
    //{
    //    /// TODO: Move this to a timer section
    //    return SDL.SDL_GetTicks64();
    //}

    public void Delay(ulong time)
    {
        SDL.SDL_Delay((uint)time);
    }

    public Engine()
    {
        InitTextDrawing();

        //initButtons();

        engineTime = new Time();
        keyboard = new Keyboard(this);
        mouse = new Mouse(this);
        gamepad = new Gamepad(this);
    }

    /// <summary>
    /// Must call this to initialize the video and controllers
    /// </summary>
    /// <returns>success</returns>
    /// <exception cref="ApplicationException"></exception>
    public bool init()
    {
        bool success = true;

        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            Console.WriteLine("No video worky");
            success = false;
        }
        else
        {
            if (SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "1") != SDL.SDL_bool.SDL_TRUE)
            {

            }
        }

        if (SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK) != 0) throw new ApplicationException(SDL.SDL_GetError());

        var numSticks = SDL.SDL_NumJoysticks();
        if (numSticks < 1)
        {
            Console.WriteLine("No joysticks connected");
            this.gamepad.noGamepad = true;
            //success = false;
        }
        else
        {
            //Console.WriteLine($"{SDL.SDL_IsGameController(0)}");
            gameController = SDL.SDL_GameControllerOpen(0);
            if (gameController == IntPtr.Zero) throw new ApplicationException(SDL.SDL_GetError());
            this.gamepad.noGamepad = false;
        }

        return success;
    }


    public IntPtr CreateWindow(WindowSize size, string title, Action renderAction, string key)
    {
        GameWindow newWin = new GameWindow(this, size, title, renderAction);
        myWindows.Add(key, newWin);
        myWindows[key].Init();

        return myWindows[key].Renderer;
    }


    public void Run()
    {
        
        SDL.SDL_Event e;
        quit = false;
        while (!quit)
        {
            engineTime.Tick();

            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    quit = true;
                }

                // capture events for MOUSE and KEYS and GAMEPADS
                //switch (e.type)
                //{
                //    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                //        mouseX = e.motion.x;
                //        mouseY = e.motion.y;
                //        break;
                //    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                //        switch (e.button.button)
                //        {
                //            case (byte)SDL.SDL_BUTTON_LEFT:
                //                leftMouseDown = true;
                //                break;
                //            case (byte)SDL.SDL_BUTTON_RIGHT:
                //                rightMouseDown = true;
                //                break;
                //        }
                //        break;
                //    case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                //        switch (e.button.button)
                //        {
                //            case (byte)SDL.SDL_BUTTON_LEFT:
                //                leftMouseDown = false;
                //                break;
                //            case (byte)SDL.SDL_BUTTON_RIGHT:
                //                rightMouseDown = false;
                //                break;
                //        }
                //        break;
                //}

                mouse.HandleEvents(e);
                keyboard.HandleEvents(e);
                gamepad.HandleEvents(e);

                bool allWindowsClosed = true;
                bool noFocusedWindows = true;

                foreach (var win in myWindows)
                {
                    win.Value.HandleEvent(e);
                    if (win.Value.isShown())
                    {
                        allWindowsClosed = false;
                    }
                    if (win.Value.hasKeyboardFocus() || win.Value.hasMouseFocus())
                    {
                        noFocusedWindows = false;
                    }
                }

            
                //myWindow.HandleEvent(e);

                if (allWindowsClosed)
                {
                    quit = true;
                }
                //myWindow.Focus();
                if (noFocusedWindows)
                {
                    //this.myWindows.ElementAt(0).Value.Focus();
                }
                
            }
            //myWindow.Focus();

            for (int i = 0; i < myWindows.Count; i++)
            {
                myWindows.ElementAt(i).Value.Render();

                //myWindows.ElementAt(i).Value.ControlGUI.Update();
                //myWindows.ElementAt(i).Value.ControlGUI.Draw();
            }

            //foreach (var win in myWindows)
            //{
            //    win.Value.Render();
            //}

            //RenderButtons(mouseX, mouseY, leftMouseDown);

            //myWindow.Render();
        }
    }

    public void Close()
    {
        foreach (var win in myWindows)
        {
            win.Value.Free();
        }
        //myWindow.Free();
        SDL.SDL_Quit();
    }





}

