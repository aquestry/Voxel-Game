using Godot;

public partial class PlayerController : CharacterBody3D
{

    [Export] public float MouseSensitivity = 0.5f;
    public float InteractionRange = 100f;
    public float Speed = 15f;
    private Node3D _head;
    private Camera3D _camera;
    private Generator _generator;
    private Manager _manager;
    private Vector3I _highlightedBlockPos = new Vector3I(-1, -1, -1);
    private Vector3I _placeBlockPos = new Vector3I(-1, -1, -1);

    public override void _Ready()
    {
        _generator = GetNode<Generator>("../Generator");
        _manager = GetNode<Manager>("../Manager");
        _head = GetNode<Node3D>("Head");
        _camera = GetNode<Camera3D>("Head/Camera3D");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && !_manager.paused)
        {
            RotateY(-motion.Relative.X * MouseSensitivity * 0.001f);
            _head.RotateObjectLocal(Vector3.Right, -motion.Relative.Y * MouseSensitivity * 0.001f);
            var rot = _head.Rotation;
            rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
            _head.Rotation = rot;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(_manager.paused) return;
        if (_generator == null || _generator.Chunks == null)
            return;
        UpdateHighlight();
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * Speed;
        Vector3 v = Velocity;
        v.X = moveDir.X;
        v.Z = moveDir.Z;
        if (Input.IsActionPressed("jump"))
            v.Y = Speed;
        else if (Input.IsActionPressed("crouch"))
            v.Y = -Speed;
        else
            v.Y = 0;
        Velocity = v;
        MoveAndSlide();
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
        _highlightedBlockPos = new Vector3I(-1, -1, -1);
        _placeBlockPos = new Vector3I(-1, -1, -1);
        Vector3I lastEmptyPos = new Vector3I(-1, -1, -1);
        while (dist < InteractionRange)
        {
            Vector3 checkPos = origin + direction * dist;
            int bx = Mathf.FloorToInt(checkPos.X);
            int by = Mathf.FloorToInt(checkPos.Y);
            int bz = Mathf.FloorToInt(checkPos.Z);
            if (_generator.GetBlock(bx, by, bz) == 1)
            {
                _highlightedBlockPos = new Vector3I(bx, by, bz);
                _placeBlockPos = lastEmptyPos;
                return;
            }
            else
            {
                lastEmptyPos = new Vector3I(bx, by, bz);
            }
            dist += step;
        }
    }

    private void BreakBlock()
    {
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
        if (_highlightedBlockPos.X < 0 || _placeBlockPos.X < 0) return;
        var c = new Vector3(_placeBlockPos.X + 0.5f, _placeBlockPos.Y + 0.5f, _placeBlockPos.Z + 0.5f);
        var box = new BoxShape3D { Size = Vector3.One };
        var q = new PhysicsShapeQueryParameters3D
        {
            Shape = box,
            Transform = new Transform3D(Basis.Identity, c),
            CollideWithBodies = true,
            CollideWithAreas = true
        };
        if (GetWorld3D().DirectSpaceState.IntersectShape(q).Count > 0) return;
        _generator.SetBlock(_placeBlockPos.X, _placeBlockPos.Y, _placeBlockPos.Z, 1);
    }

    private Vector3I GetDominantAxisOffset(Vector3 direction)
    {
        float absX = Mathf.Abs(direction.X);
        float absY = Mathf.Abs(direction.Y);
        float absZ = Mathf.Abs(direction.Z);
        if (absX >= absY && absX >= absZ)
        {
            return new Vector3I(direction.X > 0 ? 1 : -1, 0, 0);
        }
        else if (absY >= absZ)
        {
            return new Vector3I(0, direction.Y > 0 ? 1 : -1, 0);
        }
        else
        {
            return new Vector3I(0, 0, direction.Z > 0 ? 1 : -1);
        }
    }
}