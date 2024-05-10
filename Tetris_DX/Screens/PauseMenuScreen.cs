namespace Tetris_DX.Screens;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tetris_DX.ScreenManagement;

// TODO(PERE): This is only a placeholder for the pause screen.
internal class PauseMenuScreen : GameScreen
{
    private readonly GraphicsDeviceManager _graphics;

    public PauseMenuScreen(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;

        TransitionOnTime = TimeSpan.FromSeconds(0.5);
    }

    public override void HandleInput(InputManager input)
    {
        if (input.IsMenuCancel() ||
            input.IsPauseGame())
        {
            ScreenManager.AddScreen(new CountDownScreen(_graphics),
                                    ControllingPlayer);

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
        // TODO(PERE): Use a bigger font for the "PAUSED" text
        // TODO(PERE): The todo's above aren't done since this is
        // temporary and should be replaced by a proper popup screen
        // with buttons to continue or quit.
        spriteBatch.DrawString(ScreenManager.Font,
                               "PAUSED",
                               new Vector2((_graphics.PreferredBackBufferWidth * 0.5f) - 37,
                                           (_graphics.PreferredBackBufferHeight * 0.5f) - 10),
                               Color.White);

        spriteBatch.End();
    }
}
