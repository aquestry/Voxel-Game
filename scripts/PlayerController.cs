using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MouseSensitivity = 0.5f;
    [Export] public float InteractionRange = 10f;
    [Export] public float Speed = 10f;

    private Node3D _head;
    private Camera3D _camera;
    private Generator _generator;
    private MeshInstance3D _highlightMesh;
    private Vector3I _highlightedBlockPos = new Vector3I(-1, -1, -1);
    private Vector3I _placeBlockPos = new Vector3I(-1, -1, -1);


    public override void _Ready()
    {
        // Adjust path if needed, depending on scene
        _generator = GetNode<Generator>("../Generator");

        _head = GetNode<Node3D>("Head");
        _camera = GetNode<Camera3D>("Head/Camera3D");

        // Create highlight box
        _highlightMesh = new MeshInstance3D
        {
            Mesh = new BoxMesh(),
            Visible = false
        };
        var highlightMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(1, 1, 0, 0.3f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };
        _highlightMesh.MaterialOverride = highlightMat;
        AddChild(_highlightMesh);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            // Mouse look
            RotateY(-motion.Relative.X * MouseSensitivity * 0.001f);
            _head.RotateObjectLocal(Vector3.Right, -motion.Relative.Y * MouseSensitivity * 0.001f);

            var rot = _head.Rotation;
            rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
            _head.Rotation = rot;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // If generator not ready, skip
        if (_generator == null || _generator.Chunks == null)
            return;

        UpdateHighlight();

        // Movement
        // Using up/down for forward/back, left/right for strafing
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * Speed;

        Vector3 v = Velocity;
        v.X = moveDir.X;
        v.Z = moveDir.Z;

        // Vertical
        if (Input.IsActionPressed("jump"))
            v.Y = Speed;
        else if (Input.IsActionPressed("crouch"))
            v.Y = -Speed;
        else
            v.Y = 0;

        Velocity = v;
        MoveAndSlide();

        // Break / Place
        if (Input.IsActionJustPressed("break"))
            BreakBlock();

        if (Input.IsActionJustPressed("place"))
            PlaceBlock();
    }

    private void UpdateHighlight()
    {
        Vector3 origin = _camera.GlobalTransform.Origin;
        Vector3 direction = -_camera.GlobalTransform.Basis.Z.Normalized();

        float dist = 0f;
        const float step = 0.1f;

        // Default: no highlight, no place
        _highlightMesh.Visible = false;
        _highlightedBlockPos = new Vector3I(-1, -1, -1);
        _placeBlockPos = new Vector3I(-1, -1, -1);

        // We'll store the last empty coordinate we encountered
        Vector3I lastEmptyPos = new Vector3I(-1, -1, -1);

        // Raymarch
        while (dist < InteractionRange)
        {
            Vector3 checkPos = origin + direction * dist;
            int bx = Mathf.FloorToInt(checkPos.X);
            int by = Mathf.FloorToInt(checkPos.Y);
            int bz = Mathf.FloorToInt(checkPos.Z);

            if (_generator.GetBlock(bx, by, bz) == 1)
            {
                // Found a block => highlight it
                _highlightMesh.Visible = true;
                _highlightMesh.GlobalPosition = new Vector3(bx, by, bz);
                _highlightedBlockPos = new Vector3I(bx, by, bz);

                // The "place" position is the last empty coordinate we encountered
                _placeBlockPos = lastEmptyPos;
                return;
            }
            else
            {
                // This coordinate is empty, so remember it as the last empty
                lastEmptyPos = new Vector3I(bx, by, bz);
            }

            dist += step;
        }
    }


    private void BreakBlock()
    {
        // If there's no highlighted block, do nothing
        if (!_highlightMesh.Visible) return;
        if (_highlightedBlockPos.X < 0) return;

        // Remove it
        _generator.SetBlock(
            _highlightedBlockPos.X,
            _highlightedBlockPos.Y,
            _highlightedBlockPos.Z,
            0
        );
    }

    private void PlaceBlock()
    {
        if (!_highlightMesh.Visible) return;         // No highlighted block
        if (_highlightedBlockPos.X < 0) return;      // Highlight invalid
        if (_placeBlockPos.X < 0) return;            // No valid place position

        GD.Print($"[PlaceBlock] placing new block at {_placeBlockPos}");
        _generator.SetBlock(_placeBlockPos.X, _placeBlockPos.Y, _placeBlockPos.Z, 1);
    }


    /// <summary>
    /// Returns an offset of (±1,0,0), (0,±1,0), or (0,0,±1)
    /// depending on which axis has the greatest absolute value in 'direction'.
    /// This prevents overwriting the same block if direction is diagonal.
    /// </summary>
    private Vector3I GetDominantAxisOffset(Vector3 direction)
    {
        float absX = Mathf.Abs(direction.X);
        float absY = Mathf.Abs(direction.Y);
        float absZ = Mathf.Abs(direction.Z);

        // Whichever axis is largest => place there
        if (absX >= absY && absX >= absZ)
        {
            // X axis is dominant
            return new Vector3I(direction.X > 0 ? 1 : -1, 0, 0);
        }
        else if (absY >= absZ)
        {
            // Y axis is dominant
            return new Vector3I(0, direction.Y > 0 ? 1 : -1, 0);
        }
        else
        {
            // Z axis is dominant
            return new Vector3I(0, 0, direction.Z > 0 ? 1 : -1);
        }
    }
}
