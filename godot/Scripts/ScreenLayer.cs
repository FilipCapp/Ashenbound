using System;
using Godot;
using RectangleF = System.Drawing.RectangleF;

namespace Ashenbound;

public partial class ScreenLayer : Control
{
    public Font Font;
    public bool ReduceMotion;
    public event Action<int> Activated;

    readonly System.Collections.Generic.List<Button> buttons = new System.Collections.Generic.List<Button>();
    Game game;
    float clock;
    int signature = -1;

    static readonly Color Ash = ArtData.C(Palette.Ash);
    static readonly Color Ember = ArtData.C(Palette.Ember);
    static readonly Color Blood = ArtData.C(Palette.Blood);
    static readonly Color Gold = ArtData.C(Palette.Gold);
    static readonly Color Cyan = ArtData.C(Palette.Cyan);
    static readonly Color Ink = ArtData.C(Palette.Ink);
    static readonly Color Muted = ArtData.C(Palette.Muted);

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Sync(Game value, float time)
    {
        game = value;
        clock = time;
        int next = Signature();
        if (next != signature)
        {
            RebuildButtons();
            signature = next;
        }
        QueueRedraw();
    }

    int Signature()
    {
        if (game == null) return -1;
        int value = ((int)game.State + 1) * 10000;
        if (game.Ending) value += 4000;
        value += game.Room;
        if (game.State == Stage.Upgrade)
        {
            for (int i = 0; i < game.UpgradeChoices.Length; i++) value = value * 31 + (int)game.UpgradeChoices[i] + 1;
        }
        if (game.State == Stage.Abilities)
        {
            for (int i = 0; i < game.RelicStacks.Length; i++) value = value * 17 + game.RelicStacks[i] + 1;
        }
        return value;
    }

