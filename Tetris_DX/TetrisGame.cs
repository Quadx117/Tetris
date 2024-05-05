namespace Tetris_DX;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tetris_DX.Blocks;
using Tetris_DX.Components;
using Tetris_DX.GameState;

public class TetrisGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private readonly InputManager _input;

    private bool _gameOver;
    private bool _isPaused;

    /// <summary>
    /// Used to draw basic shapes with any color, such as the background
    /// rectangle under the score and other info.
    /// </summary>
    private Texture2D pixel;

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

    private TimeSpan _elapsed = TimeSpan.Zero;
    /// <summary>
    /// Amount of time before the piece moves down by one row
    /// </summary>
    private TimeSpan _dropSpeed = TimeSpan.FromSeconds(1);
    /// <summary>
    /// Amount of time before the piece is locked in place.
    /// </summary>
    private TimeSpan _lockDownDelay = TimeSpan.FromSeconds(0.5);
    private bool _lockingDown = false;
    private BlockBase _currentBlock;
    private BlockBase _heldBlock;
    private bool _canHold = true;
    private readonly BlockQueue _blockQueue = new();

    private int _softDropMultiplier = 1;
    private readonly TimeSpan _autoRepeatDelay = TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _autoRepeatRate = TimeSpan.FromMilliseconds(50);
    private TimeSpan _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
    private MovementDirection _movementDirection = MovementDirection.None;

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

    public TetrisGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _input = new InputManager();

        _gameMatrix = new GameMatrix(22, 10);
        _tiles = new Texture2D[8];
        _blocks = new Texture2D[8];

        CurrentBlock = _blockQueue.Dequeue();
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferHeight = 600;
        _graphics.PreferredBackBufferWidth = 800;

        _graphics.ApplyChanges();

        // TODO(PERE): Make sure the playing field always fits.
        int playingAreaWidth = _gameMatrix.ColumnCount * _cellSize.X;
        // NOTE(PERE): We exclude the two first rows since they are meant to be
        // invisible to the player.
        int playingAreaHeight = (_gameMatrix.RowCount - 2) * _cellSize.Y;
        _matrixVisibleOrigin = new Vector2((_graphics.PreferredBackBufferWidth - playingAreaWidth) / 2,
                                    (_graphics.PreferredBackBufferHeight - playingAreaHeight) / 2);
        _matrixOrigin = _matrixVisibleOrigin - new Vector2(0, 2 * _cellSize.Y);
        _playingAreaOrigin = _matrixVisibleOrigin - Vector2.One;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        pixel = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        pixel.SetData(new[] { Color.White }); // So we can draw whatever color we want

        BackgroundTexture = Content.Load<Texture2D>(@"Images\Background");
        PlayingAreaTexture = Content.Load<Texture2D>(@"Images\PlayingArea");

        _tiles[0] = Content.Load<Texture2D>(@"Images\TileEmpty");
        _tiles[1] = Content.Load<Texture2D>(@"Images\TileCyan");
        _tiles[2] = Content.Load<Texture2D>(@"Images\TileBlue");
        _tiles[3] = Content.Load<Texture2D>(@"Images\TileOrange");
        _tiles[4] = Content.Load<Texture2D>(@"Images\TileYellow");
        _tiles[5] = Content.Load<Texture2D>(@"Images\TileGreen");
        _tiles[6] = Content.Load<Texture2D>(@"Images\TilePurple");
        _tiles[7] = Content.Load<Texture2D>(@"Images\TileRed");

        _blocks[0] = Content.Load<Texture2D>(@"Images\Block-Empty");
        _blocks[1] = Content.Load<Texture2D>(@"Images\Block-I");
        _blocks[2] = Content.Load<Texture2D>(@"Images\Block-J");
        _blocks[3] = Content.Load<Texture2D>(@"Images\Block-L");
        _blocks[4] = Content.Load<Texture2D>(@"Images\Block-O");
        _blocks[5] = Content.Load<Texture2D>(@"Images\Block-S");
        _blocks[6] = Content.Load<Texture2D>(@"Images\Block-T");
        _blocks[7] = Content.Load<Texture2D>(@"Images\Block-Z");

        _fontNormal = Content.Load<SpriteFont>("Fonts/Game/normal");
        _fontTitle = Content.Load<SpriteFont>("Fonts/Game/title");

        // TODO(PERE): This doesn't feel like the best place for it, but textures
        // aren't yet loaded in the Initialize method.
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

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        // TODO(PERE): Exiting the game will go into the pause menu once I add
        // the different screens to the game.
        if (_input.IsButtonTransitionDown(Buttons.Back) ||
            _input.IsKeyTransitionDown(Keys.Escape))
        {
            Exit();
        }

        if (!_gameOver &&
            _input.IsPauseGame())
        {
            _isPaused = !_isPaused;
        }

        // TODO(PERE): Use a SceneGraph/SceneManager?
        if (_gameOver)
        {
            if (_input.IsKeyTransitionDown(Keys.Space) ||
                _input.IsKeyTransitionDown(Keys.Enter))
            {
                _gameMatrix.Reset();
                _blockQueue.Reset();
                _heldBlock = null;
                CurrentBlock = _blockQueue.Dequeue();
                _score = 0;
                _lines = 0;
                _level = 1;
                _comboCount = -1;
                _dropSpeed = TimeSpan.FromSeconds(1);
                _gameOver = false;
            }
        }
        else if (!_isPaused)
        {
            if (_input.IsKeyTransitionDown(Keys.Left))
            {
                _movementDirection = MovementDirection.Left;
                _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
            }
            else if (_input.IsKeyTransitionDown(Keys.Right))
            {
                _movementDirection = MovementDirection.Right;
                _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
            }

            if (_movementDirection == MovementDirection.Left &&
                _input.IsKeyTransitionUp(Keys.Left) &&
                _input.IsKeyHeld(Keys.Right))
            {
                _movementDirection = MovementDirection.Right;
                _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
            }
            else if (_movementDirection == MovementDirection.Right &&
                     _input.IsKeyTransitionUp(Keys.Right) &&
                     _input.IsKeyHeld(Keys.Left))
            {
                _movementDirection = MovementDirection.Left;
                _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
            }

            if (_movementDirection == MovementDirection.Left &&
                _input.IsKeyHeld(Keys.Left))
            {
                _elapsedSinceLastAutoRepeat += gameTime.ElapsedGameTime;
            }
            else if (_movementDirection == MovementDirection.Right &&
                     _input.IsKeyHeld(Keys.Right))
            {
                _elapsedSinceLastAutoRepeat += gameTime.ElapsedGameTime;
            }

            // NOTE(PERE): If we pressed left or right, then _elapsedSinceLastAutoRepeat
            // will be 0 so we know we want to move the Tetromino in that case. Otherwise,
            // we wait for the appropirate amount of time before auto-repeating the move.
            if (_elapsedSinceLastAutoRepeat == TimeSpan.Zero ||
                _elapsedSinceLastAutoRepeat > _autoRepeatDelay)
            {
                if (_elapsedSinceLastAutoRepeat > _autoRepeatDelay)
                {
                    _elapsedSinceLastAutoRepeat -= _autoRepeatRate;
                }

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

            if (_input.IsKeyTransitionDown(Keys.Up))
            {
                CurrentBlock.RotateCW();
                if (!BlockFits())
                {
                    CurrentBlock.RotateCCW();
                }
            }
            else if (_input.IsKeyTransitionDown(Keys.Z))
            {
                CurrentBlock.RotateCCW();
                if (!BlockFits())
                {
                    CurrentBlock.RotateCW();
                }
            }

            if (_input.IsKeyTransitionDown(Keys.Down))
            {
                _softDropMultiplier = 20;
            }
            else if (_input.IsKeyTransitionUp(Keys.Down))
            {
                _softDropMultiplier = 1;
            }

            if (_input.IsKeyTransitionDown(Keys.Space))
            {
                do
                {
                    CurrentBlock.MoveDown();
                    _score += 2;
                } while (BlockFits());

                CurrentBlock.MoveUp();
                _score -= 2;
                LockDownBlock();
            }

            if (_input.IsKeyTransitionDown(Keys.C) &&
                _canHold)
            {
                BlockBase tmp = _heldBlock;
                _heldBlock = CurrentBlock;
                CurrentBlock = tmp ?? _blockQueue.Dequeue();
                _canHold = false;
            }

            if (_lockingDown)
            {
                // TODO(PERE): Use the same variable (_elapsed) for the _lockDownDelay?
                // Rename _elapsed if it's only used for fallig blocks.
                _elapsed = TimeSpan.Zero;
                if (_lockDownDelay.TotalMilliseconds <= 0)
                {
                    // Try to move down one last time in case the player moved the piece
                    CurrentBlock.MoveDown();
                    if (!BlockFits())
                    {
                        CurrentBlock.MoveUp();
                        LockDownBlock();
                    }

                    _lockDownDelay = TimeSpan.FromSeconds(0.5);
                    _lockingDown = false;
                }
                else
                {
                    _lockDownDelay = _lockDownDelay.Subtract(gameTime.ElapsedGameTime);
                }
            }
            else
            {
                _elapsed = _elapsed.Add(gameTime.ElapsedGameTime.Multiply(_softDropMultiplier));
                if (_elapsed.TotalMilliseconds > _dropSpeed.TotalMilliseconds)
                {
                    CurrentBlock.MoveDown();
                    if (!BlockFits())
                    {
                        CurrentBlock.MoveUp();
                        _lockingDown = true;
                    }
                    else if (_softDropMultiplier > 1)
                    {
                        _score += 1;
                    }

                    _elapsed = _elapsed.Subtract(_dropSpeed);
                }
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        _spriteBatch.Draw(BackgroundTexture,
                          _graphics.GraphicsDevice.Viewport.Bounds,
                          Color.White);
        _spriteBatch.Draw(PlayingAreaTexture,
                          _playingAreaOrigin,
                          Color.White);
        DrawMatrix();
        DrawInfoPanel();

        if (_isPaused)
        {
            // TODO(PERE): Calculate center by measuring the string
            // TODO(PERE): Use a bigger font for the "PAUSED" text
            // TODO(PERE): The todo's above aren't done since this is
            // temporary and should be replaced by a proper popup screen
            // with buttons to continue or quit.
            _spriteBatch.DrawString(_fontTitle,
                                    "PAUSED",
                                    new Vector2((_graphics.PreferredBackBufferWidth * 0.5f) - 37,
                                                (_graphics.PreferredBackBufferHeight * 0.5f) - 10),
                                    Color.White);
        }
        else if (_gameOver)
        {
            _spriteBatch.Draw(pixel,
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
            _spriteBatch.DrawString(_fontTitle,
                                    "GAME OVER",
                                    new Vector2((_graphics.PreferredBackBufferWidth * 0.5f) - 50,
                                                (_graphics.PreferredBackBufferHeight * 0.5f) - 10),
                                    Color.White);
        }
        else
        {
            DrawNextTetromino();
            DrawHeldTetromino();
            DrawCurrentBlockGhost();
            DrawCurrentBlock();
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawCurrentBlock()
    {
        foreach (Point p in CurrentBlock.TilePositions())
        {
            // NOTE(PERE): We skip the first two rows which are meant to be invisible to the player.
            if (p.Y > 1)
            {
                Point location = new((int)_matrixOrigin.X + (p.X * _cellSize.X),
                                     (int)_matrixOrigin.Y + (p.Y * _cellSize.Y));
                Rectangle tileBounds = new(location, _cellSize);
                _spriteBatch.Draw(_tiles[(int)CurrentBlock.Type],
                                  tileBounds,
                                  Color.White);
            }
        }
    }

    private void DrawCurrentBlockGhost()
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
                    _spriteBatch.Draw(_tiles[(int)CurrentBlock.Type],
                                      tileBounds,
                                      Color.White * 0.25f);
                }
            }
        }
    }

    private void DrawNextTetromino()
    {
        // TODO(PERE): We could "cache" the next Tetromino and update it only
        // after locking down the current piece or after holding the first piece
        // since we know it can't change otherwise. This would be a small optimization
        // though since this is not a demanding game, we will evaluate this idea
        // when we do a better version of the preview window.
        _spriteBatch.Draw(_blocks[(int)_blockQueue.PeekNextBlockType()],
                          _previewPanelDest,
                          Color.White);
    }

    private void DrawHeldTetromino()
    {
        // TODO(PERE): We could "cache" the held Tetromino type and update it only
        // after holding a piece since we know it can't change otherwise. This would
        // be a small optimization though since this is not a demanding game.
        BlockType heldType = _heldBlock?.Type ?? BlockType.None;
        _spriteBatch.Draw(_blocks[(int)heldType],
                          _heldPanelDest,
                          Color.White);
    }

    private void DrawMatrix()
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
                _spriteBatch.Draw(_tiles[(int)blockType],
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
    private void DrawInfoPanel()
    {
        _spriteBatch.Draw(pixel,
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
        _spriteBatch.DrawString(_fontTitle,
                                "SCORE",
                                titleDest,
                                Color.White);

        // TODO(PERE): Calculate the height of the text dynamically VS
        // using "magic" numbers.
        Rectangle labelPanel = new((int)titleDest.X,
                                   (int)titleDest.Y + 24,
                                   (int)panelSize.X - ((int)panelMargin.X * 2),
                                   25);
        _spriteBatch.Draw(pixel,
                          labelPanel,
                          Color.Black);

        // TODO(PERE): Center text VS right align.
        // TODO(PERE): Make sure we have enough space for the max possible score.
        _spriteBatch.DrawString(_fontNormal,
                                $"{_score}",
                                labelPanel.Location.ToVector2() + labelMargin,
                                Color.White);

        titleDest = labelPanel.Location.ToVector2();
        titleDest.Y += labelPanel.Height + panelMargin.Y;
        _spriteBatch.DrawString(_fontTitle,
                                "LEVEL",
                                titleDest,
                                Color.White);

        // TODO(PERE): Calculate the height of the text dynamically VS
        // using "magic" numbers.
        labelPanel.Y = (int)titleDest.Y + 24;
        _spriteBatch.Draw(pixel,
                          labelPanel,
                          Color.Black);

        // TODO(PERE): Center text VS right align.
        _spriteBatch.DrawString(_fontNormal,
                                $"{_level}",
                                labelPanel.Location.ToVector2() + labelMargin,
                                Color.White);

        titleDest = labelPanel.Location.ToVector2();
        titleDest.Y += labelPanel.Height + panelMargin.Y;
        _spriteBatch.DrawString(_fontTitle,
                                "LINES",
                                titleDest,
                                Color.White);

        // TODO(PERE): Calculate the height of the text dynamically VS
        // using "magic" numbers.
        labelPanel.Y = (int)titleDest.Y + 24;
        _spriteBatch.Draw(pixel,
                          labelPanel,
                          Color.Black);

        // TODO(PERE): Center text VS right align.
        _spriteBatch.DrawString(_fontNormal,
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
        // TODO(PERE): Increment score
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
            _dropSpeed = TimeSpan.FromSeconds(speed);
        }
        else
        {
            _comboCount = -1;
        }

        CurrentBlock = _blockQueue.Dequeue();
        _canHold = true;
    }
}

/*
 * TODO(PERE):
 * - Choose whether to use properties VS fields
 * - Install StyleCop and fix related code style issues
 * - I got a GAME OVER probably by hard dropping a piece at the same time
 *   it was locking down. I hard drop the piece, then saw the GAME OVER
 *   screen while the current Tetromino was at the top of the screen.
 */
