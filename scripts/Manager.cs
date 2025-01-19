using Godot;

public partial class Manager : Node
{
    [Export] VBoxContainer menu;
    public bool paused = false;
    private bool isTransitioning = false;
    private float digitalValue = 0f;
    private float elapsedTime = 0f;
    private float duration = 1.0f;
    private float startValue = 0f;
    private float targetValue = 0f;

    public override void _Ready()
    {
        Update();
    }

    public void Toggle()
    {
        paused = !paused;
        Update();
    }

    private void Update()
    {
        if (menu != null)
        {
            menu.Visible = paused;
        }
        Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        CheckButton();
    }

    private void CheckButton()
    {
        if (paused)
        {
            if (menu.GetChild<Button>(0).ButtonPressed) Toggle();
            if (menu.GetChild<Button>(1).ButtonPressed) GetTree().Quit();
        }
        if (Input.IsActionJustPressed("pause")) Toggle();
    }
}