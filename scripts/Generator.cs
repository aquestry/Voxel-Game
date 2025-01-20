using Godot;
using Noise;

public partial class Generator : Node
{
    private OpenSimplexNoise noise;
    private int[,,] grid;
    private double cubeSize;

    [Export]
    public int Resolution { get; set; } = 1;

    [Export]
    public CompressedTexture2D BlockTexture { get; set; }

    public override void _Ready()
    {
        Regenerate();
    }

    private void Regenerate()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        int baseWidth = 256;
        int baseDepth = 256;
        int baseHeight = 12;
        int width = baseWidth * Resolution;
        int depth = baseDepth * Resolution;
        int height = baseHeight * Resolution;
        cubeSize = 1.0 / Resolution;
        noise = new OpenSimplexNoise();
        grid = new int[width, height, depth];
        GenerateTerrain(width, depth, height, 0.1 / Resolution);
        CreateMesh(width, depth, height);
    }

    private void GenerateTerrain(int width, int depth, int height, double scale)
    {
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int terrainHeight = Mathf.Clamp((int)((noise.Evaluate(x * scale, z * scale) + 1) / 2 * height), 1, height);
                for (int y = 0; y <= terrainHeight; y++)
                {
                    grid[x, y, z] = 1;
                }
            }
        }
    }

    private void CreateMesh(int width, int depth, int height)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z] == 1)
                    {
                        AddVisibleFaces(surfaceTool, x, y, z, width, height, depth, cubeSize);
                    }
                }
            }
        }
        surfaceTool.Index();
        var material = new StandardMaterial3D
        {
            AlbedoTexture = BlockTexture,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        var meshInstance = new MeshInstance3D
        {
            Mesh = surfaceTool.Commit(),
            MaterialOverride = material
        };
        AddChild(meshInstance);
    }

    private void AddVisibleFaces(SurfaceTool surfaceTool, int x, int y, int z, int width, int height, int depth, double cubeSize)
    {
        Vector3 offset = new Vector3(x, y, z) * (float)cubeSize;

        if (z == 0 || (z > 0 && grid[x, y, z - 1] == 0))
        {
            AddQuad(surfaceTool,
                offset + new Vector3(0, 0, 0) * (float)cubeSize,
                offset + new Vector3(1, 0, 0) * (float)cubeSize,
                offset + new Vector3(1, 1, 0) * (float)cubeSize,
                offset + new Vector3(0, 1, 0) * (float)cubeSize);
        }

        if (z == depth - 1 || (z < depth - 1 && grid[x, y, z + 1] == 0))
        {
            AddQuad(surfaceTool,
                offset + new Vector3(0, 0, 1) * (float)cubeSize,
                offset + new Vector3(0, 1, 1) * (float)cubeSize,
                offset + new Vector3(1, 1, 1) * (float)cubeSize,
                offset + new Vector3(1, 0, 1) * (float)cubeSize);
        }

        if (x == 0 || (x > 0 && grid[x - 1, y, z] == 0))
        {
            AddQuad(surfaceTool,
                offset + new Vector3(0, 0, 1) * (float)cubeSize,
                offset + new Vector3(0, 0, 0) * (float)cubeSize,
                offset + new Vector3(0, 1, 0) * (float)cubeSize,
                offset + new Vector3(0, 1, 1) * (float)cubeSize);
        }

        if (x == width - 1 || (x < width - 1 && grid[x + 1, y, z] == 0))
        {
            AddQuad(surfaceTool,
                offset + new Vector3(1, 0, 0) * (float)cubeSize,
                offset + new Vector3(1, 0, 1) * (float)cubeSize,
                offset + new Vector3(1, 1, 1) * (float)cubeSize,
                offset + new Vector3(1, 1, 0) * (float)cubeSize);
        }

        if (y == height - 1 || (y < height - 1 && grid[x, y + 1, z] == 0))
        {
            AddQuad(surfaceTool,
                offset + new Vector3(0, 1, 0) * (float)cubeSize,
                offset + new Vector3(1, 1, 0) * (float)cubeSize,
                offset + new Vector3(1, 1, 1) * (float)cubeSize,
                offset + new Vector3(0, 1, 1) * (float)cubeSize);
        }

        if (y == 0 || (y > 0 && grid[x, y - 1, z] == 0))
        {
            AddQuad(surfaceTool,
                offset + new Vector3(0, 0, 0) * (float)cubeSize,
                offset + new Vector3(0, 0, 1) * (float)cubeSize,
                offset + new Vector3(1, 0, 1) * (float)cubeSize,
                offset + new Vector3(1, 0, 0) * (float)cubeSize);
        }
    }

    private void AddQuad(SurfaceTool surfaceTool, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        surfaceTool.SetUV(new Vector2(0, 0));
        surfaceTool.AddVertex(v1);
        surfaceTool.SetUV(new Vector2(1, 0));
        surfaceTool.AddVertex(v2);
        surfaceTool.SetUV(new Vector2(1, 1));
        surfaceTool.AddVertex(v3);
        surfaceTool.SetUV(new Vector2(0, 0));
        surfaceTool.AddVertex(v1);
        surfaceTool.SetUV(new Vector2(1, 1));
        surfaceTool.AddVertex(v3);
        surfaceTool.SetUV(new Vector2(0, 1));
        surfaceTool.AddVertex(v4);
    }

    private bool IsValidPosition(int x, int y, int z)
    {
        return x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1) && z >= 0 && z < grid.GetLength(2);
    }

    public bool IsBlockAt(Vector3 position)
    {
        Vector3 gridPosition = position * Resolution;
        int x = Mathf.FloorToInt(gridPosition.X);
        int y = Mathf.FloorToInt(gridPosition.Y);
        int z = Mathf.FloorToInt(gridPosition.Z);
        if (x >= 0 && x < grid.GetLength(0) &&
            y >= 0 && y < grid.GetLength(1) &&
            z >= 0 && z < grid.GetLength(2))
        {
            return grid[x, y, z] == 1;
        }
        return false;
    }
}
