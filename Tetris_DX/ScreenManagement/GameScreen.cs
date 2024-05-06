namespace Tetris_DX.ScreenManagement;

using System;
using Microsoft.Xna.Framework;

/// <summary>
/// Base class for all screens handled by the <see cref="ScreenManager"/> class.
/// A screen is a single layer that has update and draw logic, and which can be
/// combined with other layers to build up a complex menu system.
/// </summary>
public abstract class GameScreen
{
    private bool _otherScreenHasFocus;

    /// <summary>
    /// Gets or sets a value indicating whether this screen is a popup.
    /// Normally, when one screen is brought up over the top of another,
    /// the first screen will transition off to make room for the new
    /// one. This property indicates whether the screen is only a small
    /// popup, in which case screens underneath it do not need to bother
    /// transitioning off.
    /// </summary>
    public bool IsPopup { get; protected set; } = false;

    /// <summary>
    /// Gets or sets how long the screen takes to transition on when it is activated.
    /// </summary>
    public TimeSpan TransitionOnTime { get; protected set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets how long the screen takes to transition off when it is deactivated.
    /// </summary>
    public TimeSpan TransitionOffTime { get; protected set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the current percentage of the screen transition, ranging
    /// from zero (fully active, no transition) to one (transitioned
    /// fully off to nothing).
    /// </summary>
    public float TransitionPercentage { get; protected set; } = 1;

    /// <summary>
    /// Gets the current alpha of the screen transition, ranging
    /// from 1 (fully active, no transition) to 0 (transitioned
    /// fully off to nothing).
    /// </summary>
    public float TransitionAlpha => 1f - TransitionPercentage;

    /// <summary>
    /// Gets or sets the current screen transition state.
    /// </summary>
    public ScreenState ScreenState { get; protected set; } = ScreenState.TransitionOn;

    /// <summary>
    /// Gets or sets a value indicating whether this screen is exitingfor real:
    /// if set, the screen will automatically remove itself as soon as the
    /// transition finishes.
    /// There are two possible reasons why a screen might be transitioning
    /// off. It could be temporarily going away to make room for another
    /// screen that is on top of it, or it could be going away for good.
    /// </summary>
    public bool IsExiting { get; protected internal set; } = false;

    /// <summary>
    /// Gets a value indicating whether this screen is active and can respond to user input.
    /// </summary>
    public bool IsActive => !_otherScreenHasFocus &&
                            (ScreenState == ScreenState.TransitionOn ||
                             ScreenState == ScreenState.Active);

    /// <summary>
    /// Gets the manager that this screen belongs to.
    /// </summary>
    public ScreenManager ScreenManager { get; internal set; }

    // TODO(PERE): Add the possibility to use any of the 4 controller "slots"
    // even though the game does not support local multiplayer so that if a
    // user has multiple controllers connected a the same time, he can play
    // the game with any one of them.

    /// <summary>
    /// Gets the index of the player who is currently controlling this screen,
    /// or null if it is accepting input from any player. This is used to lock
    /// the game to a specific player profile. The main menu responds to input
    /// from any connected gamepad, but whichever player makes a selection from
    /// this menu is given control over all subsequent screens, so other gamepads
    /// are inactive until the controlling player returns to the main menu.
    /// </summary>
    public PlayerIndex? ControllingPlayer { get; internal set; }

    /// <summary>
    /// Load graphics content for the screen.
    /// </summary>
    public virtual void LoadContent() { }

    /// <summary>
    /// Unload content for the screen.
    /// </summary>
    public virtual void UnloadContent() { }

    /// <summary>
    /// Allows the screen to run logic, such as updating the transition percentage.
    /// Unlike <see cref="HandleInput(InputState)"/>, this method is called regardless
    /// of whether the screen is active, hidden, or in the middle of a transition.
    /// </summary>
    public virtual void Update(GameTime gameTime,
                               bool otherScreenHasFocus,
                               bool coveredByOtherScreen)
    {
        _otherScreenHasFocus = otherScreenHasFocus;

        if (IsExiting)
        {
            // If the screen is going away to die, it should transition off.
            ScreenState = ScreenState.TransitionOff;

            if (!UpdateTransition(gameTime, TransitionOffTime, 1))
            {
                // When the transition finishes, remove the screen.
                ScreenManager.RemoveScreen(this);
            }
        }
        else if (coveredByOtherScreen)
        {
            // If the screen is covered by another, it should transition off.
            if (UpdateTransition(gameTime, TransitionOffTime, 1))
            {
                // Still busy transitioning.
                ScreenState = ScreenState.TransitionOff;
            }
            else
            {
                // Transition finished!
                ScreenState = ScreenState.Hidden;
            }
        }
        else
        {
            // Otherwise the screen should transition on and become active.
            if (UpdateTransition(gameTime, TransitionOnTime, -1))
            {
                // Still busy transitioning.
                ScreenState = ScreenState.TransitionOn;
            }
            else
            {
                // Transition finished!
                ScreenState = ScreenState.Active;
            }
        }
    }

    /// <summary>
    /// Allows the screen to handle user input. Unlike <see cref="Update(GameTime, bool, bool)"/>,
    /// this method is only called when the screen is active, and not when some
    /// other screen has taken the focus.
    /// </summary>
    public virtual void HandleInput(InputManager input) { }

    /// <summary>
    /// This is called when the screen should draw itself.
    /// </summary>
    public virtual void Draw(GameTime gameTime) { }

    /// <summary>
    /// Tells the screen to go away. Unlike <see cref="ScreenManager.RemoveScreen(GameScreen)"/>,
    /// which instantly kills the screen, this method respects the transition
    /// timings and will give the screen a chance to gradually transition off.
    /// </summary>
    public void ExitScreen()
    {
        if (TransitionOffTime == TimeSpan.Zero)
        {
            // If the screen has a zero transition time, remove it immediately.
            ScreenManager.RemoveScreen(this);
        }
        else
        {
            // Otherwise flag that it should transition off and then exit.
            IsExiting = true;
        }
    }

    /// <summary>
    /// Helper for updating the screen transition percentage.
    /// </summary>
    /// <param name="gameTime">Time passed since the last call to <c>Update</c>.</param>
    /// <param name="transitionDuration">How long the transition should take.</param>
    /// <param name="direction">Positive 1 if we are transitioning in, negative 1
    /// if we are transitioning out.</param>
    /// <returns><c>false</c> if the screen has reached the end of its transition,
    /// <c>true</c> otherwise.</returns>
    private bool UpdateTransition(GameTime gameTime, TimeSpan transitionDuration, int direction)
    {
        // How much should we move by?
        float transitionDelta = transitionDuration == TimeSpan.Zero
                                    ? 1
                                    : (float)(gameTime.ElapsedGameTime.TotalMilliseconds / transitionDuration.TotalMilliseconds);

        // Update the transition percentage.
        TransitionPercentage += transitionDelta * direction;

        // Did we reach the end of the transition?
        if (direction < 0 && TransitionPercentage <= 0 ||
            direction > 0 && TransitionPercentage >= 1)
        {
            TransitionPercentage = MathHelper.Clamp(TransitionPercentage, 0, 1);
            return false;
        }

        // Otherwise we are still busy transitioning.
        return true;
    }
}
