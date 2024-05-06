namespace Tetris_DX;

using Microsoft.Xna.Framework;
using Tetris_DX.ScreenManagement;
using Tetris_DX.Screens;

public class TetrisGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly ScreenManager _screenManager;

    public TetrisGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferHeight = 600,
            PreferredBackBufferWidth = 800
        };

        // NOTE(PERE): This seems to only be needed if we change the resolution
        // outside of the constructor, but I kept it just in case.
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Create the screen manager component.
        _screenManager = new ScreenManager(this);
        Components.Add(_screenManager);

        // Activate the first screens.
        _screenManager.AddScreen(new GameplayScreen(_graphics,
                                                    GraphicsDevice,
                                                    Content),
                                 null);
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // The real drawing happens inside the screen manager component.
        base.Draw(gameTime);
    }
}

/*
 * TODO(PERE):
 * - Choose whether to use properties VS fields
 * - Install StyleCop and fix related code style issues
 * - I got a GAME OVER probably by hard dropping a piece at the same time
 *   it was locking down. I hard droped the piece, then saw the GAME OVER
 *   screen while the current Tetromino was at the top of the screen.
 */
