using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.IO;

namespace Exterminator;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Player _player;
    private Enemy _enemy;
    private Texture2D _spriteSheet;
    private Texture2D _enemyTexture;
    private Texture2D _rocksTexture;
    private Texture2D _caveFloorTexture;
    private Texture2D _caveWallTexture;
    private ColorCollisionMap _caveWallCollision;
    private List<Rock> _rocks;
    private Texture2D _splashImageTexture;
    public static double DeltaTime { get; private set; }
    public static Random Random { get; private set; }
    private StreamWriter _logFile;
    
    // Game state variables
    private int _score = 0;
    private int _lives = 3;
    private bool _gameOver = false;
    private bool _levelComplete = false;
    private bool _showSplashScreen = true; // Start with splash screen
    private int _totalAntsKilled = 0;
    private int _currentLevel = 1; // Current level tracking
    private int _antsRequiredForLevel; // Calculated based on level
    private SpriteFont _font;
    private SpriteFont _titleFont;
    private Texture2D _heartTexture;

    public Game1()
    {
        try
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Random = new Random();

            // Initialize logging system
            _logFile = new StreamWriter("game_log.txt", true);
            Log("Game constructor called");
        }
        catch (Exception ex)
        {
            Log($"Error in constructor: {ex}");
            throw;
        }
    }

    private void Log(string message)
    {
        try
        {
            string logMessage = $"{DateTime.Now}: {message}";
            _logFile.WriteLine(logMessage);
            _logFile.Flush();
            System.Diagnostics.Debug.WriteLine(logMessage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to log: {ex}");
        }
    }

    protected override void Initialize()
    {
        try
        {
            Log("Initializing game...");
            
            // Configure fullscreen display
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();

            Log("Graphics settings applied");

            base.Initialize();
            Log("Initialize completed");
        }
        catch (Exception ex)
        {
            Log($"Error in Initialize: {ex}");
            throw;
        }
    }

    protected override void LoadContent()
    {
        try
        {
            Log("Loading content...");
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load main sprite sheet
            Log("Loading sprite sheet...");
            _spriteSheet = Content.Load<Texture2D>("sprite-sheet");

            // Load enemy texture
            Log("Loading enemy texture...");
            _enemyTexture = Content.Load<Texture2D>("ant");

            // Load UI fonts
            Log("Loading font...");
            _font = Content.Load<SpriteFont>("gameFont");

            Log("Loading title font...");
            _titleFont = Content.Load<SpriteFont>("titleFont");

            Log("Loading splash image...");
            _splashImageTexture = Content.Load<Texture2D>("splash_image");

            Log("Creating player...");
            _player = new Player(_spriteSheet, new Vector2(400, 300), 32, 32);

            Log("Creating enemy...");
            _enemy = new Enemy(_enemyTexture, 32, 32);
            
            // Connect ant destroyed event to scoring
            _enemy.OnAntDestroyed += AddScore;

            // Load UI elements
            Log("Loading heart texture...");
            _heartTexture = Content.Load<Texture2D>("heart");

            Log("Loading rocks texture...");
            _rocksTexture = Content.Load<Texture2D>("rocks");

            Log("Loading cave textures...");
            _caveFloorTexture = Content.Load<Texture2D>("cave_floor");
            _caveWallTexture = Content.Load<Texture2D>("cave_wall");

            Log("Creating cave wall collision map...");
            _caveWallCollision = new ColorCollisionMap(
                _caveWallTexture, 
                Vector2.Zero, // Zero offset, collision detection handles positioning
                new Color(10, 10, 11), 5f);

            Log("Creating rocks...");
            _rocks = new List<Rock>();
            CreateRocks();

            // Set up level requirements
            _antsRequiredForLevel = CalculateRequiredAntsForLevel(_currentLevel);

            Log("Content loading completed");
        }
        catch (Exception ex)
        {
            Log($"Error in LoadContent: {ex}");
            throw;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        try
        {
            Log("Update called");
            DeltaTime = gameTime.ElapsedGameTime.TotalSeconds;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Log("Exit requested");
                Exit();
            }

            // Process player movement input (WASD only)
            var keyboardState = Keyboard.GetState();
            Vector2 movement = Vector2.Zero;

            // Handle new game input
            if (keyboardState.IsKeyDown(Keys.N) && (_gameOver || _levelComplete))
            {
                StartNewGame();
            }

            // Handle splash screen input
            if (_showSplashScreen)
            {
                if (keyboardState.IsKeyDown(Keys.Space) || keyboardState.IsKeyDown(Keys.Enter))
                {
                    _showSplashScreen = false;
                    StartNewGame();
                }
                return; // Skip game logic while on splash screen
            }

            if (!_gameOver && !_levelComplete)
            {
                if (keyboardState.IsKeyDown(Keys.W))
                    movement.Y -= 1;
                if (keyboardState.IsKeyDown(Keys.S))
                    movement.Y += 1;
                if (keyboardState.IsKeyDown(Keys.A))
                    movement.X -= 1;
                if (keyboardState.IsKeyDown(Keys.D))
                    movement.X += 1;

                // Process bullet direction input (Arrow keys) - 360-degree precision
                Vector2 bulletDirection = Vector2.Zero;
                float inputSensitivity = 1.0f;

                // Handle individual arrow keys for precise diagonal support
                if (keyboardState.IsKeyDown(Keys.Up))
                    bulletDirection.Y -= inputSensitivity;
                if (keyboardState.IsKeyDown(Keys.Down))
                    bulletDirection.Y += inputSensitivity;
                if (keyboardState.IsKeyDown(Keys.Left))
                    bulletDirection.X -= inputSensitivity;
                if (keyboardState.IsKeyDown(Keys.Right))
                    bulletDirection.X += inputSensitivity;

                // Normalize for consistent bullet speed
                if (bulletDirection != Vector2.Zero)
                {
                    bulletDirection.Normalize();
                }

                // Auto-fire when arrow keys are pressed
                if (bulletDirection != Vector2.Zero)
                {
                    _player.Attack();
                }
                else
                {
                    _player.StopAttack();
                }

                _player.MoveWithCollision(movement, bounds => CheckRockCollision(bounds, false));
                _player.SetBulletDirection(bulletDirection);
            }
            else
            {
                // Disable player actions when game is over or level complete
                _player.StopAttack();
                _player.Move(Vector2.Zero);
                _player.SetBulletDirection(Vector2.Zero);
            }

            _player.Update(gameTime, bounds => CheckRockCollision(bounds, false));
            _enemy.Update(gameTime, _player.Position, bounds => CheckRockCollision(bounds, true));
            
            // Debug logging
            Log($"Player position: {_player.Position}, Enemy count: {_enemy.Boids.Count}");
            
            // Check bullet-ant collisions
            _enemy.CheckBulletCollisions(_player.Bullets);
            
            // Check ant-player collisions
            CheckAntPlayerCollisions();
            
            // Check for game over condition
            if (_lives <= 0)
            {
                _gameOver = true;
            }
            
            base.Update(gameTime);
        }
        catch (Exception ex)
        {
            Log($"Error in Update: {ex}");
            throw;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        try
        {
            Log("Draw called");
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            
            if (_showSplashScreen)
            {
                DrawSplashScreen();
            }
            else
            {
                // Calculate offset to center the 800x600 play area on screen
                Vector2 playAreaOffset = new Vector2(
                    (GraphicsDevice.Viewport.Width - 800) / 2f,
                    (GraphicsDevice.Viewport.Height - 600) / 2f
                );
                
                // Draw cave floor background centered
                if (_caveFloorTexture != null)
                {
                    _spriteBatch.Draw(_caveFloorTexture, playAreaOffset, Color.White);
                }
                
                if (!_gameOver && !_levelComplete)
                {
                    // Draw game objects with offset
                    _player.Draw(_spriteBatch, playAreaOffset);
                    _enemy.Draw(_spriteBatch, playAreaOffset);
                    
                    // Draw rocks with offset
                    foreach (var rock in _rocks)
                    {
                        rock.Draw(_spriteBatch, playAreaOffset);
                    }
                }
                
                // Draw cave wall layer on top centered
                if (_caveWallTexture != null)
                {
                    _spriteBatch.Draw(_caveWallTexture, playAreaOffset, Color.White);
                }
                
                // Draw UI overlay (no offset needed for UI)
                DrawUI();
            }
            
            _spriteBatch.End();

            base.Draw(gameTime);
        }
        catch (Exception ex)
        {
            Log($"Error in Draw: {ex}");
            throw;
        }
    }

    private void DrawUI()
    {
        if (_font == null) return;

        // Draw score display
        string scoreText = $"Score: {_score}";
        Vector2 scorePosition = new Vector2(10, 10);
        _spriteBatch.DrawString(_font, scoreText, scorePosition, Color.White);

        // Draw health hearts
        DrawHearts();

        // Draw game over message
        if (_gameOver)
        {
            string gameOverText = "GAME OVER";
            Vector2 gameOverSize = _font.MeasureString(gameOverText);
            Vector2 gameOverPosition = new Vector2(
                (GraphicsDevice.Viewport.Width - gameOverSize.X) / 2f,
                (GraphicsDevice.Viewport.Height - gameOverSize.Y) / 2f
            );
            _spriteBatch.DrawString(_font, gameOverText, gameOverPosition, Color.Red);
            
            string finalScoreText = $"Final Score: {_score}";
            Vector2 finalScoreSize = _font.MeasureString(finalScoreText);
            Vector2 finalScorePosition = new Vector2(
                (GraphicsDevice.Viewport.Width - finalScoreSize.X) / 2f,
                (GraphicsDevice.Viewport.Height - finalScoreSize.Y) / 2f + 50
            );
            _spriteBatch.DrawString(_font, finalScoreText, finalScorePosition, Color.White);
            
            string newGameText = "Press N to Start New Game";
            Vector2 newGameSize = _font.MeasureString(newGameText);
            Vector2 newGamePosition = new Vector2(
                (GraphicsDevice.Viewport.Width - newGameSize.X) / 2f,
                (GraphicsDevice.Viewport.Height - newGameSize.Y) / 2f + 100
            );
            _spriteBatch.DrawString(_font, newGameText, newGamePosition, Color.Yellow);
        }

        // Draw level complete message
        if (_levelComplete)
        {
            string levelCompleteText = "LEVEL COMPLETE!";
            Vector2 levelCompleteSize = _font.MeasureString(levelCompleteText);
            Vector2 levelCompletePosition = new Vector2(
                (GraphicsDevice.Viewport.Width - levelCompleteSize.X) / 2f,
                (GraphicsDevice.Viewport.Height - levelCompleteSize.Y) / 2f
            );
            _spriteBatch.DrawString(_font, levelCompleteText, levelCompletePosition, Color.Green);
            
            string finalScoreText = $"Final Score: {_score}";
            Vector2 finalScoreSize = _font.MeasureString(finalScoreText);
            Vector2 finalScorePosition = new Vector2(
                (GraphicsDevice.Viewport.Width - finalScoreSize.X) / 2f,
                (GraphicsDevice.Viewport.Height - finalScoreSize.Y) / 2f + 50
            );
            _spriteBatch.DrawString(_font, finalScoreText, finalScorePosition, Color.White);
            
            string newGameText = "Press N to Start New Game";
            Vector2 newGameSize = _font.MeasureString(newGameText);
            Vector2 newGamePosition = new Vector2(
                (GraphicsDevice.Viewport.Width - newGameSize.X) / 2f,
                (GraphicsDevice.Viewport.Height - newGameSize.Y) / 2f + 100
            );
            _spriteBatch.DrawString(_font, newGameText, newGamePosition, Color.Yellow);
        }
    }

    private void DrawHearts()
    {
        if (_heartTexture == null) return;

        int heartSpacing = 30; // Space between hearts
        Vector2 heartPosition = new Vector2(10, 40);

        for (int i = 0; i < _lives; i++)
        {
            Vector2 currentHeartPos = heartPosition + new Vector2(i * heartSpacing, 0);
            _spriteBatch.Draw(
                _heartTexture,
                currentHeartPos,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                0f
            );
        }
    }

    protected override void UnloadContent()
    {
        try
        {
            Log("Unloading content...");
            _logFile?.Close();
            _logFile?.Dispose();
            base.UnloadContent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in UnloadContent: {ex}");
        }
    }

    private void CheckAntPlayerCollisions()
    {
        if (_gameOver) return;

        Rectangle playerBounds = new Rectangle(
            (int)_player.Position.X,
            (int)_player.Position.Y,
            32, // Player width
            32  // Player height
        );

        foreach (var boid in _enemy.Boids)
        {
            Rectangle antBounds = new Rectangle(
                (int)(boid.Position.X - 16), // Half width
                (int)(boid.Position.Y - 16), // Half height
                32, // Ant width
                32  // Ant height
            );

            if (playerBounds.Intersects(antBounds))
            {
                _lives--;
                Log($"Player hit! Lives remaining: {_lives}");
                
                // Activate avoidance mode for all ants
                _enemy.ActivateAvoidanceMode();
                
                // Remove the ant that hit the player
                _enemy.Boids.Remove(boid);
                break; // Only count one hit per frame
            }
        }
    }

    private bool CheckRockCollision(Rectangle bounds, bool isAnt = false)
    {
        // Check rock collisions - rocks are positioned in the 800x600 area, so no offset needed
        foreach (var rock in _rocks)
        {
            if (bounds.Intersects(rock.Bounds))
            {
                Log($"Rock collision detected at {bounds.Location}");
                return true; // Collision detected
            }
        }
        
        // Check play area boundaries (800x600 area) - exclude ants
        if (!isAnt && (bounds.Left < 0 || bounds.Right > 800 || bounds.Top < 0 || bounds.Bottom > 600))
        {
            Log($"Play area boundary collision detected at {bounds.Location} - Left:{bounds.Left} Right:{bounds.Right} Top:{bounds.Top} Bottom:{bounds.Bottom}");
            return true; // Collision detected
        }
        
        // Exclude ants from cave wall collision
        if (!isAnt && _caveWallCollision != null)
        {
            // Calculate the dynamic offset for the current screen size
            Vector2 dynamicOffset = new Vector2(
                (GraphicsDevice.Viewport.Width - 800) / 2f,
                (GraphicsDevice.Viewport.Height - 600) / 2f
            );
            
            // Adjust bounds to account for the dynamic visual offset
            Rectangle adjustedBounds = new Rectangle(
                (int)(bounds.X + dynamicOffset.X),
                (int)(bounds.Y + dynamicOffset.Y),
                bounds.Width,
                bounds.Height
            );
            
            if (_caveWallCollision.IsCollisionInArea(adjustedBounds))
            {
                Log($"Cave wall collision detected at {bounds.Location} (adjusted: {adjustedBounds.Location})");
                return true; // Collision detected
            }
        }
        
        return false; // No collision
    }

    private Vector2 GetValidMovement(Vector2 currentPosition, Vector2 desiredMovement)
    {
        // Try horizontal movement first
        Rectangle horizontalTest = new Rectangle(
            (int)(currentPosition.X + desiredMovement.X),
            (int)currentPosition.Y,
            32, 32
        );
        
        if (!CheckRockCollision(horizontalTest))
        {
            currentPosition.X += desiredMovement.X;
        }
        
        // Try vertical movement
        Rectangle verticalTest = new Rectangle(
            (int)currentPosition.X,
            (int)(currentPosition.Y + desiredMovement.Y),
            32, 32
        );
        
        if (!CheckRockCollision(verticalTest))
        {
            currentPosition.Y += desiredMovement.Y;
        }
        
        return currentPosition;
    }

    public void AddScore(int points)
    {
        _score += points;
        _totalAntsKilled++;
        Log($"Score increased by {points}. New score: {_score}. Total ants killed: {_totalAntsKilled}");
        
        // Check for level completion
        if (_totalAntsKilled >= _antsRequiredForLevel)
        {
            _levelComplete = true;
            Log("Level Complete! You've killed 100 ants!");
        }
    }

    private void StartNewGame()
    {
        _score = 0;
        _lives = 3;
        _gameOver = false;
        _levelComplete = false;
        _showSplashScreen = false; // Hide splash screen
        _totalAntsKilled = 0;
        _currentLevel = 1; // Reset to level 1
        
        // Calculate required ants for current level
        _antsRequiredForLevel = CalculateRequiredAntsForLevel(_currentLevel);
        
        // Reset player position
        _player.ResetPosition(new Vector2(400, 300));
        
        // Clear all ants and restart spawning
        _enemy.Reset();
        
        Log("New game started");
    }

    private int CalculateRequiredAntsForLevel(int level)
    {
        // Difficulty formula: Base 50 ants + 25 per level, with some variation
        // Level 1: 75 ants
        // Level 2: 100 ants  
        // Level 3: 125 ants
        // Level 4: 150 ants
        // etc.
        
        int baseAnts = 50;
        int antsPerLevel = 25;
        
        return baseAnts + (level - 1) * antsPerLevel;
    }

    private void CreateRocks()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 position;
            int attempts = 0;
            const int maxAttempts = 100;
            
            do
            {
                position = new Vector2(Random.Next(50, 750), Random.Next(50, 550));
                attempts++;
            } while (!IsValidRockPosition(position) && attempts < maxAttempts);
            
            // If we found a valid position, create the rock
            if (attempts < maxAttempts)
            {
                int rockType = Random.Next(1, 7); // Random rock type 1-6
                _rocks.Add(new Rock(_rocksTexture, position, rockType));
            }
        }
    }

    private bool IsValidRockPosition(Vector2 position)
    {
        // Check if position is too close to player spawn
        Vector2 playerSpawn = new Vector2(400, 300);
        if (Vector2.Distance(position, playerSpawn) < 100f)
        {
            return false;
        }
        
        // Check if position overlaps with cave wall collision
        Rectangle rockBounds = new Rectangle((int)position.X, (int)position.Y, 32, 32);
        if (_caveWallCollision != null && _caveWallCollision.IsCollisionInArea(rockBounds))
        {
            return false;
        }
        
        // Check if position overlaps with existing rocks
        foreach (var existingRock in _rocks)
        {
            if (Vector2.Distance(position, existingRock.Position) < 50f)
            {
                return false;
            }
        }
        
        return true;
    }

    private void DrawSplashScreen()
    {
        // Draw splash background image
        if (_splashImageTexture != null)
        {
            // Draw the splash image to fill the entire screen
            _spriteBatch.Draw(_splashImageTexture, Vector2.Zero, Color.White);
        }

        if (_titleFont == null) return;

        // Draw game title
        string titleText = "EXTERMINATOR";
        Vector2 titleSize = _titleFont.MeasureString(titleText);
        Vector2 titlePosition = new Vector2(
            (GraphicsDevice.Viewport.Width - titleSize.X) / 2f,
            (GraphicsDevice.Viewport.Height - titleSize.Y) / 2f - 100
        );
        
        // Draw title with black outline and yellow fill
        // Draw outline (black)
        Vector2 outlineOffset = new Vector2(2, 2);
        _spriteBatch.DrawString(_titleFont, titleText, titlePosition + outlineOffset, Color.Black);
        _spriteBatch.DrawString(_titleFont, titleText, titlePosition - outlineOffset, Color.Black);
        _spriteBatch.DrawString(_titleFont, titleText, titlePosition + new Vector2(2, -2), Color.Black);
        _spriteBatch.DrawString(_titleFont, titleText, titlePosition + new Vector2(-2, 2), Color.Black);
        
        // Draw main text (yellow)
        _spriteBatch.DrawString(_titleFont, titleText, titlePosition, Color.Yellow);

        // Draw subtitle
        if (_font != null)
        {
            string subtitleText = "Cave Defense";
            Vector2 subtitleSize = _font.MeasureString(subtitleText);
            Vector2 subtitlePosition = new Vector2(
                (GraphicsDevice.Viewport.Width - subtitleSize.X) / 2f,
                titlePosition.Y + titleSize.Y + 20
            );
            _spriteBatch.DrawString(_font, subtitleText, subtitlePosition, Color.White);
        }

        // Draw instructions
        if (_font != null)
        {
            string instructionText = "Press SPACE or ENTER to Start";
            Vector2 instructionSize = _font.MeasureString(instructionText);
            Vector2 instructionPosition = new Vector2(
                (GraphicsDevice.Viewport.Width - instructionSize.X) / 2f,
                titlePosition.Y + titleSize.Y + 80
            );
            _spriteBatch.DrawString(_font, instructionText, instructionPosition, Color.LightGray);
            
            string controlsText = "WASD to Move | Arrow Keys to Shoot";
            Vector2 controlsSize = _font.MeasureString(controlsText);
            Vector2 controlsPosition = new Vector2(
                (GraphicsDevice.Viewport.Width - controlsSize.X) / 2f,
                instructionPosition.Y + instructionSize.Y + 20
            );
            _spriteBatch.DrawString(_font, controlsText, controlsPosition, Color.LightGray);
        }
    }
}
