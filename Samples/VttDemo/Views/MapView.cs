using System.Numerics;
using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

// The battle map: a Canvas, so terrain, grid lines, pings and tokens are flat drawing calls in map coordinates.
// Input is handled in OnUpdate, before the frame is built: drag the floor to pan, drag tokens to move them (they snap to
// squares and show the distance), right-click for menus, double-click the floor to ping.
public sealed class MapView : Canvas
{
    private const float BaseCell = 46;

    private static readonly Color Void = Color.Hex(0x0C0D10FF);
    private static readonly Color Floor = Color.Hex(0x2A2926FF);
    private static readonly Color GridLine = Color.Hex(0xFFFFFF16);
    private static readonly Color Wall = Color.Hex(0x4B4741FF);
    private static readonly Color WallEdge = Color.Hex(0x67615AFF);
    private static readonly Color Water = Color.Hex(0x1F4F78FF);
    private static readonly Color Rubble = Color.Hex(0x3E362DFF);
    private static readonly Color Carpet = Color.Hex(0x5E2633C8);
    private static readonly Color Altar = Color.Hex(0x2F5D4AFF);
    private static readonly Color Shade = Color.Hex(0x000000C8);

    private readonly Campaign _campaign;
    private readonly List<Character> _drawOrder = new();
    private readonly List<(long Key, Vector2 Point, float Age, Color Color)> _pings = new();

    private Vector2 _pan = new(330, 30);
    private float _zoom = 1;
    private Character? _dragToken;
    private Vector2 _dragDelta;
    private bool _pressOnToken;
    private bool _panning;
    private Vector2 _panStart;
    private long _nextPing = 1L << 40;
    private Rectangle _viewport;
    private float _zoomedAt = -1;

    public MapView(Campaign campaign)
    {
        _campaign = campaign;
        Background = Void;
        OnUpdate = HandleInput;
    }

    public Character? Selected { get; set; }
    public bool ShowGrid { get; set; } = true;
    public float Zoom => _zoom;

    public event Action<Character>? OpenSheet;

    private float Cell => BaseCell * _zoom;

    public void ZoomAt(Vector2 pointer, float steps)
    {
        var target = Math.Clamp(_zoom * MathF.Pow(1.12f, steps), 0.4f, 2.5f);
        var local = pointer - _viewport.Position;
        _pan = local - (local - _pan) * (target / _zoom);
        _zoom = target;
        _zoomedAt = Ui.Time;
    }

    public void ZoomBy(float steps)
    {
        ZoomAt(_viewport.Center, steps);
    }

    public void ResetView()
    {
        _zoom = 1;
        _pan = new Vector2(330, 30);
        _zoomedAt = Ui.Time;
    }

    public void CenterOn(Character character)
    {
        var cell = Cell;
        var center = new Vector2((character.CellX + character.SizeCells * 0.5f) * cell, (character.CellY + character.SizeCells * 0.5f) * cell);
        _pan = _viewport.Size * 0.5f - center;
    }

    public void Ping(Vector2 mapPoint, Color color)
    {
        _pings.Add((++_nextPing, mapPoint, 0, color));
    }

