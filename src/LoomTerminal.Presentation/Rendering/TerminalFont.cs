namespace LoomTerminal.Presentation.Rendering;

/// <summary>The font a terminal is drawn with.</summary>
/// <param name="Families">Font family names in fallback order.</param>
/// <param name="Size">Font size in DIPs.</param>
/// <param name="LineHeight">Line height multiplier.</param>
/// <param name="LetterSpacing">Extra space after each cell, in DIPs.</param>
/// <param name="Weight">Weight of normal text (CSS scale, 400 is regular).</param>
/// <param name="BoldWeight">Weight of bold text (CSS scale, 700 is bold).</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record TerminalFont(
    IReadOnlyList<string> Families,
    double Size,
    double LineHeight = 1.0,
    double LetterSpacing = 0,
    int Weight = 400,
    int BoldWeight = 700);
