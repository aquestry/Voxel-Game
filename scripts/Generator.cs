using Godot;
using Noise;

public partial class Generator : Node
{
    private OpenSimplexNoise noise;
    private int[,,] grid;

    [Export]
    public int Resolution { get; set; } = 1;
    
    [Export]
    public CompressedTexture2D BlockTexture { get; set; }

    public override void _Ready()
    {
        int baseWidth = 128, baseDepth = 128, baseHeight = 20, seed = 42;
        noise = new OpenSimplexNoise(seed);

        int width = baseWidth / Resolution;
        int depth = baseDepth / Resolution;
        int height = baseHeight / Resolution;

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
                        AddVisibleFaces(surfaceTool, x, y, z, width, height, depth);
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

    private void AddVisibleFaces(SurfaceTool surfaceTool, int x, int y, int z, int width, int height, int depth)
    {
        if (z == 0 || (z > 0 && grid[x, y, z-1] == 0))
        {
            AddQuad(surfaceTool, 
                new Vector3(x, y, z),
                new Vector3(x + 1, y, z),
                new Vector3(x + 1, y + 1, z),
                new Vector3(x, y + 1, z));
        }

        if (z == depth-1 || (z < depth-1 && grid[x, y, z+1] == 0))
        {
            AddQuad(surfaceTool,
                new Vector3(x, y, z + 1),
                new Vector3(x, y + 1, z + 1),
                new Vector3(x + 1, y + 1, z + 1),
                new Vector3(x + 1, y, z + 1));
        }

        if (x == 0 || (x > 0 && grid[x-1, y, z] == 0))
        {
            AddQuad(surfaceTool,
                new Vector3(x, y, z + 1),
                new Vector3(x, y, z),
                new Vector3(x, y + 1, z),
                new Vector3(x, y + 1, z + 1));
        }

        if (x == width-1 || (x < width-1 && grid[x+1, y, z] == 0))
        {
            AddQuad(surfaceTool,
                new Vector3(x + 1, y, z),
                new Vector3(x + 1, y, z + 1),
                new Vector3(x + 1, y + 1, z + 1),
                new Vector3(x + 1, y + 1, z));
        }

        if (y == height-1 || (y < height-1 && grid[x, y+1, z] == 0))
        {
            AddQuad(surfaceTool,
                new Vector3(x, y + 1, z),
                new Vector3(x + 1, y + 1, z),
                new Vector3(x + 1, y + 1, z + 1),
                new Vector3(x, y + 1, z + 1));
        }

        if (y == 0 || (y > 0 && grid[x, y-1, z] == 0))
        {
            AddQuad(surfaceTool,
                new Vector3(x, y, z),
                new Vector3(x, y, z + 1),
                new Vector3(x + 1, y, z + 1),
                new Vector3(x + 1, y, z));
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
}
