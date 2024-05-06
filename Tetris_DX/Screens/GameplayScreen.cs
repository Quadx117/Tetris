namespace Tetris_DX.Screens;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tetris_DX.Blocks;
using Tetris_DX.GameState;
using Tetris_DX.ScreenManagement;

/// <summary>
/// This screen implements the actual game logic.
/// </summary>
public class GameplayScreen : GameScreen
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;

    private bool _gameOver;
    private bool _isPaused;
    private bool _hardDrop;
    private bool _hold;
    private bool _autoRepeatMovement;

    /// <summary>
    /// Used to draw basic shapes with any color, such as the background
    /// rectangle under the score and other info.
    /// </summary>
    private Texture2D _pixel;

    private readonly GameMatrix _gameMatrix;

    /// <summary>
    /// The assets used to render individual blocks in the matrix or to render
    /// the blocks for the current Tetromino. The images in this array must be
    /// in the same order as the members of the <see cref="BlockType"/> <c>enum</c>
    /// since it is used to index inside this array.
    /// </summary>
    private readonly Texture2D[] _tiles;

    // NOTE(PERE): This is temporary since it is tied to the current assets
    // which don't allow to preview more than one Tetromino at a time. Eventually,
    // I want to create a custom panel and preview up to six Tetrominos.
    // TODO(PERE): Separate the "panel" from the actual Tetrominos so we can
    // configure how many Tetrominos to show in the preview and have more control
    // over the UI. We could also create a class that would render each Tetrominos
    // using the _tiles instead of having actual assets, but the tradeoff seems pretty
    // minor since there is only 7 Tetrominos and it is quite quick to create the assets
    // in the case where we would want to change the theme.
    /// <summary>
    /// The array of possible blocks inside a square rectangle with rounded borders
    /// used to display the next Tetromino and the held Tetromino. This also includes
    /// an "empty block" used when there are no held Tetromino. The images in this array
    /// must be in the same order as the members of the <see cref="BlockType"/> <c>enum</c>
    /// since it is used to index inside this array.
    /// </summary>
    private readonly Texture2D[] _blocks;

    // TODO(PERE): See how to calculate the cell size based on the window size,
    // espacially if we want to resize the screen.
    private readonly Point _cellSize = new(25);

    /// <summary>
    /// This is the absolute top lef corner of the game matrix
    /// </summary>
    private Vector2 _matrixOrigin;

    /// <summary>
    /// This is the top left corner of the visible part of the game matrix
    /// </summary>
    private Vector2 _matrixVisibleOrigin;
    private Vector2 _playingAreaOrigin;

    private Rectangle _infoPanelDest;
    private Rectangle _previewPanelDest;
    private Rectangle _heldPanelDest;

    /// <summary>
    /// Amount of time elapsed since we last moved the Tetromino down by one row.
    /// </summary>
    private TimeSpan _elapsed = TimeSpan.Zero;

    /// <summary>
    /// Amount of time before the piece moves down by one row
    /// </summary>
    private TimeSpan _dropInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Amount of time before the piece is locked in place.
    /// </summary>
    private TimeSpan _lockDownDelay = TimeSpan.FromSeconds(0.5);
    private bool _lockingDown = false;
    private BlockBase _currentBlock;
    private BlockBase _heldBlock;
    private bool _canHold = true;
    private readonly BlockQueue _blockQueue = new();

    private int _softDropSpeedMultiplier = 1;
    private readonly TimeSpan _autoRepeatDelay = TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _autoRepeatRate = TimeSpan.FromMilliseconds(50);
    private TimeSpan _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
    private MovementDirection _movementDirection = MovementDirection.None;
    private RotationDirection _rotationDirection = RotationDirection.None;

    private Texture2D BackgroundTexture { get; set; }

    // TODO(PERE): Should we use a texture with the grid instead of only
    // having the border and drawing empty cells manually? We would need
    // two textures if we wanted to support not showing the grid.
    private Texture2D PlayingAreaTexture { get; set; }

    private int _score;
    private int _lines;
    private int _level = 1;
    private int _comboCount = -1;

    // Font resources
    private SpriteFont _fontNormal;
    private SpriteFont _fontTitle;

    private BlockBase CurrentBlock
    {
        get => _currentBlock;
        set
        {
            _currentBlock = value;
            _currentBlock.Reset();

            if (BlockFits())
            {
                // Try to move the block inside the visible portion of the grid.
                for (int index = 0; index < 2; ++index)
                {
                    _currentBlock.MoveDown();
                    if (!BlockFits())
                    {
                        _currentBlock.MoveUp();
                        break;
                    }
                }
            }
            else
            {
                _gameOver = true;
            }
        }
    }

    public GameplayScreen(GraphicsDeviceManager graphics,
                          GraphicsDevice graphicsDevice,
                          ContentManager content)
    {
        _graphics = graphics;
        _graphicsDevice = graphicsDevice;
        _content = content;

        TransitionOnTime = TimeSpan.FromSeconds(1.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);

        _gameMatrix = new GameMatrix(22, 10);
        _tiles = new Texture2D[8];
        _blocks = new Texture2D[8];

        CurrentBlock = _blockQueue.Dequeue();

        // TODO(PERE): Make sure the playing field always fits.
        int playingAreaWidth = _gameMatrix.ColumnCount * _cellSize.X;
        // NOTE(PERE): We exclude the first two rows since they are meant to be
        // invisible to the player.
        int playingAreaHeight = (_gameMatrix.RowCount - 2) * _cellSize.Y;
        _matrixVisibleOrigin = new Vector2((_graphics.PreferredBackBufferWidth - playingAreaWidth) / 2,
                                    (_graphics.PreferredBackBufferHeight - playingAreaHeight) / 2);
        _matrixOrigin = _matrixVisibleOrigin - new Vector2(0, 2 * _cellSize.Y);
        _playingAreaOrigin = _matrixVisibleOrigin - Vector2.One;
    }

    public override void LoadContent()
    {
        // TODO(PERE): Add a property in ScreenManager and use this, like for Font
        // if we still need to do custom drawing.
        _pixel = new Texture2D(_graphicsDevice, 1, 1, false, SurfaceFormat.Color);
        _pixel.SetData(new[] { Color.White }); // So we can draw whatever color we want

        BackgroundTexture = _content.Load<Texture2D>(@"Images\Background");
        PlayingAreaTexture = _content.Load<Texture2D>(@"Images\PlayingArea");

        _tiles[0] = _content.Load<Texture2D>(@"Images\TileEmpty");
        _tiles[1] = _content.Load<Texture2D>(@"Images\TileCyan");
        _tiles[2] = _content.Load<Texture2D>(@"Images\TileBlue");
        _tiles[3] = _content.Load<Texture2D>(@"Images\TileOrange");
        _tiles[4] = _content.Load<Texture2D>(@"Images\TileYellow");
        _tiles[5] = _content.Load<Texture2D>(@"Images\TileGreen");
        _tiles[6] = _content.Load<Texture2D>(@"Images\TilePurple");
        _tiles[7] = _content.Load<Texture2D>(@"Images\TileRed");

        _blocks[0] = _content.Load<Texture2D>(@"Images\Block-Empty");
        _blocks[1] = _content.Load<Texture2D>(@"Images\Block-I");
        _blocks[2] = _content.Load<Texture2D>(@"Images\Block-J");
        _blocks[3] = _content.Load<Texture2D>(@"Images\Block-L");
        _blocks[4] = _content.Load<Texture2D>(@"Images\Block-O");
        _blocks[5] = _content.Load<Texture2D>(@"Images\Block-S");
        _blocks[6] = _content.Load<Texture2D>(@"Images\Block-T");
        _blocks[7] = _content.Load<Texture2D>(@"Images\Block-Z");

        _fontNormal = _content.Load<SpriteFont>("Fonts/Game/normal");
        _fontTitle = _content.Load<SpriteFont>("Fonts/Game/title");

        // TODO(PERE): This doesn't feel like the best place for it, but textures
        // aren't yet loaded in the constructor.
        // NOTE(PERE): We use a margin of 10 pixels between the infoPanel and
        // the playingField and a panelSize of 174x174.
        Vector2 infoPanelSize = new(173);
        int playingFieldMargin = 10;
        _infoPanelDest =
            new Rectangle((int)(_playingAreaOrigin.X - infoPanelSize.X - playingFieldMargin),
                          (int)(_playingAreaOrigin.Y + PlayingAreaTexture.Height - infoPanelSize.Y),
                          (int)infoPanelSize.X,
                          (int)infoPanelSize.Y);

        // NOTE(PERE): We use the same 10 pixels margin between the previewPanel
        // and the playingField
        float previewPanelScale = 0.20f;
        Texture2D previewPanelTexture = _blocks[(int)BlockType.None];
        _previewPanelDest =
            new Rectangle((int)(_playingAreaOrigin.X + PlayingAreaTexture.Width + playingFieldMargin),
                          (int)(_playingAreaOrigin.Y),
                          (int)(previewPanelTexture.Width * previewPanelScale),
                          (int)(previewPanelTexture.Height * previewPanelScale));

        float heldPanelScale = 0.20f;
        Texture2D heldPanelTexture = _blocks[(int)BlockType.None];
        Vector2 heldPanelSize = new(heldPanelTexture.Width * heldPanelScale,
                                    heldPanelTexture.Height * heldPanelScale);
        _heldPanelDest =
            new Rectangle((int)(_playingAreaOrigin.X - heldPanelSize.X - playingFieldMargin),
                          (int)(_playingAreaOrigin.Y),
                          (int)(heldPanelSize.X),
                          (int)(heldPanelSize.Y));
    }

    public override void UnloadContent()
    {
        // TODO(PERE): Should we unload the game's content here?
    }

    public override void HandleInput(InputManager input)
    {
        GamePadState gamePadState = input.CurrentGamePadState;

        // The game pauses either if the user presses the pause button, or if
        // they unplug the active gamepad. This requires us to keep track of
        // whether a gamepad was ever plugged in, because we don't want to pause
        // on PC if they are playing with a keyboard and have no gamepad at all!
        bool gamePadDisconnected = !gamePadState.IsConnected &&
                                   input.GamePadWasConnected;

        if (!_gameOver &&
            (input.IsPauseGame() || gamePadDisconnected))
        {
            _isPaused = !_isPaused;
            // TODO(PERE): This is temporary and makes sure we stay paused if 
            // the gampepad disconnected
            _isPaused |= gamePadDisconnected;
        }
        else
        {
            // TODO(PERE): Exiting the game will go into the pause menu once I add
            // the different screens to the game.
            //if (_input.IsButtonTransitionDown(Buttons.Back) ||
            //    _input.IsKeyTransitionDown(Keys.Escape))
            //{
            //    Exit();
            //}

            // TODO(PERE): Temporary code that will go inside the GameOver screen
            if (_gameOver)
            {
                if (input.IsKeyTransitionDown(Keys.Space) ||
                    input.IsKeyTransitionDown(Keys.Enter))
                {
                    _gameMatrix.Reset();
                    _blockQueue.Reset();
                    _heldBlock = null;
                    CurrentBlock = _blockQueue.Dequeue();
                    _score = 0;
                    _lines = 0;
                    _level = 1;
                    _comboCount = -1;
                    _dropInterval = TimeSpan.FromSeconds(1);
                    _gameOver = false;

                    // NOTE(PERE): Locking down the final block already resset
                    // these 2 fields, but there is no harm in doing it again.
                    _canHold = true;
                    _elapsed = TimeSpan.Zero;
                }
            }
            // TODO(PERE): Temporary if since once we have a proper pause screen,
            // we won't be handling input in this screen when the game is paused.
            else if (!_isPaused)
            {
                // NOTE(PERE): Pressing both left and right at the same time is
                // only possible when using a keyboard and getting the timing
                // right so that it happens inside the same update is almost
                // impossible. If it were to happen, we choose to prioritize
                // going left.
                if (input.IsKeyTransitionDown(Keys.Left) ||
                    (input.IsKeyHeld(Keys.Left) &&
                     input.IsKeyTransitionUp(Keys.Right)))
                {
                    _movementDirection = MovementDirection.Left;
                    _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
                }
                else if (input.IsKeyTransitionDown(Keys.Right) ||
                         (input.IsKeyHeld(Keys.Right) &&
                          input.IsKeyTransitionUp(Keys.Left)))
                {
                    _movementDirection = MovementDirection.Right;
                    _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
                }

                _autoRepeatMovement = (_movementDirection == MovementDirection.Left &&
                                       input.IsKeyHeld(Keys.Left)) ||
                                      (_movementDirection == MovementDirection.Right &&
                                       input.IsKeyHeld(Keys.Right));

                _rotationDirection = input.IsKeyTransitionDown(Keys.Up)
                                         ? RotationDirection.Clockwise
                                         : input.IsKeyTransitionDown(Keys.Z)
                                             ? RotationDirection.CounterClockwise
                                             : RotationDirection.None;

                if (input.IsKeyTransitionDown(Keys.Down))
                {
                    _softDropSpeedMultiplier = 20;
                }
                else if (input.IsKeyTransitionUp(Keys.Down))
                {
                    _softDropSpeedMultiplier = 1;
                }

                _hardDrop = input.IsKeyTransitionDown(Keys.Space);

                // TODO(PERE): Add debug info to see timings like elapsed
                // time since last drop, etc. It seems a bit long before
                // the piece goes down by one row after holding a piece.
                _hold = input.IsKeyTransitionDown(Keys.C) &&
                        _canHold;
            }
        }
    }

    public override void Update(GameTime gameTime,
                                bool otherScreenHasFocus,
                                bool coveredByOtherScreen)
    {
        base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

        // TODO(PERE): Validate if this is needed and OK to return.
        if (!IsActive)
        {
            // NOTE(PERE): We don't need to update this screen if it is not active.
            return;
        }

        // TODO(PERE): Do we need this VS !IsActive above.
        if (!_isPaused)
        {
            if (_autoRepeatMovement)
            {
                _elapsedSinceLastAutoRepeat += gameTime.ElapsedGameTime;
            }

            // NOTE(PERE): If we pressed left or right, then _elapsedSinceLastAutoRepeat
            // will be 0 so we know we want to move the Tetromino in that case. Otherwise,
            // we wait for the appropriate amount of time before auto-repeating the move.
            if (_elapsedSinceLastAutoRepeat == TimeSpan.Zero ||
                _elapsedSinceLastAutoRepeat > _autoRepeatDelay)
            {
                if (_elapsedSinceLastAutoRepeat > _autoRepeatDelay)
                {
                    _elapsedSinceLastAutoRepeat -= _autoRepeatRate;
                }

                // TODO(PERE): We can't set _movementDirection to None with the
                // current logic, since we rely on knowing what was the last
                // direction we were moving to determine if we want to auto repeat.
                // This should be improved as it is not easy to work with.
                switch (_movementDirection)
                {
                    case MovementDirection.None:
                        // Nothing to do.
                        break;
                    case MovementDirection.Left:
                        CurrentBlock.MoveLeft();
                        if (!BlockFits())
                        {
                            CurrentBlock.MoveRight();
                        }
                        break;
                    case MovementDirection.Right:
                        CurrentBlock.MoveRight();
                        if (!BlockFits())
                        {
                            CurrentBlock.MoveLeft();
                        }
                        break;
                }
            }

            switch (_rotationDirection)
            {
                case RotationDirection.None:
                    // Nothing to do.
                    break;
                case RotationDirection.Clockwise:
                    CurrentBlock.RotateCW();
                    if (!BlockFits())
                    {
                        CurrentBlock.RotateCCW();
                    }
                    break;
                case RotationDirection.CounterClockwise:
                    CurrentBlock.RotateCCW();
                    if (!BlockFits())
                    {
                        CurrentBlock.RotateCW();
                    }
                    break;
            }

            // NOTE(PERE): If the player ever manages to issue both a hold and
            // a hard drop action, we favor the hold action over the hard drop.
            if (_hold)
            {
                BlockBase tmp = _heldBlock;
                _heldBlock = CurrentBlock;
                CurrentBlock = tmp ?? _blockQueue.Dequeue();
                _canHold = false;
                _elapsed = TimeSpan.Zero;
                _lockingDown = false;
                _hardDrop = false;
            }
            else if (_hardDrop)
            {
                _hardDrop = false;

                do
                {
                    CurrentBlock.MoveDown();
                    _score += 2;
                } while (BlockFits());

                CurrentBlock.MoveUp();
                _score -= 2;
                LockDownBlock();
            }
            else if (_lockingDown)
            {
                // Try to move down one last time in case the player moved the piece sideways
                CurrentBlock.MoveDown();
                if (BlockFits())
                {
                    _lockDownDelay = TimeSpan.FromSeconds(0.5);
                    _lockingDown = false;
                }
                else
                {
                    CurrentBlock.MoveUp();
                    _lockDownDelay = _lockDownDelay.Subtract(gameTime.ElapsedGameTime);
                }

                if (_lockDownDelay.TotalMilliseconds <= 0)
                {
                    LockDownBlock();
                    _lockDownDelay = TimeSpan.FromSeconds(0.5);
                    _lockingDown = false;
                }
            }
            else
            {
                _elapsed = _elapsed.Add(gameTime.ElapsedGameTime.Multiply(_softDropSpeedMultiplier));
                if (_elapsed.TotalMilliseconds > _dropInterval.TotalMilliseconds)
                {
                    CurrentBlock.MoveDown();
                    if (!BlockFits())
                    {
                        CurrentBlock.MoveUp();
                        _lockingDown = true;
                    }
                    else if (_softDropSpeedMultiplier > 1)
                    {
                        _score += 1;
                    }

                    _elapsed = _elapsed.Subtract(_dropInterval);
                }
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

        spriteBatch.Begin();

        spriteBatch.Draw(BackgroundTexture,
                         _graphics.GraphicsDevice.Viewport.Bounds,
                         Color.White);
        spriteBatch.Draw(PlayingAreaTexture,
                         _playingAreaOrigin,
                         Color.White);
        DrawMatrix(spriteBatch);
        DrawInfoPanel(spriteBatch);

        if (_isPaused)
        {
            // TODO(PERE): Calculate center by measuring the string
            // TODO(PERE): Use a bigger font for the "PAUSED" text
            // TODO(PERE): The todo's above aren't done since this is
            // temporary and should be replaced by a proper popup screen
            // with buttons to continue or quit.
            spriteBatch.DrawString(_fontTitle,
                                   "PAUSED",
                                   new Vector2((_graphics.PreferredBackBufferWidth * 0.5f) - 37,
                                               (_graphics.PreferredBackBufferHeight * 0.5f) - 10),
                                   Color.White);
        }
        else if (_gameOver)
        {
            spriteBatch.Draw(_pixel,
                             new Rectangle(0,
                                           0,
                                           _graphics.PreferredBackBufferWidth,
                                           _graphics.PreferredBackBufferHeight),
                             Color.Black * 0.6f);

            // TODO(PERE): Calculate center by measuring the string
            // TODO(PERE): Use a bigger font for the "GAME OVER" text
            // TODO(PERE): The todo's above aren't done since this is
            // temporary and should be replaced by a proper popup screen
            // with stats and buttons to restart or quit.
            spriteBatch.DrawString(_fontTitle,
                                   "GAME OVER",
                                   new Vector2((_graphics.PreferredBackBufferWidth * 0.5f) - 50,
                                               (_graphics.PreferredBackBufferHeight * 0.5f) - 10),
                                   Color.White);
        }
        else
        {
            DrawNextTetromino(spriteBatch);
            DrawHeldTetromino(spriteBatch);
            DrawCurrentBlockGhost(spriteBatch);
            DrawCurrentBlock(spriteBatch);
        }

        spriteBatch.End();
    }

    private void DrawCurrentBlock(SpriteBatch spriteBatch)
    {
        foreach (Point p in CurrentBlock.TilePositions())
        {
            // NOTE(PERE): We skip the first two rows which are meant to be invisible to the player.
            if (p.Y > 1)
            {
                Point location = new((int)_matrixOrigin.X + (p.X * _cellSize.X),
                                     (int)_matrixOrigin.Y + (p.Y * _cellSize.Y));
                Rectangle tileBounds = new(location, _cellSize);
                spriteBatch.Draw(_tiles[(int)CurrentBlock.Type],
                                 tileBounds,
                                 Color.White);
            }
        }
    }

    private void DrawCurrentBlockGhost(SpriteBatch spriteBatch)
    {
        int ghostOffset = GetGhostOffset();
        if (ghostOffset > 0)
        {
            foreach (Point p in CurrentBlock.TilePositions())
            {
                // NOTE(PERE): We skip the first two rows which are meant to be invisible to the player.
                if (p.Y + ghostOffset > 1)
                {
                    Point location = new((int)_matrixOrigin.X + (p.X * _cellSize.X),
                                         (int)_matrixOrigin.Y + ((p.Y + ghostOffset) * _cellSize.Y));
                    Rectangle tileBounds = new(location, _cellSize);
                    spriteBatch.Draw(_tiles[(int)CurrentBlock.Type],
                                     tileBounds,
                                     Color.White * 0.25f);
                }
            }
        }
    }

    private void DrawNextTetromino(SpriteBatch spriteBatch)
    {
        // TODO(PERE): We could "cache" the next Tetromino and update it only
        // after locking down the current piece or after holding the first piece
        // since we know it can't change otherwise. This would be a small optimization
        // though since this is not a demanding game, we will evaluate this idea
        // when we do a better version of the preview window.
        spriteBatch.Draw(_blocks[(int)_blockQueue.PeekNextBlockType()],
                         _previewPanelDest,
                         Color.White);
    }

    private void DrawHeldTetromino(SpriteBatch spriteBatch)
    {
        // TODO(PERE): We could "cache" the held Tetromino type and update it only
        // after holding a piece since we know it can't change otherwise. This would
        // be a small optimization though since this is not a demanding game.
        BlockType heldType = _heldBlock?.Type ?? BlockType.None;
        spriteBatch.Draw(_blocks[(int)heldType],
                         _heldPanelDest,
                         Color.White);
    }

    private void DrawMatrix(SpriteBatch spriteBatch)
    {
        // NOTE(PERE): We skip the first two rows which are meant to be invisible to the player.
        for (int rowIndex = 2; rowIndex < _gameMatrix.RowCount; rowIndex++)
        {
            for (int colIndex = 0; colIndex < _gameMatrix.ColumnCount; colIndex++)
            {
                Point location = new((int)_matrixVisibleOrigin.X + (colIndex * _cellSize.X),
                                     (int)_matrixVisibleOrigin.Y + ((rowIndex - 2) * _cellSize.Y));
                Rectangle tileBounds = new(location, _cellSize);
                BlockType blockType = _gameMatrix[rowIndex, colIndex];
                spriteBatch.Draw(_tiles[(int)blockType],
                                 tileBounds,
                                 Color.White);
            }
        }
    }

    // TODO(PERE): Create a Panel class or similiar and move all inner
    // components inside the Panel class?
    /// <summary>
    /// Draws the score, current level and lines cleared
    /// </summary>
    private void DrawInfoPanel(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel,
                         _infoPanelDest,
                         new Color(0.1f, 0.1f, 0.1f, 0.85f));

        // TODO(PERE): See how to best handle this, probably using a Panel
        // class with a Label class or similar which could have margins
        // and padding.
        Vector2 panelLocation = _infoPanelDest.Location.ToVector2();
        Vector2 panelSize = _infoPanelDest.Size.ToVector2();

        // NOTE(PERE): The Y margin needs to be smaller, probably because the font
        // has some spacing above for diacrittics, but we won't be using any for
        // the foreseable future.
        Vector2 panelMargin = new(8f, 6f);

        // NOTE(PERE): For some reason, numbers don't need the Y adjustment.
        Vector2 labelMargin = new(5f);
        Vector2 titleDest = panelLocation + panelMargin;
        spriteBatch.DrawString(_fontTitle,
                               "SCORE",
                               titleDest,
                               Color.White);

        // TODO(PERE): Calculate the height of the text dynamically VS
        // using "magic" numbers.
        Rectangle labelPanel = new((int)titleDest.X,
                                   (int)titleDest.Y + 24,
                                   (int)panelSize.X - ((int)panelMargin.X * 2),
                                   25);
        spriteBatch.Draw(_pixel,
                         labelPanel,
                         Color.Black);

        // TODO(PERE): Center text VS right align.
        // TODO(PERE): Make sure we have enough space for the max possible score.
        spriteBatch.DrawString(_fontNormal,
                               $"{_score}",
                               labelPanel.Location.ToVector2() + labelMargin,
                               Color.White);

        titleDest = labelPanel.Location.ToVector2();
        titleDest.Y += labelPanel.Height + panelMargin.Y;
        spriteBatch.DrawString(_fontTitle,
                               "LEVEL",
                               titleDest,
                               Color.White);

        // TODO(PERE): Calculate the height of the text dynamically VS
        // using "magic" numbers.
        labelPanel.Y = (int)titleDest.Y + 24;
        spriteBatch.Draw(_pixel,
                         labelPanel,
                         Color.Black);

        // TODO(PERE): Center text VS right align.
        spriteBatch.DrawString(_fontNormal,
                               $"{_level}",
                               labelPanel.Location.ToVector2() + labelMargin,
                               Color.White);

        titleDest = labelPanel.Location.ToVector2();
        titleDest.Y += labelPanel.Height + panelMargin.Y;
        spriteBatch.DrawString(_fontTitle,
                               "LINES",
                               titleDest,
                               Color.White);

        // TODO(PERE): Calculate the height of the text dynamically VS
        // using "magic" numbers.
        labelPanel.Y = (int)titleDest.Y + 24;
        spriteBatch.Draw(_pixel,
                         labelPanel,
                         Color.Black);

        // TODO(PERE): Center text VS right align.
        spriteBatch.DrawString(_fontNormal,
                               $"{_lines}",
                               labelPanel.Location.ToVector2() + labelMargin,
                               Color.White);
    }

    /// <summary>
    /// Returns the Y offset at which to draw the ghost Tetromino based on the
    /// current Tetromino position.
    /// </summary>
    /// <returns>
    /// The Y offset at which to draw the ghost Tetromino based on the current
    /// Tetromino position.
    /// </returns>
    private int GetGhostOffset()
    {
        int result = 0;

        bool blockFits = true;
        while (blockFits)
        {
            ++result;

            foreach (Point p in CurrentBlock.TilePositions())
            {
                Point testP = new(p.X, p.Y + result);
                if (!_gameMatrix.IsEmpty(testP))
                {
                    blockFits = false;
                    break;
                }
            }
        }

        return --result;
    }

    private bool BlockFits()
    {
        foreach (Point p in CurrentBlock.TilePositions())
        {
            if (!_gameMatrix.IsEmpty(p))
            {
                return false;
            }
        }

        return true;
    }

    private void LockDownBlock()
    {
        foreach (Point p in CurrentBlock.TilePositions())
        {
            _gameMatrix[p.Y, p.X] = CurrentBlock.Type;
        }

        // TODO(PERE): Better clearing with visual feedback
        int cleared = _gameMatrix.ClearFullRows();
        _lines += cleared;

        if (cleared > 0)
        {
            // NOTE(PERE): We need to increment the score before incrementing the
            // level, since the score is based on the level before the line clear.

            ++_comboCount;
            _score += 50 * _comboCount * _level;

            // TODO(PERE): Probably use an array or dictionnary to get the score
            // based on the number of lines cleared. Evaluate this when implementing
            // the other scoring mechanics such as t-spins, mini t-spins and back-to-back
            // difficult line clears.
            int tmpScore = 100 +
                           (200 * (cleared - 1)) +
                           (cleared == 4 ? 100 : 0);
            _score += tmpScore * _level;

            // Perfect clear bonus
            if (_gameMatrix.IsEmpty())
            {
                // TODO(PERE): It seems odd that the triple-line is worht a bit more
                // compared to the others, but this is what I found so far on the Tetris
                // wiki. Should do more research to confirm this without a doubt.
                int tmpPerfectBonus = (400 * (cleared + 1)) +
                                      (cleared == 3 ? 200 : 0);
                _score += tmpPerfectBonus * _level;

            }

            // NOTE(PERE): Every 10 lines we want to increment the level.
            // Multiplying by 0.1 is equivalent as dividing by 10 but is
            // slightly faster.
            _level = (int)(_lines * 0.1f) + 1;
            float speed = (float)Math.Pow(0.8f - ((_level - 1) * 0.007f), (_level - 1));
            _dropInterval = TimeSpan.FromSeconds(speed);
        }
        else
        {
            _comboCount = -1;
        }

        CurrentBlock = _blockQueue.Dequeue();
        _canHold = true;
        _elapsed = TimeSpan.Zero;
    }
}
