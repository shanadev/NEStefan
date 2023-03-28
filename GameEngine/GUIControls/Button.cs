using System;
using GameEngine;
namespace GameEngine.GUIControls;


public class Button : IControl
{
    private GameWindow _parent;
    private Rect _rect;
    private Engine _engine;
    private string _name;
    private string _text;
    private ScreenColor _foreColor;
    private ScreenColor _backColor;

    //public bool Toggle = false;

    private Action? _clickHandler;

    //public Button() { }
    public bool LatchButton;
    private bool _clickHeld = false;


    public Button(Engine eng, GameWindow win, string name, Rect rect, string text, ScreenColor foreColor, ScreenColor backColor, Action? clickHandler, bool latchButton = false, bool startLatched = false)
	{


        _engine = eng;
        _parent = win;
        _name = name;
        _rect = rect;

        _foreColor = foreColor;
        _backColor = backColor;

        if (text.Length > (_rect.GetWidth() / 8))
        {
            text = text.Substring(0, (_rect.GetWidth() / 8));
        }
        _text = text;
        _clickHandler = clickHandler;
        
        LatchButton = latchButton;
        Latched = startLatched;

        //Toggle = toggle;
    }

    public bool Latched = false;

    public GameWindow Parent { get { return this._parent; } set { this._parent = value; } }
    public string ControlName { get { return _name; } set { _name = value; } }

    public Rect ControlRect { get { return _rect; } set { _rect = value; } }

    public int RawValue { get; set; } = 0;

    public float Value { get; set; } = 0;

    public float ValueMin { get; set; } = 0;
    public float ValueMax { get; set; } = 1;
    public bool HasFocus { get; set; } = false;
    public bool Enabled { get; set; } = true;
    public bool IsDragging { get; set; } = false;

    public void Click(Coord mousePos)
    {
        if (_clickHandler != null) _clickHandler();

        _clickHeld = true;
    }

    public void Draw()
    {
        Coord mousePos = _engine.mouse.GetMousePos();

        bool mouseOver = _rect.IsPointInRect(mousePos) && _parent.hasMouseFocus();

        //f (LatchButton && Latched) mouseOver = true;


        if (_clickHeld)
        {
            _engine.DrawQuadFilled(_parent.Renderer, _rect.topLeft.x, _rect.topLeft.y, _rect.bottomRight.x, _rect.bottomRight.y, new ScreenColor(150,150,150,255));
            _engine.DrawText(_parent.Renderer, _rect.topLeft.x + ((_rect.GetWidth() - (8 * _text.Length)) / 2), _rect.topLeft.y + ((_rect.GetHeight() - 8) / 2), _text, _backColor);
        }
        else if (mouseOver && !Latched)
        {
            _engine.DrawQuadFilled(_parent.Renderer, _rect.topLeft.x, _rect.topLeft.y, _rect.bottomRight.x, _rect.bottomRight.y, _foreColor);
            _engine.DrawText(_parent.Renderer, _rect.topLeft.x + ((_rect.GetWidth() - (8 * _text.Length)) / 2), _rect.topLeft.y + ((_rect.GetHeight() - 8) / 2), _text, _backColor);
        }
        else if (Latched)
        {
            _engine.DrawQuadFilled(_parent.Renderer, _rect.topLeft.x, _rect.topLeft.y, _rect.bottomRight.x, _rect.bottomRight.y, new ScreenColor(0, 100, 30, 255));
            _engine.DrawText(_parent.Renderer, _rect.topLeft.x + ((_rect.GetWidth() - (8 * _text.Length)) / 2), _rect.topLeft.y + ((_rect.GetHeight() - 8) / 2), _text, _backColor);
        }
        else
        {
            _engine.DrawQuad(_parent.Renderer, _rect.topLeft.x, _rect.topLeft.y, _rect.bottomRight.x, _rect.bottomRight.y, _foreColor);
            _engine.DrawText(_parent.Renderer, _rect.topLeft.x + ((_rect.GetWidth() - (8 * _text.Length)) / 2), _rect.topLeft.y + ((_rect.GetHeight() - 8) / 2), _text, _foreColor);

        }

    }

    public void MouseUp()
    {
        //throw new NotImplementedException();
        _clickHeld = false;

        if (LatchButton)
        {
            if (Latched)
            {
                Latched = false;
            }
            else
            {
                Latched = true;
            }
        }
    }


    public void Update()
    {


    }
}

