namespace Tetris_DX.GameState;

/// <summary>
/// The various rotation direction available in the game.
/// </summary>
internal enum RotationDirection
{
    /// <summary>
    /// The player is not rotating the Tetromino.
    /// </summary>
    None,

    /// <summary>
    /// The player is trying to rotate the Tetromino clockwise.
    /// </summary>
    Clockwise,

    /// <summary>
    /// The player is trying to rotate the Tetromino counter-clockwise.
    /// </summary>
    CounterClockwise,
}
