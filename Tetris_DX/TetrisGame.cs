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

    /// <summary>
    /// Used to draw basic shapes with any color, such as the background
    /// rectangle under the score and other info.
    /// </summary>
    private Texture2D pixel;

    private readonly GameMatrix _gameMatrix;
    private readonly Texture2D[] _tiles;
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

    private TimeSpan _elapsed = TimeSpan.Zero;
    /// <summary>
    /// Amount of time before the piece moves down by one row
    /// </summary>
    private readonly TimeSpan _dropSpeed = TimeSpan.FromSeconds(1);
    /// <summary>
    /// Amount of time before the piece is locked in place.
    /// </summary>
    private TimeSpan _lockDownDelay = TimeSpan.FromSeconds(0.5);
    private bool _lockingDown = false;
    private BlockBase _currentBlock;
    private readonly BlockQueue _blockQueue = new();

    // TODO(PERE): Create a PlayerController class and handle keyboard,
    // gamepad and maybe mouse controls
    private KeyboardState oldKeyboardState = Keyboard.GetState();
    private KeyboardState newKeyboardState;
    private int _softDropMultiplier = 1;
    private readonly TimeSpan _autoRepeatDelay = TimeSpan.FromMilliseconds(250);
    private readonly TimeSpan _autoRepeatRate = TimeSpan.FromMilliseconds(50);
    private TimeSpan _elapsedSinceLastAutoRepeat = TimeSpan.FromMilliseconds(50);
    private MovementDirection _movementDirection = MovementDirection.None;

    private Texture2D BackgroundTexture { get; set; }
    // TODO(PERE): Should we use a texture with the grid instead of only
    // having the border and drawing empty cells manually? We would need
    // two textures if we wanted to support not showing the grid.
    private Texture2D PlayingAreaTexture { get; set; }

    private int _score;
    private int _lines;
    private int _level = 1;

    // Font resources
    private SpriteFont _fontNormal;
    private SpriteFont _fontTitle;

    public TetrisGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _gameMatrix = new GameMatrix(22, 10);
        _tiles = new Texture2D[8];

        // TODO(PERE): Spawn block randomly
        _currentBlock = _blockQueue.Dequeue();
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

        _fontNormal = Content.Load<SpriteFont>("Fonts/Game/normal");
        _fontTitle = Content.Load<SpriteFont>("Fonts/Game/title");

        // TODO(PERE): This doesn't feel like the best place for it, but textures
        // aren't yet loaded in the Initialize method.
        // NOTE(PERE): We use a margin of 10 pixels between the infoPanel and
        // the playingField and a panelSize of 174x174.
        Vector2 infoPanelSize = new(173);
        _infoPanelDest =
            new Rectangle((int)(_playingAreaOrigin.X - infoPanelSize.X - 10),
                          (int)(_playingAreaOrigin.Y + PlayingAreaTexture.Height - infoPanelSize.Y),
                          (int)infoPanelSize.X,
                          (int)infoPanelSize.Y);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        newKeyboardState = Keyboard.GetState();
        if (KeyDown(Keys.Left))
        {
            _movementDirection = MovementDirection.Left;
            _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
        }
        else if (KeyDown(Keys.Right))
        {
            _movementDirection = MovementDirection.Right;
            _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
        }

        if (_movementDirection == MovementDirection.Left &&
            KeyUp(Keys.Left) &&
            KeyPressed(Keys.Right))
        {
            _movementDirection = MovementDirection.Right;
            _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
        }
        else if (_movementDirection == MovementDirection.Right &&
                 KeyUp(Keys.Right) &&
                 KeyPressed(Keys.Left))
        {
            _movementDirection = MovementDirection.Left;
            _elapsedSinceLastAutoRepeat = TimeSpan.Zero;
        }

        if (_movementDirection == MovementDirection.Left &&
            KeyPressed(Keys.Left))
        {
            _elapsedSinceLastAutoRepeat += gameTime.ElapsedGameTime;
        }
        else if (_movementDirection == MovementDirection.Right &&
                 KeyPressed(Keys.Right))
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
                    _currentBlock.MoveLeft();
                    if (!BlockFits())
                    {
                        _currentBlock.MoveRight();
                    }
                    break;
                case MovementDirection.Right:
                    _currentBlock.MoveRight();
                    if (!BlockFits())
                    {
                        _currentBlock.MoveLeft();
                    }
                    break;
            }
        }

        if (KeyDown(Keys.Up))
        {
            _currentBlock.RotateCW();
            if (!BlockFits())
            {
                _currentBlock.RotateCCW();
            }
        }
        else if (KeyDown(Keys.Z))
        {
            _currentBlock.RotateCCW();
            if (!BlockFits())
            {
                _currentBlock.RotateCW();
            }
        }

        if (KeyDown(Keys.Down))
        {
            _softDropMultiplier = 20;
        }
        else if (KeyUp(Keys.Down))
        {
            _softDropMultiplier = 1;
        }

        if (KeyDown(Keys.Space))
        {
            do
            {
                _currentBlock.MoveDown();
            } while (BlockFits());

            _currentBlock.MoveUp();
            LockDownBlock();
        }

        oldKeyboardState = newKeyboardState;

        _elapsed = _elapsed.Add(gameTime.ElapsedGameTime.Multiply(_softDropMultiplier));
        if (_lockingDown)
        {
            if (_lockDownDelay.TotalMilliseconds <= 0)
            {
                _lockDownDelay = TimeSpan.FromSeconds(0.5);
                _lockingDown = false;
                LockDownBlock();
            }
            else
            {
                _lockDownDelay = _lockDownDelay.Subtract(gameTime.ElapsedGameTime);
            }
        }
        else if (_elapsed.TotalMilliseconds > _dropSpeed.TotalMilliseconds)
        {
            _currentBlock.MoveDown();
            if (!BlockFits())
            {
                _currentBlock.MoveUp();
                _lockingDown = true;
            }
            _elapsed = _elapsed.Subtract(_dropSpeed);
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
        DrawCurrentBlock();
        DrawInfoPanel();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawCurrentBlock()
    {
        foreach (Point p in _currentBlock.TilePositions())
        {
            // NOTE(PERE): We skip the first two rows which are meant to be invisible to the player.
            if (p.Y > 1)
            {
                Point location = new((int)_matrixOrigin.X + (p.X * _cellSize.X),
                                     (int)_matrixOrigin.Y + (p.Y * _cellSize.Y));
                Rectangle tileBounds = new(location, _cellSize);
                _spriteBatch.Draw(_tiles[(int)_currentBlock.Type],
                                  tileBounds,
                                  Color.White);
            }
        }
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

    private bool BlockFits()
    {
        foreach (Point p in _currentBlock.TilePositions())
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
        foreach (Point p in _currentBlock.TilePositions())
        {
            _gameMatrix[p.Y, p.X] = _currentBlock.Type;
        }

        // TODO(PERE): Better clearing with visual feedback
        // TODO(PERE): Increment score
        _gameMatrix.ClearFullRows();

        _currentBlock = _blockQueue.Dequeue();
        // TODO(PERE): Enable canHold
        // TODO(PERE): Validate game over conditions
    }

    /// <summary>
    /// Returns <c>true</c> if the key is being pressed, <c>false</c> otherwise.
    /// </summary>
    /// <remarks>
    /// A key is considered being pressed only if its previous state was also pressed.
    /// Use <see cref="KeyDown(KeyboardState, Keys)"/> to know if the key transitioned
    /// from up to down.
    /// </remarks>
    /// <param name="newKeyboardState">The current state of the keyboard.</param>
    /// <param name="key">The key to evaluate.</param>
    /// <returns><c>true</c> if the key is being pressed, <c>false</c> otherwise.</returns>
    private bool KeyPressed(Keys key)
    {
        bool result = oldKeyboardState.IsKeyDown(key) && newKeyboardState.IsKeyDown(key);

        return result;
    }

    /// <summary>
    /// Returns <c>true</c> if the key transitioned from up to down, <c>false</c> otherwise.
    /// </summary>
    /// <param name="key">The key to evaluate.</param>
    /// <returns><c>true</c> if the key transitioned from up to down, <c>false</c> otherwise.</returns>
    private bool KeyDown(Keys key)
    {
        bool result = oldKeyboardState.IsKeyUp(key) && newKeyboardState.IsKeyDown(key);

        return result;
    }

    /// <summary>
    /// Returns <c>true</c> if the key transitioned from down to up, <c>false</c> otherwise.
    /// </summary>
    /// <param name="key">The key to evaluate.</param>
    /// <returns><c>true</c> if the key transitioned from down to up, <c>false</c> otherwise.</returns>
    private bool KeyUp(Keys key)
    {
        bool result = oldKeyboardState.IsKeyDown(key) && newKeyboardState.IsKeyUp(key);

        return result;
    }
}

/*
 * TODO(PERE):
 * - Choose whether to use properties VS fields
 * - Install StyleCop and fix related code style issues
 */
