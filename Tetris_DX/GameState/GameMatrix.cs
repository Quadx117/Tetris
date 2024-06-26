﻿namespace Tetris_DX.GameState;

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

    /// <summary>
    /// Returns <c>true</c> if the matrix is completely empty, <c>false</c> otherwise.
    /// </summary>
    /// <returns><c>true</c> if the matrix is completely empty, <c>false</c> otherwise.</returns>
    public bool IsEmpty()
    {
        for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                if (_grid[rowIndex, columnIndex] != BlockType.None)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsEmpty(Point p)
    {
        return IsInsideMatrix(p) &&
               _grid[p.Y, p.X] == BlockType.None;
    }

    public bool IsRowFull(int rowIndex)
    {
        for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
        {
            if (_grid[rowIndex, columnIndex] == BlockType.None)
            {
                return false;
            }
        }

        return true;
    }

    public int ClearFullRows()
    {
        int cleared = 0;

        for (int rowIndex = RowCount - 1; rowIndex >= 0; rowIndex--)
        {
            if (IsRowFull(rowIndex))
            {
                ClearRow(rowIndex);
                cleared++;
            }
            else if (cleared > 0)
            {
                MoveRowDown(rowIndex, cleared);
            }
        }

        return cleared;
    }

    public void Reset()
    {
        for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                _grid[rowIndex, columnIndex] = BlockType.None;
            }
        }
    }

    private void ClearRow(int rowIndex)
    {
        for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
        {
            _grid[rowIndex, columnIndex] = BlockType.None;
        }
    }

    private void MoveRowDown(int rowIndex, int numRows)
    {
        for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
        {
            _grid[rowIndex + numRows, columnIndex] = _grid[rowIndex, columnIndex];
            _grid[rowIndex, columnIndex] = BlockType.None;
        }
    }
}
