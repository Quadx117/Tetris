namespace Tetris_DX.Screens;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tetris_DX.ScreenManagement;

internal class GameOverScreen : GameScreen
{
    private readonly GraphicsDeviceManager _graphics;

    public event EventHandler Restart;

    public GameOverScreen(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;

        TransitionOnTime = TimeSpan.FromSeconds(0.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);
    }

    public override void HandleInput(InputManager input)
    {
        if (input.IsMenuCancel() ||
            input.IsMenuSelect())
        {
            Restart?.Invoke(this, null);

            ExitScreen();
        }
    }

    public override void Update(GameTime gameTime,
                                bool otherScreenHasFocus,
                                bool coveredByOtherScreen)
    {
        base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
    }

    public override void Draw(GameTime gameTime)
    {
        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

        ScreenManager.FadeBackBufferToBlack(0.7f);

        spriteBatch.Begin();

        // TODO(PERE): Calculate center by measuring the string
        // TODO(PERE): Use a bigger font for the "GAME OVER" text
        // TODO(PERE): The todo's above aren't done since this is
        // temporary and should be replaced by a proper popup screen
        // with stats and buttons to restart or quit.
        spriteBatch.DrawString(ScreenManager.Font,
                               "GAME OVER",
                               new Vector2((_graphics.PreferredBackBufferWidth * 0.5f) - 50,
                                           (_graphics.PreferredBackBufferHeight * 0.5f) - 10),
                               Color.White);

        spriteBatch.End();
    }
}
