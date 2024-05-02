namespace Tetris_DX;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

/// <summary>
/// Helper for reading input from keyboard and gamepad. This class tracks both
/// the current and previous state of the input devices, and implements query
/// methods for high level input actions such as "move up through the menu" or
/// "pause the game".
/// </summary>
public class InputManager
{
    // TODO(PERE): Should I keep these public or make it private?
    public KeyboardState CurrentKeyboardState;
    public KeyboardState LastKeyboardState;

    public GamePadState CurrentGamePadState;
    public GamePadState LastGamePadState;

    public bool GamePadWasConnected;

    public InputManager()
    {
        CurrentKeyboardState = new KeyboardState();
        LastKeyboardState = new KeyboardState();

        CurrentGamePadState = new GamePadState();
        LastGamePadState = new GamePadState();
    }

    /// <summary>
    /// Reads the latest state of the keyboard and gamepad.
    /// </summary>
    public void Update()
    {
        LastKeyboardState = CurrentKeyboardState;

        // NOTE(PERE): MonoGame doesn't support multiple keyboards plugged
        // into the same PC anymore.
        CurrentKeyboardState = Keyboard.GetState();

        LastGamePadState = CurrentGamePadState;
        CurrentGamePadState = GamePad.GetState(PlayerIndex.One);

        // Keep track of whether a gamepad has ever been
        // connected, so we can detect if it is unplugged.
        if (CurrentGamePadState.IsConnected)
        {
            GamePadWasConnected = true;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the key transitioned from up to down during this
    /// update, <c>false</c> otherwise.
    /// </summary>
    /// <param name="key">The key to evaluate.</param>
    /// <returns><c>true</c> if the key transitioned from up to down during this
    /// update, <c>false</c> otherwise.</returns>
    public bool IsKeyTransitionDown(Keys key)
    {
        return CurrentKeyboardState.IsKeyDown(key) &&
               LastKeyboardState.IsKeyUp(key);
    }

    /// <summary>
    /// Returns <c>true</c> if the key transitioned from down to up during this
    /// update, <c>false</c> otherwise.
    /// </summary>
    /// <param name="key">The key to evaluate.</param>
    /// <returns><c>true</c> if the key transitioned from down to up during this
    /// update, <c>false</c> otherwise.</returns>
    public bool IsKeyTransitionUp(Keys key)
    {
        return CurrentKeyboardState.IsKeyUp(key) &&
               LastKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Returns <c>true</c> if the key is being pressed, <c>false</c> otherwise.
    /// </summary>
    /// <remarks>
    /// A key is considered being pressed only if its previous state was down.
    /// Use <see cref="IsKeyTransitionDown(Keys)"/> to know if the key transitioned
    /// from up to down.
    /// </remarks>
    /// <param name="key">The key to evaluate.</param>
    /// <returns><c>true</c> if the key is being pressed, <c>false</c> otherwise.</returns>
    public bool IsKeyHeld(Keys key)
    {
        return CurrentKeyboardState.IsKeyDown(key) &&
               LastKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Returns <c>true</c> if the button transitioned from up to down during this
    /// update, <c>false</c> otherwise.
    /// </summary>
    /// <param name="button">The button to evaluate.</param>
    /// <returns><c>true</c> if the button transitioned from up to down during this
    /// update, <c>false</c> otherwise.</returns>
    public bool IsButtonTransitionDown(Buttons button)
    {
        return CurrentGamePadState.IsButtonDown(button) &&
               LastGamePadState.IsButtonUp(button);
    }

    /// <summary>
    /// Returns <c>true</c> if the button transitioned from down to up during this
    /// update, <c>false</c> otherwise.
    /// </summary>
    /// <param name="button">The button to evaluate.</param>
    /// <returns><c>true</c> if the button transitioned from down to up during this
    /// update, <c>false</c> otherwise.</returns>
    public bool IsButtonTransitionUp(Buttons button)
    {
        return CurrentGamePadState.IsButtonUp(button) &&
               LastGamePadState.IsButtonDown(button);
    }

    /// <summary>
    /// Returns <c>true</c> if the button is being pressed, <c>false</c> otherwise.
    /// </summary>
    /// <remarks>
    /// A button is considered being pressed only if its previous state was down.
    /// Use <see cref="IsButtonTransitionDown(Buttons)"/> to know if the button transitioned
    /// from up to down.
    /// </remarks>
    /// <param name="button">The button to evaluate.</param>
    /// <returns><c>true</c> if the button is being pressed, <c>false</c> otherwise.</returns>
    public bool IsButtonHeld(Buttons button)
    {
        return CurrentGamePadState.IsButtonDown(button) &&
               LastGamePadState.IsButtonDown(button);
    }

    /// <summary>
    /// Returns <c>true</c> if the player wants to confirm a selction or press
    /// a button when in a menu, <c>false</c> otherwise.
    /// </summary>
    /// <returns><c>true</c> if the player wants to confirm a selction or press
    /// a button when in a menu, <c>false</c> otherwise.</returns>
    public bool IsMenuSelect()
    {
        return IsKeyTransitionDown(Keys.Space) ||
               IsKeyTransitionDown(Keys.Enter) ||
               IsButtonTransitionDown(Buttons.A) ||
               IsButtonTransitionDown(Buttons.Start);
    }

    /// <summary>
    /// Returns <c>true</c> if the player wants to go back when in a menu,
    /// <c>false</c> otherwise.
    /// </summary>
    /// <returns><c>true</c> if the player wants to go back when in a menu,
    /// <c>false</c> otherwise.</returns>
    public bool IsMenuCancel()
    {
        return IsKeyTransitionDown(Keys.Escape) ||
               IsButtonTransitionDown(Buttons.B) ||
               IsButtonTransitionDown(Buttons.Back);
    }

    /// <summary>
    /// Returns <c>true</c> if the player wants to go up when in a menu,
    /// <c>false</c> otherwise.
    /// </summary>
    /// <returns><c>true</c> if the player wants to go up when in a menu,
    /// <c>false</c> otherwise.</returns>
    public bool IsMenuUp()
    {
        return IsKeyTransitionDown(Keys.Up) ||
               IsButtonTransitionDown(Buttons.DPadUp) ||
               IsButtonTransitionDown(Buttons.LeftThumbstickUp);
    }

    /// <summary>
    /// Returns <c>true</c> if the player wants to go down when in a menu,
    /// <c>false</c> otherwise.
    /// </summary>
    /// <returns><c>true</c> if the player wants to go down when in a menu,
    /// <c>false</c> otherwise.</returns>
    public bool IsMenuDown()
    {
        return IsKeyTransitionDown(Keys.Down) ||
               IsButtonTransitionDown(Buttons.DPadDown) ||
               IsButtonTransitionDown(Buttons.LeftThumbstickDown);
    }

    /// <summary>
    /// Returns <c>true</c> if the player wants to toggle the pause state,
    /// <c>false</c> otherwise.
    /// </summary>
    /// <returns><c>true</c> if the player wants to toggle the pause state,
    /// <c>false</c> otherwise.</returns>
    public bool IsPauseGame()
    {
        return IsKeyTransitionDown(Keys.Escape) ||
               IsButtonTransitionDown(Buttons.Back) ||
               IsButtonTransitionDown(Buttons.Start);
    }
}
