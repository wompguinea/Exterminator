using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Exterminator
{
    public class ColorCollisionMap
    {
        private bool[,] _collisionMap;
        private int _width;
        private int _height;
        private Vector2 _position;

        public ColorCollisionMap(Texture2D texture, Vector2 position, Color collisionColor, float tolerance = 0.1f)
        {
            _position = position;
            _width = texture.Width;
            _height = texture.Height;
            _collisionMap = new bool[_width, _height];

            // Extract color data from texture
            Color[] colorData = new Color[_width * _height];
            texture.GetData(colorData);

            int collisionPixels = 0;
            int totalPixels = _width * _height;

            // Create collision map based on color
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    int index = x + y * _width;
                    Color pixelColor = colorData[index];
                    
                    // Check if pixel color is close to collision color
                    float colorDistance = ColorDistance(pixelColor, collisionColor);
                    _collisionMap[x, y] = colorDistance <= tolerance;
                    
                    if (_collisionMap[x, y])
                    {
                        collisionPixels++;
                    }
                }
            }

            // Debug output
            System.Diagnostics.Debug.WriteLine($"ColorCollisionMap created: {collisionPixels}/{totalPixels} pixels marked as collision ({collisionPixels * 100.0f / totalPixels:F1}%)");
        }

        private float ColorDistance(Color color1, Color color2)
        {
            // Calculate Euclidean distance between colors in RGB space
            float rDiff = color1.R - color2.R;
            float gDiff = color1.G - color2.G;
            float bDiff = color1.B - color2.B;
            
            return (float)Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        public bool IsCollisionAt(Vector2 worldPosition)
        {
            // Convert world position to local texture coordinates
            Vector2 localPos = worldPosition - _position;
            
            int x = (int)localPos.X;
            int y = (int)localPos.Y;
            
            // Check bounds
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return false;
            
            return _collisionMap[x, y];
        }

        public bool IsCollisionInArea(Rectangle worldBounds)
        {
            // Check if any part of the rectangle intersects with collision areas
            Vector2 localTopLeft = new Vector2(worldBounds.Left, worldBounds.Top) - _position;
            Vector2 localBottomRight = new Vector2(worldBounds.Right, worldBounds.Bottom) - _position;
            
            int startX = Math.Max(0, (int)localTopLeft.X);
            int endX = Math.Min(_width - 1, (int)localBottomRight.X);
            int startY = Math.Max(0, (int)localTopLeft.Y);
            int endY = Math.Min(_height - 1, (int)localBottomRight.Y);
            
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (_collisionMap[x, y])
                        return true;
                }
            }
            
            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            spriteBatch.Draw(
                texture,
                _position,
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
} 