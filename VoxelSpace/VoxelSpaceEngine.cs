using Raylib_cs;
using System.Numerics;
using System.IO;

namespace VoxelSpace;

/// <summary>
/// Implements a 2.5D voxel space rendering engine based on the technique used in the 1992 game Comanche.
/// This engine uses height maps and color maps to render terrain with a pseudo-3D effect.
/// </summary>
public class VoxelSpaceEngine
{
    private const float HEIGHT_SCALE = 0.1f;
    private const float DISTANCE_SCALE = 0.1f;
    private const int MAX_DISTANCE = 1000;

    private byte[] heightMap;
    private Color[] colorMap;
    private int heightMapWidth;
    private int heightMapHeight;
    private int colorMapWidth;
    private int colorMapHeight;
    private float horizon;

    /// <summary>
    /// Initializes a new instance of the VoxelSpaceEngine class.
    /// </summary>
    public VoxelSpaceEngine()
    {
        // Initialize with test data
        heightMapWidth = 512;
        heightMapHeight = 512;
        colorMapWidth = 1024;
        colorMapHeight = 1024;

        // Create test height map (simple gradient)
        heightMap = new byte[heightMapWidth * heightMapHeight];
        for (int y = 0; y < heightMapHeight; y++)
        {
            for (int x = 0; x < heightMapWidth; x++)
            {
                heightMap[y * heightMapWidth + x] = (byte)((x + y) / 4);
            }
        }

        // Create test color map (simple gradient)
        colorMap = new Color[colorMapWidth * colorMapHeight];
        for (int y = 0; y < colorMapHeight; y++)
        {
            for (int x = 0; x < colorMapWidth; x++)
            {
                colorMap[y * colorMapWidth + x] = new Color(
                    (int)(x / 4),
                    (int)(y / 4),
                    (int)((x + y) / 8),
                    255
                );
            }
        }

        // Initialize view parameters with default horizon
        horizon = 300;
    }

    /// <summary>
    /// Sets the height map data from a byte array.
    /// </summary>
    /// <param name="data">The height map data. Must be mapWidth * mapHeight in size.</param>
    /// <exception cref="ArgumentException">Thrown when data size doesn't match map dimensions.</exception>
    public void SetHeightMap(byte[] data, int width, int height)
    {
        if (data == null || data.Length != width * height)
        {
            throw new ArgumentException("Invalid height map data or dimensions");
        }

        heightMap = data;
        heightMapWidth = width;
        heightMapHeight = height;
    }

    /// <summary>
    /// Sets the color map data from a Color array.
    /// </summary>
    /// <param name="data">The color map data. Must be mapWidth * mapHeight in size.</param>
    /// <exception cref="ArgumentException">Thrown when data size doesn't match map dimensions.</exception>
    public void SetColorMap(Color[] data, int width, int height)
    {
        if (data == null || data.Length != width * height)
        {
            throw new ArgumentException("Invalid color map data or dimensions");
        }

        colorMap = data;
        colorMapWidth = width;
        colorMapHeight = height;
    }

    public int GetHeightAt(int x, int y)
    {
        // Wrap coordinates to stay within map bounds
        x = ((x % heightMapWidth) + heightMapWidth) % heightMapWidth;
        y = ((y % heightMapHeight) + heightMapHeight) % heightMapHeight;
        return heightMap[y * heightMapWidth + x];
    }

    public Color GetColorAt(int x, int y)
    {
        // Wrap coordinates to stay within map bounds
        x = ((x % colorMapWidth) + colorMapWidth) % colorMapWidth;
        y = ((y % colorMapHeight) + colorMapHeight) % colorMapHeight;
        return colorMap[y * colorMapWidth + x];
    }

    /// <summary>
    /// Renders the terrain using the voxel space technique.
    /// </summary>
    /// <remarks>
    /// The rendering process:
    /// 1. Uses front-to-back rendering with occlusion culling
    /// 2. Calculates perspective projection for each vertical line
    /// 3. Uses adaptive step size for level of detail
    /// 4. Implements the painter's algorithm for proper occlusion
    /// </remarks>
    public void Render(float playerX, float playerY, float playerHeight, float playerAngle, int screenWidth, int screenHeight)
    {
        // Draw sky
        Raylib.DrawRectangle(0, 0, screenWidth, (int)horizon, new Color(135, 206, 235, 255));

        // Draw ground
        Raylib.DrawRectangle(0, (int)horizon, screenWidth, screenHeight - (int)horizon, new Color(34, 139, 34, 255));

        // Calculate view frustum
        float sinAng = (float)Math.Sin(playerAngle);
        float cosAng = (float)Math.Cos(playerAngle);

        // Track the highest point drawn for each column (for occlusion)
        int[] hiddenY = new int[screenWidth];
        for (int i = 0; i < screenWidth; i++)
        {
            hiddenY[i] = screenHeight;
        }

        // Draw from front to back
        float deltaZ = 1.0f;
        for (float z = 1; z < MAX_DISTANCE; z += deltaZ)
        {
            // Calculate the left and right points of the view frustum at this distance
            float plx = -cosAng * z - sinAng * z;
            float ply = sinAng * z - cosAng * z;
            float prx = cosAng * z - sinAng * z;
            float pry = -sinAng * z - cosAng * z;

            // Calculate the step size for interpolating between left and right points
            float dx = (prx - plx) / screenWidth;
            float dy = (pry - ply) / screenWidth;

            // Add camera position
            plx += playerX;
            ply += playerY;

            // Calculate perspective scale factor
            float invZ = 1.0f / z * 240.0f;

            // Draw each vertical line
            for (int i = 0; i < screenWidth; i++)
            {
                // Get height at current position
                int mapHeight = GetHeightAt((int)plx, (int)ply);

                // Calculate height on screen with perspective
                int heightOnScreen = (int)((playerHeight - mapHeight) * invZ + horizon);

                // Draw vertical line if it's visible
                if (heightOnScreen < hiddenY[i])
                {
                    // Get color from color map
                    Color color = GetColorAt((int)plx, (int)ply);

                    // Draw the line
                    Raylib.DrawLine(i, heightOnScreen, i, hiddenY[i], color);

                    // Update highest point drawn
                    hiddenY[i] = heightOnScreen;
                }

                // Move to next point
                plx += dx;
                ply += dy;
            }

            // Increase step size with distance for adaptive level of detail
            deltaZ += 0.005f;
        }
    }
} 