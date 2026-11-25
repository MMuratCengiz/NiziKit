using System.Numerics;
using DenOfIz;
using NiziKit.UI;
using NiziKit.UI.Widgets;
using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

/// <summary>
/// The chat log: word-wrapped messages and dice cards in a scroll view that follows the end until the user scrolls up,
/// filter tabs, quick dice, a "speak as" picker and slash commands.
/// </summary>
public sealed class ChatView
{
    private enum Filter
    {
        All,
        Rolls,
        Talk
    }

    private static readonly int[] QuickDice = [4, 6, 8, 10, 12, 20, 100];
    private static readonly string[] QuickDiceLabels = QuickDice.Select(d => $"d{d}").ToArray();

    private readonly Campaign _campaign;
    private readonly ConfirmDialog _confirm;
    private readonly uint _logId = Ui.Id("Chat.Log");
    private readonly uint _jumpId = Ui.Id("Chat.Jump");
    private readonly uint _diceId = Ui.Id("Chat.Dice");
    private readonly Button[] _filters;
    private readonly Button _clear;
    private readonly Dropdown _speaker;
    private readonly TextBox _input;
    private readonly Button _send;
    private readonly Menu _messageMenu = new();

    private Filter _filter;
    private bool _stick = true;
    private bool _follow = true;
    private int _pendingScroll;
    private int _unread;
    private float _lastContentHeight;
    private float _lastViewportHeight;

    public ChatView(Campaign campaign, ConfirmDialog confirm)
    {
        _campaign = campaign;
        _confirm = confirm;

        _filters =
        [
            FilterButton("All", Filter.All),
            FilterButton("Rolls", Filter.Rolls),
            FilterButton("Talk", Filter.Talk)
        ];
        _clear = new Button { Icon = FontAwesome.TrashCan, Kind = ButtonKind.Ghost, Tooltip = "Clear the chat log", Padding = UILengthEdges.Symmetric(8, 5) };
        _clear.Clicked += _ => ConfirmClear();

        _speaker = new Dropdown(new[] { Campaign.GmName }.Concat(campaign.Party.Select(c => c.Name))) { Width = 150, Tooltip = "Speak as" };
        _input = new TextBox { Placeholder = "Message, or /help", Fill = true, MaxLength = 2000 };
        _input.Submitted += _ => Submit();
        _send = new Button { Icon = FontAwesome.PaperPlane, Kind = ButtonKind.Primary, Tooltip = "Send (Enter)", Padding = UILengthEdges.Symmetric(10, 7) };
        _send.Clicked += _ => Submit();

        campaign.Chat.Posted += _ =>
        {
            _follow = true;
            if (!_stick)
            {
                _unread++;
            }
        };

        Window = new FloatingWindow("Chat", Im.View(Draw)) { Icon = FontAwesome.Comments, MinSize = new Vector2(300, 260) };
    }

    public FloatingWindow Window { get; }

    public string Speaker => _speaker.SelectedItem ?? Campaign.GmName;

    public void FocusInput()
    {
        _input.Focus();
    }

    /// <summary>Posts <paramref name="count"/> mixed messages to stress the log's layout and text.</summary>
    public void Spam(int count)
    {
        var random = new Random(count);
        var paragraphs = _campaign.Journal.SelectMany(j => j.Blocks).Select(b => b.Text).ToArray();
        var speakers = _campaign.Party.ToArray();
        for (var i = 0; i < count; i++)
        {
            var who = speakers[random.Next(speakers.Length)];
            switch (random.Next(4))
            {
                case 0:
                    _campaign.Chat.PostRoll(who.Name, who.Color, Dice.Roll($"{random.Next(1, 5)}d{QuickDice[random.Next(QuickDice.Length)]}+{random.Next(0, 9)}"), "Stress roll");
                    break;
                case 1:
                    _campaign.Chat.Post(MessageKind.Emote, who.Name, who.Color, "looks around nervously and checks the exits for the third time.");
                    break;
                default:
                    var text = paragraphs[random.Next(paragraphs.Length)];
                    _campaign.Chat.Post(MessageKind.Chat, who.Name, who.Color, random.Next(3) == 0 ? text : text[..Math.Min(text.Length, random.Next(20, 160))]);
                    break;
            }
        }

        _campaign.Chat.System($"Stress test: posted {count} messages.");
    }

    private Button FilterButton(string text, Filter filter)
    {
        var button = new Button(text) { Kind = ButtonKind.Ghost, FontSize = 12, Padding = UILengthEdges.Symmetric(10, 4) };
        button.Clicked += _ =>
        {
            _filter = filter;
            _stick = true;
            _follow = true;
            _unread = 0;
        };
        return button;
    }

