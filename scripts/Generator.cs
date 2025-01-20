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
        GD.Print($"[SetBlock] CALLED => coords=({x}, {y}, {z}), value={value}");

        // 1) Check world bounds
        if (x < 0 || y < 0 || z < 0)
        {
            GD.Print($"[SetBlock] ABORT => coords negative => ({x},{y},{z})");
            return;
        }
        int maxX = WorldWidthInChunks * ChunkSize;
        int maxY = WorldHeightInChunks * ChunkSize;
        int maxZ = WorldDepthInChunks * ChunkSize;
        if (x >= maxX || y >= maxY || z >= maxZ)
        {
            GD.Print($"[SetBlock] ABORT => coords out of max bounds => " +
                     $"({x},{y},{z}) / max=({maxX},{maxY},{maxZ})");
            return;
        }

        // 2) Chunk coords
        int cx = x / ChunkSize;
        int cy = y / ChunkSize;
        int cz = z / ChunkSize;

        // 3) Check chunk array
        if (Chunks == null)
        {
            GD.Print("[SetBlock] ABORT => Chunks array is null");
            return;
        }

        var chunk = Chunks[cx, cy, cz];
        if (chunk == null)
        {
            GD.Print($"[SetBlock] ABORT => chunk (cx={cx},cy={cy},cz={cz}) is null");
            return;
        }

        // 4) Local coords
        int lx = x % ChunkSize;
        int ly = y % ChunkSize;
        int lz = z % ChunkSize;
        GD.Print($"[SetBlock] => chunk=({cx},{cy},{cz}), local=({lx},{ly},{lz}), newValue={value}");

        // 5) Check old value
        int oldValue = chunk.blocks[lx, ly, lz];
        GD.Print($"[SetBlock] oldValue={oldValue}");

        // If no actual change, short-circuit
        if (oldValue == value)
        {
            GD.Print("[SetBlock] No change => block was already that value => ABORT");
            return;
        }

        // 6) Update block data
        chunk.blocks[lx, ly, lz] = value;

        // 7) Rebuild main chunk
        chunk.RebuildMesh();
        GD.Print($"[SetBlock] Rebuilt chunk=({cx},{cy},{cz}) => oldValue={oldValue}, newValue={value}");

        // 8) If we changed a face on chunk's boundary => rebuild neighbors
        if (lx == 0 && cx > 0)
        {
            var neighbor = Chunks[cx - 1, cy, cz];
            if (neighbor != null)
            {
                neighbor.RebuildMesh();
                GD.Print($"[SetBlock] Rebuilt neighbor chunk=({cx - 1},{cy},{cz})");
            }
        }
        if (lx == ChunkSize - 1 && cx < WorldWidthInChunks - 1)
        {
            var neighbor = Chunks[cx + 1, cy, cz];
            if (neighbor != null)
            {
                neighbor.RebuildMesh();
                GD.Print($"[SetBlock] Rebuilt neighbor chunk=({cx + 1},{cy},{cz})");
            }
        }

        if (ly == 0 && cy > 0)
        {
            var neighbor = Chunks[cx, cy - 1, cz];
            if (neighbor != null)
            {
                neighbor.RebuildMesh();
                GD.Print($"[SetBlock] Rebuilt neighbor chunk=({cx},{cy - 1},{cz})");
            }
        }
        if (ly == ChunkSize - 1 && cy < WorldHeightInChunks - 1)
        {
            var neighbor = Chunks[cx, cy + 1, cz];
            if (neighbor != null)
            {
                neighbor.RebuildMesh();
                GD.Print($"[SetBlock] Rebuilt neighbor chunk=({cx},{cy + 1},{cz})");
            }
        }

        if (lz == 0 && cz > 0)
        {
            var neighbor = Chunks[cx, cy, cz - 1];
            if (neighbor != null)
            {
                neighbor.RebuildMesh();
                GD.Print($"[SetBlock] Rebuilt neighbor chunk=({cx},{cy},{cz - 1})");
            }
        }
        if (lz == ChunkSize - 1 && cz < WorldDepthInChunks - 1)
        {
            var neighbor = Chunks[cx, cy, cz + 1];
            if (neighbor != null)
            {
                neighbor.RebuildMesh();
                GD.Print($"[SetBlock] Rebuilt neighbor chunk=({cx},{cy},{cz + 1})");
            }
        }

        GD.Print("[SetBlock] Done.");
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
