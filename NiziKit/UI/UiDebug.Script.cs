using System.Globalization;
using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

public static partial class UiDebug
{
    private const string Help = """
                                commands, targets are x,y in points or a widget query (#name, path, text or type):
                                  step [frames]               run frames of 1/60 s
                                  wait <seconds>              run frames for a while, e.g. for tooltips
                                  settle                      run frames until animations and image loads finish
                                  size <width> <height> [px/pt]   resize the viewport
                                  zoom <scale>                Ui.Scale
                                  move <target>               move the pointer
                                  click | doubleclick | rightclick <target>
                                  drag <x,y> <x,y>
                                  scroll <target> <lines>     positive scrolls down
                                  type <text>                 text input to the focused widget
                                  key <combo>                 e.g. Tab, shift+Tab, ctrl+a, Return, Escape
                                  focus <query>
                                  leave                       the pointer leaves the window
                                  tree [query]                the widget tree with rects, values and issues
                                  find <query> | at <x,y>     one widget
                                  issues                      overflow, clipping, truncation, overlap, covered widgets
                                  json [path] | nodes [path]  JSON snapshot, nodes adds the raw DenOfIz nodes
                                  capture <path.png> [query]  PNG of the viewport or one widget
                                  save [prefix]               prefix.txt, prefix.json and prefix.png
                                  quit
                                """;

    private static string _output = "ui-debug";

    // A NiziKit app opts in with `if (UiDebug.Run(args, () => new MyView())) return;` and is then started with
    // `--ui-debug [--size 1280x800] [--scale 2] [--out dir] [--script file|-] [--run "click #save; save out/after"]`.
    // Without a script it settles and saves the snapshot to <out>/ui.*; `--script -` reads commands from stdin.
    public static bool Run(string[] args, Func<Widget> root)
    {
        if (!args.Contains("--ui-debug"))
        {
            return false;
        }

        var size = (Option(args, "--size") ?? "1280x800").Split('x');
        var scale = Number(Option(args, "--scale") ?? "1");
        var script = Option(args, "--script");
        var commands = Option(args, "--run");
        _output = Option(args, "--out") ?? _output;
        using (Headless(Number(size[0]), Number(size[1]), scale))
        {
            Ui.Root = root();
            Settle();
            if (script != null)
            {
                using var reader = script == "-" ? Console.In : File.OpenText(script);
                RunScript(reader, Console.Out);
            }

            if (commands != null)
            {
                RunScript(new StringReader(commands.Replace(';', '\n')), Console.Out);
            }

            if (script == null && commands == null)
            {
                RunScript(new StringReader("save"), Console.Out);
            }
        }

        return true;
    }

    public static void RunScript(TextReader script, TextWriter output)
    {
        while (script.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (line is "quit" or "exit")
            {
                break;
            }

            output.WriteLine("> " + line);
            try
            {
                var result = Execute(line);
                if (result.Length > 0)
                {
                    output.WriteLine(result.TrimEnd('\n'));
                }
            }
            catch (Exception e)
            {
                output.WriteLine("error: " + e.Message);
            }

            output.Flush();
        }
    }

