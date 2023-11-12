namespace Tetris_DX.Blocks;

using Microsoft.Xna.Framework;
using System.Collections.Generic;

internal abstract class BlockBase
{
    public abstract BlockType Type { get; }
    protected abstract Point StartOffset { get; }
    protected abstract Point[][] Tiles { get; }

    private int _rotationState;
    private Point _offset;

    public BlockBase()
    {
        _offset = new Point(StartOffset.Y, StartOffset.X);
    }

    public void MoveUp()
    {
        Move(0, -1);
    }

    public void MoveDown()
    {
        Move(0, 1);
    }

    public void MoveLeft()
    {
        Move(-1, 0);
    }

    public void MoveRight()
    {
        Move(1, 0);
    }

    public void RotateCW()
    {
        _rotationState = (_rotationState + 1) % Tiles.Length;
    }

    public void RotateCCW()
    {
        if (_rotationState == 0)
        {
            _rotationState = Tiles.Length - 1;
        }
        else
        {
            _rotationState--;
        }
    }

    public void Reset()
    {
        _rotationState = 0;
        _offset.Y = StartOffset.Y;
        _offset.X = StartOffset.X;
    }

    public IEnumerable<Point> TilePositions()
    {
        foreach (Point p in Tiles[_rotationState])
        {
            yield return new Point(p.X + _offset.X, p.Y + _offset.Y);
        }
    }

    private void Move(int xAxis, int yAxis)
    {
        _offset.Y += yAxis;
        _offset.X += xAxis;
    }
}
