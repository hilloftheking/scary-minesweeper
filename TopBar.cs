using Godot;
using System;

public partial class TopBar : Control
{
    [Export]
    public Minesweeper Minesweeper { get; set; }

    [Export]
    public Label MinesLabel { get; set; }

    [Export]
    public Label FlagsLabel { get; set; }

    private string _minesText;
    private string _flagsText;

    public override void _Ready()
    {
        _minesText = MinesLabel.Text;
        _flagsText = FlagsLabel.Text;
    }

    public override void _PhysicsProcess(double delta)
    {
        MinesLabel.Text = _minesText + Minesweeper.NumMines;
        FlagsLabel.Text = _flagsText + Minesweeper.NumFlags;
    }
}
