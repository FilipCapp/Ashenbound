using Godot;

namespace Ashenbound;

public static class ArtData
{
    public static readonly string[] Bearer =
    {
        "....oooo....", "...ohhhho...", "...ohgggo...", "...ohgggo...", "....orrro...",
        "..oocttco...", ".obbcctcco..", ".obbcttcco..", ".obbcctcco..", "..oocctcco..",
        "...occtcco..", "...occttcco.", "..occtttcco.", "...oddoddo..", "...oddoddo..", "..owwoowwo.."
    };

    public static readonly string[] Hulk =
    {
        "..oooooooo..", ".ohhhhhhho..", "ohgghhggho..", "ohhhwwhhho..", ".ohhwwhho...",
        "..owwwwo....", ".oppssppo...", "oppssssppo..", "oppssssppo..", ".oppssppo...",
        "..oppssppo..", ".oppssppo...", ".orrrrrrro..", "..odooddo...", "..odooddo...", "..oddoooddo."
    };

    public static readonly string[] Wisp =
    {
        ".....tt.....", "....tttt....", "...tgggtt...", "..ttggggtt..", "..tggwwggt..",
        "...twwwwt...", "..ttwrwwtt..", ".ttwrrrrwtt.", "..ttwwwwtt..", "...ttwwtt...",
        "....tttt....", "...t..t.....", "..t....t....", "............", "............", "............"
    };

    public static readonly string[] Acolyte =
    {
        "....oooo....", "...oppppo...", "..opggggpo..", "..opggggpo..", "...oppppo...",
        "..oowwwwoo..", ".oppsssppo..", "oppssssssppo", "oppssgsssppo", "oppssssssppo",
        ".oppsssppo..", "..oppssppo..", ".oppssssppo..", "..orrrrrro...", "..odooddo....", "..oddoooddo.."
    };

    public static readonly string[] Bishop =
    {
        ".....gg.....", "....gggg....", "...oggggo...", "..oggggggo..", ".oggggggggo.",
        "..ohhhhho...", "..ohgggho...", "...orrro....", "..oppsppo...", ".oppsssppo..",
        ".opsggsspo..", ".opsggsspo..", ".opsssssppo.", "opsssssssppo", "orrrrrrrrrro", "..oddoooddo."
    };

    public static readonly string[][] Maps = { Bearer, Hulk, Wisp, Acolyte, Bishop };

    public static Color PixelColor(char c)
    {
        return c switch
        {
            'o' => new Color(14 / 255f, 15 / 255f, 25 / 255f),
            'h' => new Color(58 / 255f, 58 / 255f, 72 / 255f),
            'g' => C(Palette.Gold),
            'r' => C(Palette.Blood),
            'c' => new Color(28 / 255f, 92 / 255f, 101 / 255f),
            't' => C(Palette.Ember),
            'b' => C(Palette.Violet),
            'w' => C(Palette.Ash),
            'd' => new Color(38 / 255f, 38 / 255f, 52 / 255f),
            'p' => new Color(91 / 255f, 60 / 255f, 104 / 255f),
            's' => new Color(172 / 255f, 82 / 255f, 91 / 255f),
            _ => new Color(0, 0, 0, 0)
        };
    }

    public static Color C(System.Drawing.Color c)
    {
        return new Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
    }
}
