namespace Tetris_DX.Blocks;

using Microsoft.Xna.Framework;

internal class BlockL : BlockBase
{
    public override BlockType Type => BlockType.L;

    protected override Point StartOffset => new(0, 3);

    protected override Point[][] Tiles => new Point[][]
    {
        new Point[] {new(2, 0), new(0, 1), new(1, 1), new(2, 1)},
        new Point[] {new(1, 0), new(1, 1), new(1, 2), new(2, 2)},
        new Point[] {new(0, 1), new(1, 1), new(2, 1), new(0, 2)},
        new Point[] {new(0, 0), new(1, 0), new(1, 1), new(1, 2)}
    };
}
