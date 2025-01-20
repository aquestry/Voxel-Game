using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MouseSensitivity = 0.5f;
    [Export] public float InteractionRange = 10f;
    private float Speed = 50f;
    private Node3D _head;
    private Camera3D _camera;
    private Generator _generator;
    private Manager _manager;
    private MeshInstance3D _highlightMesh;

    public override void _Ready()
    {
        _generator = GetNode<Generator>("../Generator");
        _manager = GetNode<Manager>("../Manager");
        _head = GetNode<Node3D>("Head");
        _camera = GetNode<Camera3D>("Head/Camera3D");

        _highlightMesh = new MeshInstance3D
        {
            Mesh = new BoxMesh(),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1, 1, 0, 0.3f)
            },
            Visible = false
        };
        AddChild(_highlightMesh);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && !_manager.paused)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity * 0.001f);
            _head.RotateObjectLocal(Vector3.Right, -mouseMotion.Relative.Y * MouseSensitivity * 0.001f);
            var rotation = _head.Rotation;
            rotation.X = Mathf.Clamp(rotation.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
            _head.Rotation = rotation;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(_manager.paused) return;
        UpdateHighlight();

        Vector3 velocity = Velocity;
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * Speed;

        velocity.X = direction.X;
        velocity.Z = direction.Z;
        velocity.Y = Input.IsActionPressed("jump") ? Speed : (Input.IsActionPressed("crouch") ? -Speed : 0);

        Velocity = velocity;
        MoveAndSlide();
    }

    private void UpdateHighlight()
    {
        Vector3 rayOrigin = _camera.GlobalTransform.Origin;
        Vector3 rayDirection = -_camera.GlobalTransform.Basis.Z.Normalized();
        float currentDistance = 0f;

        while (currentDistance < InteractionRange)
        {
            Vector3 currentPosition = rayOrigin + rayDirection * currentDistance;
            Vector3 gridPosition = currentPosition.Snapped(new Vector3(
                1.0f / _generator.Resolution,
                1.0f / _generator.Resolution,
                1.0f / _generator.Resolution
            ));

            if (_generator.IsBlockAt(gridPosition))
            {
                _highlightMesh.Visible = true;
                _highlightMesh.GlobalPosition = gridPosition;
                return;
            }

            currentDistance += 0.1f;
        }

        _highlightMesh.Visible = false;
    }

    private Vector3 GetHitNormal()
    {
        Vector3 rayDirection = -_camera.GlobalTransform.Basis.Z.Normalized();
        return rayDirection.Round();
    }
}
