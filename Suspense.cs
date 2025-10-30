using Godot;
using System;

public partial class Suspense : AudioStreamPlayer
{
    [Export]
    public Minesweeper Minesweeper { get; set; }

    public override void _PhysicsProcess(double delta)
    {
        double targetVolume = 0.0;
        switch (Minesweeper.GetPlayerDistFromApparition())
        {
            case 0:
            case 1:
                targetVolume = 1.0;
                break;
            case 2:
                targetVolume = 0.4;
                break;
            case 3:
                targetVolume = 0.2;
                break;
            case 4:
                targetVolume = 0.1;
                break;
        }
        VolumeLinear = (float)Mathf.MoveToward(VolumeLinear, targetVolume, delta * 2.0);
    }
}
