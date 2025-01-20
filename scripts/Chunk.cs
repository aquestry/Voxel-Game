using Godot;
using System;

public partial class Chunk : Node3D
{
    public int[,,] blocks; // 1 = block, 0 = empty
    private Generator _generator;
    private int _chunkX, _chunkY, _chunkZ;

    /// <summary>
    /// Called by Generator to set up references and allocate our blocks array.
    /// </summary>
    public void Initialize(Generator generator, int cx, int cy, int cz)
    {
        _generator = generator;
        _chunkX = cx;
        _chunkY = cy;
        _chunkZ = cz;

        blocks = new int[_generator.ChunkSize, _generator.ChunkSize, _generator.ChunkSize];
    }

    /// <summary>
    /// Fills "blocks" array with noise-based terrain data.
    /// Called by the Generator in a separate pass (so neighbors exist).
    /// </summary>
    public void GenerateData()
    {
        for (int localX = 0; localX < _generator.ChunkSize; localX++)
        {
            for (int localZ = 0; localZ < _generator.ChunkSize; localZ++)
            {
                int worldX = _chunkX * _generator.ChunkSize + localX;
                int worldZ = _chunkZ * _generator.ChunkSize + localZ;

                // Evaluate noise at (worldX, worldZ)
                double noiseVal = _generator.noise.Evaluate(
                    worldX * _generator.NoiseScale,
                    worldZ * _generator.NoiseScale
                );
                // noiseVal is -1..1, map to 0..1
                noiseVal = (noiseVal + 1.0) * 0.5;

                // Decide how high in Y to fill blocks
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

    /// <summary>
    /// Builds or rebuilds the chunk mesh. Only called after we have valid block data.
    /// </summary>
    public void RebuildMesh()
    {
        // Clear old mesh children
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

    /// <summary>
    /// For each block, check neighboring blocks via Generator.GetBlock().
    /// If neighbor is 0 => that face is visible; draw it.
    /// </summary>
    private void AddVisibleFaces(SurfaceTool st, int lx, int ly, int lz)
    {
        int worldX = _chunkX * _generator.ChunkSize + lx;
        int worldY = _chunkY * _generator.ChunkSize + ly;
        int worldZ = _chunkZ * _generator.ChunkSize + lz;
        Vector3 offset = new Vector3(lx, ly, lz);

        if (_generator.GetBlock(worldX, worldY, worldZ - 1) == 0)
        {
            // Z- face
            AddQuad(st,
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(1, 0, 0),
                offset + new Vector3(1, 1, 0),
                offset + new Vector3(0, 1, 0)
            );
        }
        if (_generator.GetBlock(worldX, worldY, worldZ + 1) == 0)
        {
            // Z+ face
            AddQuad(st,
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(0, 1, 1),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(1, 0, 1)
            );
        }
        if (_generator.GetBlock(worldX - 1, worldY, worldZ) == 0)
        {
            // X-
            AddQuad(st,
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(0, 1, 0),
                offset + new Vector3(0, 1, 1)
            );
        }
        if (_generator.GetBlock(worldX + 1, worldY, worldZ) == 0)
        {
            // X+
            AddQuad(st,
                offset + new Vector3(1, 0, 0),
                offset + new Vector3(1, 0, 1),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(1, 1, 0)
            );
        }
        if (_generator.GetBlock(worldX, worldY + 1, worldZ) == 0)
        {
            // Y+
            AddQuad(st,
                offset + new Vector3(0, 1, 0),
                offset + new Vector3(1, 1, 0),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(0, 1, 1)
            );
        }
        if (_generator.GetBlock(worldX, worldY - 1, worldZ) == 0)
        {
            // Y-
            AddQuad(st,
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(1, 0, 1),
                offset + new Vector3(1, 0, 0)
            );
        }
    }

    /// <summary>
    /// Adds a single face (2 triangles) to the mesh with a normal 1x1 UV layout.
    /// This ensures the texture is not skewed or repeated incorrectly.
    /// </summary>
    private void AddQuad(SurfaceTool st, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        // Triangle #1
        st.SetUV(new Vector2(0, 0));
        st.AddVertex(v1);
        st.SetUV(new Vector2(1, 0));
        st.AddVertex(v2);
        st.SetUV(new Vector2(1, 1));
        st.AddVertex(v3);

        // Triangle #2
        st.SetUV(new Vector2(0, 0));
        st.AddVertex(v1);
        st.SetUV(new Vector2(1, 1));
        st.AddVertex(v3);
        st.SetUV(new Vector2(0, 1));
        st.AddVertex(v4);
    }
}
