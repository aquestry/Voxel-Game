using Godot;

public partial class Chunk : Node3D
{

    public int[,,] blocks;
    private Generator _generator;
    private int _chunkX, _chunkY, _chunkZ;

    public void Initialize(Generator generator, int cx, int cy, int cz)
    {
        _generator = generator;
        _chunkX = cx;
        _chunkY = cy;
        _chunkZ = cz;
        blocks = new int[_generator.ChunkSize, _generator.ChunkSize, _generator.ChunkSize];
    }

    public void GenerateData()
    {
        for (int localX = 0; localX < _generator.ChunkSize; localX++)
        {
            for (int localZ = 0; localZ < _generator.ChunkSize; localZ++)
            {
                int worldX = _chunkX * _generator.ChunkSize + localX;
                int worldZ = _chunkZ * _generator.ChunkSize + localZ;
                double noiseVal = _generator.noise.Evaluate(
                    worldX * _generator.NoiseScale,
                    worldZ * _generator.NoiseScale
                );
                noiseVal = (noiseVal + 1.0) * 0.5;
                int maxY = Mathf.FloorToInt(
                    (float)(noiseVal * (_generator.ChunkSize * _generator.WorldHeightInChunks - 1))
                );
                for (int localY = 0; localY < _generator.ChunkSize; localY++)
                {
                    int worldY = _chunkY * _generator.ChunkSize + localY;
                    if (worldY <= maxY)
                    {
                        blocks[localX, localY, localZ] = 1;
                    }
                }
            }
        }
    }

    public void RebuildMesh()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int x = 0; x < _generator.ChunkSize; x++)
        {
            for (int y = 0; y < _generator.ChunkSize; y++)
            {
                for (int z = 0; z < _generator.ChunkSize; z++)
                {
                    if (blocks[x, y, z] == 1)
                    {
                        AddVisibleFaces(st, x, y, z);
                    }
                }
            }
        }
        st.Index();
        var mat = new StandardMaterial3D
        {
            AlbedoTexture = _generator.BlockTexture,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        var meshInstance = new MeshInstance3D
        {
            Mesh = st.Commit(),
            MaterialOverride = mat
        };
        AddChild(meshInstance);
    }

    private void AddVisibleFaces(SurfaceTool st, int lx, int ly, int lz)
    {
        int worldX = _chunkX * _generator.ChunkSize + lx;
        int worldY = _chunkY * _generator.ChunkSize + ly;
        int worldZ = _chunkZ * _generator.ChunkSize + lz;
        Vector3 offset = new Vector3(lx, ly, lz);
        if (_generator.GetBlock(worldX, worldY, worldZ - 1) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(1, 0, 0),
                offset + new Vector3(1, 1, 0),
                offset + new Vector3(0, 1, 0)
            );
        }
        if (_generator.GetBlock(worldX, worldY, worldZ + 1) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(0, 1, 1),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(1, 0, 1)
            );
        }
        if (_generator.GetBlock(worldX - 1, worldY, worldZ) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(0, 1, 0),
                offset + new Vector3(0, 1, 1)
            );
        }
        if (_generator.GetBlock(worldX + 1, worldY, worldZ) == 0)
        {
            AddQuad(st,
                offset + new Vector3(1, 0, 0),
                offset + new Vector3(1, 0, 1),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(1, 1, 0)
            );
        }
        if (_generator.GetBlock(worldX, worldY + 1, worldZ) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 1, 0),
                offset + new Vector3(1, 1, 0),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(0, 1, 1)
            );
        }
        if (_generator.GetBlock(worldX, worldY - 1, worldZ) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(1, 0, 1),
                offset + new Vector3(1, 0, 0)
            );
        }
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