    private void Draw()
    {
        var theme = Ui.Theme;
        TrackScroll();

        using (Ui.Row().Padding(8, 6).Gap(2).AlignItems(UIAlign.Center).Shrink(0).Border(theme.Border, new UIEdges(0, 0, 0, 1)).Open())
        {
            for (var i = 0; i < _filters.Length; i++)
            {
                _filters[i].Checked = (int)_filter == i;
                Ui.Draw(_filters[i]);
            }

            Im.Spacer();
            Ui.Text($"{_campaign.Chat.Messages.Count} messages", 11, theme.TextMuted);
            Ui.Draw(_clear);
        }

        using (Ui.Column().Fill().Open())
        {
            using (Ui.Column(_logId).Fill().ScrollY().Padding(8, 8, 14, 8).Gap(2).Open())
            {
                var messages = _campaign.Chat.Messages;
                for (var i = 0; i < messages.Count; i++)
                {
                    if (Matches(messages[i]))
                    {
                        DrawMessage(messages[i]);
                    }
                }
            }

            if (!_stick && _unread > 0)
            {
                using (Ui.Row().Absolute(left: 0, right: 0, bottom: 10).Justify(UIJustify.Center).Passthrough().Open())
                {
                    if (Im.Chip(_jumpId, FontAwesome.ArrowDown, _unread == 1 ? "1 new message" : $"{_unread} new messages", ChipKind.Accent))
                    {
                        _stick = true;
                        _follow = true;
                        _unread = 0;
                    }
                }
            }
        }

        using (Ui.Row().Padding(8, 6).Gap(4).Wrap().LineGap(4).AlignItems(UIAlign.Center).Shrink(0).Border(theme.Border, new UIEdges(0, 1, 0, 0)).Open())
        {
            Im.Icon(FontAwesome.Cube, 11, theme.TextMuted);
            for (var i = 0; i < QuickDice.Length; i++)
            {
                if (Im.Chip(Ui.Id(_diceId, (uint)i), null, QuickDiceLabels[i], tooltip: $"Roll 1d{QuickDice[i]} as {Speaker}"))
                {
                    _campaign.Roll(Speaker, $"1d{QuickDice[i]}", null);
                }
            }
        }

        using (Ui.Row().Padding(8, 0, 8, 6).Gap(6).AlignItems(UIAlign.Center).Shrink(0).Open())
        {
            Ui.Draw(_speaker);
            Ui.Draw(_input);
            Ui.Draw(_send);
        }

        using (Ui.Row().Padding(10, 0, 10, 8).Shrink(0).Open())
        {
            Im.Line("Enter sends · /r 2d6+3 Label · /me · /w name · /ooc · /init · /next · /spam 500", 11, theme.TextDisabled);
        }

        if (_follow && _stick)
        {
            Ui.ScrollToEnd(_logId);
            _pendingScroll = 3;
        }

        _follow = false;
    }

    /// <summary>Follows the end of the log while the user is at the bottom, and keeps following when wrapping or new content grows it.</summary>
    private void TrackScroll()
    {
        if (!Ui.TryGetScroll(_logId, out var scroll))
        {
            return;
        }

        var atBottom = scroll.Offset.Y >= scroll.MaxOffset.Y - 24;
        var resized = MathF.Abs(scroll.ContentSize.Y - _lastContentHeight) > 0.5f || MathF.Abs(scroll.ViewportSize.Y - _lastViewportHeight) > 0.5f;
        _lastContentHeight = scroll.ContentSize.Y;
        _lastViewportHeight = scroll.ViewportSize.Y;

        if (_pendingScroll > 0)
        {
            _pendingScroll = atBottom ? 0 : _pendingScroll - 1;
        }
        else if (_stick && resized && !atBottom)
        {
            _follow = true;
        }
        else
        {
            _stick = atBottom;
        }

        if (_stick)
        {
            _unread = 0;
        }
    }

    private bool Matches(ChatMessage message)
    {
        return _filter switch
        {
            Filter.Rolls => message.Kind == MessageKind.Roll,
            Filter.Talk => message.Kind is MessageKind.Chat or MessageKind.Emote or MessageKind.Whisper or MessageKind.OutOfCharacter,
            _ => true
        };
    }

