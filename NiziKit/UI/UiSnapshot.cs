using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DenOfIz;
using DzUI = DenOfIz.UI;

namespace NiziKit.UI;

public enum UiIssueKind
{
    Overflow,
    Clipped,
    Offscreen,
    Truncated,
    Overlap,
    Obscured,
    Empty
}

public sealed record UiIssue(UiIssueKind Kind, UiWidgetInfo? Widget, Rectangle Rect, string Message);

public sealed class UiTextInfo
{
    public required string Text { get; init; }
    public Rectangle Rect { get; init; }
    public Rectangle VisibleRect { get; init; }
    public Vector2 ContentSize { get; init; }
    public int Lines { get; init; }
    public bool Truncated { get; init; }
    public bool Overflows { get; init; }
    public UIColor Color { get; init; }
    public float FontSize { get; init; }
}

public sealed class UiNodeInfo
{
    public int Index { get; init; }
    public uint Id { get; init; }
    public int Parent { get; init; }
    public int Layer { get; init; }
    public int Depth { get; init; }
    public UINodeKind Kind { get; init; }
    public UINodeFlagBits Flags { get; init; }
    public int ZIndex { get; init; }
    public Rectangle Rect { get; init; }
    public Rectangle LayoutRect { get; init; }
    public Rectangle VisibleRect { get; init; }
    public Vector2 ContentSize { get; init; }
    public Vector2 ScrollOffset { get; init; }
    public UIColor Background { get; init; }
    public UIColor Border { get; init; }
    public float Opacity { get; init; }
    public string? Text { get; init; }
    public UIColor TextColor { get; init; }
    public float FontSize { get; init; }
    public int Lines { get; init; }
    public UiWidgetInfo? Owner { get; init; }
    public bool IsWidget { get; init; }

    public bool Has(UINodeFlagBits flag) => (Flags & flag) != 0;
}

public sealed class UiWidgetInfo
{
    private static Dictionary<string, string>? _iconNames;
    private readonly Dictionary<string, object?> _properties = new();

    internal UiWidgetInfo(Widget widget)
    {
        Widget = widget;
        Type = TypeName(widget.GetType());
    }

    public Widget Widget { get; }
    public string Type { get; }
    public string? Name => Widget.Name;
    public string Path { get; internal set; } = "";
    public string? Text { get; set; }
    public object? Value { get; set; }
    public Rectangle Rect { get; internal set; }
    public Rectangle VisibleRect { get; internal set; }
    public bool Visible => VisibleRect.Width > 0 && VisibleRect.Height > 0;
    public bool Enabled { get; internal set; }
    public bool Focused { get; internal set; }
    public bool Hovered { get; internal set; }
    public bool Pressed { get; internal set; }
    public bool Clickable { get; internal set; }
    public bool Focusable { get; internal set; }
    public bool Reachable { get; internal set; } = true;
    public UiNodeInfo? Node { get; internal set; }
    public IReadOnlyDictionary<string, object?> Properties => _properties;
    public List<UiTextInfo> Texts { get; } = new();
    public List<UiWidgetInfo> Children { get; } = new();
    public UiWidgetInfo? Parent { get; internal set; }

    public void Set(string name, object? value)
    {
        if (value == null)
        {
            _properties.Remove(name);
            return;
        }

        _properties[name] = value;
    }

    // The FontAwesome name of an icon, else its code points.
    public static string? Glyph(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        _iconNames ??= typeof(FontAwesome).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string))
            .GroupBy(f => (string)f.GetRawConstantValue()!)
            .ToDictionary(g => g.Key, g => g.First().Name);
        return _iconNames.TryGetValue(text, out var name) ? name : string.Join(' ', text.EnumerateRunes().Select(r => $"U+{r.Value:X4}"));
    }

    public override string ToString()
    {
        var line = new StringBuilder(Type);
        if (Name != null)
        {
            line.Append('#').Append(Name);
        }

        if (!string.IsNullOrEmpty(Text))
        {
            line.Append(" \"").Append(UiSnapshot.Shorten(Text, 60)).Append('"');
        }

        return line.ToString();
    }

    private static string TypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var tick = type.Name.IndexOf('`');
        var name = tick >= 0 ? type.Name[..tick] : type.Name;
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }
}

public sealed class UiSnapshot
{
    private UiSnapshot()
    {
    }

