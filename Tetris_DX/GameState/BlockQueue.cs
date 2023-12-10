namespace Tetris_DX.GameState;

using System;
using System.Collections.Generic;
using Tetris_DX.Blocks;

internal class BlockQueue
{
    private readonly BlockBase[] _blocks = new BlockBase[]
    {
        new BlockI(),
        new BlockJ(),
        new BlockL(),
        new BlockO(),
        new BlockS(),
        new BlockT(),
        new BlockZ()
    };

    private readonly Random _random = new();

    private readonly Queue<BlockBase> _queue = new(7);

    public BlockQueue()
    {
        // TODO(PERE): Proper 7-bag random generator or 35-bag with 6 rolls
        BlockBase block = NextRandomBlock();
        do
        {
            _queue.Enqueue(block);
            BlockType lastBlockType = block.Type;
            do
            {
                block = NextRandomBlock();
            } while (block.Type == lastBlockType);

        } while (_queue.Count < 7);
    }

    public BlockBase Dequeue()
    {
        BlockBase result = _queue.Dequeue();
        // NOTE(PERE): We need to reset the block to its original state since we
        // use the same instance of a block again and again.
        result.Reset();
        _queue.Enqueue(NextRandomBlock());
        return result;
    }

    public BlockType PeekNextBlockType()
    {
        return _queue.Peek().Type;
    }

    private BlockBase NextRandomBlock()
    {
        return _blocks[_random.Next(_blocks.Length)];
    }
}
