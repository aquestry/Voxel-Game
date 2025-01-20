using Godot;
using System;

public partial class Chunk : Node3D
{
    public int[,,] blocks;      // 1 = block, 0 = empty
    private Generator _generator;
    private int _chunkX, _chunkY, _chunkZ;

    /// <summary>
    /// Called by Generator after creating the chunk node.
    /// This sets up references but does NOT build the mesh yet.
    /// </summary>
    public void Initialize(Generator generator, int cx, int cy, int cz)
    {
        _generator = generator;
        _chunkX = cx;
        _chunkY = cy;
        _chunkZ = cz;

        // Allocate the block array
        blocks = new int[_generator.ChunkSize, _generator.ChunkSize, _generator.ChunkSize];
    }

    /// <summary>
    /// Generate the terrain data for this chunk. 
    /// Called by the Generator in a separate pass so all chunks are generated before any are meshed.
    /// </summary>
    public void GenerateData()
    {
        // Example: use noise to fill in blocks up to a certain height
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
                // noiseVal is in [-1..1], map to 0..1
                noiseVal = (noiseVal + 1.0) * 0.5;

                // Max block height in world coords
                int maxY = Mathf.FloorToInt(
                    (float)(noiseVal * (_generator.ChunkSize * _generator.WorldHeightInChunks - 1))
                );

                // Fill blocks up to maxY
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
    /// Rebuild the mesh based on the blocks[] array and neighbors.
    /// Called after data generation, or after a player changes a block.
    /// </summary>
    public void RebuildMesh()
    {
        // Remove old mesh children
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        // Start building
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

    private void AddVisibleFaces(SurfaceTool st, int lx, int ly, int lz)
    {
        // Convert from local chunk coords to global block coords
        int worldX = _chunkX * _generator.ChunkSize + lx;
        int worldY = _chunkY * _generator.ChunkSize + ly;
        int worldZ = _chunkZ * _generator.ChunkSize + lz;

        Vector3 offset = new Vector3(lx, ly, lz);

        // If neighbor is 0 => face is visible

        // Z- face
        if (_generator.GetBlock(worldX, worldY, worldZ - 1) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(1, 0, 0),
                offset + new Vector3(1, 1, 0),
                offset + new Vector3(0, 1, 0)
            );
        }

        // Z+ face
        if (_generator.GetBlock(worldX, worldY, worldZ + 1) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(0, 1, 1),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(1, 0, 1)
            );
        }

        // X- face
        if (_generator.GetBlock(worldX - 1, worldY, worldZ) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 0, 1),
                offset + new Vector3(0, 0, 0),
                offset + new Vector3(0, 1, 0),
                offset + new Vector3(0, 1, 1)
            );
        }

        // X+ face
        if (_generator.GetBlock(worldX + 1, worldY, worldZ) == 0)
        {
            AddQuad(st,
                offset + new Vector3(1, 0, 0),
                offset + new Vector3(1, 0, 1),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(1, 1, 0)
            );
        }

        // Y+ face
        if (_generator.GetBlock(worldX, worldY + 1, worldZ) == 0)
        {
            AddQuad(st,
                offset + new Vector3(0, 1, 0),
                offset + new Vector3(1, 1, 0),
                offset + new Vector3(1, 1, 1),
                offset + new Vector3(0, 1, 1)
            );
        }

        // Y- face
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
