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

    public override void _Ready()
    {
        // Adjust if your scene structure differs
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
        if (_generator == null || _generator.Chunks == null)
            return; // If generator not ready, skip

        UpdateHighlight();

        // Movement using up/down/left/right
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * Speed;

        Vector3 v = Velocity;
        v.X = moveDir.X;
        v.Z = moveDir.Z;

        // Basic vertical movement
        if (Input.IsActionPressed("jump"))
            v.Y = Speed;
        else if (Input.IsActionPressed("crouch"))
            v.Y = -Speed;
        else
            v.Y = 0;

        Velocity = v;
        MoveAndSlide();

        // Block interaction
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

        _highlightMesh.Visible = false;
        _highlightedBlockPos = new Vector3I(-1, -1, -1);

        while (dist < InteractionRange)
        {
            Vector3 checkPos = origin + direction * dist;
            int bx = Mathf.FloorToInt(checkPos.X);
            int by = Mathf.FloorToInt(checkPos.Y);
            int bz = Mathf.FloorToInt(checkPos.Z);

            if (_generator.GetBlock(bx, by, bz) == 1)
            {
                _highlightMesh.Visible = true;
                _highlightMesh.GlobalPosition = new Vector3(bx, by, bz);
                _highlightedBlockPos = new Vector3I(bx, by, bz);
                return;
            }
            dist += step;
        }
    }

    private void BreakBlock()
    {
        if (!_highlightMesh.Visible) return;
        if (_highlightedBlockPos.X < 0) return;

        _generator.SetBlock(_highlightedBlockPos.X, _highlightedBlockPos.Y, _highlightedBlockPos.Z, 0);
    }

    private void PlaceBlock()
    {
        if (!_highlightMesh.Visible) return;
        if (_highlightedBlockPos.X < 0) return;

        // Calculate which side to place a new block on by choosing the dominant axis
        Vector3 forward = -_camera.GlobalTransform.Basis.Z;
        Vector3I offset = GetDominantAxisOffset(forward);

        Vector3I placePos = _highlightedBlockPos + offset;
        _generator.SetBlock(placePos.X, placePos.Y, placePos.Z, 1);
    }

    /// <summary>
    /// Picks the dominant axis from 'forward' so that we place 
    /// blocks on whichever axis is largest in magnitude.
    /// e.g. if looking mostly up, place above, etc.
    /// </summary>
    private Vector3I GetDominantAxisOffset(Vector3 forward)
    {
        float absX = Mathf.Abs(forward.X);
        float absY = Mathf.Abs(forward.Y);
        float absZ = Mathf.Abs(forward.Z);

        // Compare which axis is largest
        if (absX > absY && absX > absZ)
        {
            // X is largest
            return new Vector3I(forward.X > 0 ? 1 : -1, 0, 0);
        }
        else if (absY > absZ)
        {
            // Y is largest
            return new Vector3I(0, forward.Y > 0 ? 1 : -1, 0);
        }
        else
        {
            // Z is largest
            return new Vector3I(0, 0, forward.Z > 0 ? 1 : -1);
        }
    }
}
