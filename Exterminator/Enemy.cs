using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace Exterminator
{
    public class Boid
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Speed = 150f;
        public float MaxSpeed = 250f;
        public float MaxForce = 15f;
        public float SeparationRadius = 30f;
        public float AlignmentRadius = 50f;
        public float CohesionRadius = 60f;
        public float AvoidPlayerRadius = 80f;
        public float ChasePlayerRadius = 150f; // Distance ants start chasing player
        public float EdgeSwarmRadius = 150f; // Distance from edge to start swarming
        public float TimeOutsidePlayArea = 0f; // Track time spent outside playable area
        public float MaxTimeOutsidePlayArea = 2f; // Maximum time allowed outside (2 seconds)

        public Boid(Vector2 startPosition)
        {
            Position = startPosition;
            Velocity = new Vector2(
                (float)(Game1.Random.NextDouble() * 2 - 1),
                (float)(Game1.Random.NextDouble() * 2 - 1)
            );
            Velocity.Normalize();
            Velocity *= Speed;
            TimeOutsidePlayArea = 0f; // Initialize timer
        }

        public void Update(List<Boid> boids, Vector2 playerPosition, float deltaTime, bool avoidanceMode = false, Func<Rectangle, bool> collisionCheck = null)
        {
            // Check if ant is outside playable area
            bool isOutsidePlayArea = Position.X < 0 || Position.X > 800 || Position.Y < 0 || Position.Y > 600;
            
            // PRIORITY 1: Get into playable area if outside
            if (isOutsidePlayArea)
            {
                TimeOutsidePlayArea += deltaTime;
                
                // Calculate direction to nearest edge of playable area
                Vector2 nearestPlayAreaPoint = GetNearestPlayAreaPoint();
                Vector2 directionToPlayArea = nearestPlayAreaPoint - Position;
                directionToPlayArea.Normalize();
                
                // Set velocity towards playable area with high speed
                Velocity = directionToPlayArea * MaxSpeed;
                TimeOutsidePlayArea = 0f; // Reset timer
                
                // Skip all other behaviors when outside playable area
                // Update position with collision checking
                Vector2 movePosition = Position + Velocity * deltaTime;
                
                if (collisionCheck != null)
                {
                    Rectangle newBounds = new Rectangle(
                        (int)(movePosition.X - 16),
                        (int)(movePosition.Y - 16),
                        32, 32
                    );
                    
                    if (!collisionCheck(newBounds))
                    {
                        Position = movePosition;
                    }
                }
                else
                {
                    Position = movePosition;
                }
                
                return; // Exit early, don't apply other behaviors
            }
            else
            {
                TimeOutsidePlayArea = 0f; // Reset timer when inside playable area
            }

            // Only apply normal behaviors when inside playable area
            Vector2 separation = CalculateSeparation(boids);
            Vector2 alignment = CalculateAlignment(boids);
            Vector2 cohesion = CalculateCohesion(boids);
            Vector2 playerBehavior = CalculatePlayerBehavior(playerPosition, avoidanceMode);
            Vector2 edgeSwarming = CalculateEdgeSwarming();

            // Combine forces with different priorities
            Vector2 acceleration = Vector2.Zero;
            
            // Always apply separation to avoid clustering
            acceleration += separation;
            
            // Apply player behavior if active (highest priority)
            if (playerBehavior != Vector2.Zero)
            {
                acceleration += playerBehavior * 4f; // Even higher priority for player behavior
            }
            // Otherwise apply edge swarming and enhanced flocking
            else
            {
                acceleration += edgeSwarming * 2f; // Strong edge attraction
                acceleration += alignment * 1.5f; // Stronger alignment for grouping
                acceleration += cohesion * 1.5f; // Stronger cohesion for grouping
            }
            
            // Apply acceleration to velocity
            Velocity += acceleration * deltaTime;
            
            // Limit velocity
            if (Velocity.Length() > MaxSpeed)
            {
                Velocity.Normalize();
                Velocity *= MaxSpeed;
            }

            // Update position with collision checking
            Vector2 newPosition = Position + Velocity * deltaTime;
            
            // Check for rock collisions
            if (collisionCheck != null)
            {
                Rectangle newBounds = new Rectangle(
                    (int)(newPosition.X - 16),
                    (int)(newPosition.Y - 16),
                    32, 32
                );
                
                if (!collisionCheck(newBounds))
                {
                    Position = newPosition;
                }
                else
                {
                    // If collision, try to move in X direction only
                    Vector2 xOnlyPosition = new Vector2(newPosition.X, Position.Y);
                    Rectangle xBounds = new Rectangle(
                        (int)(xOnlyPosition.X - 16),
                        (int)(xOnlyPosition.Y - 16),
                        32, 32
                    );
                    
                    if (!collisionCheck(xBounds))
                    {
                        Position = xOnlyPosition;
                    }
                    else
                    {
                        // If X collision, try Y direction only
                        Vector2 yOnlyPosition = new Vector2(Position.X, newPosition.Y);
                        Rectangle yBounds = new Rectangle(
                            (int)(yOnlyPosition.X - 16),
                            (int)(yOnlyPosition.Y - 16),
                            32, 32
                        );
                        
                        if (!collisionCheck(yBounds))
                        {
                            Position = yOnlyPosition;
                        }
                    }
                }
            }
            else
            {
                Position = newPosition;
            }

            // Keep boids within screen bounds
            KeepInBounds();
        }

        private Vector2 GetNearestPlayAreaPoint()
        {
            // Calculate the nearest point on the 800x600 playable area boundary
            float nearestX = Math.Max(0, Math.Min(800, Position.X));
            float nearestY = Math.Max(0, Math.Min(600, Position.Y));
            
            return new Vector2(nearestX, nearestY);
        }

        private Vector2 CalculateSeparation(List<Boid> boids)
        {
            Vector2 steering = Vector2.Zero;
            int count = 0;

            foreach (var other in boids)
            {
                if (other == this) continue;

                float distance = Vector2.Distance(Position, other.Position);
                if (distance > 0 && distance < SeparationRadius)
                {
                    Vector2 diff = Position - other.Position;
                    diff.Normalize();
                    diff /= distance; // Weight by distance
                    steering += diff;
                    count++;
                }
            }

            if (count > 0)
            {
                steering /= count;
                steering.Normalize();
                steering *= MaxSpeed;
                steering -= Velocity;
                steering = Vector2.Clamp(steering, -Vector2.One * MaxForce, Vector2.One * MaxForce);
            }

            return steering;
        }

        private Vector2 CalculateAlignment(List<Boid> boids)
        {
            Vector2 averageVelocity = Vector2.Zero;
            int count = 0;

            foreach (var other in boids)
            {
                if (other == this) continue;

                float distance = Vector2.Distance(Position, other.Position);
                if (distance > 0 && distance < AlignmentRadius)
                {
                    averageVelocity += other.Velocity;
                    count++;
                }
            }

            if (count > 0)
            {
                averageVelocity /= count;
                averageVelocity.Normalize();
                averageVelocity *= MaxSpeed;
                Vector2 steering = averageVelocity - Velocity;
                steering = Vector2.Clamp(steering, -Vector2.One * MaxForce, Vector2.One * MaxForce);
                return steering;
            }

            return Vector2.Zero;
        }

        private Vector2 CalculateCohesion(List<Boid> boids)
        {
            Vector2 centerOfMass = Vector2.Zero;
            int count = 0;

            foreach (var other in boids)
            {
                if (other == this) continue;

                float distance = Vector2.Distance(Position, other.Position);
                if (distance > 0 && distance < CohesionRadius)
                {
                    centerOfMass += other.Position;
                    count++;
                }
            }

            if (count > 0)
            {
                centerOfMass /= count;
                Vector2 desiredVelocity = centerOfMass - Position;
                desiredVelocity.Normalize();
                desiredVelocity *= MaxSpeed;
                Vector2 steering = desiredVelocity - Velocity;
                steering = Vector2.Clamp(steering, -Vector2.One * MaxForce, Vector2.One * MaxForce);
                return steering;
            }

            return Vector2.Zero;
        }

        private Vector2 CalculatePlayerBehavior(Vector2 playerPosition, bool avoidanceMode = false)
        {
            float distanceToPlayer = Vector2.Distance(Position, playerPosition);

            if (avoidanceMode)
            {
                // Avoid player when in avoidance mode
                if (distanceToPlayer < AvoidPlayerRadius)
                {
                    Vector2 awayFromPlayer = Position - playerPosition;
                    awayFromPlayer.Normalize();
                    awayFromPlayer *= MaxSpeed;
                    Vector2 steering = awayFromPlayer - Velocity;
                    steering = Vector2.Clamp(steering, -Vector2.One * MaxForce, Vector2.One * MaxForce);
                    return steering;
                }
            }
            else
            {
                // Chase player when not in avoidance mode
                if (distanceToPlayer < ChasePlayerRadius)
                {
                    Vector2 toPlayer = playerPosition - Position;
                    toPlayer.Normalize();
                    toPlayer *= MaxSpeed;
                    Vector2 steering = toPlayer - Velocity;
                    steering = Vector2.Clamp(steering, -Vector2.One * MaxForce, Vector2.One * MaxForce);
                    return steering;
                }
            }

            return Vector2.Zero;
        }

        private Vector2 CalculateEdgeSwarming()
        {
            // Calculate distance to nearest edge
            float distanceToLeft = Position.X;
            float distanceToRight = 800 - Position.X;
            float distanceToTop = Position.Y;
            float distanceToBottom = 600 - Position.Y;

            float minDistance = Math.Min(Math.Min(distanceToLeft, distanceToRight), Math.Min(distanceToTop, distanceToBottom));

            // If close to edge, move towards center
            if (minDistance < EdgeSwarmRadius)
            {
                Vector2 center = new Vector2(400, 300);
                Vector2 toCenter = center - Position;
                toCenter.Normalize();
                toCenter *= MaxSpeed;
                Vector2 steering = toCenter - Velocity;
                steering = Vector2.Clamp(steering, -Vector2.One * MaxForce, Vector2.One * MaxForce);
                return steering;
            }

            return Vector2.Zero;
        }

        private void KeepInBounds()
        {
            // Keep ants within the 800x600 playable area
            Position.X = Math.Max(0, Math.Min(800, Position.X));
            Position.Y = Math.Max(0, Math.Min(600, Position.Y));
        }
    }

    public class Enemy
    {
        private Texture2D _texture;
        private List<Boid> _boids;
        private int _frameWidth;
        private int _frameHeight;
        private float _spawnTimer = 0f;
        private float _spawnInterval = 2f; // Base spawn interval
        private float _minSpawnInterval = 0.5f; // Minimum time between spawns
        private float _maxSpawnInterval = 4f; // Maximum time between spawns
        private int _maxBoids = 50; // Maximum number of ants on screen
        private int _minSpawnCount = 1; // Minimum ants to spawn at once
        private int _maxSpawnCount = 3; // Maximum ants to spawn at once

        // Avoidance mode variables
        private bool _avoidanceMode = false;
        private float _avoidanceTimer = 0f;
        private float _avoidanceDuration = 2f; // 2 seconds of avoidance

        public Enemy(Texture2D texture, int frameWidth, int frameHeight)
        {
            _texture = texture;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
            _boids = new List<Boid>();
        }

        private void SpawnNewBoid()
        {
            // Spawn ants from random edges
            Vector2 spawnPosition;
            int edge = Game1.Random.Next(4); // 0: top, 1: right, 2: bottom, 3: left

            switch (edge)
            {
                case 0: // Top
                    spawnPosition = new Vector2(Game1.Random.Next(0, 800), -32);
                    break;
                case 1: // Right
                    spawnPosition = new Vector2(832, Game1.Random.Next(0, 600));
                    break;
                case 2: // Bottom
                    spawnPosition = new Vector2(Game1.Random.Next(0, 800), 632);
                    break;
                default: // Left
                    spawnPosition = new Vector2(-32, Game1.Random.Next(0, 600));
                    break;
            }

            _boids.Add(new Boid(spawnPosition));
        }

        private void SpawnAnts()
        {
            if (_boids.Count >= _maxBoids) return;

            int spawnCount = Game1.Random.Next(_minSpawnCount, _maxSpawnCount + 1);
            for (int i = 0; i < spawnCount && _boids.Count < _maxBoids; i++)
            {
                SpawnNewBoid();
            }
        }

        public void Update(GameTime gameTime, Vector2 playerPosition, Func<Rectangle, bool> collisionCheck = null)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update avoidance mode
            if (_avoidanceMode)
            {
                _avoidanceTimer += deltaTime;
                if (_avoidanceTimer >= _avoidanceDuration)
                {
                    _avoidanceMode = false;
                    _avoidanceTimer = 0f;
                }
            }

            // Update spawn timer
            _spawnTimer += deltaTime;
            if (_spawnTimer >= _spawnInterval)
            {
                SpawnAnts();
                _spawnTimer = 0f;
                
                // Randomize next spawn interval
                _spawnInterval = Game1.Random.Next((int)(_minSpawnInterval * 100), (int)(_maxSpawnInterval * 100)) / 100f;
            }

            // Update all boids
            for (int i = _boids.Count - 1; i >= 0; i--)
            {
                _boids[i].Update(_boids, playerPosition, deltaTime, _avoidanceMode, collisionCheck);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2? offset = null)
        {
            foreach (var boid in _boids)
            {
                // Calculate source rectangle for ant sprite
                Rectangle sourceRect = new Rectangle(0, 0, _frameWidth, _frameHeight);

                Vector2 drawPosition = boid.Position - new Vector2(_frameWidth / 2f, _frameHeight / 2f);
                if (offset.HasValue)
                {
                    drawPosition += offset.Value;
                }

                spriteBatch.Draw(
                    _texture,
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
        }

        public List<Boid> Boids
        {
            get { return _boids; }
        }

        public void CheckBulletCollisions(List<Bullet> bullets)
        {
            for (int i = _boids.Count - 1; i >= 0; i--)
            {
                Rectangle antBounds = new Rectangle(
                    (int)(_boids[i].Position.X - 16),
                    (int)(_boids[i].Position.Y - 16),
                    32, 32
                );

                foreach (var bullet in bullets)
                {
                    if (bullet.IsActive && antBounds.Intersects(bullet.Bounds))
                    {
                        // Ant hit by bullet
                        bullet.Deactivate();
                        _boids.RemoveAt(i);
                        
                        // Trigger score event
                        OnAntDestroyed?.Invoke(10);
                        break;
                    }
                }
            }
        }

        public event Action<int> OnAntDestroyed;

        public void ActivateAvoidanceMode()
        {
            _avoidanceMode = true;
            _avoidanceTimer = 0f;
        }

        public void Reset()
        {
            _boids.Clear();
            _spawnTimer = 0f;
            _avoidanceMode = false;
            _avoidanceTimer = 0f;
        }
    }
} 