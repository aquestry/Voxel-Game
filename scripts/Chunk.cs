using Godot;
using System;

public partial class Chunk : Node3D
{
    public int[,,] blocks;              // Stores block data (1 = block, 0 = empty)
    private Generator _generator;       // Reference to the main Generator
    private int _chunkX, _chunkY, _chunkZ;

    /// <summary>
    /// Called from Generator to initialize the chunk’s data.
    /// </summary>
    public void Initialize(Generator gen, int cx, int cy, int cz)
    {
        _generator = gen;
        _chunkX = cx;
        _chunkY = cy;
        _chunkZ = cz;

        blocks = new int[_generator.ChunkSize, _generator.ChunkSize, _generator.ChunkSize];

        // Example: fill with simple noise-based terrain
        GenerateTerrain();
        RebuildMesh();
    }

    /// <summary>
    /// Generate some noise-based terrain for this chunk.
    /// You can customize further as you like.
    /// </summary>
    private void GenerateTerrain()
    {
        for (int localX = 0; localX < _generator.ChunkSize; localX++)
        {
            for (int localZ = 0; localZ < _generator.ChunkSize; localZ++)
            {
                // Convert chunk-local coords to world coords
                int worldX = _chunkX * _generator.ChunkSize + localX;
                int worldZ = _chunkZ * _generator.ChunkSize + localZ;

                // Evaluate noise at (worldX, worldZ)
                double noiseValue = _generator.noise.Evaluate(
                    worldX * _generator.NoiseScale,
                    worldZ * _generator.NoiseScale
                );

                // noiseValue is [-1..1], so remap to 0..1
                noiseValue = (noiseValue + 1.0) * 0.5;

                // Convert to a "maxY" in world coordinates
                int maxY = Mathf.FloorToInt(
                    (float)(noiseValue * (_generator.ChunkSize * _generator.WorldHeightInChunks - 1))
                );

                // Populate blocks
                for (int localY = 0; localY < _generator.ChunkSize; localY++)
                {
                    int worldY = _chunkY * _generator.ChunkSize + localY;
                    if (worldY <= maxY)
                    {
                        blocks[localX, localY, localZ] = 1; // 1 means there's a block
                    }
                }
            }
        }
    }

    /// <summary>
    /// Rebuild the mesh for this chunk based on the 'blocks' array.
    /// </summary>
    public void RebuildMesh()
    {
        // Clear out old mesh children
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        var st = new SurfaceTool();
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
    /// Checks adjacent blocks. If neighbor is 0, draw that face.
    /// </summary>
    private void AddVisibleFaces(SurfaceTool st, int x, int y, int z)
    {
        int worldX = _chunkX * _generator.ChunkSize + x;
        int worldY = _chunkY * _generator.ChunkSize + y;
        int worldZ = _chunkZ * _generator.ChunkSize + z;

        Vector3 o = new Vector3(x, y, z);

        // Z- face
        if (_generator.GetBlock(worldX, worldY, worldZ - 1) == 0)
        {
            AddQuad(st,
                o + new Vector3(0, 0, 0),
                o + new Vector3(1, 0, 0),
                o + new Vector3(1, 1, 0),
                o + new Vector3(0, 1, 0)
            );
        }

        // Z+ face
        if (_generator.GetBlock(worldX, worldY, worldZ + 1) == 0)
        {
            AddQuad(st,
                o + new Vector3(0, 0, 1),
                o + new Vector3(0, 1, 1),
                o + new Vector3(1, 1, 1),
                o + new Vector3(1, 0, 1)
            );
        }

        // X- face
        if (_generator.GetBlock(worldX - 1, worldY, worldZ) == 0)
        {
            AddQuad(st,
                o + new Vector3(0, 0, 1),
                o + new Vector3(0, 0, 0),
                o + new Vector3(0, 1, 0),
                o + new Vector3(0, 1, 1)
            );
        }

        // X+ face
        if (_generator.GetBlock(worldX + 1, worldY, worldZ) == 0)
        {
            AddQuad(st,
                o + new Vector3(1, 0, 0),
                o + new Vector3(1, 0, 1),
                o + new Vector3(1, 1, 1),
                o + new Vector3(1, 1, 0)
            );
        }

        // Y+ face
        if (_generator.GetBlock(worldX, worldY + 1, worldZ) == 0)
        {
            AddQuad(st,
                o + new Vector3(0, 1, 0),
                o + new Vector3(1, 1, 0),
                o + new Vector3(1, 1, 1),
                o + new Vector3(0, 1, 1)
            );
        }

        // Y- face
        if (_generator.GetBlock(worldX, worldY - 1, worldZ) == 0)
        {
            AddQuad(st,
                o + new Vector3(0, 0, 0),
                o + new Vector3(0, 0, 1),
                o + new Vector3(1, 0, 1),
                o + new Vector3(1, 0, 0)
            );
        }
    }

    /// <summary>
    /// Helper to add a quad (2 triangles) to the mesh.
    /// </summary>
    private void AddQuad(SurfaceTool st, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        // Triangle #1
        st.SetUV(Vector2.Zero);
        st.AddVertex(v1);
        st.SetUV(Vector2.Right);
        st.AddVertex(v2);
        st.SetUV(Vector2.One);
        st.AddVertex(v3);

        // Triangle #2
        st.SetUV(Vector2.Zero);
        st.AddVertex(v1);
        st.SetUV(Vector2.One);
        st.AddVertex(v3);
        st.SetUV(Vector2.Up);
        st.AddVertex(v4);
    }
}
