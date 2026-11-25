using System;
using Godot;
using RectangleF = System.Drawing.RectangleF;

namespace Ashenbound;

public partial class Main : Control
{
    Game game;
    AudioDirector audio;
    PixelPainter world;
    ScreenLayer screens;
    Font font;
    double previous;
    double accumulator;
    float clock;
    bool fullscreen;

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] != "--selftest") continue;
            string report = SelfTest.Run();
            GD.Print(report);
            GetTree().Quit(report.Contains("STATUS BAD") ? 1 : 0);
            return;
        }

        Fill(this);
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        SystemFont systemFont = new SystemFont();
        string[] names = { "Georgia", "Serif" };
        systemFont.FontNames = names;
        font = systemFont;

        game = new Game();
        audio = new AudioDirector();
        AddChild(audio);
        audio.Attach(game);

        world = new PixelPainter();
        AddChild(world);
        Fill(world);

        screens = new ScreenLayer();
        AddChild(screens);
        Fill(screens);
        screens.Font = font;
        screens.Activated += ActivateMenu;

        previous = Time.GetTicksMsec() / 1000.0;
        UpdateAim();
        SyncMusic();
    }

    static void Fill(Control control)
    {
        control.AnchorLeft = 0;
        control.AnchorTop = 0;
        control.AnchorRight = 1;
        control.AnchorBottom = 1;
        control.OffsetLeft = 0;
        control.OffsetTop = 0;
        control.OffsetRight = 0;
        control.OffsetBottom = 0;
    }

    public override void _Process(double delta)
    {
        if (game == null) return;
        double now = Time.GetTicksMsec() / 1000.0;
        accumulator += Math.Min(.12, now - previous);
        previous = now;
        clock += (float)delta;

        UpdateAim();
        UpdateContinuousInput();

        float step = 1f / 120f;
        while (accumulator >= step)
        {
            game.Step(step);
            accumulator -= step;
        }

        SyncMusic();
        world.Render(game, clock, screens.ReduceMotion);
        screens.Sync(game, clock);
    }

    void UpdateContinuousInput()
    {
        bool playing = game.State == Stage.Playing;
        game.Input.Left = playing && (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left));
        game.Input.Right = playing && (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right));
        game.Input.Down = playing && (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down));
        game.Input.Fire = playing && Input.IsMouseButtonPressed(MouseButton.Left);
    }

    void UpdateAim()
    {
        if (game == null) return;
        Vector2 mouse = GetGlobalMousePosition();
        game.Input.Aim = new V(mouse.X, mouse.Y);
        if (game.State == Stage.Audio && game.AudioDragging) SetSliderFromAim();
        else SyncMenuHover();
    }

    void SyncMenuHover()
    {
        if (game == null) return;
        RectangleF[] items = MenuLayout.Items(game.State, game.Ending);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Contains(game.Input.Aim.X, game.Input.Aim.Y))
            {
                game.Menu = i;
                return;
            }
        }
    }

    void SetSliderFromAim()
    {
        if (game.Menu < 0 || game.Menu > 2) return;
        RectangleF row = MenuLayout.Audio[game.Menu];
        game.SetVolume(game.Menu, (game.Input.Aim.X - (row.X + 190)) / (row.Width - 300));
    }

    void MenuMove(int delta)
    {
        game.MoveMenu(delta, MenuLayout.Items(game.State, game.Ending).Length);
    }

    void ClearInput()
    {
        game.Input.Left = game.Input.Right = game.Input.Fire = game.Input.Down = false;
        game.Input.ClearEdges();
    }

    void SyncMusic()
    {
        if (game == null || audio == null) return;
        audio.Sync(game);
    }

    void ApplyVolumes()
    {
        audio.ApplyVolumes(game);
    }

    void ToggleSound()
    {
        game.ToggleSound();
        ApplyVolumes();
        ClearInput();
        SyncMusic();
    }

    void ToggleFullscreen()
    {
        fullscreen = !fullscreen;
        DisplayServer.WindowSetMode(fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
    }

    void ReturnToTitle()
    {
        game.ReturnToTitle();
        ClearInput();
        SyncMusic();
    }

    void NewRun()
    {
        ClearInput();
        game.Start(System.Environment.TickCount);
        SyncMusic();
    }

    void StartDifficulty(int index)
    {
        ClearInput();
        game.Start(System.Environment.TickCount, index);
        SyncMusic();
    }

    void ActivateMenu(int index)
    {
        if (game == null) return;
        Stage state = game.State;
        if (state == Stage.Title)
        {
            if (index == 0) game.ShowDifficulty();
            else if (index == 1) game.ShowHelp();
            else if (index == 2) game.ShowAudio();
            else if (index == 3) game.ShowAbilities();
            else if (index == 4) GetTree().Quit(0);
            ClearInput();
        }
        else if (state == Stage.Story)
        {
            game.ContinueStory();
            ClearInput();
        }
        else if (state == Stage.Help)
        {
            game.CloseHelp();
            ClearInput();
        }
        else if (state == Stage.Audio)
        {
            if (index >= 3)
            {
                game.CloseAudio();
                ApplyVolumes();
                ClearInput();
            }
            else game.Menu = index;
        }
        else if (state == Stage.Abilities)
        {
            game.CloseAbilities();
            ClearInput();
        }
        else if (state == Stage.Difficulty)
        {
            if (index >= 0 && index < Campaign.Difficulties.Length) StartDifficulty(index);
            else if (index == Campaign.Difficulties.Length)
            {
                game.CloseDifficulty();
                ClearInput();
            }
        }
        else if (state == Stage.Paused)
        {
            if (index == 0) game.Pause();
            else if (index == 1) game.ShowHelp();
            else if (index == 2)
            {
                game.ShowAudio();
                ClearInput();
            }
            else if (index == 3) ReturnToTitle();
        }
        else if (state == Stage.Upgrade)
        {
            game.SelectUpgrade(index);
            ClearInput();
        }
        else if (state == Stage.Dead || state == Stage.Won)
        {
            if (index == 0) NewRun();
            else if (index == 1) ReturnToTitle();
        }
        SyncMusic();
    }

    void MarkInputHandled()
    {
        GetViewport().SetInputAsHandled();
    }

    public override void _Input(InputEvent @event)
    {
        if (game == null) return;

        if (@event is InputEventMouseMotion)
        {
            UpdateAim();
            return;
        }

        if (@event is InputEventMouseButton mouse)
        {
            UpdateAim();
            if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
            {
                if (game.State == Stage.Playing) game.Input.Dash = true;
                MarkInputHandled();
                return;
            }
            if (mouse.ButtonIndex != MouseButton.Left) return;
            if (!mouse.Pressed)
            {
                game.Input.Fire = false;
                if (game.AudioDragging)
                {
                    game.AudioDragging = false;
                    ApplyVolumes();
                    MarkInputHandled();
                }
                ClearInput();
                return;
            }

            Stage currentStage = game.State;
            if (currentStage == Stage.Audio)
            {
                int slider = MenuLayout.Hit(Stage.Audio, game.Input.Aim);
                if (slider >= 0 && slider < 3)
                {
                    game.Menu = slider;
                    game.AudioDragging = true;
                    SetSliderFromAim();
                }
                else if (slider == 3) ActivateMenu(3);
                MarkInputHandled();
                return;
            }
            if (currentStage == Stage.Sunrise)
            {
                game.SkipSunrise();
                ClearInput();
                SyncMusic();
                MarkInputHandled();
                return;
            }
            if (currentStage == Stage.Playing)
            {
                game.Input.Fire = true;
                MarkInputHandled();
                return;
            }
            return;
        }

        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        Key code = key.Keycode;

        if (code == Key.F11)
        {
            ToggleFullscreen();
            return;
        }
        if (code == Key.M)
        {
            ToggleSound();
            return;
        }
        if (code == Key.F8)
        {
            screens.ReduceMotion = !screens.ReduceMotion;
            return;
        }

        Stage state = game.State;
        if (state == Stage.Sunrise && (code == Key.Enter || code == Key.Escape))
        {
            game.SkipSunrise();
            ClearInput();
            SyncMusic();
            return;
        }

        bool menu = state == Stage.Title || state == Stage.Help || state == Stage.Audio || state == Stage.Abilities || state == Stage.Difficulty || state == Stage.Upgrade || state == Stage.Paused || state == Stage.Dead || state == Stage.Won;
        if (menu)
        {
            if (code == Key.W || code == Key.Up)
            {
                MenuMove(-1);
                return;
            }
            if (code == Key.S || code == Key.Down)
            {
                MenuMove(1);
                return;
            }
            if (state == Stage.Upgrade && code >= Key.Key1 && code <= Key.Key3)
            {
                game.SelectUpgrade((int)code - (int)Key.Key1);
                ClearInput();
                SyncMusic();
                return;
            }
            if (state == Stage.Difficulty && code >= Key.Key1 && code <= Key.Key4)
            {
                StartDifficulty((int)code - (int)Key.Key1);
                return;
            }
            if (state == Stage.Audio && (code == Key.A || code == Key.Left || code == Key.D || code == Key.Right))
            {
                game.AdjustVolume(game.Menu, code == Key.D || code == Key.Right ? .05f : -.05f);
                ApplyVolumes();
                SyncMusic();
                return;
            }
            if (code == Key.Enter)
            {
                ActivateMenu(game.Menu);
                return;
            }
            if (code == Key.Escape)
            {
                if (state == Stage.Help) game.CloseHelp();
                else if (state == Stage.Audio)
                {
                    game.CloseAudio();
                    ApplyVolumes();
                }
                else if (state == Stage.Abilities) game.CloseAbilities();
                else if (state == Stage.Difficulty) game.CloseDifficulty();
                else if (state == Stage.Paused) game.Pause();
                ClearInput();
                SyncMusic();
                return;
            }
            return;
        }

        if (code == Key.Enter && state == Stage.Story)
        {
            game.ContinueStory();
            ClearInput();
            SyncMusic();
            return;
        }
        if (code == Key.J && state == Stage.Playing)
        {
            game.State = Stage.Story;
            game.Menu = 0;
            ClearInput();
            SyncMusic();
            return;
        }
        if (code == Key.Escape)
        {
            game.Pause();
            ClearInput();
            SyncMusic();
            return;
        }
        if (state != Stage.Playing) return;
        if (code == Key.Space || code == Key.W || code == Key.Up) game.Input.Jump = true;
        if (code == Key.Shift) game.Input.Dash = true;
        if (code == Key.Q) game.Input.Flask = true;
        if (code == Key.E) game.Input.Use = true;
    }
}
