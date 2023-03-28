using System;
using System.Drawing;

namespace GameEngine;

public struct ScreenColor
{
    public byte red;
    public byte green;
    public byte blue;
    public byte alpha;

    public ScreenColor(byte r, byte g, byte b, byte a)
    {
        red = r;
        green = g;
        blue = b;
        alpha = a;
    }
}

public enum Flip
{
    NONE = 0,
    HORIZ = 1,
    VERT = 2
}

public enum WindowSettingTypes
{
    NES,
    NES_Double,
    NES_Triple,
    NES_Quad,
    SD,
    SD_Double,
    HD,
    HD_Double,
    FullHD,
    QuadHD,
    UHD,
    FullUHD,
    CPUView,
    PatternView,
    AudioView,
    RAMView,
    MainWin
}

public struct WindowSize
{
    public int Width;
    public int Height;
    public int PixelSize;

    public WindowSize(int width, int height, int pixelSize)
    {
        this.Width = width;
        this.Height = height;
        this.PixelSize = pixelSize;
    }

    public float AspectRatio
    {
        get { return (float)Height / (float)Width; }
    }
}

public class KeyEventArgs : EventArgs
{
    public string? KeyCode { get; set; }
}


public struct Coord
{
    public Coord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public int x;
    public int y;
}

public struct Rect
{
    public Coord topLeft;
    public Coord bottomRight;

    public Rect(Coord topLeftPos, Coord bottomRightPos)
    {
        this.topLeft = topLeftPos;
        this.bottomRight = bottomRightPos;
    }

    public Rect(int x, int y, int width, int height)
    {
        this.topLeft = new Coord(x, y);
        this.bottomRight = new Coord(x + width, y + height);
    }


    public bool IsPointInRect(Coord pos)
    {
        bool output = false;

        if (pos.x >= topLeft.x &&
            pos.x <= bottomRight.x &&
            pos.y <= bottomRight.y &&
            pos.y >= topLeft.y)
        {
            output = true;
        }

        return output;
    }

    public int GetWidth()
    {
        return this.bottomRight.x - this.topLeft.x;
    }

    public int GetHeight()
    {
        return this.bottomRight.y - this.topLeft.y;
    }


}