    private void DrawMessage(ChatMessage message)
    {
        var theme = Ui.Theme;
        var id = Ui.Id(_logId, message.Id);
        var interaction = Ui.Interaction(id);
        if (interaction.RightClicked)
        {
            OpenMessageMenu(message);
        }

        var hover = interaction.Hovered ? theme.Hover : default;
        switch (message.Kind)
        {
            case MessageKind.System:
                using (Ui.Row(id).Padding(8, 5).Gap(8).AlignItems(UIAlign.Start).CornerRadius(6).Background(hover).Open())
                {
                    using (Ui.Row().Padding(0, 2, 0, 0).Shrink(0).Open())
                    {
                        Im.Icon(FontAwesome.CircleInfo, 11, theme.TextMuted);
                    }

                    using (Ui.Column().Fill().Open())
                    {
                        Im.Para(message.Text, 12, theme.TextMuted);
                    }
                }

                break;
            case MessageKind.Roll:
                DrawRoll(id, message, interaction.Hovered);
                break;
            default:
                var whisper = message.Kind == MessageKind.Whisper;
                var background = whisper ? Im.Alpha(UIColor.Hex(0xA77BF3FF), interaction.Hovered ? (byte)46 : (byte)30) : hover;
                using (Ui.Column(id).Padding(8, 6).Gap(3).CornerRadius(6).Background(background).Open())
                {
                    using (Ui.Row().Gap(6).AlignItems(UIAlign.Center).Open())
                    {
                        Im.Line(message.Speaker, 13, message.SpeakerColor);
                        if (whisper)
                        {
                            Im.Icon(FontAwesome.Envelope, 10, theme.TextMuted);
                            Im.Line(message.Target ?? "", 12, theme.TextMuted);
                        }
                        else if (message.Kind == MessageKind.OutOfCharacter)
                        {
                            Im.Badge("OOC", theme.SurfaceAlt, theme.TextMuted);
                        }

                        Im.Spacer();
                        Im.Line(message.TimeText, 11, theme.TextDisabled);
                    }

                    if (message.Kind == MessageKind.Emote)
                    {
                        Im.Para(message.Text, 14, Im.Mix(message.SpeakerColor, theme.Text, 0.45f));
                    }
                    else
                    {
                        Im.Para(message.Text, 14, message.Kind == MessageKind.OutOfCharacter ? theme.TextMuted : theme.Text);
                    }
                }

                break;
        }
    }

    private void DrawRoll(uint id, ChatMessage message, bool hovered)
    {
        var theme = Ui.Theme;
        var roll = message.Roll!;
        var result = roll.IsCritical ? Im.Good : roll.IsFumble ? Im.Bad : theme.Accent;
        using (Ui.Column(id)
                   .Margin(0, 3)
                   .Padding(10, 8)
                   .Gap(6)
                   .Background(hovered ? theme.Hover : theme.SurfaceAlt)
                   .Border(message.SpeakerColor, new UIEdges(3, 0, 0, 0))
                   .CornerRadius(6)
                   .Open())
        {
            using (Ui.Row().Gap(6).AlignItems(UIAlign.Center).Open())
            {
                Im.Line(message.Speaker, 13, message.SpeakerColor);
                Im.Spacer();
                Im.Line(message.TimeText, 11, theme.TextDisabled);
            }

            using (Ui.Row().Gap(10).AlignItems(UIAlign.Center).Open())
            {
                using (Ui.Column().Fill().Gap(5).Open())
                {
                    Im.Line(message.Label ?? "Roll", 13, theme.Text);
                    using (Ui.Row().Wrap().Gap(4).LineGap(4).AlignItems(UIAlign.Center).Open())
                    {
                        Im.Line(roll.Formula, 12, theme.TextMuted);
                        foreach (var die in roll.Dice)
                        {
                            var color = die.Sides == 20 && die.IsMax ? Im.Good : die.Sides == 20 && die.IsMin ? Im.Bad : theme.BorderStrong;
                            using (Ui.Row().MinWidth(22).Height(20).Padding(5, 0).Center().Border(color, 1).CornerRadius(4).Open())
                            {
                                Im.Line(Fmt.Int(die.Value), 11, theme.Text);
                            }
                        }

                        if (roll.Modifier != 0)
                        {
                            Im.Line(Fmt.Sign(roll.Modifier), 12, theme.TextMuted);
                        }
                    }
                }

                var totalId = Ui.Id(id, 7);
                using (Ui.Column(totalId).MinWidth(54).Height(46).Padding(8, 0).Shrink(0).Center()
                           .Background(Im.Alpha(result, 36)).Border(result, 1.5f).CornerRadius(8).Open())
                {
                    Im.Line(Fmt.Int(Math.Max(0, roll.Total)), 22, theme.Text);
                }

                if (Ui.Interaction(totalId).Hovered)
                {
                    Ui.Tooltip(totalId, roll.Breakdown());
                }
            }

            if (roll.IsCritical || roll.IsFumble)
            {
                Im.Line(roll.IsCritical ? "Natural 20!" : "Natural 1…", 12, result);
            }
        }
    }

