using System.Numerics;
using NiziKit.UI.Widgets;
using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

// The battle map, drawn entirely with UI elements: terrain rectangles, grid lines and round tokens. Drag the floor to pan,
// the wheel to zoom, drag tokens to move them (they snap to squares and show the distance), right-click for menus,
// double-click the floor to ping.
public sealed class MapView
{
    private const float BaseCell = 46;

    private static readonly UIColor Void = UIColor.Hex(0x0C0D10FF);
    private static readonly UIColor Floor = UIColor.Hex(0x2A2926FF);
    private static readonly UIColor GridLine = UIColor.Hex(0xFFFFFF16);
    private static readonly UIColor Wall = UIColor.Hex(0x4B4741FF);
    private static readonly UIColor WallEdge = UIColor.Hex(0x67615AFF);
    private static readonly UIColor Water = UIColor.Hex(0x1F4F78FF);
    private static readonly UIColor Rubble = UIColor.Hex(0x3E362DFF);
    private static readonly UIColor Carpet = UIColor.Hex(0x5E2633C8);
    private static readonly UIColor Altar = UIColor.Hex(0x2F5D4AFF);

    private readonly Campaign _campaign;
    private readonly uint _viewportId = Ui.Id("Map.Viewport");
    private readonly uint _contentId = Ui.Id("Map.Content");
    private readonly uint _pingId = Ui.Id("Map.Ping");
    private readonly Menu _tokenMenu = new();
    private readonly Menu _floorMenu = new();
    private readonly List<Character> _drawOrder = new();
    private readonly List<(uint Id, Vector2 Point, float Age, UIColor Color)> _pings = new();

    private Vector2 _pan = new(330, 30);
    private float _zoom = 1;
    private Character? _dragToken;
    private Vector2 _dragDelta;
    private bool _pressOnToken;
    private bool _panning;
    private Vector2 _panStart;
    private uint _nextPing;
    private UIRect _viewport;
    private float _zoomedAt = -1;

    public MapView(Campaign campaign)
    {
        _campaign = campaign;
    }

    public Character? Selected { get; set; }
    public bool ShowGrid { get; set; } = true;
    public bool IsHovered { get; private set; }
    public float Zoom => _zoom;

    public event Action<Character>? OpenSheet;

    public void ZoomAt(Vector2 pointer, float steps)
    {
        var target = Math.Clamp(_zoom * MathF.Pow(1.12f, steps), 0.4f, 2.5f);
        var local = pointer - new Vector2(_viewport.X, _viewport.Y);
        _pan = local - (local - _pan) * (target / _zoom);
        _zoom = target;
        _zoomedAt = Ui.Time;
    }

    public void ZoomBy(float steps)
    {
        ZoomAt(new Vector2(_viewport.X + _viewport.Width * 0.5f, _viewport.Y + _viewport.Height * 0.5f), steps);
    }

    public void ResetView()
    {
        _zoom = 1;
        _pan = new Vector2(330, 30);
        _zoomedAt = Ui.Time;
    }

    public void CenterOn(Character character)
    {
        var cell = BaseCell * _zoom;
        var center = new Vector2((character.CellX + character.SizeCells * 0.5f) * cell, (character.CellY + character.SizeCells * 0.5f) * cell);
        _pan = new Vector2(_viewport.Width * 0.5f, _viewport.Height * 0.5f) - center;
    }

    public void Ping(Vector2 mapPoint, UIColor color)
    {
        _pings.Add((Ui.Id(_pingId, ++_nextPing), mapPoint, 0, color));
    }