    void RebuildButtons()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            RemoveChild(buttons[i]);
            buttons[i].QueueFree();
        }
        buttons.Clear();
        if (game == null) return;
        RectangleF[] items = MenuLayout.Items(game.State, game.Ending);
        for (int i = 0; i < items.Length; i++)
        {
            RectangleF rect = items[i];
            Button button = new Button();
            button.Position = new Vector2(rect.X, rect.Y);
            button.Size = new Vector2(rect.Width, rect.Height);
            button.Flat = true;
            button.FocusMode = FocusModeEnum.None;
            button.MouseFilter = MouseFilterEnum.Stop;
            button.Modulate = new Color(1, 1, 1, 0);
            int index = i;
            button.Pressed += () => Activated?.Invoke(index);
            AddChild(button);
            buttons.Add(button);
        }
    }

    static Color A(Color c, int a)
    {
        return new Color(c.R, c.G, c.B, a / 255f);
    }

    static Color C(int r, int g, int b, int a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    static Color Accent(int room)
    {
        return ArtData.C(Campaign.Accent(room));
    }

    void Box(float x, float y, float w, float h, Color c)
    {
        if (w <= 0 || h <= 0) return;
        DrawRect(new Rect2(x, y, w, h), c);
    }

    void Line(float x1, float y1, float x2, float y2, Color c, float width = 1)
    {
        DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), c, width);
    }

    void Circle(float x, float y, float r, Color c, bool filled = true, float width = 1)
    {
        if (filled) DrawCircle(new Vector2(x, y), r, c, true);
        else DrawCircle(new Vector2(x, y), r, c, false, width);
    }

    void Text(string text, float x, float y, float size, Color c)
    {
        DrawString(Font, new Vector2(x, y), text ?? "", HorizontalAlignment.Left, -1, (int)size, c);
    }

    void Bold(string text, float x, float y, float size, Color c)
    {
        Text(text, x, y, size, c);
    }

    void TextRight(string text, float right, float y, float size, Color c)
    {
        Text(text, right - ApproxWidth(text, size, false), y, size, c);
    }

    float ApproxWidth(string text, float size, bool bold)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int lines = 0;
        int longest = 0;
        int current = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
                longest = Math.Max(longest, current);
                current = 0;
            }
            else current++;
        }
        longest = Math.Max(longest, current);
        return longest * size * (bold ? .58f : .54f) + lines * size * .4f;
    }

    void TextWrap(string text, float x, float y, float maxWidth, float size, float lineHeight, Color c)
    {
        if (string.IsNullOrEmpty(text)) return;
        float cursorY = y;
        string[] paragraphs = text.Split('\n');
        for (int p = 0; p < paragraphs.Length; p++)
        {
            if (paragraphs[p].Length == 0)
            {
                cursorY += lineHeight;
                continue;
            }
            string[] words = paragraphs[p].Split(' ');
            string line = "";
            for (int n = 0; n < words.Length; n++)
            {
                string candidate = line.Length == 0 ? words[n] : line + " " + words[n];
                if (ApproxWidth(candidate, size, false) > maxWidth && line.Length > 0)
                {
                    Text(line, x, cursorY, size, c);
                    cursorY += lineHeight;
                    line = words[n];
                }
                else line = candidate;
            }
            if (line.Length > 0)
            {
                Text(line, x, cursorY, size, c);
                cursorY += lineHeight;
            }
        }
    }

    void Grain(float x, float y, float w, float h, int count, int seed)
    {
        int spanX = Math.Max(1, (int)w);
        int spanY = Math.Max(1, (int)h);
        for (int i = 0; i < count; i++)
        {
            float px = x + (i * 73 + seed * 37) % spanX;
            float py = y + (i * 41 + seed * 19) % spanY;
            Color c = i % 4 == 0 ? C(255, 205, 116, 26) : C(228, 222, 208, 18);
            Box(px, py, i % 7 == 0 ? 2 : 1, 1, c);
        }
    }

    void Panel(RectangleF r, bool active, int seed)
    {
        Box(r.X, r.Y, r.Width, r.Height, C(active ? 52 : 20, 9, 9, 18));
        Box(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6, C(active ? 38 : 16, 20, 20, 32));
        Box(r.X, r.Y, r.Width, 2, active ? Gold : C(255, 139, 74, 92));
        Box(r.X, r.Bottom - 2, r.Width, 2, C(255, 139, 74, active ? 130 : 70));
        Box(r.X, r.Y, 2, r.Height, C(255, 205, 116, active ? 150 : 80));
        Box(r.Right - 2, r.Y, 2, r.Height, C(255, 139, 74, active ? 110 : 60));
        Grain(r.X + 6, r.Y + 6, r.Width - 12, r.Height - 12, 18, seed);
    }

    void Frame(float x, float y, float w, float h, Color edge)
    {
        Box(x, y, w, h, C(8, 8, 16, 238));
        Box(x, y, w, 2, edge);
        Box(x, y + h - 2, w, 2, A(edge, 90));
        Box(x, y, 2, h, edge);
        Box(x + w - 2, y, 2, h, A(edge, 90));
        Box(x, y, 20, 2, Gold);
        Box(x, y, 2, 20, Gold);
        Box(x + w - 20, y + h - 2, 20, 2, Gold);
        Box(x + w - 2, y + h - 20, 2, 20, Gold);
        Box(x + 7, y + 7, w - 14, 1, C(255, 205, 116, 28));
        Box(x + 7, y + h - 8, w - 14, 1, C(255, 139, 74, 24));
        Grain(x + 9, y + 9, w - 18, h - 18, 42, (int)x + (int)y);
    }

    void Sparks(float x, float y, float w, float h)
    {
        if (ReduceMotion) return;
        for (int i = 0; i < 16; i++)
        {
            float t = (clock * .22f + i * .137f) % 1f;
            float px = x + ((i * 137) % (int)Math.Max(1, w)) + Mathf.Sin(clock + i) * 4;
            float py = y + h - t * h;
            Box(px, py, 2, 2, C(255, 139, 74, (int)(140 * (1 - t))));
        }
    }

    void Sigil(float x, float y)
    {
        Circle(x, y, 40, C(255, 139, 74, 70), false, 2);
        Circle(x, y, 26, Gold, false, 2);
        Line(x, y - 32, x, y + 32, Ember, 2);
        Line(x - 23, y + 19, x, y - 27, Ember, 2);
        Line(x + 23, y + 19, x, y - 27, Ember, 2);
        Circle(x, y, 4, Gold);
    }

    void Header(string kicker, string title, string sub, float x, float y)
    {
        Text(kicker, x, y, 15, Ember);
        Bold(title, x, y + 24, 44, new Color(1, 1, 1));
        Text(sub, x, y + 78, 18, C(204, 208, 198));
        Box(x, y + 108, 560, 2, C(255, 139, 74, 90));
    }

    void MenuItemVisual(string label, string hint, RectangleF r, int index)
    {
        bool hover = r.Contains(game.Input.Aim.X, game.Input.Aim.Y);
        bool active = hover || game.Menu == index;
        Panel(r, active, index + 3);
        Box(r.X + 8, r.Y + 8, 10, r.Height - 16, active ? Gold : C(255, 139, 74, 90));
        Box(r.X + 11, r.Y + r.Height / 2 - 5, 4, 4, active ? new Color(1, 1, 1) : Ember);
        Box(r.X + 24, r.Bottom - 9, r.Width - 48, 1, C(255, 139, 74, active ? 90 : 40));
        string shown = (active ? "> " : "  ") + label;
        float labelX = r.X + 32;
        Bold(shown, labelX, r.Y + 12, 21, active ? new Color(1, 1, 1) : Ash);
        if (hint.Length > 0)
        {
            float hintX = r.Right - 18 - ApproxWidth(hint, 14, false);
            if (hintX >= labelX + ApproxWidth(shown, 21, true) + 12)
            {
                Text(hint, hintX, r.Y + 16, 14, active ? Gold : Muted);
            }
        }
    }

    void MenuCardVisual(string key, string title, string detail, RectangleF r, int index, string meta = "", bool enabled = true)
    {
        bool hover = r.Contains(game.Input.Aim.X, game.Input.Aim.Y);
        bool active = enabled && (hover || game.Menu == index);
        bool compact = r.Height < 72;
        Panel(r, active, index + 13);
        Box(r.X + 8, r.Y + 8, 8, r.Height - 16, active ? Gold : Ember);
        Box(r.X + 72, r.Y + 10, 1, r.Height - 20, C(255, 205, 116, 50));
        float keySize = compact ? 25 : 32;
        float titleSize = compact ? 21 : 24;
        float detailSize = compact ? 13 : 16;
        float keyY = compact ? r.Y + 13 : r.Y + 29;
        float titleY = compact ? r.Y + 13 : r.Y + 19;
        float detailY = compact ? r.Y + 37 : r.Y + 58;
        Bold(key, r.X + 24, keyY, keySize, active ? Gold : Muted);
        Bold(title, r.X + 86, titleY, titleSize, active ? new Color(1, 1, 1) : Ash);
        Text(detail, r.X + 86, detailY, detailSize, active ? C(255, 205, 116, 220) : Muted);
        if (meta.Length > 0)
        {
            float metaSize = compact ? 12 : 13;
            float metaX = r.Right - 18 - ApproxWidth(meta, metaSize, false);
            float titleEnd = r.X + 86 + ApproxWidth(title, titleSize, true);
            if (metaX >= titleEnd + 12)
            {
                Text(meta, metaX, compact ? r.Y + 8 : r.Y + 16, metaSize, enabled ? (active ? Gold : C(255, 205, 116, 180)) : Muted);
            }
        }
        if (active)
        {
            float badgeX = r.Right - 18 - ApproxWidth("SELECTED", 14, false);
            float badgeY = compact ? r.Y + 39 : r.Y + 64;
            if (badgeX >= r.X + 86 + ApproxWidth(detail, detailSize, false) + 12)
            {
                Text("SELECTED", badgeX, badgeY, 14, Gold);
            }
        }
    }

    void ContextFooter(string message)
    {
        Box(0, 670, Game.Width, 50, Ink);
        Text(message, 28, 678, 14, Muted);
    }

    void Guide(string key, string value, float x, float y)
    {
        Bold(key, x, y, 15, Gold);
        Text(value, x + 112, y, 15, Ash);
    }

    public override void _Draw()
    {
        if (game == null) return;
        if (game.State == Stage.Title) DrawTitle();
        else if (game.State == Stage.Sunrise) DrawSunrise();
        else
        {
            DrawHud();
            if (game.State == Stage.Story) DrawStory();
            else if (game.State == Stage.Upgrade) DrawUpgrades();
            else if (game.State == Stage.Paused) DrawPause();
            else if (game.State == Stage.Help) DrawHelp();
            else if (game.State == Stage.Audio) DrawAudio();
            else if (game.State == Stage.Abilities) DrawAbilities();
            else if (game.State == Stage.Difficulty) DrawDifficulty();
            else if (game.State == Stage.Won || game.State == Stage.Dead) DrawEnd();
        }
        DrawCursorMark();
    }

    void DrawTitle()
    {
        Box(0, 0, Game.Width, Game.Height, C(6, 6, 14, 84));
        Sparks(0, 40, 1280, 620);
        Frame(58, 54, 600, 590, Ember);
        Text("AN ORIGINAL DARK FANTASY RUN-AND-GUN", 88, 88, 15, Ember);
        Sigil(556, 112);
        Bold("ASHENBOUND", 86, 150, 68, new Color(1, 1, 1));
        Bold("THE LAST CINDER", 88, 226, 48, Ember);
        Box(90, 300, 520, 2, C(255, 139, 74, 110));
        Text("THE SUN HAS BEEN DEAD FOR THREE WINTERS.", 90, 324, 19, Gold);
        TextWrap("Carry the Last Cinder through three ruined sanctums. Break the Cinder Bishop's choir. Give the world one more dawn.", 90, 360, 520, 17, 24, C(204, 208, 200));
        PixelPainter.DrawSprite(this, 0, new V(360, 548), 3, false, 0);
        float titleStep = 4 * 3;
        V titleHand = new V(360, 548) + new V(28 * 3, -12 * 3);
        Box(Mathf.Floor(titleHand.X / titleStep) * titleStep, Mathf.Floor(titleHand.Y / titleStep) * titleStep, 16 * 3, 4 * 3, Gold);
        Box(58, 600, 600, 3, Ember);
        Text("VEYRA / EMBER BEARER", 88, 612, 16, Muted);
        Frame(700, 160, 510, 470, Gold);
        Text("MENU", 740, 190, 16, Gold);
        RectangleF[] items = MenuLayout.Title;
        MenuItemVisual("NEW VIGIL", "ENTER", items[0], 0);
        MenuItemVisual("FIELD MANUAL", "", items[1], 1);
        MenuItemVisual("AUDIO SETTINGS", "", items[2], 2);
        MenuItemVisual("ABILITIES MANUAL", "", items[3], 3);
        MenuItemVisual("RETURN TO DESKTOP", "", items[4], 4);
        ContextFooter("W/S SELECT   ENTER CONFIRM   M MUTE   F11 FULLSCREEN   F8 MOTION");
    }

    void DrawHud()
    {
        Box(0, 0, Game.Width, 88, C(9, 10, 20, 245));
        Text("ASHENBOUND", 28, 16, 17, Ember);
        Box(28, 50, 230, 11, C(34, 45, 65));
        Box(28, 50, 230 * game.Health / game.MaxHealth, 11, Blood);
        Text(((int)game.Health) + " / " + (int)game.MaxHealth, 274, 44, 16, new Color(1, 1, 1));
        Text("SANCTUM " + (game.Room + 1) + " / " + Campaign.TotalRooms + "  -  " + Campaign.Names[game.Room], 411, 15, 17, new Color(1, 1, 1));
        Text(game.ExitOpen ? "SANCTUM CLEAR / GATE OPEN ON THE RIGHT" : "HOSTILES  " + (game.Enemies.Count + game.Pending), 434, 49, 14, game.ExitOpen ? Cyan : Muted);
        Bold(game.Score.ToString("D6"), 1080, 11, 29, Gold);
        Text(game.DashCooldown <= 0 ? "DASH READY" : "DASH  " + game.DashCooldown.ToString("0.0"), 1090, 51, 14, Ember);
        Text("FLASKS " + game.Flasks, 28, 92, 14, Gold);
        if (game.Chain > 0)
        {
            Bold(game.Chain + " x CHAIN" + (game.Overcharged ? " / OVERCHARGE" : ""), 30, 112, 27, game.Overcharged ? Gold : Ash);
            Box(32, 154, 190 * Game.Clamp(game.ChainTime / 4.2f, 0, 1), 4, game.Overcharged ? Gold : Ember);
        }
        if (game.ToastTime > 0 && game.Room < 2) Text(game.Toast, 410, 103, 16, Cyan);
        foreach (Enemy enemy in game.Enemies)
        {
            if (enemy.Kind != 3) continue;
            Text(Campaign.Boss + (enemy.Hp < enemy.MaxHp * .34f ? " / ENRAGED" : enemy.Hp < enemy.MaxHp * .67f ? " / CHOIR" : ""), 410, 105, 18, Gold);
            Box(350, 133, 580, 8, C(68, 36, 48));
            Box(350, 133, 580 * Mathf.Max(0, enemy.Hp / enemy.MaxHp), 8, Accent(game.Room));
        }
        Box(0, 670, Game.Width, 50, Ink);
        Text("A/D MOVE  SPACE JUMP  MOUSE AIM + FIRE  SHIFT/RMB DASH  Q FLASK  E GATE  S DROP", 28, 678, 13, Muted);
        TextRight("ESC PAUSE  M SOUND  F11", 1252, 678, 13, Muted);
    }

    void DrawStory()
    {
        if (game.Ending)
        {
            DrawEpilogue();
            return;
        }
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(248, 124, 784, 526, Gold);
        Header("SANCTUM " + (game.Room + 1) + " / " + Campaign.TotalRooms, "THE LAST CINDER", Campaign.Speakers[Mathf.Min(game.Room, 3)], 294, 152);
        Sparks(248, 300, 784, 260);
        TextWrap(Campaign.Scenes[Mathf.Min(game.Room, 3)], 298, 276, 724, 17, 24, C(214, 216, 220));
        MenuItemVisual("CONTINUE", "ENTER", MenuLayout.Story[0], 0);
        ContextFooter("ENTER / CLICK TO CONTINUE   J REOPENS THE PAGE IN PLAY");
    }

    void DrawEpilogue()
    {
        Box(0, 88, Game.Width, 632, C(7, 7, 15, 242));
        Frame(210, 124, 860, 526, Gold);
        Header("VIGIL COMPLETE", "ONE MORE MORNING", "The Cinder has left Veyra's hands.", 254, 150);
        Sparks(250, 300, 780, 150);
        Circle(640, 338, 98, C(255, 205, 116, 22));
        Circle(640, 338, 66, C(255, 139, 74, 32));
        Box(370, 370, 540, 3, Gold);
        for (int i = 0; i < 9; i++)
        {
            float x = 400 + i * 60;
            float y = 356 - Mathf.Sin(i / 8f * Mathf.Pi) * 34;
            Box(x, y, 3, 44 - (int)(i / 3f) * 8, C(255, 205, 116, 180 - i * 12));
        }
        Bold("THE BISHOP BREAKS", 270, 398, 19, Gold);
        TextWrap("A dried host. A silenced choir. The long dusk loses its voice.", 270, 430, 220, 15, 22, Ash);
        Bold("THE CINDER FLIES", 530, 398, 19, Gold);
        TextWrap("It rises through the vaulted dark and takes flight beyond the nave.", 530, 430, 220, 15, 22, Ash);
        Bold("ONE MORE MORNING", 790, 398, 19, Gold);
        TextWrap("First a red line, then a river. Veyra carries the rest.", 790, 430, 220, 15, 22, Ash);
        MenuItemVisual("WITNESS THE DAWN", "ENTER", MenuLayout.Epilogue[0], 0);
        ContextFooter("ENTER / CLICK TO SEE THE VICTORY RECORD");
    }

    void DrawUpgrades()
    {
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(220, 132, 840, 500, Ember);
        Header("SANCTUM CLEAR", "A BREATH BETWEEN BELLS", "Choose one relic. The next sanctum is waiting.", 266, 152);
        for (int i = 0; i < game.UpgradeChoices.Length; i++)
        {
            RelicKind kind = game.UpgradeChoices[i];
            UpgradeDef def = Campaign.UpgradeFor(kind);
            if (def == null)
            {
                MenuCardVisual((i + 1).ToString(), "NO RELIC", "ALL RELICS CLAIMED", MenuLayout.Upgrade[i], i, "", false);
                continue;
            }
            MenuCardVisual((i + 1).ToString(), def.Name, def.Detail, MenuLayout.Upgrade[i], i, RelicLabel(def, game.OwnedStacks(kind), true));
        }
        ContextFooter("1/2/3 OR W/S + ENTER   SELECT A RELIC");
    }

    void DrawPause()
    {
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(360, 150, 560, 420, Cyan);
        Header("TIME SUSPENDED", "THE ASH HOLDS STILL", "Take a breath. The choir can wait.", 406, 170);
        RectangleF[] items = MenuLayout.Pause;
        MenuItemVisual("RESUME VIGIL", "ESC", items[0], 0);
        MenuItemVisual("FIELD MANUAL", "", items[1], 1);
        MenuItemVisual("AUDIO SETTINGS", "", items[2], 2);
        MenuItemVisual("RETURN TO TITLE", "", items[3], 3);
        ContextFooter("W/S SELECT   ENTER CONFIRM   ESC RESUME   M MUTE");
    }

    void DrawHelp()
    {
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(220, 100, 840, 525, Cyan);
        Header("FIELD MANUAL", "HOW TO SURVIVE THE ASH", "Controls and cinder-lore for the bearer.", 266, 132);
        Text("MOVEMENT", 266, 264, 16, Gold);
        Guide("A / D", "MOVE", 266, 292);
        Guide("SPACE / W", "JUMP", 266, 320);
        Guide("S / DOWN", "DROP THROUGH LEDGES", 266, 348);
        Guide("SHIFT / RMB", "DASH WITH I-FRAMES", 266, 376);
        Text("COMBAT", 650, 264, 16, Gold);
        Guide("MOUSE", "AIM", 650, 292);
        Guide("LEFT CLICK", "FIRE", 650, 320);
        Guide("Q", "CINDER FLASK", 650, 348);
        Guide("E", "OPEN CLEARED GATE", 650, 376);
        Guide("J", "REOPEN SCENE", 650, 404);
        Box(266, 430, 724, 2, C(255, 139, 74, 90));
        Text("CINDER RULES", 266, 448, 16, Gold);
        TextWrap("Chain kills quickly to overcharge the Trigger and heal on every takedown. Break every foe before the gate opens. Walls hold them inside the sanctum. Dashes ignore hits. Flasks explode on impact. Stay ahead of the Bishop's choir.", 266, 474, 724, 15, 22, Ash);
        MenuItemVisual("BACK", "ESC", MenuLayout.Help[0], 0);
        ContextFooter("ESC / ENTER TO RETURN");
    }

    void SliderVisual(string label, float value, RectangleF r, int index)
    {
        bool active = game.Menu == index || r.Contains(game.Input.Aim.X, game.Input.Aim.Y);
        Panel(r, active, 60 + index);
        Box(r.X + 8, r.Y + 8, 8, r.Height - 16, active ? Gold : Ember);
        Bold(label, r.X + 28, r.Y + 14, 21, active ? new Color(1, 1, 1) : Ash);
        float barX = r.X + 190;
        float barY = r.Y + 24;
        float barW = r.Width - 300;
        Box(barX - 2, barY - 2, barW + 4, 14, C(255, 205, 116, active ? 70 : 40));
        Box(barX, barY, barW, 10, C(34, 45, 65));
        Box(barX, barY, barW * value, 10, active ? Gold : Ember);
        Box(barX, barY, barW * value, 2, C(255, 205, 116, active ? 180 : 110));
        for (int i = 0; i <= 10; i++) Box(barX + barW * i / 10 - 1, barY - 3, 2, 16, C(12, 12, 18, 70));
        TextRight(Mathf.Round(value * 100) + "%", r.Right - 24, r.Y + 18, 17, active ? Gold : Muted);
    }

    void DrawAudio()
    {
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(250, 130, 780, 470, Gold);
        Header("AUDIO RITES", "TUNE THE ASH", "Master, music, and effects.", 296, 148);
        SliderVisual("MASTER", game.Volume[Game.MasterVolumeIndex], MenuLayout.Audio[0], 0);
        SliderVisual("MUSIC", game.Volume[Game.MusicVolumeIndex], MenuLayout.Audio[1], 1);
        SliderVisual("EFFECTS", game.Volume[Game.SfxVolumeIndex], MenuLayout.Audio[2], 2);
        MenuItemVisual("BACK", "ESC", MenuLayout.Audio[3], 3);
        ContextFooter("A/D ADJUST   W/S SELECT   ENTER / ESC BACK   M MUTE");
    }

    void DrawAbilities()
    {
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(180, 104, 920, 560, Gold);
        Header("CINDER LEDGER", "ABILITIES MANUAL", "Nine relics can change the shape of a vigil.", 226, 126);
        for (int i = 0; i < Campaign.Upgrades.Length; i++)
        {
            float x = 226 + (i % 3) * 290;
            float y = 246 + (i / 3) * 114;
            RectangleF card = new RectangleF(x, y, 270, 98);
            UpgradeDef def = Campaign.Upgrades[i];
            int stack = game.OwnedStacks(def.Kind);
            bool active = card.Contains(game.Input.Aim.X, game.Input.Aim.Y);
            Panel(card, active, 40 + i);
            Box(card.X + 54, card.Y + 10, 1, card.Height - 20, C(255, 205, 116, 50));
            Bold((i + 1).ToString("00"), card.X + 16, card.Y + 20, 17, active ? Gold : Muted);
            Bold(def.Name, card.X + 68, card.Y + 16, 17, active ? new Color(1, 1, 1) : Ash);
            Text(def.Detail, card.X + 18, card.Y + 56, 14, active ? Gold : Muted);
            string meta = RelicLabel(def, stack, false);
            if (meta.Length > 0)
            {
                Text(meta, card.Right - 14 - ApproxWidth(meta, 12, false), card.Y + 77, 12, stack > 0 ? Gold : Muted);
            }
        }
        MenuItemVisual("BACK", "ESC", MenuLayout.Abilities[0], 0);
        ContextFooter("ESC / ENTER TO RETURN");
    }

    void DrawDifficulty()
    {
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(250, 110, 780, 510, Gold);
        Header("NEW VIGIL", "DIFFICULTY", "Foe strength and incoming damage change. Rooms and relics do not.", 296, 130);
        for (int i = 0; i < Campaign.Difficulties.Length; i++)
        {
            DifficultyDef def = Campaign.Difficulties[i];
            MenuCardVisual((i + 1).ToString(), def.Name, def.Detail, MenuLayout.Difficulty[i], i);
        }
        MenuItemVisual("BACK", "ESC", MenuLayout.Difficulty[4], 4);
        ContextFooter("W/S SELECT   ENTER / NUMBER 1-4 CONFIRM   ESC BACK");
    }

    void DrawEnd()
    {
        bool won = game.State == Stage.Won;
        Box(0, 88, Game.Width, 632, C(8, 8, 16, 238));
        Frame(300, 160, 680, 410, won ? Gold : Blood);
        Header(won ? "VIGIL COMPLETE" : "VIGIL ENDED", won ? "DAWN RETURNS" : "THE DUSK KEEPS YOU", won ? "The Cinder takes flight. A thin red line becomes a river." : "The choir keeps the night. Another bearer will answer.", 346, 190);
        Box(346, 310, 588, 76, C(20, 24, 28));
        Bold("SCORE " + game.Score, 364, 322, 23, Gold);
        Text("BEST CHAIN " + game.BestChain + "     SILENCED " + game.Kills + "     VIGIL " + (game.Seed & 65535).ToString("X4"), 364, 360, 17, Muted);
        MenuItemVisual("NEW VIGIL", "ENTER", MenuLayout.End[0], 0);
        MenuItemVisual("RETURN TO TITLE", "", MenuLayout.End[1], 1);
        ContextFooter("W/S SELECT   ENTER CONFIRM");
    }

    void DrawSunrise()
    {
        float progress = ReduceMotion ? 1 : Game.Clamp(game.SunriseTime / Game.SunriseDuration, 0, 1);
        Box(0, 0, Game.Width, Game.Height, C(7, 7, 17));
        for (int i = 0; i < 36; i++)
        {
            float t = i / 35f;
            Box(0, t * Game.Height, Game.Width, Game.Height / 36f + 1, new Color(
                Mathf.Lerp(7 / 255f, 104 / 255f, t),
                Mathf.Lerp(7 / 255f, 20 / 255f, t),
                Mathf.Lerp(17 / 255f, 36 / 255f, t)));
        }
        float horizon = 548 - progress * 96;
        float sunY = 520 - progress * 310;
        float sunX = 826;
        float glow = 40 + progress * 52;
        Circle(sunX, sunY, glow * 3, C(190, 32, 44, (int)(8 + progress * 10)));
        Circle(sunX, sunY, glow * 2, C(206, 44, 52, (int)(16 + progress * 16)));
        Circle(sunX, sunY, glow, C(154, 24, 36, (int)(130 + progress * 90)));
        Circle(sunX, sunY, Mathf.Max(6, glow / 2), C(12, 7, 14));
        Circle(sunX, sunY, glow + 4, C(255, 92, 72, (int)(70 + progress * 90)), false, 3);
        if (!ReduceMotion)
        {
            for (int i = 0; i < 14; i++)
            {
                float angle = i * .4488f;
                float length = 210 + ((i * 47) % 170) * progress;
                Line(sunX, sunY, sunX + Mathf.Cos(angle) * length, sunY + Mathf.Sin(angle) * length, C(255, 92, 72, (int)(12 + progress * 18)), 2);
            }
        }
        Box(0, horizon, Game.Width, Game.Height - horizon, C(12, 8, 17));
        Color far = C(28, 18, 34);
        for (int i = 0; i < 9; i++)
        {
            float x = i * 162 - 60;
            float peak = horizon - 70 - ((i * 37) % 66);
            Triangle(x, horizon + 8, x + 80, peak, x + 170, horizon + 8, far);
        }
        Color dark = C(9, 8, 15);
        Color edge = C(255, 139, 74, 90);
        for (int i = 0; i < 7; i++)
        {
            float x = i * 205 - 80;
            float h = 58 + ((i * 61) % 82);
            Box(x, horizon - h, 116, h + 10, dark);
            Triangle(x, horizon - h, x + 58, horizon - h - 22 - ((i * 17) % 30), x + 116, horizon - h, dark);
            Box(x + 18, horizon - h + 8, 12, 18, C(92, 28, 36, 180));
            Box(x + 74, horizon - h + 14, 10, 12, C(92, 28, 36, 150));
            Line(x, horizon - h, x, horizon, edge);
        }
        GothicArch(704, horizon, 244, 206, dark, edge);
        GothicArch(1042, horizon, 150, 142, dark, C(255, 139, 74, 60));
        for (int i = 0; i < 5; i++)
        {
            float x = 180 + i * 84;
            float h = 104 + ((i * 29) % 70);
            Box(x, horizon - h, 18, h, dark);
            Box(x - 8, horizon - h, 34, 10, dark);
            Box(x - 6, horizon - 8, 30, 8, dark);
            Line(x + 9, horizon - h, x + 9, horizon, edge);
        }
        Box(0, horizon - 6, Game.Width, 10, C(24, 12, 20));
        for (int i = 0; i < 10; i++)
        {
            float x = (i * 151 + 83) % 1280;
            float y = horizon - 24 - ((i * 43) % 80);
            Box(x, y, 3, 12, C(255, 139, 74, 180));
        }
        if (!ReduceMotion)
        {
            for (int i = 0; i < 28; i++)
            {
                float t = (clock * .28f + i * .179f) % 1f;
                float px = (i * 97 + (int)(clock * 18)) % 1280;
                float py = horizon - 180 + t * 260;
                Box(px, py, 2, 5, C(255, 139, 74, (int)(120 * (1 - t))));
            }
        }
        Text("THE LONG DUSK BREAKS", 58, 58, 16, Ember);
        Bold("ONE MORE MORNING", 54, 88, 50, new Color(1, 1, 1));
        Box(58, 158, 520, 2, C(120, 255, 205, 116));
        TextWrap("First a red line, then a river. The Cinder leaves Veyra's hands, and the world remembers the sun.", 60, 178, 590, 17, 24, C(235, 226, 214, 230));
        string phase = progress < .34f ? "I  THE RED LINE" : progress < .72f ? "II  THE RIVER" : "III  THE MORNING";
        Bold(phase, 60, 276, 24, Gold);
        if (!ReduceMotion)
        {
            float line = 600 - progress * 480;
            Box(line, horizon - 2, 1280 - line, 3, C(255, 205, 116, (int)(90 + progress * 120)));
        }
        Box(440, 628, 400, 6, C(44, 228, 222, 208));
        Box(440, 628, 400 * progress, 6, Gold);
        Text("WITNESSING THE DAWN", 440, 602, 14, Muted);
        TextRight("ENTER / ESC / CLICK TO SKIP", 840, 602, 14, Ash);
    }

    void Triangle(float x1, float y1, float x2, float y2, float x3, float y3, Color c)
    {
        float top = Mathf.Min(y1, Mathf.Min(y2, y3));
        float bottom = Mathf.Max(y1, Mathf.Max(y2, y3));
        if (bottom <= top) return;
        for (float y = Mathf.Ceil(top); y <= bottom; y += 1)
        {
            float[] crossings = new float[4];
            int count = 0;
            AddCrossing(x1, y1, x2, y2, y, crossings, ref count);
            AddCrossing(x2, y2, x3, y3, y, crossings, ref count);
            AddCrossing(x3, y3, x1, y1, y, crossings, ref count);
            if (count < 2) continue;
            Array.Sort(crossings, 0, count);
            for (int i = 0; i + 1 < count; i += 2)
            {
                if (crossings[i + 1] > crossings[i]) Line(crossings[i], y, crossings[i + 1], y, c);
            }
        }
    }

    void AddCrossing(float x1, float y1, float x2, float y2, float y, float[] crossings, ref int count)
    {
        if ((y1 <= y && y2 > y) || (y2 <= y && y1 > y))
        {
            if (count < crossings.Length) crossings[count++] = x1 + (x2 - x1) * (y - y1) / (y2 - y1);
        }
    }

    void GothicArch(float cx, float baseY, float w, float h, Color fill, Color edge)
    {
        float left = cx - w / 2;
        float right = cx + w / 2;
        float top = baseY - h;
        Triangle(cx, top - 38, left, top + 34, right, top + 34, fill);
        Box(left, top + 34, w, h - 34, fill);
        Color opening = C(34, 20, 32);
        Triangle(cx, top - 14, left + 26, top + 48, right - 26, top + 48, opening);
        Box(left + 26, top + 48, w - 52, h - 48, opening);
        Line(left, baseY, left, top + 34, edge, 2);
        Line(right, baseY, right, top + 34, edge, 2);
        Line(left, top + 34, cx, top - 38, edge, 2);
        Line(cx, top - 38, right, top + 34, edge, 2);
        Line(left + 26, baseY, left + 26, top + 48, C(255, 139, 74, 60), 2);
        Line(right - 26, baseY, right - 26, top + 48, C(255, 139, 74, 60), 2);
    }

    void DrawCursorMark()
    {
        V aim = game.Input.Aim;
        Color color = game.State == Stage.Playing ? Cyan : Gold;
        Circle(aim.X, aim.Y, 13, color, false, 2);
        Circle(aim.X, aim.Y, 5, Gold, false, 1);
        Line(aim.X - 20, aim.Y, aim.X - 8, aim.Y, color, 2);
        Line(aim.X + 8, aim.Y, aim.X + 20, aim.Y, color, 2);
        Line(aim.X, aim.Y - 20, aim.X, aim.Y - 8, color, 2);
        Line(aim.X, aim.Y + 8, aim.X, aim.Y + 20, color, 2);
        Circle(aim.X, aim.Y, 2, new Color(1, 1, 1));
    }

    static string RelicLabel(UpgradeDef def, int stack, bool offer)
    {
        if (def.Stackable)
        {
            if (def.MaxStacks < 99 && stack >= def.MaxStacks) return "STACKABLE MAX";
            return def.MaxStacks >= 99 ? "STACKABLE x" + stack : "STACKABLE x" + stack + "/" + def.MaxStacks;
        }
        return !offer && stack > 0 ? "OWNED" : "";
    }
}
