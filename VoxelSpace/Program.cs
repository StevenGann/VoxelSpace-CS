using Raylib_cs;
using System.Numerics;
using System.IO;

namespace VoxelSpace;

class Program
{
    // Map definitions
    private static readonly (string colorMap, string heightMap)[] Maps = new[]
    {
        ("C1W", "D1"),   // Map 1
        ("C2W", "D2"),   // Map 2
        ("C3", "D3"),    // Map 3
        ("C4", "D4"),    // Map 4
        ("C5W", "D5"),   // Map 5
        ("C6W", "D6"),   // Map 6
        ("C7W", "D7"),   // Map 7
        ("C10W", "D10"),    // Map 8
        ("C9W", "D9"),   // Map 9
    };

    private static (byte[] heightMap, Color[] colorMap, int heightMapWidth, int heightMapHeight, int colorMapWidth, int colorMapHeight) LoadMaps(string colorMapName, string heightMapName)
    {
        // Try different possible paths for the maps directory
        string[] possiblePaths = new[]
        {
            "maps",
            "../maps",
            "../../maps",
            "VoxelSpace/bin/Debug/net9.0/maps"
        };

        string colorMapPath = string.Empty;
        string heightMapPath = string.Empty;

        // Find the first path that contains both maps
        foreach (string path in possiblePaths)
        {
            string testColorPath = Path.Combine(path, $"{colorMapName}.png");
            string testHeightPath = Path.Combine(path, $"{heightMapName}.png");
            
            if (File.Exists(testColorPath) && File.Exists(testHeightPath))
            {
                colorMapPath = testColorPath;
                heightMapPath = testHeightPath;
                break;
            }
        }

        if (colorMapPath == null || heightMapPath == null)
        {
            throw new FileNotFoundException("Could not find map files in any of the expected locations");
        }

        // Load the height map
        Image heightMapImage = Raylib.LoadImage(heightMapPath);
        if (heightMapImage.Width == 0 || heightMapImage.Height == 0)
        {
            throw new Exception($"Failed to load height map: {heightMapName}");
        }

        // Load the color map
        Image colorMapImage = Raylib.LoadImage(colorMapPath);
        if (colorMapImage.Width == 0 || colorMapImage.Height == 0)
        {
            Raylib.UnloadImage(heightMapImage);
            throw new Exception($"Failed to load color map: {colorMapName}");
        }

        try
        {
            // Convert height map to grayscale values
            byte[] heightMap = new byte[heightMapImage.Width * heightMapImage.Height];
            Color[] heightMapColors = new Color[heightMapImage.Width * heightMapImage.Height];
            unsafe
            {
                Color* ptr = Raylib.LoadImageColors(heightMapImage);
                for (int i = 0; i < heightMapColors.Length; i++)
                {
                    heightMapColors[i] = ptr[i];
                }
                Raylib.UnloadImageColors(ptr);
            }
            for (int i = 0; i < heightMapColors.Length; i++)
            {
                // Convert RGB to grayscale using standard luminance formula
                heightMap[i] = (byte)((heightMapColors[i].R * 0.299f + heightMapColors[i].G * 0.587f + heightMapColors[i].B * 0.114f));
            }

            // Get color map data
            Color[] colorMap = new Color[colorMapImage.Width * colorMapImage.Height];
            unsafe
            {
                Color* ptr = Raylib.LoadImageColors(colorMapImage);
                for (int i = 0; i < colorMap.Length; i++)
                {
                    colorMap[i] = ptr[i];
                }
                Raylib.UnloadImageColors(ptr);
            }

            return (heightMap, colorMap, heightMapImage.Width, heightMapImage.Height, colorMapImage.Width, colorMapImage.Height);
        }
        finally
        {
            // Clean up
            Raylib.UnloadImage(heightMapImage);
            Raylib.UnloadImage(colorMapImage);
        }
    }

    private static bool TryLoadMap(VoxelSpaceEngine engine, int mapIndex)
    {
        if (mapIndex < 0 || mapIndex >= Maps.Length)
        {
            return false;
        }

        try
        {
            var (heightMap, colorMap, heightMapWidth, heightMapHeight, colorMapWidth, colorMapHeight) = LoadMaps(Maps[mapIndex].colorMap, Maps[mapIndex].heightMap);
            engine.SetHeightMap(heightMap, heightMapWidth, heightMapHeight);
            engine.SetColorMap(colorMap, colorMapWidth, colorMapHeight);
            Console.WriteLine($"Switched to map {mapIndex + 1}: {Maps[mapIndex].colorMap}/{Maps[mapIndex].heightMap}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading map {mapIndex + 1}: {ex.Message}");
            return false;
        }
    }

    private static void Main(string[] args)
    {
        // Initialize window
        Raylib.InitWindow(800, 600, "VoxelSpace");
        Raylib.SetTargetFPS(60);

        // Create engine
        var engine = new VoxelSpaceEngine();

        // Load maps
        var (heightMap, colorMap, heightMapWidth, heightMapHeight, colorMapWidth, colorMapHeight) = LoadMaps(Maps[0].colorMap, Maps[0].heightMap);
        engine.SetHeightMap(heightMap, heightMapWidth, heightMapHeight);
        engine.SetColorMap(colorMap, colorMapWidth, colorMapHeight);

        // Initialize player state
        float playerX = heightMapWidth / 2;
        float playerY = heightMapHeight / 2;
        float playerHeight = 100;
        float playerAngle = 0;
        float horizon = 300;

        // Movement constants
        const float MOVE_SPEED = 100.0f;
        const float ROTATE_SPEED = 2.0f;

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            // Handle rotation
            if (Raylib.IsKeyDown(KeyboardKey.Left))
            {
                playerAngle += ROTATE_SPEED * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.Right))
            {
                playerAngle -= ROTATE_SPEED * deltaTime;
            }

            // Handle movement
            float moveX = 0;
            float moveY = 0;

            if (Raylib.IsKeyDown(KeyboardKey.W))
            {
                moveX = -(float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
                moveY = -(float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.S))
            {
                moveX = (float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
                moveY = (float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.A))
            {
                moveX = -(float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
                moveY = (float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.D))
            {
                moveX = (float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
                moveY = -(float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
            }

            // Update position with collision detection
            float newX = playerX + moveX;
            float newY = playerY + moveY;

            // Get height at current and new position
            int currentHeight = engine.GetHeightAt((int)playerX, (int)playerY);
            int newHeight = engine.GetHeightAt((int)newX, (int)newY);

            // Only move if height difference is not too steep
            if (Math.Abs(newHeight - currentHeight) < 20)
            {
                playerX = newX;
                playerY = newY;
            }

            // Handle height adjustment
            if (Raylib.IsKeyDown(KeyboardKey.Q))
            {
                playerHeight -= MOVE_SPEED * 2 * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.E))
            {
                playerHeight += MOVE_SPEED * 2 * deltaTime;
            }

            // Handle horizon adjustment
            if (Raylib.IsKeyDown(KeyboardKey.R))
            {
                horizon += 2 * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.F))
            {
                horizon -= 2 * deltaTime;
            }

            // Clamp values
            playerHeight = Math.Max(0, Math.Min(255, playerHeight));
            horizon = Math.Max(0, Math.Min(600, horizon));

            // Render frame
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 255));

            engine.Render(playerX, playerY, playerHeight, playerAngle);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
} 