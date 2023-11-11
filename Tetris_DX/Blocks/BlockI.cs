namespace Tetris_DX.Blocks;

using Microsoft.Xna.Framework;

internal class BlockI : BlockBase
{
    public override BlockType Type => BlockType.I;

    protected override Point StartOffset => new(-1, 3);

    protected override Point[][] Tiles => new Point[][]
    {
        new Point[] { new(0, 1), new(1, 1), new(2, 1), new(3, 1) },
        new Point[] { new(2, 0), new(2, 1), new(2, 2), new(2, 3) },
        new Point[] { new(0, 2), new(1, 2), new(2, 2), new(3, 2) },
        new Point[] { new(1, 0), new(1, 1), new(1, 2), new(1, 3) },
    };
}
