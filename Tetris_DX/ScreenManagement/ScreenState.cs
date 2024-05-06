namespace Tetris_DX.ScreenManagement;

/// <summary>
/// The possible states that a screen can be in.
/// </summary>
public enum ScreenState
{
    /// <summary>
    /// State used when the screen is actively transitioning on.
    /// This is used to animate the screen when it is first diplayed.
    /// </summary>
    TransitionOn,

    /// <summary>
    /// State used when the screen is being displayed on not transitioning.
    /// </summary>
    Active,

    /// <summary>
    /// State used when the screen is actively transitioning off.
    /// This is used to animate the screen when it is removed.
    /// </summary>
    TransitionOff,

    /// <summary>
    /// State used when the screen should not be displayed.
    /// </summary>
    Hidden,
}
