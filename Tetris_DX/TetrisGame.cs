﻿namespace Tetris_DX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tetris_DX.Blocks;
using Tetris_DX.Components;

public class TetrisGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly GameMatrix _gameMatrix;
    private readonly Texture2D[] _tiles;
    // TODO(PERE): See how to calculate the cell size based on the window size,
    // espacially if we want to resize the screen.
    private readonly Point _cellSize = new(25);
    private Vector2 _matrixOrigin;
    private Vector2 _playingAreaOrigin;

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
        _matrixOrigin = new Vector2((_graphics.PreferredBackBufferWidth - playingAreaWidth) / 2,
                                    (_graphics.PreferredBackBufferHeight - playingAreaHeight) / 2);
        _playingAreaOrigin = _matrixOrigin - Vector2.One;

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

        // TODO: Add your update logic here

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
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawMatrix()
    {
        // NOTE(PERE): We skip the first two rows which are meant to be invisible to the player.
        for (int rowIndex = 2; rowIndex < _gameMatrix.RowCount; rowIndex++)
        {
            for (int colIndex = 0; colIndex < _gameMatrix.ColumnCount; colIndex++)
            {
                Point location = new((int)_matrixOrigin.X + (colIndex * _cellSize.X),
                                     (int)_matrixOrigin.Y + ((rowIndex - 2) * _cellSize.Y));
                Rectangle tileBounds = new(location, _cellSize);
                BlockType blockType = _gameMatrix[rowIndex, colIndex];
                _spriteBatch.Draw(_tiles[(int)blockType],
                                  tileBounds,
                                  Color.White);
            }
        }
    }
}
