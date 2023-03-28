using System;
namespace GameEngine.GUIControls;


public class Knob : IControl
{
    private Engine _engine;

    private int _rawValue = 0;
    private int _rawMin = 0;
    private int _rawMax = 340;
    private int _rawOffset = 100;

    private bool _knobPressed = false;
    private int _knobRadius = 20;
    private int _knobAngle = 100;

    private int _knobControlWidth = 250;
    private int _knobControlHeight = 41;

    private GameWindow _parent;

    private Coord _knobCenter;

    private Coord _lastMousePos = new Coord(0, 0);

    public GameWindow Parent { get { return this._parent; } }

    public string ControlName { get; set; }

    public Rect ControlRect { get; }

    public int RawValue
    {
        get
        {
            return _rawValue;
        }
    }

    public float Value
    {
        get
        {
            float vscale = (float)_rawValue / (float)_rawMax;
            float vnorm = ValueRange;
            return ValueMin + (vnorm * vscale);
        }
    }


    public float ValueMin { get; set; }
    public float ValueMax { get; set; }
    public bool HasFocus { get; set; } = false;
    public bool Enabled { get; set; } = true;
    public bool IsDragging { get; set; } = false;

    public float ValueRange
    {
        get
        {
            return ValueMax - ValueMin;
        }
    }

    GameWindow IControl.Parent { get { return _parent; } set { _parent = value; } }

    /// <summary>
    /// Create a new knob control
    /// </summary>
    /// <param name="name">Name of the control</param>
    /// <param name="controlPos">This is the TOP LEFT of the control box where the circle will be</param>
    /// <param name="valueMin">float value minimum that the knob represents</param>
    /// <param name="valueMax">float value max that the knob represents</param>
    /// <param name="valueStart">float value of the knob starting position</param>
    public Knob(Engine eng, GameWindow parentWin, string name, Coord controlPos, float valueMin, float valueMax, float valueStart = 0.0f)
    {
        _engine = eng;
        this._parent = parentWin;

        this.ControlName = name;
        this.ControlRect = new Rect(controlPos, new Coord(controlPos.x + _knobControlWidth, controlPos.y + _knobControlHeight));
        this.ValueMin = valueMin;
        this.ValueMax = valueMax;

        float scale = (valueStart - valueMin) / (valueMax - valueMin);
        int rawstart = (int)(scale * _rawMax);
        this._rawValue = rawstart;

        //_knobAngle = (_rawMax - _rawValue) + _rawOffset;
        _knobAngle = _rawValue + _rawOffset;

        _knobCenter = new Coord(ControlRect.topLeft.x + _knobRadius, ControlRect.topLeft.y + _knobRadius);

    }

    public void Update()
    {


    }

    public Knob()
    {
    }

    public void Click(Coord mousePos)
    {
        if (!_knobPressed)
        {
            if (Engine.PointInRadius(_knobCenter.x, _knobCenter.y, _knobRadius, mousePos.x, mousePos.y))
            {
                _knobPressed = true;
                _lastMousePos = mousePos;
            }
        }
    }

    public void Draw()
    {
        Coord mousePos = _engine.mouse.GetMousePos();
        if (_knobPressed)
        {
            int mouseMovement = _lastMousePos.y - mousePos.y;
            if (mouseMovement != 0)
            {
                _rawValue += mouseMovement;
                if (_rawValue > _rawMax)
                {
                    _rawValue = _rawMax;
                }
                if (_rawValue < _rawMin)
                {
                    _rawValue = _rawMin;
                }

                //int newAngle = (_rawMax - _rawValue) + _rawOffset;
                int newAngle = _rawValue + _rawOffset;
                if (newAngle > 360)
                {
                    newAngle -= 360;
                }

                _knobAngle = newAngle;
            }

            _lastMousePos = mousePos;

            _engine.DrawCircleFilled_Scanning(this._parent.Renderer, _knobCenter.x, _knobCenter.y, _knobRadius, new ScreenColor(255, 255, 255, 255));

            double endX = _knobCenter.x + _knobRadius * Math.Cos(_knobAngle * (Math.PI / 180));
            double endY = _knobCenter.y + _knobRadius * Math.Sin(_knobAngle * (Math.PI / 180));
            double startX = _knobCenter.x + (_knobRadius / 2.5) * Math.Cos(_knobAngle * (Math.PI / 180));
            double startY = _knobCenter.y + (_knobRadius / 2.5) * Math.Sin(_knobAngle * (Math.PI / 180));
            _engine.DrawLine(this._parent.Renderer, ((int)startX) + 1, ((int)startY), ((int)endX) + 1, ((int)endY), new ScreenColor(0, 0, 0, 255));
            _engine.DrawLine(this._parent.Renderer, ((int)startX), ((int)startY) + 1, ((int)endX), ((int)endY) + 1, new ScreenColor(0, 0, 0, 255));
            _engine.DrawLine(this._parent.Renderer, ((int)startX), ((int)startY), ((int)endX), ((int)endY), new ScreenColor(0, 0, 0, 255));
        }
        else
        {
            _engine.DrawCircle(this._parent.Renderer, _knobCenter.x, _knobCenter.y, _knobRadius, new ScreenColor(255, 255, 255, 255));

            double endX = _knobCenter.x + _knobRadius * Math.Cos(_knobAngle * (Math.PI / 180));
            double endY = _knobCenter.y + _knobRadius * Math.Sin(_knobAngle * (Math.PI / 180));
            double startX = _knobCenter.x + (_knobRadius / 2.5) * Math.Cos(_knobAngle * (Math.PI / 180));
            double startY = _knobCenter.y + (_knobRadius / 2.5) * Math.Sin(_knobAngle * (Math.PI / 180));
            _engine.DrawLine(this._parent.Renderer, ((int)startX) + 1, ((int)startY), ((int)endX) + 1, ((int)endY), new ScreenColor(255, 255, 255, 255));
            _engine.DrawLine(this._parent.Renderer, ((int)startX), ((int)startY) + 1, ((int)endX), ((int)endY) + 1, new ScreenColor(255, 255, 255, 255));
            _engine.DrawLine(this._parent.Renderer, ((int)startX), ((int)startY), ((int)endX), ((int)endY), new ScreenColor(255, 255, 255, 255));
        }

        _engine.DrawText(this._parent.Renderer, _knobCenter.x + _knobRadius + 5, _knobCenter.y - 8, $"{ControlName}", new ScreenColor(255, 255, 255, 255));
        _engine.DrawText(this._parent.Renderer, _knobCenter.x + _knobRadius + 5, _knobCenter.y - 8 + 8, $"Value: {Value}", new ScreenColor(255, 255, 255, 255));
        //_engine.DrawText(_knobCenter.x + _knobRadius + 5, _knobCenter.y - 8 + 16, $"RAW: {_rawValue} - Angle: {_knobAngle}", new ScreenColor(255, 255, 255, 255));

    }

    public void MouseUp()
    {
        if (_knobPressed)
        {
            _knobPressed = false;
        }
    }
}