    public void Draw(float dt)
    {
        var theme = Ui.Theme;
        var cell = BaseCell * _zoom;
        var view = Ui.Interaction(_viewportId);
        _viewport = view.Rect;
        IsHovered = view.Hovered;

        var tokenPressStarted = false;
        var tokenRightClicked = false;
        foreach (var token in _campaign.Characters)
        {
            var interaction = Ui.Interaction(token.TokenId);
            if (interaction.PressStarted)
            {
                tokenPressStarted = true;
                Selected = token;
                _dragToken = token;
                _dragDelta = Vector2.Zero;
            }

            if (ReferenceEquals(token, _dragToken))
            {
                if (interaction.Dragging)
                {
                    _dragDelta = interaction.DragDelta;
                }

                if (interaction.DragEnded || (!interaction.Pressed && !interaction.PressStarted))
                {
                    if (_dragDelta != Vector2.Zero)
                    {
                        Drop(token, cell);
                    }

                    _dragToken = null;
                    _dragDelta = Vector2.Zero;
                }
            }

            if (interaction.DoubleClicked)
            {
                OpenSheet?.Invoke(token);
            }

            if (interaction.RightClicked)
            {
                tokenRightClicked = true;
                Selected = token;
                OpenTokenMenu(token);
            }

            if (interaction.Hovered && !interaction.Pressed)
            {
                Ui.Tooltip(token.TokenId, token.Faction == Faction.Party
                    ? $"{token.Name} · {token.Hp}/{token.MaxHp} HP · AC {token.Ac}"
                    : $"{token.Name} · {HealthWord(token)}");
            }
        }

        if (view.PressStarted)
        {
            _pressOnToken = tokenPressStarted;
            _panStart = _pan;
        }

        _panning = view.Dragging && !_pressOnToken;
        if (_panning)
        {
            _pan = _panStart + view.DragDelta;
        }

        if (!_pressOnToken && view.Clicked)
        {
            Selected = null;
        }

        if (!_pressOnToken && view.DoubleClicked)
        {
            Ping(ToMap(Ui.PointerPosition, cell), Campaign.GmColor);
        }

        if (view.RightClicked && !tokenRightClicked)
        {
            OpenFloorMenu(ToMap(Ui.PointerPosition, cell));
        }

        using (Ui.Element(_viewportId)
                   .Absolute(0, 0, 0, 0)
                   .Clip()
                   .Background(Void)
                   .Cursor(_panning ? SystemCursor.Move : SystemCursor.Default)
                   .Open())
        {
            using (Ui.Element(_contentId)
                       .Absolute(left: _pan.X, top: _pan.Y)
                       .Size(_campaign.Columns * cell, _campaign.Rows * cell)
                       .Background(Floor)
                       .CornerRadius(4)
                       .Open())
            {
                DrawTerrain(cell);
                if (ShowGrid)
                {
                    DrawGrid(cell);
                }

                DrawDragGhost(cell);
                DrawPings(dt);
                DrawTokens(cell, theme);
            }
        }
    }

    private static string HealthWord(Character token)
    {
        var fraction = token.Hp / (float)token.MaxHp;
        return token.IsDown ? "down" : fraction > 0.75f ? "unhurt" : fraction > 0.4f ? "bloodied" : "badly wounded";
    }

    private Vector2 ToMap(Vector2 pointer, float cell)
    {
        return (pointer - new Vector2(_viewport.X, _viewport.Y) - _pan) / cell;
    }

    private (int X, int Y) SnapTarget(Character token, float cell)
    {
        var x = (int)MathF.Round(token.CellX + _dragDelta.X / cell);
        var y = (int)MathF.Round(token.CellY + _dragDelta.Y / cell);
        return (Math.Clamp(x, 0, _campaign.Columns - token.SizeCells), Math.Clamp(y, 0, _campaign.Rows - token.SizeCells));
    }

    // Pathfinder-style diagonals: every second diagonal costs 10 ft.
    private static int Feet(int dx, int dy)
    {
        dx = Math.Abs(dx);
        dy = Math.Abs(dy);
        var diagonal = Math.Min(dx, dy);
        return 5 * (Math.Max(dx, dy) - diagonal + diagonal + diagonal / 2);
    }

    private void Drop(Character token, float cell)
    {
        var (x, y) = SnapTarget(token, cell);
        if (x == token.CellX && y == token.CellY)
        {
            return;
        }

        var feet = Feet(x - token.CellX, y - token.CellY);
        token.CellX = x;
        token.CellY = y;
        _campaign.Chat.System($"{token.Name} moves {feet} ft.");
    }