    public static string Execute(string command)
    {
        var args = Tokenize(command);
        if (args.Count == 0)
        {
            return "";
        }

        var verb = args[0].ToLowerInvariant();
        args.RemoveAt(0);
        var query = string.Join(' ', args);
        switch (verb)
        {
            case "help":
                return Help;
            case "step":
                Step(args.Count > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 1);
                return $"frame {Ui.FrameNumber}";
            case "wait":
                Wait(Number(args[0]));
                return $"frame {Ui.FrameNumber}";
            case "settle":
                var frames = Settle();
                return IsAnimating ? $"still animating after {frames} frames" : $"settled after {frames} frame(s)";
            case "size":
                Resize(Number(args[0]), Number(args[1]), args.Count > 2 ? Number(args[2]) : null);
                Settle();
                return $"viewport {UiSnapshot.F(Ui.ViewportSize.X)}x{UiSnapshot.F(Ui.ViewportSize.Y)} pt at {UiSnapshot.F(Ui.PixelsPerPoint)} px/pt";
            case "zoom":
                Ui.Scale = Number(args[0]);
                Settle();
                return $"viewport {UiSnapshot.F(Ui.ViewportSize.X)}x{UiSnapshot.F(Ui.ViewportSize.Y)} pt";
            case "move":
            {
                var (point, _) = Target(args, 0, true, out _);
                MoveTo(point);
                return "hovered " + (Snapshot().Hovered?.Path ?? "nothing");
            }
            case "click":
            case "doubleclick":
            case "rightclick":
            {
                var (point, widget) = Target(args, 0, true, out _);
                var hit = Snapshot().At(point);
                Click(point, verb == "rightclick" ? MouseButton.Right : MouseButton.Left, verb == "doubleclick" ? 2 : 1);
                var focused = Snapshot().Focused;
                var missed = widget != null && hit != null && !hit.Path.StartsWith(widget.Path, StringComparison.Ordinal)
                    ? $" (aimed at {widget.Path}, which is covered there)"
                    : "";
                return $"{verb}ed {hit?.Path ?? "nothing"} at {UiSnapshot.F(point.X)},{UiSnapshot.F(point.Y)}{missed}" +
                       (focused != null ? $", focused {focused.Path}" : "");
            }
            case "drag":
            {
                var (from, widget) = Target(args, 0, false, out var used);
                var (to, _) = Target(args, used, false, out _);
                Drag(from, to);
                return $"dragged {widget?.Path ?? "nothing"} from {UiSnapshot.F(from.X)},{UiSnapshot.F(from.Y)} to {UiSnapshot.F(to.X)},{UiSnapshot.F(to.Y)}";
            }
            case "scroll":
            {
                var lines = Number(args[^1]);
                var (point, widget) = Target(args.GetRange(0, args.Count - 1), 0, true, out _);
                Scroll(point, lines);
                return $"scrolled {lines} line(s) over {widget?.Path ?? "nothing"}";
            }
            case "type":
                Type(query);
                return "typed into " + (Snapshot().Focused?.Path ?? "nothing (no focused widget)");
            case "key":
            {
                var key = ParseKey(args[0], out var mods);
                Key(key, mods);
                return $"pressed {args[0]}" + (Snapshot().Focused is { } focused ? $", focused {focused.Path}" : "");
            }
            case "focus":
            {
                var widget = Require(Snapshot(), query);
                Focus(widget.Widget);
                return "focused " + widget.Path;
            }
            case "leave":
                Leave();
                return "pointer left";
            case "tree":
            {
                var snapshot = Snapshot();
                return args.Count == 0 ? snapshot.ToString() : snapshot.Tree(Require(snapshot, query));
            }
            case "find":
            {
                var (widget, rect) = Snapshot().Locate(query) ?? throw new ArgumentException($"nothing matches '{query}'");
                var line = widget.Path + "\n" + UiSnapshot.Line(widget);
                return rect == widget.VisibleRect ? line : $"{line}\ntext at {UiSnapshot.F(rect.X)},{UiSnapshot.F(rect.Y)} {UiSnapshot.F(rect.Width)}x{UiSnapshot.F(rect.Height)}";
            }
            case "at":
            {
                var (point, _) = Target(args, 0, true, out _);
                return Snapshot().At(point) is { } widget ? widget.Path + "\n" + UiSnapshot.Line(widget) : "nothing";
            }
            case "issues":
            {
                var text = new StringBuilder();
                Snapshot().AppendIssues(text);
                return text.ToString();
            }
            case "json":
            case "nodes":
            {
                var json = Snapshot().ToJson(verb == "nodes");
                if (args.Count == 0)
                {
                    return json;
                }

                CreateDirectory(args[0]);
                File.WriteAllText(args[0], json);
                return "wrote " + args[0];
            }
            case "capture":
            {
                var region = args.Count > 1 ? Require(Snapshot(), string.Join(' ', args.Skip(1))).VisibleRect : (Rectangle?)null;
                Capture(args[0], region);
                return "wrote " + args[0];
            }
            case "save":
                return "wrote " + string.Join(", ", Save(args.Count > 0 ? args[0] : Path.Combine(_output, "ui")));
            default:
                throw new ArgumentException($"unknown command '{verb}', see help");
        }
    }

