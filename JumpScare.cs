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
    public AudioStreamPlayer SoundPlayer { get; set; }

    private bool _shouldQuit = false;

    public override void _Ready()
    {
        Minesweeper.OnLose += Scare;
        Minesweeper.OnPostWin += () =>
        {
            _shouldQuit = true;
            Scare();
        };
    }

    public void Scare()
    {
        CurrentScreen = DisplayServer.Singleton.GetPrimaryScreen();
        Size = DisplayServer.Singleton.ScreenGetSize();
        Size += new Vector2I(0, 1); // This is needed to actually have a transparent BG for some reason
        Position = DisplayServer.Singleton.ScreenGetPosition();


        SoundPlayer.Play();
        Show();
        GrabFocus();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().CreateTimer(DisplayTime).Timeout += () =>
        {
            Hide();
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (_shouldQuit == true)
            {
                GetTree().Quit();
            }
        };
    }
}
