using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

public sealed class ChatView
{
    private enum Filter
    {
        All,
        Rolls,
        Talk
    }

    private static readonly int[] QuickDice = [4, 6, 8, 10, 12, 20, 100];
    private static readonly Color WhisperTint = Color.Hex(0xA77BF3FF);

    private readonly Campaign _campaign;
    private readonly Dropdown _speaker;
    private readonly TextField _input;
    private readonly ListView<ChatMessage> _log;

    private Filter _filter;
    private int _unread;

    public ChatView(Campaign campaign)
    {
        _campaign = campaign;
        _speaker = new Dropdown([Campaign.GmName, .. campaign.Party.Select(c => c.Name)]) { Width = 150, Selected = 0, Tooltip = "Speak as" };
        _input = new TextField { Placeholder = "Message, or /help", Flex = 1, MaxLength = 2000, OnSubmit = _ => Submit() };
        _log = new ListView<ChatMessage>(() => campaign.Chat.Messages, MessageRow)
        {
            Filter = Matches,
            FollowEnd = true,
            Padding = (8, 8, 14, 8),
            Gap = 2,
            OnReachEnd = () => _unread = 0
        };

        campaign.Chat.Posted += _ =>
        {
            if (!_log.IsAtEnd)
            {
                _unread++;
            }
        };

        var root = new Column
        {
            Flex = 1,
            Children =
            [
                Header(),
                LogArea(),
                QuickDiceRow(),
                Composer(),
                new Label("Enter sends · /r 2d6+3 Label · /me · /w name · /ooc · /init · /next · /spam 500", TextStyle.Hint) { Margin = (10, 0, 10, 8) }
            ]
        };
        Window = new FloatingWindow("Chat", root) { Icon = FontAwesome.Comments, MinSize = new(300, 260) };
    }

    public FloatingWindow Window { get; }

    public string Speaker
    {
        get
        {
            var index = _speaker.Selected.Value;
            return index >= 0 && index < _speaker.Items.Count ? _speaker.Items[index] : Campaign.GmName;
        }
    }

    public void FocusInput()
    {
        _input.Focus();
    }

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

    private Widget Header()
    {
        return new Row
        {
            Padding = (8, 6),
            Gap = 2,
            Border = Border.Bottom(Color.Border),
            Children =
            [
                new Segmented<Filter>([new(Filter.All, "All"), new(Filter.Rolls, "Rolls"), new(Filter.Talk, "Talk")])
                {
                    Selected = Bind(() => _filter, value =>
                    {
                        _filter = value;
                        JumpToEnd();
                    })
                },
                new Spacer(),
                new Label(() => $"{_campaign.Chat.Messages.Count} messages", TextStyle.Small),
                Kit.IconButton(FontAwesome.TrashCan, "Clear the chat log", ConfirmClear)
            ]
        };
    }

    private Widget LogArea()
    {
        return new Column
        {
            Flex = 1,
            Children =
            [
                _log,
                new Button(() => $"{_unread} new {(_unread == 1 ? "message" : "messages")}", FontAwesome.ArrowDown)
                {
                    Float = UIAnchor.BottomCenter,
                    Offset = new(0, -10),
                    Tone = Tone.Primary,
                    Small = true,
                    Visible = Bind(() => _unread > 0),
                    OnClick = JumpToEnd
                }
            ]
        };
    }

    private Widget QuickDiceRow()
    {
        return new Row
        {
            Padding = (8, 6),
            Gap = 4,
            Wrap = true,
            LineGap = 4,
            Border = Border.Top(Color.Border),
            Children = [new Icon(FontAwesome.Cube) { Size = 11, Color = Color.TextMuted }, .. QuickDice.Select(DieButton)]
        };
    }

    private Widget DieButton(int sides)
    {
        return new Button($"d{sides}")
        {
            Small = true,
            Tooltip = Bind<string?>(() => $"Roll 1d{sides} as {Speaker}"),
            OnClick = () => _campaign.Roll(Speaker, $"1d{sides}", null)
        };
    }

