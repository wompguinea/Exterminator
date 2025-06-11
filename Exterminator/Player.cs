using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace Exterminator
{
    public enum Direction
    {
        Down,      // Row 0
        Left,      // Row 1
        Right,     // Row 2
        Up,        // Row 3
        UpLeft,    // Row 4
        UpRight,   // Row 5
        DownLeft,  // Row 6
        DownRight  // Row 7
    }

    public class Player
    {
        private Texture2D _spriteSheet;
        private Vector2 _position;
        private int _currentFrame;
        private float _animationTimer;
        private float _animationSpeed = 0.1f;
        private int _frameWidth;
        private int _frameHeight;
        private bool _isMoving;
        private float _moveSpeed = 200f;
        private Direction _currentDirection = Direction.Down;
        private Vector2 _bulletDirection = Vector2.Zero; // Separate bullet direction
        private float _aimAngle = 0f; // Continuous aim angle in radians
        private string _currentAnimation = "idle";
        private List<Bullet> _bullets;
        private bool _isFiring = false;
        private float _fireRate = 0.1f; // Time between shots (reduced from 0.2f)
        private float _fireTimer = 0f;

        public Player(Texture2D spriteSheet, Vector2 startPosition, int frameWidth, int frameHeight)
        {
            _spriteSheet = spriteSheet;
            _position = startPosition;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
            _currentFrame = 0;
            _bullets = new List<Bullet>();
        }

        public void Update(GameTime gameTime, Func<Rectangle, bool> collisionCheck = null)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Update fire timer
            if (_fireTimer > 0)
            {
                _fireTimer -= deltaTime;
            }

            // Handle firing
            if (_isFiring && _fireTimer <= 0)
            {
                FireBullet();
                _fireTimer = _fireRate;
            }

            // Update bullets
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                _bullets[i].Update(deltaTime, collisionCheck);
                if (!_bullets[i].IsActive)
                {
                    _bullets.RemoveAt(i);
                }
            }
            
            // Update animation state
            string targetAnimation = _isMoving ? "walk" : "idle";

            if (_currentAnimation != targetAnimation)
            {
                _currentAnimation = targetAnimation;
                _currentFrame = 0;
            }
            
            // Update animation frame only for walk animation
            if (_isMoving)
            {
                _animationTimer += deltaTime;
                if (_animationTimer >= _animationSpeed)
                {
                    _animationTimer = 0f;
                    _currentFrame = (_currentFrame + 1) % 3; // 3 frames for walk animation
                }
            }
            else
            {
                // Idle animation is always frame 0 (first frame of current direction)
                _currentFrame = 0;
            }
        }

        private void FireBullet()
        {
            // Calculate bullet spawn position (center of player)
            Vector2 bulletSpawnPos = _position + new Vector2(_frameWidth / 2f, _frameHeight / 2f);
            
            // Use the continuous aim angle for precise direction
            Vector2 bulletVelocity = new Vector2(
                (float)Math.Cos(_aimAngle),
                (float)Math.Sin(_aimAngle)
            );
            
            // Create bullet with custom velocity instead of direction enum
            _bullets.Add(new Bullet(bulletSpawnPos, bulletVelocity));
        }

        private Direction GetDirectionFromVector(Vector2 direction)
        {
            if (direction == Vector2.Zero)
            {
                return _currentDirection; // Default to current movement direction if no bullet direction
            }

            // Normalize the direction vector
            direction.Normalize();

            // Determine the closest direction based on the vector
            if (Math.Abs(direction.X) > 0.7f && Math.Abs(direction.Y) > 0.7f)
            {
                // Diagonal movement
                if (direction.Y < 0) // Up
                {
                    return direction.X > 0 ? Direction.UpRight : Direction.UpLeft;
                }
                else // Down
                {
                    return direction.X > 0 ? Direction.DownRight : Direction.DownLeft;
                }
            }
            else if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            {
                // Horizontal movement is dominant
                return direction.X > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                // Vertical movement is dominant
                return direction.Y > 0 ? Direction.Down : Direction.Up;
            }
        }

        public void SetBulletDirection(Vector2 direction)
        {
            _bulletDirection = direction;
            
            // Update aim angle based on input
            if (direction != Vector2.Zero)
            {
                // Calculate angle from input vector
                _aimAngle = (float)Math.Atan2(direction.Y, direction.X);
            }
        }

        private Rectangle GetCurrentFrameRectangle()
        {
            int framesPerRow = _spriteSheet.Width / _frameWidth;
            int animationRow = GetAnimationRow();
            int frameX = _currentFrame % framesPerRow;
            int frameY = animationRow;
            
            return new Rectangle(
                frameX * _frameWidth,
                frameY * _frameHeight,
                _frameWidth,
                _frameHeight
            );
        }

        private int GetAnimationRow()
        {
            // Each direction has its own row
            return (int)_currentDirection;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2? offset = null)
        {
            Rectangle sourceRect = GetCurrentFrameRectangle();
            Vector2 drawPosition = _position;
            if (offset.HasValue)
            {
                drawPosition += offset.Value;
            }
            
            spriteBatch.Draw(
                _spriteSheet,
                drawPosition,
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                0f
            );

            // Draw bullets with offset
            foreach (var bullet in _bullets)
            {
                bullet.Draw(spriteBatch, offset);
            }
        }

        public void Attack()
        {
            // Start firing
            _isFiring = true;
        }

        public void StopAttack()
        {
            // Stop firing
            _isFiring = false;
        }

        public Vector2 Position
        {
            get { return _position; }
        }

        public List<Bullet> Bullets
        {
            get { return _bullets; }
        }

        public void ResetPosition(Vector2 newPosition)
        {
            _position = newPosition;
            _bullets.Clear();
            _isFiring = false;
            _fireTimer = 0f;
        }

        public void Move(Vector2 direction)
        {
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _position += direction * _moveSpeed * (float)Game1.DeltaTime;
                _isMoving = true;
                
                // Update direction based on movement
                _currentDirection = GetDirectionFromVector(direction);
            }
            else
            {
                _isMoving = false;
            }
        }

        public void MoveWithCollision(Vector2 direction, Func<Rectangle, bool> collisionCheck)
        {
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                Vector2 newPosition = _position + direction * _moveSpeed * (float)Game1.DeltaTime;
                
                // Check for collisions
                Rectangle newBounds = new Rectangle(
                    (int)newPosition.X,
                    (int)newPosition.Y,
                    _frameWidth,
                    _frameHeight
                );
                
                if (!collisionCheck(newBounds))
                {
                    _position = newPosition;
                    _isMoving = true;
                    
                    // Update direction based on movement
                    _currentDirection = GetDirectionFromVector(direction);
                }
            }
            else
            {
                _isMoving = false;
            }
        }
    }
} 