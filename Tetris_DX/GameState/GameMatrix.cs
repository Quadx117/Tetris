namespace Tetris_DX.Components;

using Microsoft.Xna.Framework;
using Tetris_DX.Blocks;

internal class GameMatrix
{
    private readonly BlockType[,] _grid;

    public int RowCount { get; }
    public int ColumnCount { get; }

    public BlockType this[int rowIndex, int columnIndex]
    {
        get => _grid[rowIndex, columnIndex];
        set => _grid[rowIndex, columnIndex] = value;
    }

    public GameMatrix(int rowCount, int columnCount)
    {
        RowCount = rowCount;
        ColumnCount = columnCount;
        _grid = new BlockType[rowCount, columnCount];
    }

    public bool IsInsideMatrix(Point p)
    {
        return p.X >= 0 && p.X < ColumnCount &&
               p.Y >= 0 && p.Y < RowCount;
    }

    public bool IsEmpty(Point p)
    {
        return IsInsideMatrix(p) &&
               _grid[p.Y, p.X] == BlockType.None;
    }
}
