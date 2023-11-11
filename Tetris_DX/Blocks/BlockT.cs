namespace Tetris_DX.Blocks;

using Microsoft.Xna.Framework;

internal class BlockT : BlockBase
{
    public override BlockType Type => BlockType.T;

    protected override Point StartOffset => new(0, 3);

    protected override Point[][] Tiles => new Point[][]
    {
        new Point[] {new(1, 0), new(0, 1), new(1, 1), new(2, 1)},
        new Point[] {new(1, 0), new(1, 1), new(2, 1), new(1, 2)},
        new Point[] {new(0, 1), new(1, 1), new(2, 1), new(1, 2)},
        new Point[] {new(1, 0), new(0, 1), new(1, 1), new(1, 2)}
    };
}
