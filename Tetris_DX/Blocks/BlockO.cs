namespace Tetris_DX.Blocks;

using Microsoft.Xna.Framework;

internal class BlockO : BlockBase
{
    public override BlockType Type => BlockType.O;

    protected override Point StartOffset => new(0, 4);

    protected override Point[][] Tiles => new Point[][]
    {
        new Point[] { new(0, 0), new(0, 1), new(1, 0), new(1, 1) }
    };
}
