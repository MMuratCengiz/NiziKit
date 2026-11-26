using DenOfIz;

namespace NiziKit.UI;

public static class Binding
{
    public static Prop<T> Bind<T>(Func<T> get)
    {
        return new Prop<T>(get);
    }

    public static Bound<T> Bind<T>(Func<T> get, Action<T> set)
    {
        return new Bound<T>(get, set);
    }
}

public readonly struct Prop<T>
{
    private readonly T _value;
    private readonly Func<T>? _get;

    public Prop(T value)
    {
        _value = value;
        _get = null;
    }

    internal Prop(Func<T> get)
    {
        _value = default!;
        _get = get;
    }

    public bool IsBound => _get != null;

    public T Value => _get != null ? _get() : _value;

    internal Func<T>? Getter => _get;

    public static implicit operator Prop<T>(T value)
    {
        return new Prop<T>(value);
    }
}

public struct Bound<T>
{
    private T _value;
    private readonly Func<T>? _get;
    private readonly Action<T>? _set;

    public Bound(T value)
    {
        _value = value;
        _get = null;
        _set = null;
    }

    internal Bound(Func<T> get, Action<T>? set)
    {
        _value = default!;
        _get = get;
        _set = set;
    }

    public readonly T Value => _get != null ? _get() : _value;

    internal void Set(T value)
    {
        if (_get == null)
        {
            _value = value;
            return;
        }

        _set?.Invoke(value);
    }

    public static implicit operator Bound<T>(T value)
    {
        return new Bound<T>(value);
    }

    public static implicit operator Bound<T>(Prop<T> prop)
    {
        return prop.Getter is { } get ? new Bound<T>(get, null) : new Bound<T>(prop.Value);
    }
}

public delegate FormattedText TextSource();

public readonly struct TextValue
{
    private readonly string? _text;
    private readonly TextSource? _source;

    public TextValue(string? text)
    {
        _text = text;
        _source = null;
    }

    public TextValue(TextSource source)
    {
        _text = null;
        _source = source;
    }

    public bool IsEmpty => _source == null && string.IsNullOrEmpty(_text);

    public bool IsBound => _source != null;

    internal ReadOnlySpan<char> Read()
    {
        return _source != null ? _source().Text : _text.AsSpan();
    }

    public override string ToString()
    {
        return new string(Read());
    }

    public static implicit operator TextValue(string? text)
    {
        return new TextValue(text);
    }
}

public readonly struct ColorProp
{
    private readonly Color _value;
    private readonly Func<Color>? _get;
    private readonly Func<UIColor>? _getRaw;

    private ColorProp(Color value, Func<Color>? get, Func<UIColor>? getRaw)
    {
        _value = value;
        _get = get;
        _getRaw = getRaw;
    }

    public bool IsSet => _get != null || _getRaw != null || _value.IsSet;

    public Color Value => _get != null ? _get() : _getRaw != null ? _getRaw() : _value;

    public static implicit operator ColorProp(Color value)
    {
        return new ColorProp(value, null, null);
    }

    public static implicit operator ColorProp(UIColor value)
    {
        return new ColorProp(value, null, null);
    }

    public static implicit operator ColorProp(Prop<Color> prop)
    {
        return new ColorProp(prop.IsBound ? default : prop.Value, prop.Getter, null);
    }

    public static implicit operator ColorProp(Prop<UIColor> prop)
    {
        return new ColorProp(prop.IsBound ? default : prop.Value, null, prop.Getter);
    }
}
