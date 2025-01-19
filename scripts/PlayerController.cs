using Godot;
public partial class PlayerController : CharacterBody3D {
    [Export] public float MouseSensitivity = 0.5f;
    private float Speed = 50f;
    private Node3D _head;
    private Camera3D _camera;
    private Manager _manager;

    public override void _Ready() {
        _manager = GetNode<Manager>("../Manager");
        _head = GetNode<Node3D>("Head");
        _camera = GetNode<Camera3D>("Head/Camera3D");
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseMotion && !_manager.paused) {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity * 0.001f);
            _head.RotateObjectLocal(Vector3.Right, -mouseMotion.Relative.Y * MouseSensitivity * 0.001f);
            var rotation = _head.Rotation;
            rotation.X = Mathf.Clamp(rotation.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
            _head.Rotation = rotation;
        }
    }

    public override void _PhysicsProcess(double delta) {
        Vector3 velocity = Velocity;
        
        if (Input.IsActionPressed("jump")) {
            velocity.Y = Speed;
        } else if(Input.IsActionPressed("crouch")) {
            velocity.Y = -Speed;
        } else {
            velocity.Y = 0;
        }
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero) {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        } else {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }
        Velocity = velocity;
        MoveAndSlide();
    }
}
