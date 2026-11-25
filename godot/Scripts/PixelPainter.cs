using System;
using Godot;
using RectangleF = System.Drawing.RectangleF;

namespace Ashenbound;

public partial class PixelPainter : Control
{
    Game game;
    float clock;
    bool reduceMotion;

    static readonly Color Ash = ArtData.C(Palette.Ash);
    static readonly Color Ember = ArtData.C(Palette.Ember);
    static readonly Color Blood = ArtData.C(Palette.Blood);
    static readonly Color Gold = ArtData.C(Palette.Gold);
    static readonly Color Cyan = ArtData.C(Palette.Cyan);
    static readonly Color Ink = ArtData.C(Palette.Ink);
    static readonly Color Muted = ArtData.C(Palette.Muted);

    public void Render(Game value, float time, bool motion)
    {
        game = value;
        clock = time;
        reduceMotion = motion;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (game == null) return;
        DrawRect(new Rect2(0, 0, Game.Width, Game.Height), C(0, 0, 0));
        Background(game.Room);

        if (game.State != Stage.Title && game.State != Stage.Sunrise)
        {
            float shakeX = reduceMotion ? 0 : Mathf.Sin(clock * 110) * game.Shake / 4;
            float shakeY = reduceMotion ? 0 : Mathf.Cos(clock * 130) * game.Shake / 8;
            DrawWorld(shakeX, shakeY);
        }
    }

