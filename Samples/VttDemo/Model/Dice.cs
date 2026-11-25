using System.Text;
using System.Text.RegularExpressions;

namespace VttDemo.Model;

public readonly record struct Die(int Sides, int Value)
{
    public bool IsMax => Value == Sides;
    public bool IsMin => Value == 1;
}

public sealed class RollResult
{
    public required string Formula { get; init; }
    public required List<Die> Dice { get; init; }
    public int Modifier { get; init; }
    public int Total { get; init; }

    /// <summary>A natural 20 on a roll of a single d20.</summary>
    public bool IsCritical => SingleD20 is { Value: 20 };

    public bool IsFumble => SingleD20 is { Value: 1 };

    private Die? SingleD20 => Dice.Count == 1 && Dice[0].Sides == 20 ? Dice[0] : null;

    public string Breakdown()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Dice.Count; i++)
        {
            sb.Append(i == 0 ? "" : " + ").Append(Dice[i].Value);
        }

        if (Modifier != 0)
        {
            sb.Append(Modifier > 0 ? " + " : " - ").Append(Math.Abs(Modifier));
        }

        return sb.ToString();
    }
}

/// <summary>Parses and rolls sums of dice and constants: <c>1d20+5</c>, <c>2d6 + 1d4 - 1</c>, <c>d100</c>.</summary>
public static partial class Dice
{
    private const int MaxDice = 200;
    private const int MaxSides = 1000;

    public static Random Random { get; set; } = new();

    [GeneratedRegex(@"\G([+-])?(?:(\d*)d(\d+)|(\d+))", RegexOptions.IgnoreCase)]
    private static partial Regex Term();

    public static RollResult Roll(string formula)
    {
        if (!TryRoll(formula, out var result, out var error))
        {
            throw new FormatException(error);
        }

        return result;
    }

    public static bool TryRoll(string formula, out RollResult result, out string error)
    {
        result = null!;
        error = "";
        var text = formula.Replace(" ", "");
        if (text.Length == 0)
        {
            error = "Empty formula.";
            return false;
        }

        var dice = new List<Die>();
        var modifier = 0;
        var total = 0;
        var position = 0;
        var regex = Term();
        while (position < text.Length)
        {
            var match = regex.Match(text, position);
            if (!match.Success || match.Length == 0)
            {
                error = $"Can't read \"{formula}\" at \"{text[position..]}\". Try something like 1d20+5.";
                return false;
            }

            var sign = match.Groups[1].Value == "-" ? -1 : 1;
            if (match.Groups[3].Success)
            {
                var count = match.Groups[2].Value.Length == 0 ? 1 : int.Parse(match.Groups[2].Value);
                var sides = int.Parse(match.Groups[3].Value);
                if (count < 1 || count > MaxDice || sides < 2 || sides > MaxSides)
                {
                    error = $"Dice must be 1-{MaxDice} dice of 2-{MaxSides} sides.";
                    return false;
                }

                for (var i = 0; i < count; i++)
                {
                    var value = Random.Next(1, sides + 1);
                    dice.Add(new Die(sides, value));
                    total += sign * value;
                }
            }
            else
            {
                var value = sign * int.Parse(match.Groups[4].Value);
                modifier += value;
                total += value;
            }

            position += match.Length;
        }

        result = new RollResult { Formula = text.ToLowerInvariant(), Dice = dice, Modifier = modifier, Total = total };
        return true;
    }

    public static string WithModifier(string dice, int modifier)
    {
        return modifier == 0 ? dice : modifier > 0 ? $"{dice}+{modifier}" : $"{dice}{modifier}";
    }
}
