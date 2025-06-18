using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Exterminator
{
    public class Bullet
    {
        private Vector2 _position;
        private Vector2 _velocity;
        private float _speed = 300f;
        private bool _isActive = true;
        private float _lifetime = 3f; // Bullets disappear after 3 seconds
        private float _currentLifetime = 0f;
        private const float BULLET_SIZE = 8f; // Increased size for plasma blast effect

        public Bullet(Vector2 startPosition, Vector2 velocity)
        {
            _position = startPosition;
            _velocity = velocity * _speed;
        }

        public Bullet(Vector2 startPosition, Direction direction)
        {
            _position = startPosition;
            _velocity = GetDirectionVector(direction) * _speed;
        }

        private Vector2 GetDirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return new Vector2(0, -1);
                case Direction.Down:
                    return new Vector2(0, 1);
                case Direction.Left:
                    return new Vector2(-1, 0);
                case Direction.Right:
                    return new Vector2(1, 0);
                case Direction.UpLeft:
                    return Vector2.Normalize(new Vector2(-1, -1));
                case Direction.UpRight:
                    return Vector2.Normalize(new Vector2(1, -1));
                case Direction.DownLeft:
                    return Vector2.Normalize(new Vector2(-1, 1));
                case Direction.DownRight:
                    return Vector2.Normalize(new Vector2(1, 1));
                default:
                    return new Vector2(0, 1);
            }
        }

        public void Update(float deltaTime, Func<Rectangle, bool> collisionCheck = null)
        {
            if (!_isActive) return;

            Vector2 newPosition = _position + _velocity * deltaTime;
            
            // Check for rock collisions
            if (collisionCheck != null)
            {
                Rectangle bulletBounds = new Rectangle(
                    (int)(newPosition.X - BULLET_SIZE / 2f),
                    (int)(newPosition.Y - BULLET_SIZE / 2f),
                    (int)BULLET_SIZE,
                    (int)BULLET_SIZE
                );
                
                if (collisionCheck(bulletBounds))
                {
                    _isActive = false; // Destroy bullet on rock collision
                    return;
                }
            }
            
            _position = newPosition;
            _currentLifetime += deltaTime;

            // Deactivate bullet if it goes off screen or times out
            if (_currentLifetime >= _lifetime || 
                _position.X < -10 || _position.X > 810 || 
                _position.Y < -10 || _position.Y > 610)
            {
                _isActive = false;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2? offset = null)
        {
            if (!_isActive) return;

            // Draw a simple white circle for the bullet
            var bulletTexture = CreateBulletTexture(spriteBatch.GraphicsDevice);
            Vector2 drawPosition = _position;
            if (offset.HasValue)
            {
                drawPosition += offset.Value;
            }
            
            spriteBatch.Draw(
                bulletTexture,
                drawPosition,
                null,
                Color.White,
                0f,
                new Vector2(BULLET_SIZE, BULLET_SIZE),
                1f,
                SpriteEffects.None,
                0f
            );
        }

        private Texture2D CreateBulletTexture(GraphicsDevice graphicsDevice)
        {
            // Create a plasma blast texture with bright center and glow effect
            int size = (int)BULLET_SIZE * 2; // Make it slightly larger for better plasma effect
            Texture2D texture = new Texture2D(graphicsDevice, size, size);
            Color[] colorData = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDistance = size / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int index = x + y * size;
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance <= maxDistance)
                    {
                        // Create plasma effect with multiple layers
                        float normalizedDistance = distance / maxDistance;
                        
                        // Bright cyan/blue center
                        if (normalizedDistance < 0.3f)
                        {
                            float intensity = 1f - (normalizedDistance / 0.3f);
                            colorData[index] = new Color(
                                (byte)(100 + 155 * intensity), // Blue component
                                (byte)(200 + 55 * intensity),  // Green component (cyan)
                                (byte)(255),                   // Full blue
                                (byte)(255 * intensity)        // Alpha
                            );
                        }
                        // White glow around center
                        else if (normalizedDistance < 0.6f)
                        {
                            float intensity = 1f - ((normalizedDistance - 0.3f) / 0.3f);
                            colorData[index] = new Color(
                                (byte)(200 + 55 * intensity), // White with blue tint
                                (byte)(220 + 35 * intensity), // White with slight green tint
                                (byte)(255),                   // Full blue
                                (byte)(200 * intensity)        // Alpha
                            );
                        }
                        // Outer glow
                        else
                        {
                            float intensity = 1f - ((normalizedDistance - 0.6f) / 0.4f);
                            colorData[index] = new Color(
                                (byte)(100 + 100 * intensity), // Blue
                                (byte)(150 + 70 * intensity),  // Cyan
                                (byte)(200 + 55 * intensity),  // Blue
                                (byte)(100 * intensity)        // Alpha
                            );
                        }
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        public bool IsActive
        {
            get { return _isActive; }
        }

        public Vector2 Position
        {
            get { return _position; }
        }

        public Rectangle Bounds
        {
            get { return new Rectangle((int)(_position.X - BULLET_SIZE / 2f), (int)(_position.Y - BULLET_SIZE / 2f), (int)BULLET_SIZE, (int)BULLET_SIZE); }
        }

        public void Deactivate()
        {
            _isActive = false;
        }
    }
} 