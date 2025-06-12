# Exterminator - Cave Defense Game

A 2D top-down shooter game built with MonoGame where you defend a cave from invading ants. Use your weapon to exterminate the ant hordes before they reach you!

## 🎮 Game Features

- **Top-down shooter gameplay** - Navigate through a cave environment while defending against ant invasions
- **Dynamic enemy AI** - Ants use flocking behavior to move intelligently around obstacles
- **Progressive difficulty** - Each level increases the number of ants you need to eliminate
- **Collision detection** - Realistic physics with rocks, cave walls, and enemy interactions
- **Fullscreen support** - Optimized for fullscreen gameplay
- **Splash screen** - Professional game introduction
- **Health system** - Three lives with visual heart indicators
- **Scoring system** - Track your progress and compete for high scores

## 🎯 How to Play

### Controls
- **WASD** - Move your character
- **Arrow Keys** - Aim and shoot (auto-fire when held)
- **Space/Enter** - Start game from splash screen
- **N** - Start new game (when game over or level complete)
- **Escape** - Exit game

### Objective
- Eliminate all ants in each level to progress
- Avoid being touched by ants - each contact costs you a life
- Survive as long as possible and achieve the highest score

### Game Mechanics
- **Movement**: Navigate through the cave using WASD keys
- **Combat**: Use arrow keys to aim and automatically fire bullets
- **Enemy Behavior**: Ants will flock together and try to reach your position
- **Obstacles**: Rocks and cave walls block movement and provide tactical cover
- **Level Progression**: Each level requires more ants to be eliminated

## 🛠️ Technical Details

### Built With
- **MonoGame** - Cross-platform game development framework
- **C#** - Programming language
- **Visual Studio** - Development environment

### Project Structure
```
Exterminator/
├── Game1.cs              # Main game class
├── Player.cs             # Player character logic
├── Enemy.cs              # Enemy AI and flocking behavior
├── Bullet.cs             # Projectile system
├── Rock.cs               # Obstacle objects
├── ColorCollisionMap.cs  # Collision detection system
├── Content/              # Game assets (textures, fonts, etc.)
├── Program.cs            # Entry point
└── Exterminator.csproj   # Project file
```

### Key Features
- **Flocking AI**: Enemies use boid algorithm for realistic group movement
- **Collision System**: Color-based collision detection for complex shapes
- **Sprite Management**: Efficient sprite sheet usage for animations
- **State Management**: Proper game state handling (splash, playing, game over)
- **Logging System**: Comprehensive logging for debugging

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2019 or later
- .NET 6.0 or later
- MonoGame templates installed

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/exterminator.git
   cd exterminator
   ```

2. Open the solution in Visual Studio:
   ```bash
   Exterminator.sln
   ```

3. Build and run the project:
   - Press `F5` to run in debug mode
   - Press `Ctrl+F5` to run without debugging

### Building from Command Line
```bash
dotnet build
dotnet run
```

## 🎨 Game Assets

The game includes various visual assets:
- **Character sprites** - Player and enemy animations
  (Bug sprites credited to Admurin: https://admurin.itch.io/,
  Rocks by dustdfg: https://dustdfg.itch.io/,
  hearts by gamedevshlok: https://gamedevshlok.itch.io/,
  player by Retro Sprite Creator using art from Famitsu & EnterBrain)
  
- **Environment textures** - Cave floor, walls
- **UI elements** - Hearts for health display, fonts for text
- **Splash screen** - Professional game introduction

## 📝 License

This project is open source and available under the [MIT License](LICENSE).

## 📞 Support

If you encounter any issues or have questions:
1. Check the existing issues
2. Create a new issue with detailed information
3. Include system information and steps to reproduce

---

**Enjoy playing Exterminator!** 🐜💥 