    private void DrawTerrain(float cell)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(Ui.Time * 1.3f);
        foreach (var t in _campaign.Terrain)
        {
            var element = Ui.Element()
                .Absolute(left: t.X * cell, top: t.Y * cell)
                .Size(t.Width * cell, t.Height * cell)
                .Center()
                .Passthrough();
            switch (t.Kind)
            {
                case TerrainKind.Wall:
                    element.Background(Wall).Border(WallEdge, 1);
                    break;
                case TerrainKind.Water:
                    element.Background(Im.Mix(Water, UIColor.Hex(0x2A6597FF), pulse * 0.35f)).Border(UIColor.Hex(0x3A78A8A0), 1).CornerRadius(cell * 0.3f);
                    break;
                case TerrainKind.Pillar:
                    element.Background(WallEdge).Border(UIColor.Hex(0x8A8278FF), 2).CornerRadius(cell * 0.5f);
                    break;
                case TerrainKind.Rubble:
                    element.Background(Rubble).Border(UIColor.Hex(0x57493AFF), 1).CornerRadius(cell * 0.2f);
                    break;
                case TerrainKind.Carpet:
                    element.Background(Carpet);
                    break;
                case TerrainKind.Altar:
                    element.Background(Altar).Border(UIColor.Hex(0x5E9C80FF), 2).CornerRadius(4);
                    break;
            }

            using (element.Open())
            {
                if (t.Label != null && _zoom > 0.6f)
                {
                    var desc = Ui.TextStyle(11 * _zoom, UIColor.Hex(0xFFFFFF80));
                    desc.Align = UITextAlign.Center;
                    Ui.Text(t.Label, in desc);
                }
            }
        }
    }

    private void DrawGrid(float cell)
    {
        var width = _campaign.Columns * cell;
        var height = _campaign.Rows * cell;
        for (var x = 1; x < _campaign.Columns; x++)
        {
            Ui.Element().Absolute(left: x * cell, top: 0).Size(1, height).Background(GridLine).Passthrough().Leaf();
        }

        for (var y = 1; y < _campaign.Rows; y++)
        {
            Ui.Element().Absolute(left: 0, top: y * cell).Size(width, 1).Background(GridLine).Passthrough().Leaf();
        }
    }

    private void DrawDragGhost(float cell)
    {
        if (_dragToken is not { } token || _dragDelta == Vector2.Zero)
        {
            return;
        }

        var (x, y) = SnapTarget(token, cell);
        var size = token.SizeCells * cell;
        using (Ui.Element()
                   .Absolute(left: x * cell, top: y * cell)
                   .Size(size, size)
                   .Background(UIColor.Hex(0xFFFFFF1C))
                   .Border(UIColor.Hex(0xFFFFFFB0), 2)
                   .CornerRadius(6)
                   .Passthrough()
                   .Open())
        {
            using (Ui.Row().Absolute(left: -60, right: -60, top: size + 4).Justify(UIJustify.Center).Open())
            {
                using (Ui.Row().Padding(8, 3).Background(UIColor.Hex(0x000000C0)).CornerRadius(4).Open())
                {
                    Ui.Text($"{Feet(x - token.CellX, y - token.CellY)} ft", 12, Im.White);
                }
            }
        }
    }

    private void DrawPings(float dt)
    {
        for (var i = _pings.Count - 1; i >= 0; i--)
        {
            var ping = _pings[i];
            ping.Age += dt;
            if (ping.Age > 1.6f)
            {
                _pings.RemoveAt(i);
                continue;
            }

            _pings[i] = ping;
        }

        var cell = BaseCell * _zoom;
        foreach (var ping in _pings)
        {
            var size = 64 * _zoom;
            var center = ping.Point * cell;
            using (Ui.Element(ping.Id)
                       .Absolute(left: center.X - size * 0.5f, top: center.Y - size * 0.5f)
                       .Size(size, size)
                       .Center()
                       .CornerRadius(size * 0.5f)
                       .Border(ping.Color, 3)
                       .Background(Im.Alpha(ping.Color, 50))
                       .Passthrough()
                       .Animate(UITransitionPropertyFlagBits.None, 0.45f, UIEasing.EaseOutBack)
                       .Enter(UITransitionState.FromScale(0.2f).WithOpacity(0))
                       .Exit(UITransitionState.FromOpacity(0).WithScale(1.8f))
                       .Open())
            {
                Ui.Element().Size(10 * _zoom, 10 * _zoom).CornerRadius(5 * _zoom).Background(ping.Color).Leaf();
            }
        }
    }

    private void DrawTokens(float cell, Theme theme)
    {
        _drawOrder.Clear();
        foreach (var token in _campaign.Characters)
        {
            if (!ReferenceEquals(token, Selected) && !ReferenceEquals(token, _dragToken))
            {
                _drawOrder.Add(token);
            }
        }

        if (Selected != null && !ReferenceEquals(Selected, _dragToken))
        {
            _drawOrder.Add(Selected);
        }

        if (_dragToken != null)
        {
            _drawOrder.Add(_dragToken);
        }

        var current = _campaign.Current;
        var glow = (byte)(140 + 110 * (0.5f + 0.5f * MathF.Sin(Ui.Time * 4)));
        foreach (var token in _drawOrder)
        {
            var inset = 4 * _zoom;
            var size = token.SizeCells * cell - inset * 2;
            var position = new Vector2(token.CellX * cell + inset, token.CellY * cell + inset);
            var dragging = ReferenceEquals(token, _dragToken);
            if (dragging)
            {
                position += _dragDelta;
            }

            var selected = ReferenceEquals(token, Selected);
            var hovered = Ui.Interaction(token.TokenId).Hovered;
            var ring = ReferenceEquals(token, current) ? Im.Alpha(Campaign.GmColor, glow)
                : selected ? Im.White
                : token.Faction == Faction.Party ? Im.Alpha(Im.White, 120) : UIColor.Hex(0x2A0E0CFF);

            var element = Ui.Element(token.TokenId)
                .Absolute(left: position.X, top: position.Y)
                .Size(size, size)
                .Center()
                .Background(token.Color)
                .Border(ring, selected || ReferenceEquals(token, current) ? 3 : 2)
                .CornerRadius(size * 0.5f)
                .Overlay(token.IsDown ? UIColor.Hex(0x000000A8) : default)
                .Cursor(dragging ? SystemCursor.Move : SystemCursor.Pointer);
            if (!dragging && Ui.Time - _zoomedAt > 0.3f)
            {
                element.Animate(UITransitionPropertyFlagBits.Position | UITransitionPropertyFlagBits.OverlayColor, 0.18f);
            }

            using (element.Open())
            {
                if (token.IsDown)
                {
                    Im.Icon(FontAwesome.Xmark, size * 0.42f, Im.White);
                }
                else
                {
                    var desc = Ui.TextStyle(size * (token.SizeCells > 1 ? 0.26f : 0.36f), Im.White);
                    desc.Wrap = UITextWrap.None;
                    Ui.Text(token.Initials, in desc);
                }

                if (token.Conditions.Count > 0)
                {
                    using (Ui.Row().Absolute(right: -2, top: -2).Size(14 * _zoom, 14 * _zoom).Center().Background(Im.Warn).CornerRadius(7 * _zoom).Open())
                    {
                        Ui.Text(Fmt.Int(token.Conditions.Count), 9 * _zoom, UIColor.Hex(0x1A1A1AFF));
                    }
                }

                using (Ui.Row().Absolute(left: size * 0.12f, right: size * 0.12f, bottom: -6 * _zoom).Height(4 * _zoom).Open())
                {
                    using (Ui.Row().Fill().Background(UIColor.Hex(0x000000B0)).CornerRadius(2).Clip().Open())
                    {
                        var fraction = Math.Clamp(token.Hp / (float)token.MaxHp, 0, 1);
                        Ui.Element().Width(UIValue.Percent(fraction)).Background(Im.HpColor(fraction)).Animate(UITransitionPropertyFlagBits.Size, 0.3f).Leaf();
                    }
                }

                if (hovered || selected || dragging)
                {
                    using (Ui.Row().Absolute(left: -80, right: -80, top: size + 6 * _zoom).Justify(UIJustify.Center).Passthrough().Open())
                    {
                        using (Ui.Row().Padding(7, 2).Background(UIColor.Hex(0x000000C8)).CornerRadius(4).Open())
                        {
                            Im.Line(token.Name, 11, Im.White);
                        }
                    }
                }
            }
        }
    }

    private void OpenTokenMenu(Character token)
    {
        var menu = _tokenMenu;
        menu.ClearItems();
        menu.Add("Open character sheet", () => OpenSheet?.Invoke(token), FontAwesome.User, "dbl-click");
        menu.Add("Center view here", () => CenterOn(token), FontAwesome.Crosshairs);
        menu.AddSeparator();
        menu.Add("Damage 1", () => _campaign.Damage(token, 1), FontAwesome.Minus);
        menu.Add("Damage 5", () => _campaign.Damage(token, 5), FontAwesome.Minus);
        menu.Add("Damage 10", () => _campaign.Damage(token, 10), FontAwesome.Minus);
        menu.Add("Heal 5", () => _campaign.Heal(token, 5), FontAwesome.Plus);
        menu.Add("Heal to full", () => _campaign.Heal(token, token.MaxHp), FontAwesome.Heart);
        menu.AddSeparator();
        var prone = token.Conditions.Contains("Prone");
        menu.Add(prone ? "Stand up" : "Knock prone", () => _campaign.SetCondition(token, "Prone", !prone), FontAwesome.ArrowDown);
        if (token.Attacks.Count > 0)
        {
            var attack = token.Attacks[0];
            menu.Add($"Attack: {attack.Name}", () => _campaign.Roll(token.Name, Dice.WithModifier("1d20", attack.Bonus), $"{attack.Name} attack"), FontAwesome.Crosshairs);
        }

        menu.Add("Ping token", () => Ping(new Vector2(token.CellX + token.SizeCells * 0.5f, token.CellY + token.SizeCells * 0.5f), token.Color), FontAwesome.LocationCrosshairs);
        menu.ShowAt(Ui.PointerPosition);
    }

    private void OpenFloorMenu(Vector2 mapPoint)
    {
        var menu = _floorMenu;
        menu.ClearItems();
        menu.Add("Ping here", () => Ping(mapPoint, Campaign.GmColor), FontAwesome.LocationCrosshairs, "dbl-click");
        menu.Add(ShowGrid ? "Hide grid" : "Show grid", () => ShowGrid = !ShowGrid, FontAwesome.BorderAll, "G");
        menu.Add("Reset view", ResetView, FontAwesome.Compress);
        menu.ShowAt(Ui.PointerPosition);
    }
}
