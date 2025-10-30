using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using static Minesweeper;

public partial class Minesweeper : TileMapLayer
{
    public enum Tile
    {
        None,
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Block,
        Flag
    }

    public const int SizeX = 24;
    public const int SizeY = 16;
    public const int InitialSafeRadius = 2;

    [Signal]
    public delegate void OnWinEventHandler();
    [Signal]
    public delegate void OnLoseEventHandler();
    [Signal]
    public delegate void OnPostWinEventHandler();

    [Export]
    public int NumMines { get; set; } = 60;

    [Export]
    public Sprite2D PlayerSprite { get; set; }

    [Export]
    public AudioStreamPlayer RevealSoundPlayer { get; set; }

    [Export]
    public AudioStreamPlayer FlagSoundPlayer { get; set; }

    [Export]
    public AudioStreamPlayer VictorySoundPlayer { get; set; }

    [Export]
    public double ApparitionMoveTime { get; set; } = 2.0;

    public int NumFlags
    {
        get
        {
            return _numFlags;
        }
    }

    public Vector2I ApparitionCell
    {
        get { return _apparitionCell; }
        set
        {
            Vector2I oldValue = _apparitionCell;
            _apparitionCell = value;
            RefreshApparitionTile(oldValue);
            RefreshApparitionTile(_apparitionCell);
        }
    }

    public Vector2I PlayerCell
    {
        get { return _playerCell; }
    }

    private bool[] _minefield;
    private int _numTiles;
    private int _numFlags;
    private Vector2I _playerCell = Vector2I.MinValue;
    private Vector2I _apparitionCell = Vector2I.MinValue;
    private double _apparitionTimer;
    private bool _hasWon = false;
    private bool _ignoreInput = false;

    public override void _Ready()
    {
        Reset();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_minefield == null || _hasWon)
            return;

        _apparitionTimer -= delta;
        if (_apparitionTimer <= 0.0)
        {
            _apparitionTimer = ApparitionMoveTime;
            if (GetPlayerDistFromApparition() <= 1)
                Lose();
            else
                ApparitionCell = new Vector2I(GD.RandRange(0, SizeX - 1), GD.RandRange(0, SizeY - 1));
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_ignoreInput) return;

