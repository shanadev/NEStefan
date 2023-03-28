
using SDL2;
using GameEngine.GUIControls;

namespace GameEngine;



public class GameWindowList<GameWindow>
{
    private GameWindow[] arr = new GameWindow[100];
}


/// <summary>
/// A window object
/// </summary>
public unsafe class GameWindow
{
    // dependencies set when constructed
    private WindowSize mWindowSize;

    public string WindowTitle;

    private uint mWindowId;
    private bool mMouseFocus;
    private bool mKeyboardFocus;
    //private bool mFullScreen;
    private bool mMinimized;
    private bool mShown;

    // Not sure about these
    private Action renderFrame;
    private IntPtr mWindow;
    public IntPtr Renderer;
    private bool closeFlag = false;

    public GUI ControlGUI;
    private Engine mEngine;

    public GameWindow(Engine engine, WindowSize size, string title, Action renderAction)
    {
        mEngine = engine;

        mWindow = IntPtr.Zero;
        Renderer = IntPtr.Zero;
        mMouseFocus = false;
        mKeyboardFocus = false;
        //mFullScreen = false;
        mMinimized = false;

        WindowTitle = title;
        mWindowSize = size;

        renderFrame = renderAction;
        // Initialize SDL, the window and the renderer

        this.ControlGUI = new GUI(mEngine, this);
    }

    public bool Init()
    {
        try
        {
            mWindow = SDL.SDL_CreateWindow(WindowTitle,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                mWindowSize.Width,
                mWindowSize.Height,
                SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            if (mWindow == IntPtr.Zero) throw new ApplicationException(SDL.SDL_GetError());

            mMouseFocus = true;
            mKeyboardFocus = true;


            Renderer = SDL.SDL_CreateRenderer(mWindow,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC |
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            if (Renderer == IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(mWindow);
                mWindow = IntPtr.Zero;
                throw new ApplicationException(SDL.SDL_GetError());
            }
            else
            {
                SDL.SDL_SetRenderDrawColor(Renderer, 0xCC, 0xCC, 0xCC, 0xCC);
                mWindowId = SDL.SDL_GetWindowID(mWindow);
                mShown = true;
            }
        }
        catch (ApplicationException ex)
        {
            Console.WriteLine($"Problem Initializing Window: {ex.Message}");
        }


        return (mWindow != IntPtr.Zero && Renderer != IntPtr.Zero);
    }

    public void Render()
    {
        if (!mMinimized)
        {
            SDL.SDL_RenderSetScale(Renderer, mWindowSize.PixelSize, mWindowSize.PixelSize);
            renderFrame();

            this.ControlGUI.Update();
            this.ControlGUI.Draw();

            //SDL.SDL_SetRenderDrawColor(mRenderer, 0xCC, 0xCC, 0xCC, 0xFF);
            //rSDL.SDL_RenderClear(mRenderer);
            SDL.SDL_RenderSetScale(Renderer, mWindowSize.PixelSize, mWindowSize.PixelSize);

            SDL.SDL_RenderPresent(Renderer);
        }
    }

    public void HandleEvent(SDL.SDL_Event e)
    {
        if (e.type == SDL.SDL_EventType.SDL_WINDOWEVENT && e.window.windowID == mWindowId)
        {
            switch (e.window.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                    mShown = true;
                    break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                    mShown = false;
                    break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    mMouseFocus = true;
                    break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    mMouseFocus = false;
                    break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    mKeyboardFocus = true;
                    break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    mKeyboardFocus = false;
                    break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                    closeFlag = true;
                    SDL.SDL_HideWindow(mWindow);
                    break;
            
            }
        }
    }

    public void Focus()
    {
        if (!mShown)
        {
            SDL.SDL_ShowWindow(mWindow);
            
        }

        SDL.SDL_RaiseWindow(mWindow);
        closeFlag = false;
    }

    public void Free()
    {
        SDL.SDL_DestroyWindow(mWindow);
        SDL.SDL_DestroyRenderer(Renderer);
    }

    public bool hasMouseFocus()
    {
        return mMouseFocus;
    }

    public bool hasKeyboardFocus()
    {
        return mKeyboardFocus;
    }

    public bool isMininmized()
    {
        return mMinimized;
    }

    public bool isShown()
    {
        return mShown;
    }

    public bool isClosed()
    {
        return closeFlag;
    }


    public void Close()
    {
        closeFlag = true;
    }

    ~GameWindow()
    {
        SDL.SDL_DestroyWindow(mWindow);
        SDL.SDL_DestroyRenderer(Renderer);
    }
}

