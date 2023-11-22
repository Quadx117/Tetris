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