        if (@event is InputEventMouseButton buttonEvent)
        {
            if (buttonEvent.Pressed)
            {
                Vector2 localPos = ToLocal(buttonEvent.GlobalPosition);
                Vector2I cellPos = LocalToMap(localPos);
                if (cellPos.X >= 0 && cellPos.Y >= 0 && cellPos.X < SizeX && cellPos.Y < SizeY)
                {
                    if (buttonEvent.ButtonIndex == MouseButton.Left)
                        RevealCell(cellPos);
                    else if (buttonEvent.ButtonIndex == MouseButton.Right)
                        ToggleFlag(cellPos);
                }
            }
        }
    }

    public void Reset()
    {
        _minefield = null;
        _numTiles = SizeX * SizeY;
        _numFlags = 0;
        _playerCell = Vector2I.MinValue;
        _apparitionCell = Vector2I.MinValue;
        _apparitionTimer = ApparitionMoveTime;
        PlayerSprite.Visible = false;

        for (int x = 0; x < SizeX; x++)
        {
            for (int y = 0; y < SizeY; y++)
            {
                SetTile(new Vector2I(x, y), Tile.Block);
            }
        }
    }

    // Generates minefield that has a safe area around safeCell
    public void GenerateMinefield(Vector2I safeCell)
    {
        _minefield = new bool[SizeX * SizeY];
        for (int i = 0; i < NumMines; i++)
        {
            bool placedMine = false;
            while (placedMine == false)
            {
                Vector2I pos = new(GD.RandRange(0, SizeX - 1), GD.RandRange(0, SizeY - 1));

                Vector2I diff = (pos - safeCell).Abs();
                if (diff.X < InitialSafeRadius && diff.Y < InitialSafeRadius)
                {
                    // This mine would be too close to the initial click
                    continue;
                }

                if (IsMineAt(pos))
                {
                    // There is already a mine in this location
                    continue;
                }

                PlaceMineAt(pos);
                placedMine = true;
            }
        }
    }

    public void SetTile(Vector2I cell, Tile tile)
    {
        SetCell(cell, 0, new Vector2I((int)tile, 0));
    }

    public Tile GetTile(Vector2I cell)
    {
        return (Tile)GetCellAtlasCoords(cell).X;
    }

    private int CellToIndex(Vector2I cell)
    {
        return cell.X * SizeY + cell.Y;
    }

    private Vector2I IndexToCell(int index)
    {
        return new Vector2I(index / SizeY, index % SizeY);
    }

    public void PlaceMineAt(Vector2I cell)
    {
        _minefield[CellToIndex(cell)] = true;
    }

    public bool IsMineAt(Vector2I cell)
    {
        return _minefield[CellToIndex(cell)];
    }

    public int GetNumAdjacentMines(Vector2I cell)
    {
        int adjMines = 0;
        for (int xOff = -1; xOff <= 1; xOff++)
        {
            for (int yOff = -1; yOff <= 1; yOff++)
            {
                if (xOff == 0 && yOff == 0) continue;
                if (cell.X + xOff < 0 || cell.X + xOff >= SizeX) continue;
                if (cell.Y + yOff < 0 || cell.Y + yOff >= SizeY) continue;
                if (IsMineAt(new Vector2I(cell.X + xOff, cell.Y + yOff)))
                {
                    adjMines++;
                }
            }
        }

        return adjMines;
    }

    public void RevealCell(Vector2I cell)
    {
        if (_minefield == null)
        {
            // The minefield is not generated until after the first click
            GenerateMinefield(cell);
        }

        if (GetTile(cell) != Tile.Block)
        {
            return;
        }

        if (_hasWon)
        {
            EmitSignal(SignalName.OnPostWin);
            _ignoreInput = true;
            return;
        }

        if (IsMineAt(cell))
        {
            Lose();
            return;
        }

        _playerCell = cell;
        PlayerSprite.Position = Position + (Vector2)(cell * 32) + new Vector2(16, 16);
        PlayerSprite.Visible = true;

        RevealSoundPlayer.Play();

        Queue<Vector2I> queue = new();
        queue.Enqueue(cell);
        while (queue.Count > 0)
        {
            Vector2I currCell = queue.Dequeue();
            if ((int)GetTile(currCell) < (int)Tile.Block)
            {
                continue;
            }

            int adjMines = GetNumAdjacentMines(currCell);
            if (GetTile(currCell) == Tile.Flag)
            {
                // This tile is being destroyed, so the flag count must be updated
                _numFlags--;
            }
            SetTile(currCell, (Tile)adjMines);

            _numTiles--;

            if (adjMines > 0)
            {
                continue;
            }
            else
            {
                for (int xOff = -1; xOff <= 1; xOff++)
                {
                    for (int yOff = -1; yOff <= 1; yOff++)
                    {
                        if (xOff == 0 && yOff == 0) continue;
                        if (currCell.X + xOff < 0 || currCell.X + xOff >= SizeX) continue;
                        if (currCell.Y + yOff < 0 || currCell.Y + yOff >= SizeY) continue;
                        queue.Enqueue(currCell + new Vector2I(xOff, yOff));
                    }
                }
            }
        }

        if (_numTiles == NumMines)
        {
            Win();
        }
    }

    public void ToggleFlag(Vector2I cell)
    {
        Tile tile = GetTile(cell);
        if ((int)tile < (int)Tile.Block) return;

        if (tile == Tile.Block)
        {
            tile = Tile.Flag;
            _numFlags++;
        }
        else
        {
            tile = Tile.Block;
            _numFlags--;
        }

        FlagSoundPlayer.Play();
        SetTile(cell, tile);
    }

    public void RefreshApparitionTile(Vector2I cell)
    {
        if (cell == Vector2I.MinValue) return;

        for (int xOff = -1; xOff <= 1; xOff++)
        {
            for (int yOff = -1; yOff <= 1; yOff++)
            {
                if (xOff == 0 && yOff == 0) continue;
                if (cell.X + xOff < 0 || cell.X + xOff >= SizeX) continue;
                if (cell.Y + yOff < 0 || cell.Y + yOff >= SizeY) continue;

                Vector2I adjCell = cell + new Vector2I(xOff, yOff);
                if ((int)GetTile(adjCell) < (int)Tile.Block)
                {
                    int adjMines = GetNumAdjacentMines(adjCell);
                    if (cell == _apparitionCell)
                        adjMines = Mathf.Min(adjMines + 4, 8);

                    SetTile(adjCell, (Tile)adjMines);
                }
            }
        }
    }

    private void Win()
    {
        EmitSignal(SignalName.OnWin);
        SetPattern(Vector2I.Zero, TileSet.GetPattern(0));
        _hasWon = true;
        ApparitionCell = Vector2I.MinValue;
        PlayerSprite.Visible = false;
        VictorySoundPlayer.Play();
    }

    private void Lose()
    {
        EmitSignal(SignalName.OnLose);
        Reset();
    }

    public int GetPlayerDistFromApparition()
    {
        if (_playerCell == Vector2I.MinValue || _apparitionCell == Vector2I.MinValue)
        {
            return int.MaxValue;
        }
        Vector2I diff = (_playerCell - _apparitionCell).Abs();
        return Mathf.Max(diff.X, diff.Y);
    }
}
