namespace Tetris_DX.Components;

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
}
