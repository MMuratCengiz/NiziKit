using DenOfIz;

namespace VttDemo.Model;

public enum MessageKind
{
    Chat,
    Emote,
    Whisper,
    OutOfCharacter,
    System,
    Roll
}

public sealed class ChatMessage
{
    public uint Id { get; init; }
    public MessageKind Kind { get; init; }
    public string Speaker { get; init; } = "";
    public UIColor SpeakerColor { get; init; }
    public string Text { get; init; } = "";
    public DateTime Time { get; init; }
    public string TimeText { get; init; } = "";

    /// <summary>The whisper's recipient.</summary>
    public string? Target { get; init; }

    public RollResult? Roll { get; init; }

    /// <summary>What a roll was for, e.g. "Perception" or "Longsword attack".</summary>
    public string? Label { get; init; }
}

public sealed class ChatLog
{
    private readonly List<ChatMessage> _messages = new();
    private uint _nextId;

    public IReadOnlyList<ChatMessage> Messages => _messages;

    /// <summary>Bumped on every change, so views can tell when to re-filter or follow the end.</summary>
    public int Version { get; private set; }

    public event Action<ChatMessage>? Posted;

    public ChatMessage Post(MessageKind kind, string speaker, UIColor color, string text, string? target = null)
    {
        return Add(new ChatMessage
        {
            Id = ++_nextId,
            Kind = kind,
            Speaker = speaker,
            SpeakerColor = color,
            Text = text,
            Target = target,
            Time = DateTime.Now,
            TimeText = DateTime.Now.ToString("HH:mm")
        });
    }

    public ChatMessage System(string text)
    {
        return Post(MessageKind.System, "", default, text);
    }

    public ChatMessage PostRoll(string speaker, UIColor color, RollResult roll, string? label)
    {
        return Add(new ChatMessage
        {
            Id = ++_nextId,
            Kind = MessageKind.Roll,
            Speaker = speaker,
            SpeakerColor = color,
            Roll = roll,
            Label = label,
            Text = $"{label ?? "Roll"}: {roll.Formula} = {roll.Total}",
            Time = DateTime.Now,
            TimeText = DateTime.Now.ToString("HH:mm")
        });
    }

    public bool Remove(ChatMessage message)
    {
        if (!_messages.Remove(message))
        {
            return false;
        }

        Version++;
        return true;
    }

    public void Clear()
    {
        _messages.Clear();
        Version++;
    }

    private ChatMessage Add(ChatMessage message)
    {
        _messages.Add(message);
        Version++;
        Posted?.Invoke(message);
        return message;
    }
}
