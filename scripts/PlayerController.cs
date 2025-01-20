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
        // Adjust the path if needed. For example, if Generator is a sibling, "../Generator" is correct.
        // Or you might have to do GetNode<Generator>("/root/YourMainNode/Generator"), etc.
        _generator = GetNode<Generator>("../Generator");

        _head = GetNode<Node3D>("Head");
        _camera = GetNode<Camera3D>("Head/Camera3D");

        // Create a highlight mesh
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

            var headRot = _head.Rotation;
            headRot.X = Mathf.Clamp(headRot.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
            _head.Rotation = headRot;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // If generator or chunks not ready, skip
        if (_generator == null || _generator.Chunks == null)
            return;

        UpdateHighlight();

        // Movement
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * Speed;

        Vector3 velocity = Velocity;
        velocity.X = moveDir.X;
        velocity.Z = moveDir.Z;

        if (Input.IsActionPressed("jump"))
            velocity.Y = Speed;
        else if (Input.IsActionPressed("crouch"))
            velocity.Y = -Speed;
        else
            velocity.Y = 0;

        Velocity = velocity;
        MoveAndSlide();

        // Breaking and placing blocks
        if (Input.IsActionJustPressed("break"))
            BreakBlock();

        if (Input.IsActionJustPressed("place"))
            PlaceBlock();
    }

    private void UpdateHighlight()
    {
        // Ray from camera
        Vector3 origin = _camera.GlobalTransform.Origin;
        Vector3 direction = -_camera.GlobalTransform.Basis.Z.Normalized();

        float distance = 0f;
        float step = 0.1f;
        _highlightMesh.Visible = false;
        _highlightedBlockPos = new Vector3I(-1, -1, -1);

        while (distance < InteractionRange)
        {
            Vector3 checkPos = origin + direction * distance;

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

            distance += step;
        }
    }

    private void BreakBlock()
    {
        if (!_highlightMesh.Visible) return;
        if (_highlightedBlockPos.X < 0) return;

        _generator.SetBlock(
            _highlightedBlockPos.X,
            _highlightedBlockPos.Y,
            _highlightedBlockPos.Z,
            0
        );
    }

    private void PlaceBlock()
    {
        if (!_highlightMesh.Visible) return;
        if (_highlightedBlockPos.X < 0) return;

        // Determine which side of the highlighted block to place upon
        Vector3 forward = -_camera.GlobalTransform.Basis.Z.Normalized();
        Vector3I offset = new Vector3I(
            Mathf.RoundToInt(forward.X),
            Mathf.RoundToInt(forward.Y),
            Mathf.RoundToInt(forward.Z)
        );
        Vector3I placePos = _highlightedBlockPos + offset;

        _generator.SetBlock(
            placePos.X,
            placePos.Y,
            placePos.Z,
            1
        );
    }
}
