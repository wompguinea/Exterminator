using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Exterminator
{
    public class Rock
    {
        private Texture2D _spriteSheet;
        private Vector2 _position;
        private int _rockType; // 1-6 for different rock sprites
        private int _frameWidth = 32;
        private int _frameHeight = 32;
        private Rectangle _bounds;

        public Rock(Texture2D spriteSheet, Vector2 position, int rockType)
        {
            _spriteSheet = spriteSheet;
            _position = position;
            _rockType = Math.Clamp(rockType, 1, 6); // Ensure rock type is 1-6
            
            // Calculate bounds for collision detection - 30x30 instead of 32x32
            // Offset by 1 pixel on each side to center the smaller collision area
            _bounds = new Rectangle((int)_position.X + 1, (int)_position.Y + 1, 30, 30);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2? offset = null)
        {
            // Calculate which rock sprite to use (1-6)
            int rockIndex = _rockType - 1; // Convert to 0-based index
            
            // Calculate position in spritesheet (2 rows of 3)
            int row = rockIndex / 3; // 0 or 1
            int col = rockIndex % 3; // 0, 1, or 2
            
            Rectangle sourceRect = new Rectangle(
                col * _frameWidth,
                row * _frameHeight,
                _frameWidth,
                _frameHeight
            );

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
        }

        public Rectangle Bounds
        {
            get { return _bounds; }
        }

        public Vector2 Position
        {
            get { return _position; }
        }
    }
} 