    private void OpenMessageMenu(ChatMessage message)
    {
        _messageMenu.ClearItems();
        _messageMenu.Add("Copy text", () =>
        {
            using var text = new StringView.Pinned(message.Text);
            Clipboard.SetText(text);
        }, FontAwesome.Copy);
        if (message.Roll is { } roll)
        {
            _messageMenu.Add("Roll again", () => _campaign.Roll(message.Speaker, roll.Formula, message.Label), FontAwesome.RotateRight);
        }

        _messageMenu.AddSeparator();
        _messageMenu.Add("Delete message", () => _campaign.Chat.Remove(message), FontAwesome.TrashCan);
        _messageMenu.ShowAt(Ui.PointerPosition);
    }

    private void ConfirmClear()
    {
        var count = _campaign.Chat.Messages.Count;
        _confirm.Show("Clear the chat log?", $"This removes all {count} messages for everyone at the table. Rolls already made stay made.", "Clear log", () =>
        {
            _campaign.Chat.Clear();
            _campaign.Chat.System("The chat log was cleared.");
        });
    }

    private void Submit()
    {
        var text = _input.Text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        _input.Clear();
        _stick = true;
        _follow = true;
        Execute(text);
    }

    private void Execute(string text)
    {
        var chat = _campaign.Chat;
        var speaker = Speaker;
        var color = _campaign.ColorOf(speaker);
        if (!text.StartsWith('/'))
        {
            chat.Post(MessageKind.Chat, speaker, color, text);
            return;
        }

        var space = text.IndexOf(' ');
        var command = (space < 0 ? text : text[..space]).ToLowerInvariant();
        var rest = space < 0 ? "" : text[(space + 1)..].Trim();
        switch (command)
        {
            case "/r":
            case "/roll":
                var formulaEnd = rest.IndexOf(' ');
                var formula = formulaEnd < 0 ? rest : rest[..formulaEnd];
                var label = formulaEnd < 0 ? null : rest[(formulaEnd + 1)..].Trim();
                if (formula.Length == 0)
                {
                    formula = "1d20";
                }

                if (Dice.TryRoll(formula, out _, out var error))
                {
                    _campaign.Roll(speaker, formula, string.IsNullOrEmpty(label) ? null : label);
                }
                else
                {
                    chat.System(error);
                }

                break;
            case "/me":
            case "/em":
                chat.Post(MessageKind.Emote, speaker, color, rest);
                break;
            case "/w":
            case "/whisper":
                var nameEnd = rest.IndexOf(' ');
                var target = nameEnd < 0 ? null : _campaign.Find(rest[..nameEnd]);
                if (target == null && !rest.StartsWith("gm", StringComparison.OrdinalIgnoreCase))
                {
                    chat.System("Whisper to whom? Try /w tamsin Meet me by the door.");
                    break;
                }

                chat.Post(MessageKind.Whisper, speaker, color, rest[(nameEnd + 1)..].Trim(), target?.Name ?? Campaign.GmName);
                break;
            case "/ooc":
                chat.Post(MessageKind.OutOfCharacter, speaker, color, rest);
                break;
            case "/init":
                _campaign.RollInitiative();
                break;
            case "/next":
                _campaign.NextTurn();
                break;
            case "/clear":
                ConfirmClear();
                break;
            case "/x":
                chat.System("Someone tapped the X-card. The scene moves on - no questions asked.");
                break;
            case "/spam":
                Spam(int.TryParse(rest, out var count) ? Math.Clamp(count, 1, 20000) : 200);
                break;
            case "/help":
                chat.System("Commands: /r <dice> [label] rolls (e.g. /r 2d6+3 Damage), /me <action> emotes, /w <name> <text> whispers, /ooc <text> speaks out of character, /init rolls initiative for everyone, /next advances the turn, /clear clears the log, /x uses the X-card, /spam <n> posts n messages as a stress test. Right-click a message to copy, re-roll or delete it.");
                break;
            default:
                chat.System($"Unknown command {command}. Type /help for the list.");
                break;
        }
    }
}
