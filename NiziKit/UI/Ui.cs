using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.UI;

/// <summary>
/// Thin immediate-mode facade over the DenOfIz Clay UI library.
/// Call <see cref="Initialize"/> from Game.Load, forward events via <see cref="HandleEvent"/>,
/// then per frame: <see cref="BeginFrame"/>, declare elements, <see cref="EndFrame"/>
/// (inside an active render pass).
/// </summary>
public static class Ui
{
    private static Clay? _clay;
    private static readonly Dictionary<string, uint> IdCache = new();

    public static Clay Clay => _clay ?? throw new InvalidOperationException("Ui not initialized. Call Ui.Initialize() from Game.Load.");

    /// <summary>
    /// True if the pointer was over any pointer-capturing element in the last declared frame.
    /// Published at <see cref="EndFrame"/>; read it in Update (standard imgui pattern).
    /// </summary>
    public static bool IsPointerOverUi { get; private set; }

    public static void Initialize()
    {
        if (_clay != null)
        {
            return;
        }

        _clay = new Clay(new ClayDesc
        {
            LogicalDevice = GraphicsContext.Device,
            ResourceTracking = GraphicsContext.ResourceTracking,
            RenderTargetFormat = GraphicsContext.BackBufferFormat,
            NumFrames = GraphicsContext.NumFrames,
            Width = GraphicsContext.Width,
            Height = GraphicsContext.Height,
            MaxNumElements = 8192,
            MaxNumTextMeasureCacheElements = 16384,
            MaxNumFonts = 4
        });
        _clay.SetViewportSize(GraphicsContext.Width, GraphicsContext.Height);
    }

    public static void Shutdown()
    {
        if (_clay == null)
        {
            return;
        }

        GraphicsContext.WaitIdle();
        _clay.Dispose();
        _clay = null;
        IdCache.Clear();
    }

    public static void HandleEvent(ref Event ev)
    {
        Clay.HandleEvent(in ev);
    }

    public static void BeginFrame(float dt)
    {
        Clay.UpdateScrollContainers(false, Vector2.Zero, dt);
        Clay.BeginLayout();
    }

    public static void EndFrame(uint frameIndex, float dt, CommandList commandList)
    {
        Clay.EndLayout(frameIndex, dt, commandList);
    }

    public static UiElement Element(string? id = null)
    {
        return new UiElement(id == null ? 0u : Id(id), ClayLayoutDirection.LeftToRight);
    }

    public static UiElement Row(string? id = null)
    {
        return Element(id);
    }

    public static UiElement Column(string? id = null)
    {
        return new UiElement(id == null ? 0u : Id(id), ClayLayoutDirection.TopToBottom);
    }

    public static void Text(string text, int fontSize, UiColor color)
    {
        var desc = ClayTextDesc.Default();
        desc.TextColor = color.ToClay();
        desc.FontId = 0;
        desc.FontSize = (ushort)fontSize;
        desc.WrapMode = ClayTextWrapMode.Words;
        Clay.Text(text, in desc);
    }

    /// <summary>Declares a button, returns true if it was clicked this frame.</summary>
    public static bool Button(string id, string label, UiButtonStyle style = default)
    {
        if (style.FontSize == 0)
        {
            style = UiButtonStyle.Default;
        }

        var elementId = Id(id);
        var hovered = PointerOver(elementId);
        var pressed = Clay.ElementPressed(elementId);
        var background = pressed ? style.Pressed : hovered ? style.Hover : style.Background;

        var element = Element()
            .WithId(elementId)
            .Height(style.Height > 0 ? style.Height : 32)
            .Background(background)
            .CornerRadius(6)
            .CenterChildren();
        element = style.Width > 0 ? element.Width(style.Width) : element.GrowWidth();
        using (element.Open())
        {
            Text(label, style.FontSize, style.TextColor);
        }

        return Clay.ElementClicked(elementId);
    }

    public static void Spacer()
    {
        Element().Grow().OpenClose();
    }

    public static bool PointerOver(string id)
    {
        return PointerOver(Id(id));
    }

    public static bool Clicked(string id)
    {
        return Clay.ElementClicked(Id(id));
    }

    public static void SetScrollPosition(string id, Vector2 scrollPosition)
    {
        Clay.SetScrollPosition(Id(id), scrollPosition);
    }

    private static uint Id(string name)
    {
        if (IdCache.TryGetValue(name, out var id))
        {
            return id;
        }

        id = Clay.HashString(name, 0, 0);
        IdCache[name] = id;
        return id;
    }

    private static bool PointerOver(uint id)
    {
        return Clay.PointerOver(id);
    }
}

public struct UiButtonStyle
{
    public UiColor Background;
    public UiColor Hover;
    public UiColor Pressed;
    public UiColor TextColor;
    public int FontSize;
    public float Width;
    public float Height;

    public static UiButtonStyle Default => new()
    {
        Background = UiColor.Rgb(58, 66, 86),
        Hover = UiColor.Rgb(74, 84, 108),
        Pressed = UiColor.Rgb(46, 52, 70),
        TextColor = UiColor.Rgb(235, 235, 240),
        FontSize = 14,
        Width = 0,
        Height = 32
    };
}
