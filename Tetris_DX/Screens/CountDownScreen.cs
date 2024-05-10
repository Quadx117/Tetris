namespace Tetris_DX.Screens;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tetris_DX.ScreenManagement;

internal class CountDownScreen : GameScreen
{
    // TODO(PERE): See if we want to store this inside the ScreenManager or inside
    // some globally accessible class.
    private readonly GraphicsDeviceManager _graphics;

    private SpriteFont _font;
    private readonly TimeSpan _delayBeforeNextDigit = TimeSpan.FromMilliseconds(800);
    private TimeSpan _elapsed = TimeSpan.Zero;
    private int _count = 3;

    public CountDownScreen(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;

        // TODO(PERE): Should we consider this as a popup ? If we do that, we
        // should probably consider all screens that don't fully cover the main
        // GameScreen as popup and pass coveredByOtherScreen to Update instead
        // of false.
        IsPopup = true;
    }

    public override void LoadContent()
    {
        ContentManager content = ScreenManager.Game.Content;
        _font = content.Load<SpriteFont>("Fonts/Game/countDown");
    }

    public override void HandleInput(InputManager input)
    {
        if (input.IsMenuCancel() ||
            input.IsPauseGame())
        {
            ScreenManager.AddScreen(new PauseMenuScreen(_graphics),
                                    ControllingPlayer);

            ExitScreen();
        }
    }

    public override void Update(GameTime gameTime,
                                bool otherScreenHasFocus,
                                bool coveredByOtherScreen)
    {
        base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed > _delayBeforeNextDigit)
        {
            _elapsed -= _delayBeforeNextDigit;
            --_count;
        }

        if (_count <= 0)
        {
            ExitScreen();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        if (_count > 0)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin();

            // TODO(PERE): Calculate center by measuring the string
            spriteBatch.DrawString(_font,
                                   _count.ToString(),
                                   new Vector2((_graphics.PreferredBackBufferWidth * 0.5f) - 21,
                                               (_graphics.PreferredBackBufferHeight * 0.5f) - 40),
                                   Color.White);

            spriteBatch.End();
        }
    }
}