    private void HandleInput()
    {
        var cell = Cell;
        var view = Input;
        _viewport = Bounds;

        var tokenPressStarted = false;
        var tokenRightClicked = false;
        foreach (var token in _campaign.Characters)
        {
            var hit = HitOf(token.Id);
            if (hit.PressStarted)
            {
                tokenPressStarted = true;
                Selected = token;
                _dragToken = token;
                _dragDelta = Vector2.Zero;
            }

            if (ReferenceEquals(token, _dragToken))
            {
                if (hit.Dragging)
                {
                    _dragDelta = hit.DragDelta;
                }

                if (hit.DragEnded || (!hit.Pressed && !hit.PressStarted))
                {
                    if (_dragDelta != Vector2.Zero)
                    {
                        Drop(token, cell);
                    }

                    _dragToken = null;
                    _dragDelta = Vector2.Zero;
                }
            }

            if (hit.DoubleClicked)
            {
                OpenSheet?.Invoke(token);
            }

            if (hit.RightClicked)
            {
                tokenRightClicked = true;
                Selected = token;
                TokenMenu(token).ShowAt(Ui.PointerPosition);
            }

            if (hit.Hovered && !hit.Pressed)
            {
                ShowTooltip(token.Id, token.Faction == Faction.Party
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
            FloorMenu(ToMap(Ui.PointerPosition, cell)).ShowAt(Ui.PointerPosition);
        }

        Cursor = _panning ? SystemCursor.Move : SystemCursor.Default;
        for (var i = _pings.Count - 1; i >= 0; i--)
        {
            var ping = _pings[i];
            ping.Age += Ui.DeltaTime;
            if (ping.Age > 1.6f)
            {
                _pings.RemoveAt(i);
                continue;
            }

            _pings[i] = ping;
        }
    }

    protected override void Paint(Painter p)
    {
        var cell = Cell;
        p.Box(new Rectangle(_pan.X, _pan.Y, _campaign.Columns * cell, _campaign.Rows * cell), new Paint { Background = Floor, Radius = 4 });
        PaintTerrain(p, cell);
        if (ShowGrid)
        {
            PaintGrid(p, cell);
        }

        PaintDragGhost(p, cell);
        PaintPings(p, cell);
        PaintTokens(p, cell);
    }

    private static string HealthWord(Character token)
    {
        var fraction = token.Hp / (float)token.MaxHp;
        return token.IsDown ? "down" : fraction > 0.75f ? "unhurt" : fraction > 0.4f ? "bloodied" : "badly wounded";
    }

    private Vector2 ToMap(Vector2 pointer, float cell)
    {
        return (pointer - _viewport.Position - _pan) / cell;
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

    private void PaintTerrain(Painter p, float cell)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(Ui.Time * 1.3f);
        foreach (var t in _campaign.Terrain)
        {
            var paint = t.Kind switch
            {
                TerrainKind.Wall => new Paint { Background = Wall, Border = WallEdge },
                TerrainKind.Water => new Paint { Background = Color.Mix(Water, Color.Hex(0x2A6597FF), pulse * 0.35f), Border = Color.Hex(0x3A78A8A0), Radius = cell * 0.3f },
                TerrainKind.Pillar => new Paint { Background = WallEdge, Border = Color.Hex(0x8A8278FF), BorderWidth = 2, Radius = cell * 0.5f },
                TerrainKind.Rubble => new Paint { Background = Rubble, Border = Color.Hex(0x57493AFF), Radius = cell * 0.2f },
                TerrainKind.Carpet => new Paint { Background = Carpet },
                _ => new Paint { Background = Altar, Border = Color.Hex(0x5E9C80FF), BorderWidth = 2, Radius = 4 }
            };
            if (t.Label != null && _zoom > 0.6f)
            {
                paint.Text = t.Label;
                paint.TextStyle = TextStyle.Sized(11 * _zoom, Color.Hex(0xFFFFFF80));
            }

            p.Box(new Rectangle(_pan.X + t.X * cell, _pan.Y + t.Y * cell, t.Width * cell, t.Height * cell), paint);
        }
    }

    private void PaintGrid(Painter p, float cell)
    {
        var width = _campaign.Columns * cell;
        var height = _campaign.Rows * cell;
        var line = new Paint { Background = GridLine };
        for (var x = 1; x < _campaign.Columns; x++)
        {
            p.Box(new Rectangle(_pan.X + x * cell, _pan.Y, 1, height), line);
        }

        for (var y = 1; y < _campaign.Rows; y++)
        {
            p.Box(new Rectangle(_pan.X, _pan.Y + y * cell, width, 1), line);
        }
    }

    private void PaintDragGhost(Painter p, float cell)
    {
        if (_dragToken is not { } token || _dragDelta == Vector2.Zero)
        {
            return;
        }

        var (x, y) = SnapTarget(token, cell);
        var size = token.SizeCells * cell;
        var origin = _pan + new Vector2(x * cell, y * cell);
        p.Box(new Rectangle(origin.X, origin.Y, size, size), new Paint { Background = Color.Hex(0xFFFFFF1C), Border = Color.Hex(0xFFFFFFB0), BorderWidth = 2, Radius = 6 });
        var feet = Fmt.Int(Feet(x - token.CellX, y - token.CellY));
        Tag(p, $"{feet} ft", origin + new Vector2(size * 0.5f, size + 4), 12);
    }

    private void PaintPings(Painter p, float cell)
    {
        foreach (var ping in _pings)
        {
            var size = 64 * _zoom;
            var center = _pan + ping.Point * cell;
            p.Box(new Rectangle(center.X - size * 0.5f, center.Y - size * 0.5f, size, size), new Paint
            {
                Background = ping.Color.WithAlpha(50),
                Border = ping.Color,
                BorderWidth = 3,
                Radius = size * 0.5f,
                Duration = 0.45f,
                Easing = UIEasing.EaseOutBack,
                Enter = UITransitionState.FromScale(0.2f).WithOpacity(0),
                Exit = UITransitionState.FromOpacity(0).WithScale(1.8f)
            }, ping.Key);
            var dot = 10 * _zoom;
            p.Box(new Rectangle(center.X - dot * 0.5f, center.Y - dot * 0.5f, dot, dot), new Paint { Background = ping.Color, Radius = dot * 0.5f });
        }
    }

    private void PaintTokens(Painter p, float cell)
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
        var settled = Ui.Time - _zoomedAt > 0.3f;
        foreach (var token in _drawOrder)
        {
            var inset = 4 * _zoom;
            var size = token.SizeCells * cell - inset * 2;
            var position = _pan + new Vector2(token.CellX * cell + inset, token.CellY * cell + inset);
            var dragging = ReferenceEquals(token, _dragToken);
            if (dragging)
            {
                position += _dragDelta;
            }

            var selected = ReferenceEquals(token, Selected);
            var isCurrent = ReferenceEquals(token, current);
            var ring = isCurrent ? ((Color)Campaign.GmColor).WithAlpha(glow)
                : selected ? Palette.White
                : token.Faction == Faction.Party ? Palette.White.WithAlpha(120) : Color.Hex(0x2A0E0CFF);
            var motion = !dragging && settled ? UITransitionPropertyFlagBits.Position : UITransitionPropertyFlagBits.None;

            var hit = p.Box(new Rectangle(position.X, position.Y, size, size), new Paint
            {
                Background = token.Color,
                Border = ring,
                BorderWidth = selected || isCurrent ? 3 : 2,
                Radius = size * 0.5f,
                Overlay = token.IsDown ? Color.Hex(0x000000A8) : Color.Transparent,
                Cursor = dragging ? SystemCursor.Move : SystemCursor.Pointer,
                Animate = motion | UITransitionPropertyFlagBits.OverlayColor,
                Duration = 0.18f,
                Text = token.IsDown ? FontAwesome.Xmark : token.Initials,
                TextStyle = token.IsDown
                    ? new TextStyle { Size = size * 0.42f, Color = Palette.White, Font = FontAwesome.FontId }
                    : TextStyle.Sized(size * (token.SizeCells > 1 ? 0.26f : 0.36f), Palette.White)
            }, token.Id);

            if (token.Conditions.Count > 0)
            {
                var badge = 14 * _zoom;
                p.Box(new Rectangle(position.X + size - badge + 2, position.Y - 2, badge, badge), new Paint
                {
                    Background = Palette.Warn,
                    Radius = badge * 0.5f,
                    Animate = motion,
                    Duration = 0.18f,
                    Text = Fmt.Int(token.Conditions.Count),
                    TextStyle = TextStyle.Sized(9 * _zoom, Color.Hex(0x1A1A1AFF))
                });
            }

            var fraction = Math.Clamp(token.Hp / (float)token.MaxHp, 0, 1);
            var barX = position.X + size * 0.12f;
            var barWidth = size * 0.76f;
            var barY = position.Y + size + 2 * _zoom;
            var barHeight = 4 * _zoom;
            p.Box(new Rectangle(barX, barY, barWidth, barHeight), new Paint { Background = Shade, Radius = 2, Animate = motion, Duration = 0.18f });
            p.Box(new Rectangle(barX, barY, barWidth * fraction, barHeight), new Paint
            {
                Background = Palette.Hp(fraction),
                Radius = 2,
                Animate = motion | UITransitionPropertyFlagBits.Size,
                Duration = 0.3f
            });

            if (hit.Hovered || selected || dragging)
            {
                Tag(p, token.Name, new Vector2(position.X + size * 0.5f, position.Y + size + 6 * _zoom), 11);
            }
        }
    }

    private static void Tag(Painter p, string text, Vector2 topCenter, float size)
    {
        var style = TextStyle.Sized(size, Palette.White);
        var measured = Ui.MeasureText(text, style);
        var width = measured.X + 14;
        var height = measured.Y + 4;
        p.Box(new Rectangle(topCenter.X - width * 0.5f, topCenter.Y, width, height), new Paint { Background = Shade, Radius = 4, Text = text, TextStyle = style });
    }

    private Menu TokenMenu(Character token)
    {
        var prone = token.Conditions.Contains("Prone");
        var menu = new Menu
        {
            Items =
            [
                new MenuItem("Open character sheet", FontAwesome.User, "dbl-click") { OnClick = () => OpenSheet?.Invoke(token) },
                new MenuItem("Center view here", FontAwesome.Crosshairs) { OnClick = () => CenterOn(token) },
                new MenuSeparator(),
                new MenuItem("Damage 1", FontAwesome.Minus) { OnClick = () => _campaign.Damage(token, 1) },
                new MenuItem("Damage 5", FontAwesome.Minus) { OnClick = () => _campaign.Damage(token, 5) },
                new MenuItem("Damage 10", FontAwesome.Minus) { OnClick = () => _campaign.Damage(token, 10) },
                new MenuItem("Heal 5", FontAwesome.Plus) { OnClick = () => _campaign.Heal(token, 5) },
                new MenuItem("Heal to full", FontAwesome.Heart) { OnClick = () => _campaign.Heal(token, token.MaxHp) },
                new MenuSeparator(),
                new MenuItem(prone ? "Stand up" : "Knock prone", FontAwesome.ArrowDown) { OnClick = () => _campaign.SetCondition(token, "Prone", !prone) }
            ]
        };
        if (token.Attacks.Count > 0)
        {
            var attack = token.Attacks[0];
            menu.Items.Add(new MenuItem($"Attack: {attack.Name}", FontAwesome.Crosshairs)
            {
                OnClick = () => _campaign.Roll(token.Name, Dice.WithModifier("1d20", attack.Bonus), $"{attack.Name} attack")
            });
        }

        menu.Items.Add(new MenuItem("Ping token", FontAwesome.LocationCrosshairs)
        {
            OnClick = () => Ping(new Vector2(token.CellX + token.SizeCells * 0.5f, token.CellY + token.SizeCells * 0.5f), token.Color)
        });
        return menu;
    }

    private Menu FloorMenu(Vector2 mapPoint)
    {
        return new Menu
        {
            Items =
            [
                new MenuItem("Ping here", FontAwesome.LocationCrosshairs, "dbl-click") { OnClick = () => Ping(mapPoint, Campaign.GmColor) },
                new MenuItem(ShowGrid ? "Hide grid" : "Show grid", FontAwesome.BorderAll, "G") { OnClick = () => ShowGrid = !ShowGrid },
                new MenuItem("Reset view", FontAwesome.Compress) { OnClick = ResetView }
            ]
        };
    }
}
