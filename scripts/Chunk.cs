using Godot;

public partial class Chunk : Node3D
{
    public int[,,] blocks;
    private Generator _generator;
    private int _chunkX, _chunkY, _chunkZ;

    public void Initialize(Generator g, int cx, int cy, int cz)
    {
        _generator = g;
        _chunkX = cx;
        _chunkY = cy;
        _chunkZ = cz;
        blocks = new int[g.ChunkSize, g.ChunkSize, g.ChunkSize];
    }

    public void GenerateData()
    {
        for (int x = 0; x < _generator.ChunkSize; x++)
        {
            for (int z = 0; z < _generator.ChunkSize; z++)
            {
                int wx = _chunkX * _generator.ChunkSize + x;
                int wz = _chunkZ * _generator.ChunkSize + z;
                double n = _generator.noise.Evaluate(wx * _generator.NoiseScale, wz * _generator.NoiseScale);
                n = (n + 1.0) * 0.5;
                int maxY = Mathf.FloorToInt((float)(n * (_generator.ChunkSize * _generator.WorldHeightInChunks - 1)));
                for (int y = 0; y < _generator.ChunkSize; y++)
                {
                    int wy = _chunkY * _generator.ChunkSize + y;
                    if (wy <= maxY) blocks[x, y, z] = 1;
                }
            }
        }
    }

    public void RebuildMesh()
    {
        foreach (Node c in GetChildren())
        {
            RemoveChild(c);
            c.QueueFree();
        }
        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int x = 0; x < _generator.ChunkSize; x++)
        {
            for (int y = 0; y < _generator.ChunkSize; y++)
            {
                for (int z = 0; z < _generator.ChunkSize; z++)
                {
                    if (blocks[x, y, z] == 1) AddVisibleFaces(st, x, y, z);
                }
            }
        }
        st.Index();
        Mesh mesh = st.Commit();
        MeshInstance3D mi = new MeshInstance3D();
        mi.Mesh = mesh;
        mi.MaterialOverride = new StandardMaterial3D
        {
            AlbedoTexture = _generator.BlockTexture,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        AddChild(mi);
        StaticBody3D body = new StaticBody3D();
        CollisionShape3D shape = new CollisionShape3D();
        shape.Shape = mesh.CreateTrimeshShape();
        body.AddChild(shape);
        AddChild(body);
    }

    private void AddVisibleFaces(SurfaceTool st, int lx, int ly, int lz)
    {
        int wx = _chunkX * _generator.ChunkSize + lx;
        int wy = _chunkY * _generator.ChunkSize + ly;
        int wz = _chunkZ * _generator.ChunkSize + lz;
        Vector3 o = new Vector3(lx, ly, lz);
        if (_generator.GetBlock(wx, wy, wz - 1) == 0) AddQuad(st, o + new Vector3(0, 0, 0), o + new Vector3(1, 0, 0), o + new Vector3(1, 1, 0), o + new Vector3(0, 1, 0));
        if (_generator.GetBlock(wx, wy, wz + 1) == 0) AddQuad(st, o + new Vector3(0, 0, 1), o + new Vector3(0, 1, 1), o + new Vector3(1, 1, 1), o + new Vector3(1, 0, 1));
        if (_generator.GetBlock(wx - 1, wy, wz) == 0) AddQuad(st, o + new Vector3(0, 0, 1), o + new Vector3(0, 0, 0), o + new Vector3(0, 1, 0), o + new Vector3(0, 1, 1));
        if (_generator.GetBlock(wx + 1, wy, wz) == 0) AddQuad(st, o + new Vector3(1, 0, 0), o + new Vector3(1, 0, 1), o + new Vector3(1, 1, 1), o + new Vector3(1, 1, 0));
        if (_generator.GetBlock(wx, wy + 1, wz) == 0) AddQuad(st, o + new Vector3(0, 1, 0), o + new Vector3(1, 1, 0), o + new Vector3(1, 1, 1), o + new Vector3(0, 1, 1));
        if (_generator.GetBlock(wx, wy - 1, wz) == 0) AddQuad(st, o + new Vector3(0, 0, 0), o + new Vector3(0, 0, 1), o + new Vector3(1, 0, 1), o + new Vector3(1, 0, 0));
    }

    private void AddQuad(SurfaceTool st, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        st.SetUV(new Vector2(0, 0));
        st.AddVertex(v1);
        st.SetUV(new Vector2(1, 0));
        st.AddVertex(v2);
        st.SetUV(new Vector2(1, 1));
        st.AddVertex(v3);
        st.SetUV(new Vector2(0, 0));
        st.AddVertex(v1);
        st.SetUV(new Vector2(1, 1));
        st.AddVertex(v3);
        st.SetUV(new Vector2(0, 1));
        st.AddVertex(v4);
    }
}
