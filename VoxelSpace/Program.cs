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
        ("C10W", "D10"), // Map 8
        ("C9W", "D9"),   // Map 9
    };

    // Movement constants
    private const float MOVE_SPEED = 100.0f;
    private const float ROTATE_SPEED = 5.0f;
    private const float HEIGHT_ADJUST_SPEED = 100.0f;
    private const float HORIZON_ADJUST_SPEED = 20.0f;
    private const float MAX_HEIGHT = 255.0f;
    private const float MAX_HORIZON = 600.0f;
    private const float MAX_HEIGHT_DIFF = 20.0f;

    private static (byte[] heightMap, Color[] colorMap, int heightMapWidth, int heightMapHeight, int colorMapWidth, int colorMapHeight) LoadMaps(string colorMapName, string heightMapName)
    {
        // Try different possible paths for the maps directory
        string[] possiblePaths = { "maps", "../maps", "../../maps", "VoxelSpace/bin/Debug/net9.0/maps" };
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

        if (string.IsNullOrEmpty(colorMapPath) || string.IsNullOrEmpty(heightMapPath))
        {
            throw new FileNotFoundException("Could not find map files in any of the expected locations");
        }

        // Load the maps
        Image heightMapImage = Raylib.LoadImage(heightMapPath);
        Image colorMapImage = Raylib.LoadImage(colorMapPath);

        if (heightMapImage.Width == 0 || heightMapImage.Height == 0 || colorMapImage.Width == 0 || colorMapImage.Height == 0)
        {
            Raylib.UnloadImage(heightMapImage);
            Raylib.UnloadImage(colorMapImage);
            throw new Exception("Failed to load map images");
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

            // Convert RGB to grayscale using standard luminance formula
            for (int i = 0; i < heightMapColors.Length; i++)
            {
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

        // Create engine and load initial map
        var engine = new VoxelSpaceEngine();
        var (heightMap, colorMap, heightMapWidth, heightMapHeight, colorMapWidth, colorMapHeight) = LoadMaps(Maps[0].colorMap, Maps[0].heightMap);
        engine.SetHeightMap(heightMap, heightMapWidth, heightMapHeight);
        engine.SetColorMap(colorMap, colorMapWidth, colorMapHeight);

        // Initialize player state
        float playerX = heightMapWidth / 2;
        float playerY = heightMapHeight / 2;
        float playerHeight = 100;
        float playerAngle = 0;
        float horizon = 300;

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            // Handle rotation
            if (Raylib.IsKeyDown(KeyboardKey.Left)) playerAngle += ROTATE_SPEED * deltaTime;
            if (Raylib.IsKeyDown(KeyboardKey.Right)) playerAngle -= ROTATE_SPEED * deltaTime;

            // Handle movement
            float moveX = 0;
            float moveY = 0;

            // Forward/backward movement
            if (Raylib.IsKeyDown(KeyboardKey.W))
            {
                moveX -= (float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
                moveY -= (float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.S))
            {
                moveX += (float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
                moveY += (float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
            }

            // Strafe movement
            if (Raylib.IsKeyDown(KeyboardKey.A))
            {
                moveX -= (float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
                moveY += (float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
            }
            if (Raylib.IsKeyDown(KeyboardKey.D))
            {
                moveX += (float)Math.Cos(playerAngle) * MOVE_SPEED * deltaTime;
                moveY -= (float)Math.Sin(playerAngle) * MOVE_SPEED * deltaTime;
            }

            // Normalize diagonal movement
            if (moveX != 0 && moveY != 0)
            {
                float length = (float)Math.Sqrt(moveX * moveX + moveY * moveY);
                moveX = moveX / length * MOVE_SPEED * deltaTime;
                moveY = moveY / length * MOVE_SPEED * deltaTime;
            }

            // Update position with collision detection
            float newX = playerX + moveX;
            float newY = playerY + moveY;

            // Get height at current and new position
            int currentHeight = engine.GetHeightAt((int)playerX, (int)playerY);
            int newHeight = engine.GetHeightAt((int)newX, (int)newY);

            // Check if the height difference is too steep or if we would go below the terrain
            if (Math.Abs(newHeight - currentHeight) < MAX_HEIGHT_DIFF && newHeight <= playerHeight + 10)
            {
                playerX = newX;
                playerY = newY;
            }
            else
            {
                // If we can't move in the current direction, try moving only in X or Y
                float testX = playerX + moveX;
                float testY = playerY;
                int testHeightX = engine.GetHeightAt((int)testX, (int)testY);
                
                if (Math.Abs(testHeightX - currentHeight) < MAX_HEIGHT_DIFF && testHeightX <= playerHeight + 10)
                {
                    playerX = testX;
                }

                testX = playerX;
                testY = playerY + moveY;
                int testHeightY = engine.GetHeightAt((int)testX, (int)testY);
                
                if (Math.Abs(testHeightY - currentHeight) < MAX_HEIGHT_DIFF && testHeightY <= playerHeight + 10)
                {
                    playerY = testY;
                }
            }

            // Handle height and horizon adjustment
            if (Raylib.IsKeyDown(KeyboardKey.Q)) playerHeight -= HEIGHT_ADJUST_SPEED * deltaTime;
            if (Raylib.IsKeyDown(KeyboardKey.E)) playerHeight += HEIGHT_ADJUST_SPEED * deltaTime;
            if (Raylib.IsKeyDown(KeyboardKey.R)) horizon += HORIZON_ADJUST_SPEED * deltaTime;
            if (Raylib.IsKeyDown(KeyboardKey.F)) horizon -= HORIZON_ADJUST_SPEED * deltaTime;

            // Clamp values
            playerHeight = Math.Max(0, Math.Min(MAX_HEIGHT, playerHeight));
            horizon = Math.Max(0, Math.Min(MAX_HORIZON, horizon));

            // Render frame
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 255));
            engine.Render(playerX, playerY, playerHeight, playerAngle);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
} 