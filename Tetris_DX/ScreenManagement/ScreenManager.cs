namespace Tetris_DX.ScreenManagement;

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// The screen manager is a component which manages one or more GameScreen
/// instances. It maintains a stack of screens, calls their Update and Draw
/// methods at the appropriate times, and automatically routes input to the
/// topmost active screen.
/// </summary>
public class ScreenManager : DrawableGameComponent
{
    private readonly List<GameScreen> _screens = new();
    private readonly List<GameScreen> _screensToUpdate = new();

    private readonly InputManager _input = new();
    private Texture2D _blankTexture;

    private bool _isInitialized;

    /// <summary>
    /// A default SpriteBatch shared by all the screens. This saves
    /// each screen having to bother creating their own local instance.
    /// </summary>
    public SpriteBatch SpriteBatch { get; private set; }

    /// <summary>
    /// A default font shared by all the screens. This saves
    /// each screen having to bother loading their own local copy.
    /// </summary>
    public SpriteFont Font { get; private set; }

    /// <summary>
    /// If true, the manager prints out a list of all the screens
    /// each time it is updated. This can be useful for making sure
    /// everything is being added and removed at the right times.
    /// </summary>
    public bool TraceEnabled { get; set; }

    /// <summary>
    /// Initializes a new instance of the screen manager component.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> class who owns this screen manager.</param>
    public ScreenManager(Game game)
        : base(game)
    {
    }

    /// <summary>
    /// Initializes the screen manager component.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _isInitialized = true;
    }

    /// <summary>
    /// Load your graphics content.
    /// </summary>
    protected override void LoadContent()
    {
        // Load content belonging to the screen manager.
        ContentManager content = Game.Content;

        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Font = content.Load<SpriteFont>("Fonts/default");
        
        _blankTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        _blankTexture.SetData(new[] { Color.White }); // So we can draw whatever color we want

        // Tell each of the screens to load their content.
        foreach (GameScreen screen in _screens)
        {
            screen.LoadContent();
        }
    }

    /// <summary>
    /// Unload your graphics content.
    /// </summary>
    protected override void UnloadContent()
    {
        // Tell each of the screens to unload their content.
        foreach (GameScreen screen in _screens)
        {
            screen.UnloadContent();
        }
    }

    /// <summary>
    /// Allows each screen to run logic.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        // Read the keyboard and gamepad.
        _input.Update();

        // Make a copy of the master screen list, to avoid confusion if
        // the process of updating one screen adds or removes others.
        _screensToUpdate.Clear();

        foreach (GameScreen screen in _screens)
        {
            _screensToUpdate.Add(screen);
        }

        bool otherScreenHasFocus = !Game.IsActive;
        bool coveredByOtherScreen = false;

        // Loop as long as there are screens waiting to be updated.
        while (_screensToUpdate.Count > 0)
        {
            // Pop the topmost screen off the waiting list.
            GameScreen screen = _screensToUpdate[^1];

            _screensToUpdate.RemoveAt(_screensToUpdate.Count - 1);

            // Update the screen.
            screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (screen.ScreenState == ScreenState.TransitionOn ||
                screen.ScreenState == ScreenState.Active)
            {
                // If this is the first active screen we came across,
                // give it a chance to handle input.
                if (!otherScreenHasFocus)
                {
                    screen.HandleInput(_input);
                    otherScreenHasFocus = true;
                }

                // If this is an active non-popup, inform any subsequent
                // screens that they are covered by it.
                if (!screen.IsPopup)
                {
                    coveredByOtherScreen = true;
                }
            }
        }

        // Print debug trace?
        if (TraceEnabled)
        {
            TraceScreens();
        }
    }

    /// <summary>
    /// Prints a list of all the screens, for debugging.
    /// </summary>
    void TraceScreens()
    {
        List<string> screenNames = new();

        foreach (GameScreen screen in _screens)
        {
            screenNames.Add(screen.GetType().Name);
        }

        Debug.WriteLine(string.Join(", ", screenNames.ToArray()));
    }

    /// <summary>
    /// Tells each screen to draw itself.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        foreach (GameScreen screen in _screens)
        {
            if (screen.ScreenState == ScreenState.Hidden)
            {
                continue;
            }

            screen.Draw(gameTime);
        }
    }

    /// <summary>
    /// Adds a new screen to the screen manager.
    /// </summary>
    /// <param name="screen">The screen instance to add.</param>
    /// <param name="controllingPlayer">The player for which the screen should
    /// accept input or <c>null</c> to accept input from any player.</param>
    public void AddScreen(GameScreen screen, PlayerIndex? controllingPlayer)
    {
        screen.ControllingPlayer = controllingPlayer;
        screen.ScreenManager = this;
        screen.IsExiting = false;

        // If we have a graphics device, tell the screen to load content.
        if (_isInitialized)
        {
            screen.LoadContent();
        }

        _screens.Add(screen);
    }

    /// <summary>
    /// Removes a screen from the screen manager. You should normally use
    /// <see cref="GameScreen.ExitScreen"/> instead of calling this directly,
    /// so the screen can gradually transition off rather than just being
    /// instantly removed.
    /// </summary>
    /// <param name="screen">The screen instance to be removed.</param>
    public void RemoveScreen(GameScreen screen)
    {
        // If we have a graphics device, tell the screen to unload content.
        if (_isInitialized)
        {
            screen.UnloadContent();
        }

        _screens.Remove(screen);
        _screensToUpdate.Remove(screen);
    }

    /// <summary>
    /// Exposes an array holding all the screens. We return a copy rather
    /// than the real master list, because screens should only ever be added
    /// or removed using the AddScreen and RemoveScreen methods.
    /// </summary>
    public GameScreen[] GetScreens()
    {
        return _screens.ToArray();
    }

    /// <summary>
    /// Draws a translucent black fullscreen sprite, used for fading screens
    /// in and out, and for darkening the background behind popups.
    /// </summary>
    /// <remarks>
    /// This method needs to be called outside of any <see cref="SpriteBatch.Begin"/>
    /// and <see cref="SpriteBatch.End"/> calls, since it has its own.
    /// </remarks>
    /// <param name="opacity">A value from 0 to 1 which represents the percentage
    /// of opacity of the black sprite. 1 is fully opaque, 0 is fully transparent.</param>
    public void FadeBackBufferToBlack(float opacity)
    {
        Viewport viewport = GraphicsDevice.Viewport;

        SpriteBatch.Begin();

        SpriteBatch.Draw(_blankTexture,
                         new Rectangle(0, 0, viewport.Width, viewport.Height),
                         Color.Black * opacity);

        SpriteBatch.End();
    }
}
