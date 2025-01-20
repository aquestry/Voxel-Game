using Godot;
using System;
using Noise; // IMPORTANT: This references your existing noise.cs

public partial class Generator : Node
{
    [Export] public int ChunkSize = 16;                    // Each chunk’s dimensions: 16x16x16
    [Export] public int WorldWidthInChunks = 4;            // How many chunks along X
    [Export] public int WorldHeightInChunks = 2;           // How many chunks along Y
    [Export] public int WorldDepthInChunks = 4;            // How many chunks along Z

    [Export] public CompressedTexture2D BlockTexture;       // Your block texture
    [Export] public float NoiseScale = 0.1f;                // Adjust for terrain variation

    public OpenSimplexNoise noise;                          // The noise instance (from noise.cs)
    public Chunk[,,] Chunks;                                // 3D array of Chunk references

    public override void _Ready()
    {
        // Initialize the noise
        noise = new OpenSimplexNoise(); // or new OpenSimplexNoise(DateTime.Now.Ticks)
        // Optionally tweak properties if you want:
        // noise.Octaves = ...
        // noise.Seed = ...
        // noise.Period = ...
        // noise.Persistence = ...

        // Create chunk array
        Chunks = new Chunk[WorldWidthInChunks, WorldHeightInChunks, WorldDepthInChunks];

        // Instantiate each chunk
        for (int cx = 0; cx < WorldWidthInChunks; cx++)
        {
            for (int cy = 0; cy < WorldHeightInChunks; cy++)
            {
                for (int cz = 0; cz < WorldDepthInChunks; cz++)
                {
                    // Create a new Chunk node
                    var chunk = new Chunk();

                    // Add it as a child so it's in the scene
                    AddChild(chunk);

                    // Move chunk to correct position in world space
                    chunk.GlobalPosition = new Vector3(cx * ChunkSize, cy * ChunkSize, cz * ChunkSize);

                    // Initialize chunk data
                    chunk.Initialize(this, cx, cy, cz);

                    // Store reference
                    Chunks[cx, cy, cz] = chunk;
                }
            }
        }
    }

    /// <summary>
    /// Returns block value at world coordinates (x,y,z). 1 = block, 0 = empty.
    /// </summary>
    public int GetBlock(int x, int y, int z)
    {
        // Check overall bounds
        if (x < 0 || y < 0 || z < 0) return 0;
        int maxX = WorldWidthInChunks * ChunkSize;
        int maxY = WorldHeightInChunks * ChunkSize;
        int maxZ = WorldDepthInChunks * ChunkSize;
        if (x >= maxX || y >= maxY || z >= maxZ) return 0;

        // Which chunk are we in?
        int cx = x / ChunkSize;
        int cy = y / ChunkSize;
        int cz = z / ChunkSize;

        Chunk c = Chunks[cx, cy, cz];
        if (c == null) return 0; // should never happen if fully generated

        // Local coords inside that chunk
        int lx = x % ChunkSize;
        int ly = y % ChunkSize;
        int lz = z % ChunkSize;

        return c.blocks[lx, ly, lz];
    }

    /// <summary>
    /// Sets block at world coords (x,y,z) to 'value' (1 or 0), then rebuilds chunk & neighbors if needed.
    /// </summary>
    public void SetBlock(int x, int y, int z, int value)
    {
        // Bounds check
        if (x < 0 || y < 0 || z < 0) return;
        int maxX = WorldWidthInChunks * ChunkSize;
        int maxY = WorldHeightInChunks * ChunkSize;
        int maxZ = WorldDepthInChunks * ChunkSize;
        if (x >= maxX || y >= maxY || z >= maxZ) return;

        int cx = x / ChunkSize;
        int cy = y / ChunkSize;
        int cz = z / ChunkSize;
        Chunk chunk = Chunks[cx, cy, cz];
        if (chunk == null) return;

        int lx = x % ChunkSize;
        int ly = y % ChunkSize;
        int lz = z % ChunkSize;

        // Set the block data
        chunk.blocks[lx, ly, lz] = value;

        // Rebuild this chunk
        chunk.RebuildMesh();

        // If we changed a face on the edge, we might need to rebuild adjacent chunks:
        if (lx == 0 && cx > 0)
            Chunks[cx - 1, cy, cz]?.RebuildMesh();
        if (lx == ChunkSize - 1 && cx < WorldWidthInChunks - 1)
            Chunks[cx + 1, cy, cz]?.RebuildMesh();

        if (ly == 0 && cy > 0)
            Chunks[cx, cy - 1, cz]?.RebuildMesh();
        if (ly == ChunkSize - 1 && cy < WorldHeightInChunks - 1)
            Chunks[cx, cy + 1, cz]?.RebuildMesh();

        if (lz == 0 && cz > 0)
            Chunks[cx, cy, cz - 1]?.RebuildMesh();
        if (lz == ChunkSize - 1 && cz < WorldDepthInChunks - 1)
            Chunks[cx, cy, cz + 1]?.RebuildMesh();
    }

    /// <summary>
    /// Quick helper used by the player for highlight checks.
    /// Returns true if there's a block at the float position.
    /// </summary>
    public bool IsBlockAt(Vector3 pos)
    {
        int bx = Mathf.FloorToInt(pos.X);
        int by = Mathf.FloorToInt(pos.Y);
        int bz = Mathf.FloorToInt(pos.Z);
        return GetBlock(bx, by, bz) == 1;
    }
}
