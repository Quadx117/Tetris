namespace Tetris_DX.GameState;

/// <summary>
/// The various movement direction available in the game.
/// The player can only move the piece towards the left or
/// right.
/// </summary>
internal enum MovementDirection
{
    /// <summary>
    /// The player is not moving the Tetromino.
    /// </summary>
    None,

    /// <summary>
    /// The player is trying to move the Tetromino towards the left.
    /// </summary>
    Left,

    /// <summary>
    /// The player is trying to move the Tetromino towards the right.
    /// </summary>
    Right,
}
