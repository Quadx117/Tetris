namespace Tetris_DX.GameState;

using System;
using System.Collections.Generic;
using Tetris_DX.Blocks;

internal class BlockQueue
{
    private readonly BlockBase[] _blocks =
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

    // NOTE(PERE): We use 14 pieces, which corresponds to 2 full bags, so we can
    // eventually show the next 6 pieces and always have at least 6 pieces in the
    // queue. We will add another bag of pieces when there are 7 pieces left.
    private readonly Queue<BlockBase> _queue = new(14);

    private List<BlockType> _blockBag = new()
    {
        BlockType.I,
        BlockType.J,
        BlockType.L,
        BlockType.O,
        BlockType.S,
        BlockType.T,
        BlockType.Z,
    };

    public BlockQueue()
    {
        FillQueue();
    }

    public BlockBase Dequeue()
    {
        BlockBase result = _queue.Dequeue();

        if (_queue.Count < 8)
        {
            FillQueue();
        }

        // NOTE(PERE): We need to reset the block to its original state since we
        // use the same instance of a block again and again.
        result.Reset();
        return result;
    }

    public BlockType PeekNextBlockType()
    {
        return _queue.Peek().Type;
    }

    public void Reset()
    {
        _queue.Clear();
        FillQueue();
    }

    private void FillQueue()
    {
        // We make sure that we have enough pieces to be able to show the next 6.
        // Since this is called only when we start a new game or when we have less
        // then 8 pieces, we always have at least 7 pieces in the queue.
        while (_queue.Count < 14)
        {
            List<BlockType> tmp = new();
            while (_blockBag.Count > 0)
            {
                BlockType blockType = _blockBag[_random.Next(_blockBag.Count)];
                _ = _blockBag.Remove(blockType);
                tmp.Add(blockType);
                // NOTE(PERE): We need to subtract 1 because the enum has BlockType.None
                // as the first value
                _queue.Enqueue(_blocks[(int)blockType - 1]);
            }

            _blockBag = tmp;
        }
    }
}
