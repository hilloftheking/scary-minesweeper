using Godot;
using System;
using System.Threading.Tasks;

public partial class JumpScare : Window
{
    [Export]
    public double DisplayTime { get; set; } = 2.0;

    [Export]
    public Minesweeper Minesweeper { get; set; }

    [Export]
    public TextureRect MineTextureRect { get; set; }

    [Export]
    public AudioStreamPlayer SoundPlayer { get; set; }

    private bool _shouldQuit = false;

    public override void _Ready()
    {
        Minesweeper.OnLose += Scare;
        Minesweeper.OnPostWin += (Vector2I mineCell) =>
        {
            _shouldQuit = true;
            Scare(mineCell);
        };
    }

    public void Scare(Vector2I mineCell)
    {
        CurrentScreen = GetTree().Root.CurrentScreen;
        Size = DisplayServer.Singleton.ScreenGetSize(CurrentScreen);
        Size -= new Vector2I(0, 1); // This is needed to actually have a transparent BG for some reason
        Show(); // Window must be shown before it can be moved over the taskbar
        Position = DisplayServer.Singleton.ScreenGetPosition(CurrentScreen);

        Vector2 viewportMinePos = Minesweeper.ToGlobal(Minesweeper.MapToLocal(mineCell));
        Transform2D viewportToScreen = GetTree().Root.GetScreenTransform();
        Vector2 minePos = GetScreenTransform().Inverse() * (viewportToScreen.Inverse() * GetTree().Root.Position +
            viewportToScreen * viewportMinePos);

        Vector2 initialSize = new(8, 8);
        Vector2 finalSize = Size;
        MineTextureRect.Size = initialSize;
        MineTextureRect.Position = minePos - initialSize * 0.5f;

        Tween t = MineTextureRect.CreateTween();
        t.SetParallel(true);
        t.SetTrans(Tween.TransitionType.Elastic);
        t.SetEase(Tween.EaseType.Out);
        t.TweenProperty(MineTextureRect, "size", finalSize, DisplayTime);
        t.TweenProperty(MineTextureRect, "position", minePos - finalSize * 0.5f, DisplayTime);

        SoundPlayer.Play();
        GrabFocus();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().CreateTimer(DisplayTime).Timeout += () =>
        {
            Hide();
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (_shouldQuit == true)
                GetTree().Quit();
            else
                Minesweeper.Reset();
        };
    }
}