    // A target is x,y or x y in points, or a widget query: the rest of the line when rest is set, else one (quoted) argument.
    private static (Vector2 Point, UiWidgetInfo? Widget) Target(List<string> args, int start, bool rest, out int next)
    {
        if (args.Count <= start)
        {
            throw new ArgumentException("a target is missing, x,y or a widget query");
        }

        var snapshot = Snapshot();
        var comma = args[start].Split(',');
        Vector2? point = null;
        next = start + 1;
        if (comma.Length == 2 && IsNumber(comma[0]) && IsNumber(comma[1]))
        {
            point = new Vector2(Number(comma[0]), Number(comma[1]));
        }
        else if (args.Count > start + 1 && IsNumber(args[start]) && IsNumber(args[start + 1]))
        {
            point = new Vector2(Number(args[start]), Number(args[start + 1]));
            next = start + 2;
        }

        if (point is { } p)
        {
            return (p, snapshot.At(p));
        }

        var query = rest ? string.Join(' ', args.Skip(start)) : args[start];
        next = rest ? args.Count : start + 1;
        var (widget, rect) = snapshot.Locate(query) ?? throw new ArgumentException($"nothing matches '{query}'");
        return rect is { Width: > 0, Height: > 0 } ? (rect.Center, widget) : throw new ArgumentException($"{widget.Path} is not visible");
    }

    private static UiWidgetInfo Require(UiSnapshot snapshot, string query)
    {
        return snapshot.Find(query) ?? throw new ArgumentException($"no widget matches '{query}'");
    }

    private static KeyCode ParseKey(string combo, out KeyMod mods)
    {
        mods = 0;
        var parts = combo.Split('+');
        foreach (var mod in parts[..^1])
        {
            mods |= mod.ToLowerInvariant() switch
            {
                "shift" => KeyMod.Lshift,
                "ctrl" or "control" => KeyMod.Lctrl,
                "alt" or "option" => KeyMod.Lalt,
                "cmd" or "gui" or "super" or "meta" => KeyMod.Lgui,
                _ => throw new ArgumentException($"unknown modifier '{mod}'")
            };
        }

        var name = parts[^1];
        if (name.Length == 1)
        {
            return (KeyCode)char.ToLowerInvariant(name[0]);
        }

        name = name.ToLowerInvariant() switch
        {
            "enter" => "Return",
            "esc" => "Escape",
            "del" => "Delete",
            _ => name
        };
        return !IsNumber(name) && Enum.TryParse<KeyCode>(name, true, out var key) ? key : throw new ArgumentException($"unknown key '{name}'");
    }

    private static List<string> Tokenize(string command)
    {
        var tokens = new List<string>();
        var token = new StringBuilder();
        var quoted = false;
        var hasToken = false;
        foreach (var c in command)
        {
            if (c == '"')
            {
                quoted = !quoted;
                hasToken = true;
            }
            else if (char.IsWhiteSpace(c) && !quoted)
            {
                if (hasToken)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                    hasToken = false;
                }
            }
            else
            {
                token.Append(c);
                hasToken = true;
            }
        }

        if (hasToken)
        {
            tokens.Add(token.ToString());
        }

        return tokens;
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static bool IsNumber(string text)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private static float Number(string text)
    {
        return float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
