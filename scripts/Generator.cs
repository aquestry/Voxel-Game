using Godot;
using Noise;

public partial class Generator : Node
{

	[Export] public int ChunkSize = 16;
	[Export] public int WorldWidthInChunks = 4;
	[Export] public int WorldHeightInChunks = 2;
	[Export] public int WorldDepthInChunks = 4;
	[Export] public CompressedTexture2D BlockTexture;
	[Export] public float NoiseScale = 0.1f;
	public OpenSimplexNoise noise;
	public Chunk[,,] Chunks;

	public override void _Ready()
	{
		noise = new OpenSimplexNoise();
		Chunks = new Chunk[WorldWidthInChunks, WorldHeightInChunks, WorldDepthInChunks];
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
		if (x < 0 || y < 0 || z < 0) return 0;
		int maxX = WorldWidthInChunks * ChunkSize;
		int maxY = WorldHeightInChunks * ChunkSize;
		int maxZ = WorldDepthInChunks * ChunkSize;
		if (x >= maxX || y >= maxY || z >= maxZ) return 0;
		int cx = x / ChunkSize;
		int cy = y / ChunkSize;
		int cz = z / ChunkSize;
		var c = Chunks[cx, cy, cz];
		if (c == null) return 0;
		int lx = x % ChunkSize;
		int ly = y % ChunkSize;
		int lz = z % ChunkSize;
		return c.blocks[lx, ly, lz];
	}

	public void SetBlock(int x, int y, int z, int value)
	{
		GD.Print($"[SetBlock] CALLED => coords=({x}, {y}, {z}), value={value}");
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
		int cx = x / ChunkSize;
		int cy = y / ChunkSize;
		int cz = z / ChunkSize;
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
		int lx = x % ChunkSize;
		int ly = y % ChunkSize;
		int lz = z % ChunkSize;
		GD.Print($"[SetBlock] => chunk=({cx},{cy},{cz}), local=({lx},{ly},{lz}), newValue={value}");
		int oldValue = chunk.blocks[lx, ly, lz];
		GD.Print($"[SetBlock] oldValue={oldValue}");
		if (oldValue == value)
		{
			GD.Print("[SetBlock] No change => block was already that value => ABORT");
			return;
		}
		chunk.blocks[lx, ly, lz] = value;
		chunk.RebuildMesh();
		GD.Print($"[SetBlock] Rebuilt chunk=({cx},{cy},{cz}) => oldValue={oldValue}, newValue={value}");
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

	public bool IsBlockAt(Vector3 pos)
	{
		int bx = Mathf.FloorToInt(pos.X);
		int by = Mathf.FloorToInt(pos.Y);
		int bz = Mathf.FloorToInt(pos.Z);
		return GetBlock(bx, by, bz) == 1;
	}
}