    public Vector2 Viewport { get; private init; }
    public float PixelsPerPoint { get; private init; }
    public float Scale { get; private init; }
    public int Frame { get; private init; }
    public float Time { get; private init; }
    public bool Animating { get; private init; }
    public Vector2 Pointer { get; private init; }
    public UiWidgetInfo? Focused { get; private set; }
    public UiWidgetInfo? Hovered { get; private set; }
    public string? Tooltip { get; private init; }
    public IReadOnlyList<string> Notifications { get; private init; } = [];
    public IReadOnlyList<UiWidgetInfo> Roots { get; private init; } = [];
    public IReadOnlyList<UiWidgetInfo> Widgets { get; private init; } = [];
    public IReadOnlyList<UiNodeInfo> Nodes { get; private init; } = [];
    public IReadOnlyList<UiIssue> Issues { get; private set; } = [];

    // Text boxes are rounded up to whole pixels, everything else to the nearest pixel.
    private float Slack => 1 / PixelsPerPoint + 0.01f;

    public UiWidgetInfo? Info(Widget widget)
    {
        return Widgets.FirstOrDefault(w => ReferenceEquals(w.Widget, widget));
    }

    public UiWidgetInfo? Find(string query)
    {
        query = query.Trim();
        if (query.Length == 0)
        {
            return null;
        }

        if (query[0] == '#')
        {
            return Best(w => w.Name == query[1..]);
        }

        if (query.Contains('/'))
        {
            return Best(w => w.Path == query) ?? Best(w => w.Path.EndsWith("/" + query, StringComparison.Ordinal));
        }

        return Best(w => w.Name == query) ??
               Best(w => w.Text == query) ??
               Best(w => w.Type == query || w.Type.StartsWith(query + "<", StringComparison.Ordinal)) ??
               Best(w => w.Text != null && w.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    // Like Find, then a drawn text that is not a widget of its own, e.g. a row of a dropdown list: the rect is where to point at.
    public (UiWidgetInfo Widget, Rectangle Rect)? Locate(string query)
    {
        query = query.Trim();
        var widget = Find(query);
        if (widget != null && (widget.Text == query || widget.Name == query || query.StartsWith('#') || query.Contains('/') || widget.Type == query ||
                               widget.Type.StartsWith(query + "<", StringComparison.Ordinal)))
        {
            return (widget, widget.VisibleRect);
        }

        foreach (var exact in new[] { true, false })
        {
            for (var i = Widgets.Count - 1; i >= 0; i--)
            {
                foreach (var text in Widgets[i].Texts)
                {
                    var match = exact ? text.Text == query : text.Text.Contains(query, StringComparison.OrdinalIgnoreCase);
                    if (match && text.VisibleRect is { Width: > 0, Height: > 0 } && (exact || widget == null))
                    {
                        return (Widgets[i], text.VisibleRect);
                    }
                }
            }
        }

        return widget != null ? (widget, widget.VisibleRect) : null;
    }

    public UiWidgetInfo? At(Vector2 point)
    {
        var id = Ui.Native.HitTest(point);
        var index = id != 0 ? Ui.Native.FindNode(id) : DzUI.NoNode;
        return index < Nodes.Count ? Nodes[(int)index].Owner : null;
    }

    private UiWidgetInfo? Best(Func<UiWidgetInfo, bool> match)
    {
        UiWidgetInfo? best = null;
        var bestScore = -1;
        foreach (var widget in Widgets)
        {
            if (!match(widget))
            {
                continue;
            }

            var score = (widget.Visible ? 2 : 0) + (widget.Clickable || widget.Focusable ? 1 : 0);
            if (score > bestScore)
            {
                best = widget;
                bestScore = score;
            }
        }

        return best;
    }

    internal static UiSnapshot Take()
    {
        var ui = Ui.Native;
        var byId = new Dictionary<uint, UiWidgetInfo>();
        var widgets = new List<UiWidgetInfo>();
        foreach (var widget in Ui.Built)
        {
            if (byId.ContainsKey(widget.Id))
            {
                continue;
            }

            var info = new UiWidgetInfo(widget)
            {
                Enabled = widget.EnabledNow,
                Focused = widget.State.Focused,
                Hovered = widget.State.Hovered,
                Pressed = widget.State.Pressed,
                Clickable = widget.IsClickable,
                Focusable = widget.IsFocusable
            };
            try
            {
                widget.Describe(info);
            }
            catch (Exception e)
            {
                info.Set("describeError", e.Message);
            }

            byId[widget.Id] = info;
            widgets.Add(info);
        }

        var count = (int)ui.NodeCount;
        var nodes = new UiNodeInfo[count];
        for (var i = 0; i < count; i++)
        {
            ui.TryGetNode((uint)i, out var n);
            var parent = n.Parent == DzUI.NoNode ? -1 : (int)n.Parent;
            var own = byId.GetValueOrDefault(n.Id);
            var owner = own ?? (parent >= 0 ? nodes[parent].Owner : null);
            var node = nodes[i] = new UiNodeInfo
            {
                Index = i,
                Id = n.Id,
                Parent = parent,
                Layer = (int)n.Layer,
                Depth = (int)n.Depth,
                Kind = n.Kind,
                Flags = (UINodeFlagBits)n.Flags,
                ZIndex = n.ZIndex,
                Rect = Rectangle.From(n.Rect),
                LayoutRect = Rectangle.From(n.LayoutRect),
                VisibleRect = Rectangle.From(n.VisibleRect),
                ContentSize = n.ContentSize,
                ScrollOffset = n.ScrollOffset,
                Background = n.BackgroundColor,
                Border = n.BorderColor,
                Opacity = n.Opacity,
                Text = n.Kind == UINodeKind.Text ? n.Text.ToString() : null,
                TextColor = n.TextColor,
                FontSize = n.FontSize,
                Lines = (int)n.LineCount,
                Owner = owner,
                IsWidget = own != null
            };
            if (own != null && own.Node == null)
            {
                own.Node = node;
            }

            if (node is { Kind: UINodeKind.Text, Text: not null } && owner != null && !node.Has(UINodeFlagBits.Hidden))
            {
                owner.Texts.Add(new UiTextInfo
                {
                    Text = node.Text,
                    Rect = node.Rect,
                    VisibleRect = node.VisibleRect,
                    ContentSize = node.ContentSize,
                    Lines = node.Lines,
                    Truncated = node.Has(UINodeFlagBits.Truncated),
                    Overflows = node.Has(UINodeFlagBits.Overflows),
                    Color = node.TextColor,
                    FontSize = node.FontSize
                });
            }
        }

        var roots = new List<UiWidgetInfo>();
        foreach (var info in widgets)
        {
            var parent = info.Widget.Parent;
            while (parent != null && !byId.ContainsKey(parent.Id))
            {
                parent = parent.Parent;
            }

            if (parent != null && byId[parent.Id] is var parentInfo && !ReferenceEquals(parentInfo, info))
            {
                info.Parent = parentInfo;
                parentInfo.Children.Add(info);
            }
            else
            {
                roots.Add(info);
            }
        }

        foreach (var root in roots)
        {
            Resolve(root);
        }

        AssignPaths(roots, "");
        var snapshot = new UiSnapshot
        {
            Viewport = ui.ViewportSize,
            PixelsPerPoint = ui.PixelsPerPoint,
            Scale = Ui.Scale,
            Frame = Ui.FrameNumber,
            Time = Ui.Time,
            Animating = ui.IsAnimating || Images.Loading,
            Pointer = ui.PointerPosition,
            Tooltip = Ui.ShownTooltip,
            Notifications = Ui.NotificationTexts.ToArray(),
            Roots = roots,
            Widgets = widgets,
            Nodes = nodes
        };
        snapshot.Focused = widgets.FirstOrDefault(w => w.Focused);
        var hovered = ui.HoveredId;
        snapshot.Hovered = hovered != 0 && ui.FindNode(hovered) is var hoveredNode && hoveredNode < count ? nodes[hoveredNode].Owner : null;
        snapshot.Issues = snapshot.FindIssues();
        return snapshot;
    }

    private static void Resolve(UiWidgetInfo info)
    {
        foreach (var child in info.Children)
        {
            Resolve(child);
        }

        if (info.Node is { } node && !node.Has(UINodeFlagBits.Hidden))
        {
            info.Rect = node.Rect;
            info.VisibleRect = node.VisibleRect;
        }
        else if (info.Node == null)
        {
            var shown = info.Children.Where(c => c.Rect.Width > 0 || c.Rect.Height > 0).ToList();
            info.Rect = Union(shown.Select(c => c.Rect));
            info.VisibleRect = Union(shown.Where(c => c.Visible).Select(c => c.VisibleRect));
        }

        if (!(info.Clickable || info.Focusable) || !info.Visible || info.Node == null)
        {
            return;
        }

        Span<uint> chain = stackalloc uint[256];
        Ui.Native.HitTest(info.VisibleRect.Center, chain, out var hits);
        info.Reachable = chain[..hits].Contains(info.Widget.Id);
    }

    private static void AssignPaths(List<UiWidgetInfo> siblings, string prefix)
    {
        var counts = new Dictionary<string, int>();
        foreach (var info in siblings)
        {
            counts[info.Type] = counts.GetValueOrDefault(info.Type) + 1;
        }

        var seen = new Dictionary<string, int>();
        foreach (var info in siblings)
        {
            var index = seen[info.Type] = seen.GetValueOrDefault(info.Type, -1) + 1;
            var segment = info.Name != null ? $"{info.Type}#{info.Name}" : counts[info.Type] > 1 ? $"{info.Type}[{index}]" : info.Type;
            info.Path = prefix.Length == 0 ? segment : $"{prefix}/{segment}";
            AssignPaths(info.Children, info.Path);
        }
    }

    private List<UiIssue> FindIssues()
    {
        var issues = new List<UiIssue>();
        var nodes = Nodes;
        var viewport = new Rectangle(0, 0, Viewport.X, Viewport.Y);
        for (var i = 1; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Has(UINodeFlagBits.Hidden) || node.Owner == null)
            {
                continue;
            }

            if (node.Has(UINodeFlagBits.Truncated))
            {
                var collapsed = node.Rect.Width < 1 ? ", its box has no width" : "";
                issues.Add(new UiIssue(UiIssueKind.Truncated, node.Owner, node.Rect, $"text \"{Shorten(node.Text, 40)}\" is cut off after {node.Lines} line(s){collapsed}"));
            }
            else if (node.Has(UINodeFlagBits.Overflows))
            {
                issues.Add(new UiIssue(UiIssueKind.Overflow, node.Owner, node.Rect,
                    $"text \"{Shorten(node.Text, 40)}\" needs {F(node.ContentSize.X)}x{F(node.ContentSize.Y)} but has {F(node.Rect.Width)}x{F(node.Rect.Height)}"));
            }

            if ((node.IsWidget || node.Kind == UINodeKind.Text) && Cut(node) && (node.Parent < 0 || !Cut(nodes[node.Parent])) && !Overflows(node))
            {
                var clip = ClippingAncestor(node);
                if (clip == null)
                {
                    issues.Add(new UiIssue(UiIssueKind.Offscreen, node.Owner, node.Rect, $"{Describe(node)} extends past the viewport {Sides(node.Rect, viewport)}"));
                }
                else if (!clip.Has(UINodeFlagBits.Scrollable))
                {
                    issues.Add(new UiIssue(UiIssueKind.Clipped, node.Owner, node.Rect,
                        $"{Describe(node)} is cut by {clip.Owner?.Path ?? "an element"} {Sides(node.Rect, clip.VisibleRect)}"));
                }
            }

            if (node.Kind == UINodeKind.Element && !node.Has(UINodeFlagBits.Contents) && !node.Has(UINodeFlagBits.Clips))
            {
                CheckChildren(node, issues);
            }
        }

        foreach (var widget in Widgets)
        {
            if (widget.Node == null || widget.Node.Has(UINodeFlagBits.Hidden) || !(widget.Clickable || widget.Focusable))
            {
                continue;
            }

            if (widget.Rect.Width < 1 || widget.Rect.Height < 1)
            {
                issues.Add(new UiIssue(UiIssueKind.Empty, widget, widget.Rect, $"{widget.Path} is interactive but {F(widget.Rect.Width)}x{F(widget.Rect.Height)}"));
            }
            else if (widget is { Visible: true, Reachable: false } && At(widget.VisibleRect.Center) is var cover && cover != null &&
                     cover.Node?.Layer == widget.Node.Layer)
            {
                issues.Add(new UiIssue(UiIssueKind.Obscured, widget, widget.VisibleRect, $"{widget.Path} is covered by {cover.Path} at its centre"));
            }
        }

        return issues;
    }

    private void CheckChildren(UiNodeInfo parent, List<UiIssue> issues)
    {
        var bounds = parent.LayoutRect;
        float left = 0, top = 0, right = 0, bottom = 0;
        UiNodeInfo? culprit = null;
        UiNodeInfo? previous = null;
        for (var i = parent.Index + 1; i < Nodes.Count && Nodes[i].Depth > parent.Depth; i++)
        {
            var child = Nodes[i];
            if (child.Parent != parent.Index || child.Has(UINodeFlagBits.Hidden) || child.Has(UINodeFlagBits.Floating) || child.Has(UINodeFlagBits.Absolute))
            {
                continue;
            }

            var rect = child.LayoutRect;
            var worst = Math.Max(Math.Max(bounds.X - rect.X, rect.Right - bounds.Right), Math.Max(bounds.Y - rect.Y, rect.Bottom - bounds.Bottom));
            if (worst > Slack)
            {
                left = Math.Max(left, bounds.X - rect.X);
                top = Math.Max(top, bounds.Y - rect.Y);
                right = Math.Max(right, rect.Right - bounds.Right);
                bottom = Math.Max(bottom, rect.Bottom - bounds.Bottom);
                culprit ??= child;
            }

            if (previous != null && rect.Width > 0 && rect.Height > 0)
            {
                var a = previous.LayoutRect;
                var w = Math.Min(a.Right, rect.Right) - Math.Max(a.X, rect.X);
                var h = Math.Min(a.Bottom, rect.Bottom) - Math.Max(a.Y, rect.Y);
                if (w > 1 && h > 1)
                {
                    issues.Add(new UiIssue(UiIssueKind.Overlap, child.Owner, rect, $"{Describe(previous)} and {Describe(child)} overlap by {F(w)}x{F(h)}"));
                }
            }

            if (rect.Width > 0 && rect.Height > 0)
            {
                previous = child;
            }
        }

        if (culprit != null)
        {
            var sides = new List<string>();
            AddSide(sides, "left", left);
            AddSide(sides, "top", top);
            AddSide(sides, "right", right);
            AddSide(sides, "bottom", bottom);
            issues.Add(new UiIssue(UiIssueKind.Overflow, parent.Owner, parent.Rect,
                $"content of {Describe(parent)} overflows by {string.Join(", ", sides)} (first: {Describe(culprit)})"));
        }
    }

    // Sticks out of an element whose overflow is reported already.
    private bool Overflows(UiNodeInfo node)
    {
        if (node.Parent <= 0 || node.Has(UINodeFlagBits.Floating) || node.Has(UINodeFlagBits.Absolute))
        {
            return false;
        }

        var parent = Nodes[node.Parent];
        var bounds = parent.LayoutRect;
        var rect = node.LayoutRect;
        return parent.Kind == UINodeKind.Element && !parent.Has(UINodeFlagBits.Contents) && !parent.Has(UINodeFlagBits.Clips) &&
               Math.Max(Math.Max(bounds.X - rect.X, rect.Right - bounds.Right), Math.Max(bounds.Y - rect.Y, rect.Bottom - bounds.Bottom)) > Slack;
    }

    private bool Cut(UiNodeInfo node)
    {
        var rect = node.Rect;
        var visible = node.VisibleRect;
        return rect.Width > Slack && rect.Height > Slack && (visible.X - rect.X > Slack || visible.Y - rect.Y > Slack || rect.Right - visible.Right > Slack ||
                                                              rect.Bottom - visible.Bottom > Slack || visible.Width <= 0 || visible.Height <= 0);
    }

    private UiNodeInfo? ClippingAncestor(UiNodeInfo node)
    {
        for (var p = node.Parent; p >= 0; p = Nodes[p].Parent)
        {
            if (Nodes[p].Has(UINodeFlagBits.Clips))
            {
                return Nodes[p];
            }

            if (Nodes[p].Has(UINodeFlagBits.Floating))
            {
                return null;
            }
        }

        return null;
    }

    private static string Describe(UiNodeInfo node)
    {
        if (node.Kind == UINodeKind.Text)
        {
            return $"text \"{Shorten(node.Text, 40)}\"";
        }

        return node.Owner == null ? "element" : node.IsWidget ? node.Owner.Path : $"part of {node.Owner.Path}";
    }

    private string Sides(Rectangle rect, Rectangle bounds)
    {
        var sides = new List<string>();
        AddSide(sides, "left", bounds.X - rect.X);
        AddSide(sides, "top", bounds.Y - rect.Y);
        AddSide(sides, "right", rect.Right - bounds.Right);
        AddSide(sides, "bottom", rect.Bottom - bounds.Bottom);
        return sides.Count > 0 ? "by " + string.Join(", ", sides) : "entirely";
    }

    private void AddSide(List<string> sides, string side, float amount)
    {
        if (amount > Slack)
        {
            sides.Add($"{F(amount)} {side}");
        }
    }

    private static Rectangle Union(IEnumerable<Rectangle> rects)
    {
        float left = float.MaxValue, top = float.MaxValue, right = float.MinValue, bottom = float.MinValue;
        foreach (var r in rects)
        {
            left = Math.Min(left, r.X);
            top = Math.Min(top, r.Y);
            right = Math.Max(right, r.Right);
            bottom = Math.Max(bottom, r.Bottom);
        }

        return left > right ? default : new Rectangle(left, top, right - left, bottom - top);
    }

    internal static string F(float value)
    {
        return MathF.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }

    internal static string Shorten(string? text, int length)
    {
        if (text == null)
        {
            return "";
        }

        text = text.ReplaceLineEndings("\\n");
        return text.Length > length ? text[..(length - 1)] + "…" : text;
    }

    private static string R(Rectangle r)
    {
        return $"{F(r.X)},{F(r.Y)} {F(r.Width)}x{F(r.Height)}";
    }

    private static string Hex(UIColor c)
    {
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";
    }

    public override string ToString()
    {
        var text = new StringBuilder();
        text.Append($"viewport {F(Viewport.X)}x{F(Viewport.Y)} pt, {F(PixelsPerPoint)} px/pt, frame {Frame}, {(Animating ? "animating" : "settled")}\n");
        if (Focused != null)
        {
            text.Append($"focused {Focused.Path}\n");
        }

        if (Hovered != null)
        {
            text.Append($"hovered {Hovered.Path} at {F(Pointer.X)},{F(Pointer.Y)}\n");
        }

        if (Tooltip != null)
        {
            text.Append($"tooltip \"{Tooltip}\"\n");
        }

        foreach (var notification in Notifications)
        {
            text.Append($"notification \"{notification}\"\n");
        }

        foreach (var root in Roots)
        {
            AppendTree(text, root, 0);
        }

        AppendIssues(text);
        return text.ToString();
    }

    public string Tree(UiWidgetInfo root)
    {
        var text = new StringBuilder();
        AppendTree(text, root, 0);
        return text.ToString();
    }

    internal void AppendIssues(StringBuilder text)
    {
        text.Append(Issues.Count == 0 ? "no issues\n" : $"issues ({Issues.Count})\n");
        foreach (var issue in Issues)
        {
            text.Append($"  {issue.Kind.ToString().ToLowerInvariant()}: {issue.Message}\n");
        }
    }

    internal static string Line(UiWidgetInfo w)
    {
        var line = new StringBuilder(w.ToString());
        if (w.Value != null && !(w.Value is string shown && shown == w.Text))
        {
            line.Append(" =").Append(Value(w.Value));
        }

        foreach (var (key, value) in w.Properties)
        {
            line.Append(' ').Append(key).Append('=').Append(Value(value));
        }

        line.Append(' ').Append(R(w.Rect));
        if (w.Node is { } node && node.Has(UINodeFlagBits.Scrollable))
        {
            line.Append($" scroll {F(node.ScrollOffset.X)},{F(node.ScrollOffset.Y)} of {F(node.ContentSize.X)}x{F(node.ContentSize.Y)}");
        }

        var flags = new List<string>();
        if (!w.Enabled)
        {
            flags.Add("disabled");
        }

        if (w.Focused)
        {
            flags.Add("focused");
        }

        if (w.Hovered)
        {
            flags.Add("hovered");
        }

        if (w.Pressed)
        {
            flags.Add("pressed");
        }

        if (!w.Visible && w.Rect.Width > 0 && w.Rect.Height > 0)
        {
            flags.Add("not visible");
        }
        else if (w.Visible && w.VisibleRect != w.Rect)
        {
            flags.Add("visible " + R(w.VisibleRect));
        }

        if (!w.Reachable)
        {
            flags.Add("covered");
        }

        if (flags.Count > 0)
        {
            line.Append(" [").Append(string.Join(", ", flags)).Append(']');
        }

        return line.ToString();
    }

    private static void AppendTree(StringBuilder text, UiWidgetInfo w, int depth)
    {
        text.Append(' ', depth * 2).Append(Line(w)).Append('\n');
        foreach (var child in w.Children)
        {
            AppendTree(text, child, depth + 1);
        }
    }

    private static string Value(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{Shorten(s, 60)}\"",
            float f => F(f),
            bool b => b ? "true" : "false",
            System.Collections.IEnumerable items => List(items.Cast<object?>().ToList()),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
        };
    }