    static Color C(int r, int g, int b, int a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    static Color A(Color c, int a)
    {
        return new Color(c.R, c.G, c.B, a / 255f);
    }

    static Color Accent(int room)
    {
        return ArtData.C(Campaign.Accent(room));
    }

    void R(float x, float y, float w, float h, Color c)
    {
        if (w <= 0 || h <= 0) return;
        DrawRect(new Rect2(x, y, w, h), c);
    }

    void L(float x, float y, float w, float h, Color c)
    {
        R(x * 4, y * 4, w * 4, h * 4, c);
    }

    void LLine(float x1, float y1, float x2, float y2, Color c, float width = 1)
    {
        Line(x1 * 4, y1 * 4, x2 * 4, y2 * 4, c, width);
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
        DrawString(null, new Vector2(x, y), text, HorizontalAlignment.Left, -1, (int)size, c);
    }

    void Background(int room)
    {
        Color accent = Accent(room);
        L(0, 0, 320, 180, C(13, 12, 21));
        for (int y = 24; y < 156; y += 4)
        {
            for (int x = 0; x < 320; x += 4)
            {
                if ((x + y) % 14 == 0) L(x, y, 1, 1, C(22, 20, 32));
            }
        }

        for (int i = 0; i < 6; i++)
        {
            int cx = 34 + i * 54;
            L(cx - 22, 44, 44, 96, C(27, 25, 39));
            for (int y = 48; y < 140; y += 3)
            {
                float dy = y - 94;
                int span = (int)(18 * Mathf.Sqrt(Mathf.Max(0, 1 - dy * dy / 2401f)));
                if (span > 0) L(cx - span, y, span * 2, 1, C(16, 15, 26));
            }
            L(cx - 25, 138, 50, 5, C(36, 33, 47));
            L(cx - 23, 143, 4, 13, C(43, 39, 54));
            L(cx + 19, 143, 4, 13, C(43, 39, 54));
        }

        if (room == 0)
        {
            for (int i = 0; i < 9; i++)
            {
                int x = i * 40 - 8;
                L(x, 96, 5, 60, C(38, 35, 49));
                L(x - 2, 94, 9, 3, C(52, 48, 63));
            }
            L(0, 102, 320, 2, C(39, 36, 51));
            for (int x = 0; x < 320; x += 8) L(x, 155, 5, 2, C(58, 54, 67));
            L(283, 108, 18, 30, C(32, 29, 44));
            L(288, 113, 8, 8, accent);
        }
        else if (room == 1)
        {
            for (int i = 0; i < 8; i++)
            {
                int x = i * 42 - 6;
                L(x, 120, 38, 36, C(48, 33, 39));
                for (int t = 0; t < 8; t++) L(x + t * 5, 116, 5, 8, t % 2 == 0 ? Ember : Blood);
                L(x + 3, 136, 32, 2, C(92, 58, 54));
                for (int j = 0; j < 5; j++) L(x + 6 + j * 6, 139, 3, 9, C(76 + j * 12, 48, 55));
            }
            L(0, 66, 320, 1, C(84, 57, 57));
            for (int x = 16; x < 320; x += 40)
            {
                L(x + 3, 67, 1, 12, Muted);
                L(x, 78, 8, 12, C(103, 51, 58));
                L(x + 2, 79, 4, 10, Gold);
                L(x + 3, 90, 2, 4, Ember);
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                int x = i * 43;
                L(x, 68, 35, 88, C(39, 31, 52));
                for (int row = 0; row < 4; row++)
                {
                    L(x + 2, 84 + row * 19, 31, 3, C(88, 68, 91));
                    for (int j = 0; j < 6; j++)
                    {
                        int h = 7 + (i + j + row) % 5;
                        L(x + 4 + j * 5, 84 + row * 19 - h, 3, h, (i + j) % 2 == 0 ? ArtData.C(Palette.Violet) : C(58, 86, 98));
                    }
                }
            }
            L(0, 149, 320, 7, C(23, 37, 48));
            for (int x = 0; x < 320; x += 19) L(x, 151, 10, 1, C(51, 71, 84));
            L(145, 36, 30, 66, C(30, 24, 40));
            L(151, 42, 18, 18, Gold);
        }

        L(0, 156, 320, 24, C(11, 11, 18));
        for (int x = 0; x < 320; x += 8)
        {
            L(x, 164, 7, 1, C(30, 29, 40));
            if (x % 24 == 0) L(x, 169, 2, 4, C(24, 22, 33));
        }

        if (reduceMotion) return;
        for (int i = 0; i < 45; i++)
        {
            int x = (int)(i * 37 + clock * (room == 2 ? 3 : 9)) % 320;
            int y = 24 + (int)(i * 19 + clock * (room == 2 ? 7 : 65)) % 130;
            L(x, y, 1, room == 2 ? 1 : 3, C(52, 48, 62));
        }
    }

    void DrawWorld(float shakeX, float shakeY)
    {
        foreach (RectangleF platform in game.Platforms)
        {
            R(platform.X + shakeX, platform.Y + shakeY, platform.Width, platform.Height, C(30, 29, 42));
            R(platform.X + shakeX, platform.Y + shakeY, platform.Width, 3, Accent(game.Room));
            for (float x = platform.X + 12; x < platform.Right - 14; x += 34)
            {
                R(x + shakeX, platform.Y + 9 + shakeY, 17, 2, C(56, 53, 66));
            }
            if (platform.Y < Game.Floor - 20)
            {
                R(platform.X + 12 + shakeX, platform.Y + 18 + shakeY, 8, 24, C(26, 25, 37));
                R(platform.Right - 20 + shakeX, platform.Y + 18 + shakeY, 8, 24, C(26, 25, 37));
            }
        }

        foreach (Hazard hazard in game.Hazards)
        {
            RectangleF b = hazard.Bounds;
            if (hazard.Kind == 0)
            {
                for (float x = b.Left; x < b.Right - 4; x += 12)
                {
                    Line(x + shakeX, b.Bottom + shakeY, x + 6 + shakeX, b.Top + shakeY, Blood, 3);
                    Line(x + 6 + shakeX, b.Top + shakeY, x + 12 + shakeX, b.Bottom + shakeY, Blood, 3);
                }
            }
            else
            {
                Color color = hazard.Active ? Ember : C(70, 55, 60);
                R(b.X + shakeX, b.Y + shakeY, b.Width, b.Height, A(color, hazard.Active ? 70 : 24));
                R(b.X + b.Width / 2 - 1 + shakeX, b.Y + shakeY, 2, b.Height, color);
            }
        }

        if (game.ExitOpen)
        {
            float alpha = .5f + Mathf.Sin(clock * 3) * .15f;
            R(1177 + shakeX, 493 + shakeY, 70, 129, new Color(Cyan.R, Cyan.G, Cyan.B, alpha * .1f));
            R(1177 + shakeX, 493 + shakeY, 4, 129, Cyan);
            R(1243 + shakeX, 493 + shakeY, 4, 129, Cyan);
            R(1177 + shakeX, 493 + shakeY, 70, 4, Cyan);
            Text("E", 1198 + shakeX, 534 + shakeY, 32, new Color(1, 1, 1));
        }

        foreach (Enemy enemy in game.Enemies) DrawEnemy(enemy, shakeX, shakeY);
        foreach (Bullet bullet in game.Bullets) DrawBullet(bullet, shakeX, shakeY);
        foreach (Particle particle in game.Particles)
        {
            Color c = ArtData.C(particle.Color);
            R(particle.P.X + shakeX, particle.P.Y + shakeY, 4, 4, A(c, (int)(255 * Game.Clamp(particle.Life / particle.Max, 0, 1))));
        }

        if (game.Invulnerable <= 0 || (int)(clock * 24) % 2 == 0)
        {
            DrawActor(game.Player, game.Aim, 1, 0, shakeX, shakeY);
        }
    }

    void DrawBullet(Bullet bullet, float shakeX, float shakeY)
    {
        float x = bullet.P.X + shakeX;
        float y = bullet.P.Y + shakeY;
        if (bullet.Kind == 1)
        {
            Circle(x, y, 8, Ember);
            Circle(x, y, 4, Gold);
            return;
        }
        if (!bullet.Hostile && bullet.Kind == 0)
        {
            V direction = bullet.Velocity.Unit;
            float flicker = Mathf.Sin(clock * 38 + bullet.P.X * .11f) * 1.5f;
            Circle(x, y, 11 + flicker, A(C(196, 58, 22), 58));
            Circle(x, y, 8 + flicker * .4f, Ember);
            for (int i = 1; i <= 3; i++)
            {
                V p = bullet.P - direction * (8 + i * 7) + new V(Mathf.Sin(clock * 22 + i) * 2, Mathf.Cos(clock * 19 + i) * 2);
                Circle(p.X + shakeX, p.Y + shakeY, 5 - i, C(255, 139, 74, 210 - i * 45));
            }
            Circle(x - direction.X * 4, y - direction.Y * 4, 5, Gold);
            Circle(x + direction.X * 2, y + direction.Y * 2, 2, new Color(1, 1, 1));
            return;
        }
        Circle(x, y, 9, A(Blood, 48));
        Circle(x, y, 5, Blood);
        Circle(x, y, 2, new Color(1, 1, 1));
    }

    void DrawEnemy(Enemy enemy, float shakeX, float shakeY)
    {
        int type = enemy.Kind == 3 ? 4 : enemy.Kind + 1;
        float size = enemy.Kind == 3 ? 2.5f : 1f;
        DrawSprite(type, enemy.P, size, game.Player.X < enemy.P.X, (int)(clock * 7) % 2, shakeX, shakeY);
        Color color = enemy.Kind == 3 ? Accent(game.Room) : Blood;
        if (enemy.Hit > 0) R(enemy.P.X - 8 + shakeX, enemy.P.Y - 12 + shakeY, 16, 8, new Color(1, 1, 1));
        if (enemy.Kind != 2)
        {
            R(enemy.P.X - 22 + shakeX, enemy.P.Y - 44 + shakeY, 44, 4, C(66, 36, 48));
            R(enemy.P.X - 22 + shakeX, enemy.P.Y - 44 + shakeY, 44 * Mathf.Max(0, enemy.Hp / enemy.MaxHp), 4, color);
        }
    }

    void DrawActor(V position, V aim, float size, int type, float shakeX, float shakeY)
    {
        int frame = game.State == Stage.Playing && Mathf.Abs(game.Velocity.X) > 10 ? (int)(clock * 9) % 2 : 0;
        DrawSprite(type, position, size, aim.X < 0, frame, shakeX, shakeY);
        float step = 4 * size;
        V hand = position + new V(aim.X * 28 * size, -12 * size + aim.Y * 22 * size);
        R(Mathf.Floor((hand.X + shakeX) / step) * step, Mathf.Floor((hand.Y + shakeY) / step) * step, 16 * size, 4 * size, Gold);
    }

    public static void DrawSprite(CanvasItem item, int type, V position, float size, bool flip, int frame)
    {
        if (type < 0 || type >= ArtData.Maps.Length) return;
        string[] map = ArtData.Maps[type];
        float pixel = 4 * size;
        float originX = Mathf.Round(position.X / 4) * 4;
        float originY = Mathf.Round(position.Y / 4) * 4;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                int xx = x;
                if (frame % 2 == 1 && type != 2 && y >= 13) xx = Mathf.Clamp(x + (x < 6 ? -1 : 1), 0, 11);
                if (flip) xx = 11 - xx;
                Color c = ArtData.PixelColor(map[y][x]);
                if (c.A <= 0) continue;
                item.DrawRect(new Rect2(originX + (xx - 6) * pixel, originY + (y - 10) * pixel, pixel, pixel), c);
            }
        }
    }

    void DrawSprite(int type, V position, float size, bool flip, int frame, float shakeX, float shakeY)
    {
        DrawSprite(this, type, position + new V(shakeX, shakeY), size, flip, frame);
    }

}