    private Widget Composer()
    {
        return new Row
        {
            Padding = (8, 0, 8, 6),
            Gap = 6,
            Children =
            [
                _speaker,
                _input,
                new Button { Icon = FontAwesome.PaperPlane, Tone = Tone.Primary, Tooltip = "Send (Enter)", OnClick = Submit }
            ]
        };
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

    private Widget MessageRow(ChatMessage message)
    {
        var row = message.Kind switch
        {
            MessageKind.System => SystemRow(message),
            MessageKind.Roll => RollCard(message),
            _ => TalkRow(message)
        };
        row.ContextMenu = () => MessageMenu(message);
        return row;
    }

    private static Panel SystemRow(ChatMessage message)
    {
        return new Row
        {
            Padding = (8, 5),
            Gap = 8,
            Align = UIAlign.Start,
            Radius = 6,
            HoverBackground = Color.Hover,
            Children =
            [
                new Row { Padding = (0, 2, 0, 0), Children = [new Icon(FontAwesome.CircleInfo) { Size = 11, Color = Color.TextMuted }] },
                new Paragraph(message.Text, TextStyle.Sized(12, Color.TextMuted)) { Flex = 1 }
            ]
        };
    }

    private static Panel TalkRow(ChatMessage message)
    {
        var whisper = message.Kind == MessageKind.Whisper;
        var header = new Row { Gap = 6, Children = [new Label(message.Speaker, TextStyle.Sized(13, message.SpeakerColor))] };
        if (whisper)
        {
            header.Children.Add(new Icon(FontAwesome.Envelope) { Size = 10, Color = Color.TextMuted });
            header.Children.Add(new Label(message.Target ?? "", TextStyle.Sized(12, Color.TextMuted)));
        }
        else if (message.Kind == MessageKind.OutOfCharacter)
        {
            header.Children.Add(Kit.Badge("OOC", Color.SurfaceAlt, Color.TextMuted));
        }

        header.Children.Add(new Spacer());
        header.Children.Add(TimeLabel(message));

        var body = message.Kind == MessageKind.Emote
            ? new Paragraph(message.Text, TextStyle.Sized(14)) { Color = Bind(() => Color.Mix(message.SpeakerColor, Color.Text, 0.45f)) }
            : new Paragraph(message.Text, TextStyle.Sized(14, message.Kind == MessageKind.OutOfCharacter ? Color.TextMuted : Color.Text));

        return new Column
        {
            Padding = (8, 6),
            Gap = 3,
            Radius = 6,
            Background = whisper ? WhisperTint.WithAlpha(30) : Color.Transparent,
            HoverBackground = whisper ? WhisperTint.WithAlpha(46) : Color.Hover,
            Children = [header, body]
        };
    }

    private static Panel RollCard(ChatMessage message)
    {
        var roll = message.Roll!;
        var result = roll.IsCritical ? Palette.Good : roll.IsFumble ? Palette.Bad : Color.Accent;
        var dice = new Row
        {
            Wrap = true,
            Gap = 4,
            LineGap = 4,
            Children = [new Label(roll.Formula, TextStyle.Sized(12, Color.TextMuted)), .. roll.Dice.Select(DieChip)]
        };
        if (roll.Modifier != 0)
        {
            dice.Children.Add(new Label(Fmt.Sign(roll.Modifier), TextStyle.Sized(12, Color.TextMuted)));
        }

        var card = new Column
        {
            Margin = (0, 3),
            Padding = (10, 8),
            Gap = 6,
            Background = Color.SurfaceAlt,
            HoverBackground = Color.Hover,
            Border = Border.Left(message.SpeakerColor, 3),
            Radius = 6,
            Children =
            [
                new Row { Gap = 6, Children = [new Label(message.Speaker, TextStyle.Sized(13, message.SpeakerColor)), new Spacer(), TimeLabel(message)] },
                new Row
                {
                    Gap = 10,
                    Children =
                    [
                        new Column { Flex = 1, Gap = 5, Children = [new Label(message.Label ?? "Roll", TextStyle.Sized(13)), dice] },
                        new Column
                        {
                            MinWidth = 54,
                            Height = 46,
                            Padding = (8, 0),
                            Justify = UIJustify.Center,
                            Align = UIAlign.Center,
                            Background = result.WithAlpha(36),
                            Border = new Border(result, 1.5f),
                            Radius = 8,
                            Tooltip = Bind<string?>(() => roll.Breakdown()),
                            Children = [new Label(Fmt.Int(Math.Max(0, roll.Total)), TextStyle.Sized(22))]
                        }
                    ]
                }
            ]
        };
        if (roll.IsCritical || roll.IsFumble)
        {
            card.Children.Add(new Label(roll.IsCritical ? "Natural 20!" : "Natural 1…", TextStyle.Sized(12, result)));
        }

        return card;
    }

    private static Widget DieChip(Die die)
    {
        var color = die.Sides == 20 && die.IsMax ? Palette.Good : die.Sides == 20 && die.IsMin ? Palette.Bad : Color.BorderStrong;
        return new Row
        {
            MinWidth = 22,
            Height = 20,
            Padding = (5, 0),
            Justify = UIJustify.Center,
            Border = color,
            Radius = 4,
            Children = [new Label(Fmt.Int(die.Value), TextStyle.Sized(11))]
        };
    }

    private static Label TimeLabel(ChatMessage message)
    {
        return new Label(message.TimeText, TextStyle.Sized(11, Color.TextDisabled));
    }

    private Menu MessageMenu(ChatMessage message)
    {
        var items = new WidgetList
        {
            new MenuItem("Copy text", FontAwesome.Copy)
            {
                OnClick = () =>
                {
                    using var text = new StringView.Pinned(message.Text);
                    Clipboard.SetText(text);
                }
            }
        };
        if (message.Roll is { } roll)
        {
            items.Add(new MenuItem("Roll again", FontAwesome.RotateRight) { OnClick = () => _campaign.Roll(message.Speaker, roll.Formula, message.Label) });
        }

        items.Add(new MenuSeparator());
        items.Add(new MenuItem("Delete message", FontAwesome.TrashCan) { OnClick = () => _campaign.Chat.Remove(message) });
        return new Menu { Items = items };
    }

    private void JumpToEnd()
    {
        _unread = 0;
        _log.ScrollToEnd();
    }

    private void ConfirmClear()
    {
        var count = _campaign.Chat.Messages.Count;
        Dialogs.Confirm("Clear the chat log?", $"This removes all {count} messages for everyone at the table. Rolls already made stay made.", "Clear log", Tone.Danger, () =>
        {
            _campaign.Chat.Clear();
            _campaign.Chat.System("The chat log was cleared.");
        });
    }

    private void Submit()
    {
        var text = _input.Text.Value.Trim();
        if (text.Length == 0)
        {
            return;
        }

        _input.Clear();
        JumpToEnd();
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
