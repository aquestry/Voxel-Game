using Godot;
using System;
using Noise; // Using your noise.cs namespace

public partial class Generator : Node
{
    [Export] public int ChunkSize = 16;
    [Export] public int WorldWidthInChunks = 4;
    [Export] public int WorldHeightInChunks = 2;
    [Export] public int WorldDepthInChunks = 4;

    [Export] public CompressedTexture2D BlockTexture;
    [Export] public float NoiseScale = 0.1f;

    public OpenSimplexNoise noise;
    public Chunk[,,] Chunks; // 3D array of chunk references

    public override void _Ready()
    {
        // 1) Create and seed the noise
        noise = new OpenSimplexNoise();
        // Optionally tweak or set your own seed, etc.

        // 2) Allocate the chunk array
        Chunks = new Chunk[WorldWidthInChunks, WorldHeightInChunks, WorldDepthInChunks];

        // 3) Instantiate all chunk nodes (but do NOT build their mesh yet)
        for (int cx = 0; cx < WorldWidthInChunks; cx++)
        {
            for (int cy = 0; cy < WorldHeightInChunks; cy++)
            {
                for (int cz = 0; cz < WorldDepthInChunks; cz++)
                {
                    var chunk = new Chunk();
                    AddChild(chunk);
                    chunk.GlobalPosition = new Vector3(cx * ChunkSize, cy * ChunkSize, cz * ChunkSize);
                    chunk.Initialize(this, cx, cy, cz);
                    Chunks[cx, cy, cz] = chunk;
                }
            }
        }

        // 4) Generate data for all chunks (so neighbors see correct blocks)
        for (int cx = 0; cx < WorldWidthInChunks; cx++)
        {
            for (int cy = 0; cy < WorldHeightInChunks; cy++)
            {
                for (int cz = 0; cz < WorldDepthInChunks; cz++)
                {
                    Chunks[cx, cy, cz].GenerateData();
                }
            }
        }

        // 5) Now rebuild all chunk meshes once the data is complete
        for (int cx = 0; cx < WorldWidthInChunks; cx++)
        {
            for (int cy = 0; cy < WorldHeightInChunks; cy++)
            {
                for (int cz = 0; cz < WorldDepthInChunks; cz++)
                {
                    Chunks[cx, cy, cz].RebuildMesh();
                }
            }
        }
    }

    public int GetBlock(int x, int y, int z)
    {
        // Out of world bounds -> 0
        if (x < 0 || y < 0 || z < 0) return 0;
        int maxX = WorldWidthInChunks * ChunkSize;
        int maxY = WorldHeightInChunks * ChunkSize;
        int maxZ = WorldDepthInChunks * ChunkSize;
        if (x >= maxX || y >= maxY || z >= maxZ) return 0;

        // Which chunk are we in?
        int cx = x / ChunkSize;
        int cy = y / ChunkSize;
        int cz = z / ChunkSize;

        var c = Chunks[cx, cy, cz];
        if (c == null) return 0; // Should never happen if everything is loaded

        // Local coords
        int lx = x % ChunkSize;
        int ly = y % ChunkSize;
        int lz = z % ChunkSize;
        return c.blocks[lx, ly, lz];
    }

    /// <summary>
    /// Sets block at (x,y,z) to 'value' (1 or 0), and rebuilds chunk + neighbors if needed.
    /// </summary>
    public void SetBlock(int x, int y, int z, int value)
    {
        // Bounds check
        if (x < 0 || y < 0 || z < 0) return;
        int maxX = WorldWidthInChunks * ChunkSize;
        int maxY = WorldHeightInChunks * ChunkSize;
        int maxZ = WorldDepthInChunks * ChunkSize;
        if (x >= maxX || y >= maxY || z >= maxZ) return;

        // Which chunk?
        int cx = x / ChunkSize;
        int cy = y / ChunkSize;
        int cz = z / ChunkSize;
        var chunk = Chunks[cx, cy, cz];
        if (chunk == null) return;

        // Local coords in chunk
        int lx = x % ChunkSize;
        int ly = y % ChunkSize;
        int lz = z % ChunkSize;

        // Set the data
        chunk.blocks[lx, ly, lz] = value;

        // Rebuild this chunk
        chunk.RebuildMesh();

        // Possibly rebuild neighbors if we changed a face on the boundary
        if (lx == 0 && cx > 0) Chunks[cx - 1, cy, cz]?.RebuildMesh();
        if (lx == ChunkSize - 1 && cx < WorldWidthInChunks - 1) Chunks[cx + 1, cy, cz]?.RebuildMesh();

        if (ly == 0 && cy > 0) Chunks[cx, cy - 1, cz]?.RebuildMesh();
        if (ly == ChunkSize - 1 && cy < WorldHeightInChunks - 1) Chunks[cx, cy + 1, cz]?.RebuildMesh();

        if (lz == 0 && cz > 0) Chunks[cx, cy, cz - 1]?.RebuildMesh();
        if (lz == ChunkSize - 1 && cz < WorldDepthInChunks - 1) Chunks[cx, cy, cz + 1]?.RebuildMesh();
    }

    /// <summary>
    /// Used by player highlight logic. True if block is present at 'pos'.
    /// </summary>
    public bool IsBlockAt(Vector3 pos)
    {
        int bx = Mathf.FloorToInt(pos.X);
        int by = Mathf.FloorToInt(pos.Y);
        int bz = Mathf.FloorToInt(pos.Z);
        return GetBlock(bx, by, bz) == 1;
    }
}