    private static string List(List<object?> items)
    {
        const int shown = 8;
        var head = string.Join(", ", items.Take(shown).Select(Value));
        return items.Count > shown ? $"[{head}, … {items.Count - shown} more]" : $"[{head}]";
    }

    public string ToJson(bool nodes = false, bool indented = false)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            json.WriteStartObject();
            json.WriteString("units", "points; rects are [x, y, width, height] in viewport space, colours #RRGGBBAA");
            json.WriteStartObject("viewport");
            Number(json, "width", Viewport.X);
            Number(json, "height", Viewport.Y);
            Number(json, "pixelsPerPoint", PixelsPerPoint);
            Number(json, "scale", Scale);
            json.WriteEndObject();
            json.WriteNumber("frame", Frame);
            Number(json, "time", Time);
            json.WriteBoolean("animating", Animating);
            Vector(json, "pointer", Pointer);
            json.WriteString("focused", Focused?.Path);
            json.WriteString("hovered", Hovered?.Path);
            json.WriteString("tooltip", Tooltip);
            json.WriteStartArray("notifications");
            foreach (var notification in Notifications)
            {
                json.WriteStringValue(notification);
            }

            json.WriteEndArray();
            json.WriteStartArray("issues");
            foreach (var issue in Issues)
            {
                json.WriteStartObject();
                json.WriteString("kind", issue.Kind.ToString().ToLowerInvariant());
                json.WriteString("widget", issue.Widget?.Path);
                Rect(json, "rect", issue.Rect);
                json.WriteString("message", issue.Message);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("widgets");
            foreach (var root in Roots)
            {
                WriteWidget(json, root);
            }

            json.WriteEndArray();
            if (nodes)
            {
                WriteNodes(json);
            }

            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteWidget(Utf8JsonWriter json, UiWidgetInfo w)
    {
        json.WriteStartObject();
        json.WriteString("type", w.Type);
        if (w.Name != null)
        {
            json.WriteString("name", w.Name);
        }

        json.WriteString("path", w.Path);
        if (w.Text != null)
        {
            json.WriteString("text", w.Text);
        }

        if (w.Value != null)
        {
            json.WritePropertyName("value");
            Any(json, w.Value);
        }

        Rect(json, "rect", w.Rect);
        if (w.VisibleRect != w.Rect)
        {
            Rect(json, "visible", w.VisibleRect);
        }

        Flag(json, "enabled", w.Enabled, true);
        Flag(json, "focused", w.Focused, false);
        Flag(json, "hovered", w.Hovered, false);
        Flag(json, "pressed", w.Pressed, false);
        Flag(json, "clickable", w.Clickable, false);
        Flag(json, "focusable", w.Focusable, false);
        Flag(json, "reachable", w.Reachable, true);
        if (w.Node is { } node)
        {
            Colour(json, "background", node.Background);
            Colour(json, "border", node.Border);
            if (node.Opacity < 1)
            {
                Number(json, "opacity", node.Opacity);
            }

            if (node.Has(UINodeFlagBits.Scrollable))
            {
                json.WriteStartObject("scroll");
                Vector(json, "offset", node.ScrollOffset);
                Vector(json, "content", node.ContentSize);
                json.WriteEndObject();
            }
        }

        if (w.Properties.Count > 0)
        {
            json.WriteStartObject("properties");
            foreach (var (key, value) in w.Properties)
            {
                json.WritePropertyName(key);
                Any(json, value);
            }

            json.WriteEndObject();
        }

        if (w.Texts.Count > 0)
        {
            json.WriteStartArray("texts");
            foreach (var text in w.Texts)
            {
                json.WriteStartObject();
                json.WriteString("text", text.Text);
                Rect(json, "rect", text.Rect);
                if (text.VisibleRect != text.Rect)
                {
                    Rect(json, "visible", text.VisibleRect);
                }

                json.WriteNumber("lines", text.Lines);
                Number(json, "size", text.FontSize);
                Colour(json, "color", text.Color);
                Flag(json, "truncated", text.Truncated, false);
                Flag(json, "overflows", text.Overflows, false);
                json.WriteEndObject();
            }

            json.WriteEndArray();
        }

        if (w.Children.Count > 0)
        {
            json.WriteStartArray("children");
            foreach (var child in w.Children)
            {
                WriteWidget(json, child);
            }

            json.WriteEndArray();
        }

        json.WriteEndObject();
    }

    private void WriteNodes(Utf8JsonWriter json)
    {
        json.WriteStartArray("nodes");
        foreach (var node in Nodes)
        {
            json.WriteStartObject();
            json.WriteNumber("index", node.Index);
            json.WriteNumber("id", node.Id);
            json.WriteNumber("parent", node.Parent);
            json.WriteString("kind", node.Kind.ToString().ToLowerInvariant());
            if (node.Flags != UINodeFlagBits.None)
            {
                json.WriteStartArray("flags");
                foreach (var flag in Enum.GetValues<UINodeFlagBits>())
                {
                    if (flag != UINodeFlagBits.None && node.Has(flag))
                    {
                        json.WriteStringValue(flag.ToString().ToLowerInvariant());
                    }
                }

                json.WriteEndArray();
            }

            if (node.Layer != 0)
            {
                json.WriteNumber("layer", node.Layer);
            }

            Rect(json, "rect", node.Rect);
            if (node.LayoutRect != node.Rect)
            {
                Rect(json, "layout", node.LayoutRect);
            }

            if (node.VisibleRect != node.Rect)
            {
                Rect(json, "visible", node.VisibleRect);
            }

            if (node.ContentSize != Vector2.Zero)
            {
                Vector(json, "content", node.ContentSize);
            }

            Colour(json, "background", node.Background);
            Colour(json, "border", node.Border);
            if (node.Text != null)
            {
                json.WriteString("text", node.Text);
                Colour(json, "color", node.TextColor);
                Number(json, "size", node.FontSize);
            }

            if (node.Owner != null)
            {
                json.WriteString("widget", node.Owner.Path);
            }

            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    private static void Number(Utf8JsonWriter json, string name, float value)
    {
        json.WriteNumber(name, Math.Round(value, 2));
    }

    private static void Rect(Utf8JsonWriter json, string name, Rectangle r)
    {
        json.WriteStartArray(name);
        json.WriteNumberValue(Math.Round(r.X, 2));
        json.WriteNumberValue(Math.Round(r.Y, 2));
        json.WriteNumberValue(Math.Round(r.Width, 2));
        json.WriteNumberValue(Math.Round(r.Height, 2));
        json.WriteEndArray();
    }

    private static void Vector(Utf8JsonWriter json, string name, Vector2 v)
    {
        json.WriteStartArray(name);
        json.WriteNumberValue(Math.Round(v.X, 2));
        json.WriteNumberValue(Math.Round(v.Y, 2));
        json.WriteEndArray();
    }

    private static void Flag(Utf8JsonWriter json, string name, bool value, bool fallback)
    {
        if (value != fallback)
        {
            json.WriteBoolean(name, value);
        }
    }

    private static void Colour(Utf8JsonWriter json, string name, UIColor c)
    {
        if (c.A > 0)
        {
            json.WriteString(name, Hex(c));
        }
    }

    private static void Any(Utf8JsonWriter json, object? value)
    {
        switch (value)
        {
            case null:
                json.WriteNullValue();
                break;
            case string s:
                json.WriteStringValue(s);
                break;
            case bool b:
                json.WriteBooleanValue(b);
                break;
            case float f:
                json.WriteNumberValue(Math.Round(f, 2));
                break;
            case double d:
                json.WriteNumberValue(Math.Round(d, 2));
                break;
            case int i:
                json.WriteNumberValue(i);
                break;
            case long l:
                json.WriteNumberValue(l);
                break;
            case uint u:
                json.WriteNumberValue(u);
                break;
            case Enum e:
                json.WriteStringValue(e.ToString());
                break;
            case Vector2 v:
                json.WriteStartArray();
                json.WriteNumberValue(Math.Round(v.X, 2));
                json.WriteNumberValue(Math.Round(v.Y, 2));
                json.WriteEndArray();
                break;
            case System.Collections.IEnumerable items:
                json.WriteStartArray();
                foreach (var item in items)
                {
                    Any(json, item);
                }

                json.WriteEndArray();
                break;
            default:
                json.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }
}
