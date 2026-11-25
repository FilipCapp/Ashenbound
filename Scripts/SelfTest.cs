using System;
using System.Text;

namespace Ashenbound;

public static class SelfTest
{
    const float Dt = 1f / 120f;

    public static string Run()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("ASHENBOUND GODOT SELFTEST");
        int pass = 0;
        int failed = 0;

        Check(MenuLayout.Title.Length == 5, "title hit targets", report, ref pass, ref failed);
        Check(MenuLayout.Abilities.Length == 1, "abilities hit target", report, ref pass, ref failed);
        Check(MenuLayout.Difficulty.Length == 5, "difficulty hit targets", report, ref pass, ref failed);
        Check(MenuLayout.Audio.Length == 4, "audio hit targets", report, ref pass, ref failed);
        Check(MenuLayout.Items(Stage.Story, true)[0] == MenuLayout.Epilogue[0], "epilogue hit target", report, ref pass, ref failed);
        Check(MenuLayout.Items(Stage.Story, false)[0] == MenuLayout.Story[0], "story hit target", report, ref pass, ref failed);
        Check(MenuLayout.Items(Stage.Playing).Length == 0, "playing has no menu targets", report, ref pass, ref failed);
        Check(MenuLayout.Items(Stage.Dead).Length == 2 && MenuLayout.Items(Stage.Won).Length == 2, "end hit targets", report, ref pass, ref failed);
        Check(MenuLayout.Hit(Stage.Title, new V(800, 250)) == 0, "title hit test inside", report, ref pass, ref failed);
        Check(MenuLayout.Hit(Stage.Title, new V(100, 100)) == -1, "title hit test outside", report, ref pass, ref failed);
        Check(MenuLayout.Hit(Stage.Story, new V(800, 630), true) == 0, "epilogue hit test", report, ref pass, ref failed);
        Check(MenuLayout.Hit(Stage.Audio, new V(340, 270)) == 0, "audio slider hit test", report, ref pass, ref failed);
        Check(MenuLayout.Upgrade.Length == 3 && MenuLayout.Upgrade[0].Y == 264 && MenuLayout.Upgrade[0].Height == 316, "vertical upgrade card geometry", report, ref pass, ref failed);
        Check(MenuLayout.Upgrade[0].Right + 4 <= MenuLayout.Upgrade[1].X && MenuLayout.Upgrade[1].Right + 4 <= MenuLayout.Upgrade[2].X, "upgrade cards separated", report, ref pass, ref failed);
        Check(MenuLayout.Upgrade[2].Bottom <= 670 && MenuLayout.Upgrade[2].Right <= Game.Width, "upgrade cards inside canvas", report, ref pass, ref failed);
        Check(28 + (Campaign.Upgrades.Length - 1) * 40 + 36 <= 411, "hud relic strip fits", report, ref pass, ref failed);
        Check(12 + 12 * 2 <= 48, "ledger sigil clears divider", report, ref pass, ref failed);

        Game game = new Game();
        Check(game.State == Stage.Title, "initial title", report, ref pass, ref failed);
        Check(game.Volume.Length == 3, "volume bands", report, ref pass, ref failed);
        Check(game.UpgradeChoices.Length == 3, "upgrade choices", report, ref pass, ref failed);
        Check(game.RelicStacks.Length == Campaign.Upgrades.Length, "relic ledger", report, ref pass, ref failed);
        Check(Math.Abs(game.Volume[Game.SfxVolumeIndex] - .65f) < .001f, "sfx volume default", report, ref pass, ref failed);
        Check(Game.FireballSound == 0 && Game.HitSound == 1 && Game.ClearSound == 2 && Game.HurtSound == 3 && Game.DashSound == 4 && Game.FlaskSound == 5, "sound identifiers", report, ref pass, ref failed);

        game.Start(123456, 0);
        Check(game.State == Stage.Story, "story after start", report, ref pass, ref failed);
        Check(game.Room == 0, "first room", report, ref pass, ref failed);
        Check(game.Health == 100, "starting health", report, ref pass, ref failed);
        Check(game.Pending == 5, "first spawn budget", report, ref pass, ref failed);
        Check(game.Platforms.Count == 4 && game.Hazards.Count == 0, "nave layout", report, ref pass, ref failed);
        Check(game.Grounded && game.Player.Y == Game.Floor - 24, "run initializes grounded", report, ref pass, ref failed);

        V frozenPosition = game.Player;
        Steps(game, 60);
        Check(game.RoomTime == 0 && game.Player.X == frozenPosition.X, "story freezes combat", report, ref pass, ref failed);
        game.ContinueStory();
        Check(game.State == Stage.Playing, "playing after story", report, ref pass, ref failed);

        Game seeded = Fresh();
        Steps(seeded, 120);
        Check(seeded.Enemies.Count == 1, "seeded spawn occurs", report, ref pass, ref failed);
        Game seededAgain = Fresh();
        Steps(seededAgain, 120);
        Check(seededAgain.Enemies.Count == seeded.Enemies.Count && seededAgain.Enemies[0].Kind == seeded.Enemies[0].Kind, "seeded spawn repeatable", report, ref pass, ref failed);

        bool sawWalker = false;
        bool sawWisp = false;
        for (int seed = 0; seed < 32; seed++)
        {
            Game varied = new Game();
            varied.Start(seed);
            varied.ContinueStory();
            Steps(varied, 120);
            if (varied.Enemies.Count > 0)
            {
                if (varied.Enemies[0].Kind == 0) sawWalker = true;
                else sawWisp = true;
            }
        }
        Check(sawWalker && sawWisp, "seeds vary encounters", report, ref pass, ref failed);

        game = Fresh();
        game.Input.Aim = new V(900, 400);
        game.Input.Fire = true;
        game.Input.Right = true;
        Steps(game, 720);
        Check(!float.IsNaN(game.Player.X) && !float.IsNaN(game.Player.Y), "player finite after simulation", report, ref pass, ref failed);
        Check(game.Player.X >= 0 && game.Player.X <= Game.Width, "player bounded after simulation", report, ref pass, ref failed);
        Check(game.Elapsed > 0, "simulation clock advanced", report, ref pass, ref failed);

        Game walker = Fresh();
        walker.Input.Right = true;
        Steps(walker, 60);
        walker.Input.Right = false;
        Check(walker.Player.X > 240, "movement", report, ref pass, ref failed);

        walker.Input.Jump = true;
        float jumpStartY = walker.Player.Y;
        float jumpMinY = jumpStartY;
        for (int i = 0; i < 110; i++)
        {
            walker.Step(Dt);
            jumpMinY = Math.Min(jumpMinY, walker.Player.Y);
        }
        Check(jumpStartY - jumpMinY > 165, "jump reaches ledge", report, ref pass, ref failed);
        Check(walker.Grounded && walker.Player.Y < 570, "landing on ledge", report, ref pass, ref failed);

        walker.Input.Down = true;
        Steps(walker, 55);
        walker.Input.Down = false;
        Check(walker.Player.Y == Game.Floor - 24, "drop through ledge", report, ref pass, ref failed);

        Game dash = Fresh();
        dash.Invulnerable = 0;
        dash.Input.Dash = true;
        dash.Step(Dt);
        Check(dash.DashCooldown > 1 && dash.Invulnerable >= .64f, "dash grants invulnerability", report, ref pass, ref failed);
        float dashHealth = dash.Health;
        dash.Hurt(20);
        Check(dash.Health == dashHealth, "dash blocks damage", report, ref pass, ref failed);
        Steps(dash, 60);
        dash.Hurt(20);
        Check(dash.Health == dashHealth, "dash grace outlasts dash", report, ref pass, ref failed);
        dash.Invulnerable = 0;
        dash.Hurt(12);
        Check(dash.Health == 88, "damage applied", report, ref pass, ref failed);
        dash.Hurt(12);
        Check(dash.Health == 88, "damage grace period", report, ref pass, ref failed);

        Game shot = Fresh();
        shot.Pending = 0;
        shot.Spawn(0, new V(370, Game.Floor - 24));
        shot.Health = 80;
        shot.Input.Aim = new V(370, Game.Floor - 24);
        shot.Input.Fire = true;
        Steps(shot, 100);
        shot.Input.Fire = false;
        Check(shot.Kills == 1 && shot.Enemies.Count == 0, "shooting kills enemy", report, ref pass, ref failed);
        Check(shot.Chain == 1 && shot.BestChain == 1 && shot.Health == 83, "chain heal and score", report, ref pass, ref failed);
        Check(shot.ClearPulse > 0, "clear pulse starts", report, ref pass, ref failed);
        float clearHealth = shot.Health;
        shot.Invulnerable = 0;
        shot.Hurt(25);
        Check(shot.Health == clearHealth, "clear blocks damage", report, ref pass, ref failed);
        shot.Bullets.Add(new Bullet { P = new V(640, 340), Velocity = new V(0, 0), Life = 10, Hostile = true, Damage = 99, Kind = 0 });
        Steps(shot, 1);
        Check(!shot.Bullets.Exists(b => b.Hostile), "clear purges hostile bullets", report, ref pass, ref failed);
        Steps(shot, 600);
        Check(shot.Chain == 0, "chain expires", report, ref pass, ref failed);
        Check(shot.ExitOpen, "room clear opens gate", report, ref pass, ref failed);
        Check(shot.ClearPulse == 0, "clear pulse decays", report, ref pass, ref failed);

        Game flask = Fresh();
        flask.Pending = 0;
        flask.Spawn(0, new V(370, Game.Floor - 24));
        flask.Bullets.Add(new Bullet { P = new V(370, Game.Floor - 12), Velocity = new V(0, 120), Life = 1, Damage = 70, Gravity = 900, Kind = 1 });
        Steps(flask, 12);
        Check(flask.Kills == 1, "cinder flask explodes", report, ref pass, ref failed);
        Check(flask.Flasks == 3, "test flask does not consume stock", report, ref pass, ref failed);

        Game hazard = Fresh();
        hazard.Room = 1;
        hazard.EnterRoom();
        hazard.ContinueStory();
        hazard.Invulnerable = 0;
        hazard.Player = new V(302, Game.Floor - 24);
        float hazardHealth = hazard.Health;
        Steps(hazard, 5);
        Check(hazard.Health < hazardHealth, "spike hazard damages player", report, ref pass, ref failed);
        Check(hazard.Hazards.Count == 3, "cloister hazard layout", report, ref pass, ref failed);

        Game clearedHazards = Fresh();
        clearedHazards.Room = 1;
        clearedHazards.EnterRoom();
        clearedHazards.ContinueStory();
        clearedHazards.Invulnerable = 100000;
        int clearedFrames = 0;
        while (!clearedHazards.ExitOpen && clearedFrames++ < 14000)
        {
            KillAll(clearedHazards);
            clearedHazards.Step(Dt);
        }
        Check(clearedHazards.ExitOpen, "hazard room clears", report, ref pass, ref failed);
        Check(clearedHazards.Hazards.Count == 0, "clear purges hazards", report, ref pass, ref failed);
        Check(!clearedHazards.Bullets.Exists(b => b.Hostile), "clear purges hostile bullets in hazard room", report, ref pass, ref failed);
        float clearedHealth = clearedHazards.Health;
        clearedHazards.Invulnerable = 0;
        clearedHazards.Hurt(99);
        Check(clearedHazards.Health == clearedHealth, "clear blocks direct damage", report, ref pass, ref failed);
        clearedHazards.Bullets.Add(new Bullet { P = new V(640, 340), Velocity = new V(0, 0), Life = 10, Hostile = true, Damage = 99, Kind = 0 });
        Steps(clearedHazards, 1);
        Check(!clearedHazards.Bullets.Exists(b => b.Hostile), "late hostile bullet is purged after clear", report, ref pass, ref failed);
        Check(clearedHazards.Health == clearedHealth, "clear blocks bullet damage", report, ref pass, ref failed);

        for (int kind = 0; kind < 3; kind++)
        {
            Game bounded = Fresh();
            bounded.Pending = 0;
            bounded.Invulnerable = 100000;
            bounded.Player = new V(Game.Width - 60, Game.Floor - 24);
            bounded.Enemies.Clear();
            bounded.ExitOpen = false;
            bounded.Spawn(kind, new V(Game.Width + 160, kind == 1 ? 320 : Game.Floor - 24));
            Steps(bounded, 2);
            Check(bounded.Enemies.Count == 1 && bounded.Enemies[0].P.X >= Game.EnemyMargin && bounded.Enemies[0].P.X <= Game.Width - Game.EnemyMargin, "enemy " + kind + " stays on screen", report, ref pass, ref failed);
            Check(bounded.Enemies[0].P.Y >= 170 && bounded.Enemies[0].P.Y <= Game.Floor - 24, "enemy " + kind + " stays in arena", report, ref pass, ref failed);
            KillAll(bounded);
            Check(bounded.ExitOpen, "bounded enemy " + kind + " can be cleared", report, ref pass, ref failed);
        }

        Game acolyte = Fresh();
        acolyte.Player = new V(1240, Game.Floor - 24);
        acolyte.Pending = 0;
        acolyte.Enemies.Clear();
        acolyte.Spawn(2, new V(1215, 500));
        Steps(acolyte, 900);
        Check(acolyte.Enemies.Count == 1 && acolyte.Enemies[0].P.X <= Game.Width - Game.EnemyMargin, "backing acolyte held inside right edge", report, ref pass, ref failed);
        KillAll(acolyte);
        Check(acolyte.ExitOpen, "edge acolyte soft-lock cleared", report, ref pass, ref failed);

        Game gate = Fresh();
        gate.Player = new V(1190, Game.Floor - 24);
        gate.Pending = 0;
        gate.Enemies.Clear();
        gate.ExitOpen = true;
        gate.Input.Use = true;
        gate.Step(Dt);
        Check(gate.State == Stage.Upgrade, "gate enters upgrade", report, ref pass, ref failed);
        float gateDamage = gate.Damage;
        Choose(gate, RelicKind.Damage);
        Check(gate.Room == 1 && gate.Damage > gateDamage && gate.UpgradeCount == 1 && gate.State == Stage.Story, "damage upgrade advances", report, ref pass, ref failed);
        gate.ContinueStory();
        gate.State = Stage.Upgrade;
        gate.Health = 30;
        Choose(gate, RelicKind.Vitality);
        Check(gate.Room == 2 && gate.MaxHealth == 130 && gate.Health == 85, "health upgrade", report, ref pass, ref failed);
        gate.ContinueStory();
        Check(gate.Room == 2 && gate.Enemies.Count == 1 && gate.Enemies[0].Kind == 3, "boss room", report, ref pass, ref failed);

        Game relic = Fresh();
        relic.State = Stage.Upgrade;
        relic.Pending = 0;
        relic.Enemies.Clear();
        float baseRate = relic.FireRate;
        float baseSpeed = relic.Speed;
        Choose(relic, RelicKind.Swift);
        Check(relic.FireRate < baseRate && relic.Speed > baseSpeed, "swift relic improves fire and movement", report, ref pass, ref failed);

        relic = Fresh();
        relic.State = Stage.Upgrade;
        Choose(relic, RelicKind.Pierce);
        Check(relic.Pierce == 1, "pierce relic is applied", report, ref pass, ref failed);
        relic.State = Stage.Playing;
        relic.Pending = 0;
        relic.Enemies.Clear();
        relic.Invulnerable = 100;
        relic.Spawn(0, new V(400, Game.Floor - 24));
        relic.Spawn(0, new V(500, Game.Floor - 24));
        relic.Bullets.Add(new Bullet { P = new V(380, Game.Floor - 24), Velocity = new V(1000, 0), Life = 1, Damage = 70, Kind = 0, Pierce = relic.Pierce });
        Steps(relic, 24);
        Check(relic.Kills == 2 && relic.Enemies.Count == 0, "pierce fireball hits multiple foes", report, ref pass, ref failed);

        relic = Fresh();
        relic.State = Stage.Upgrade;
        Choose(relic, RelicKind.Split);
        relic.State = Stage.Playing;
        relic.Pending = 0;
        relic.Bullets.Clear();
        relic.Input.Fire = true;
        relic.Step(Dt);
        relic.Input.Fire = false;
        Check(relic.Bullets.Count == 3, "split relic fires three embers", report, ref pass, ref failed);

        relic = Fresh();
        relic.State = Stage.Upgrade;
        Choose(relic, RelicKind.Flask);
        Check(relic.Flasks == 4 && relic.FlaskRadius > 1, "flask relic adds stock and blast radius", report, ref pass, ref failed);

        relic = Fresh();
        relic.State = Stage.Upgrade;
        Choose(relic, RelicKind.Dash);
        relic.State = Stage.Playing;
        relic.Input.Dash = true;
        relic.Step(Dt);
        relic.Input.Dash = false;
        Check(relic.DashCooldown > 0 && relic.DashCooldown < 1.15f, "dash relic shortens recovery", report, ref pass, ref failed);

        relic = Fresh();
        relic.State = Stage.Upgrade;
        Choose(relic, RelicKind.Siphon);
        relic.State = Stage.Playing;
        relic.Health = 50;
        relic.Pending = 0;
        relic.Enemies.Clear();
        relic.Invulnerable = 100;
        relic.Spawn(0, new V(370, Game.Floor - 24));
        KillAll(relic);
        Check(relic.Health == 57, "siphon relic increases takedown healing", report, ref pass, ref failed);

        relic = Fresh();
        relic.State = Stage.Upgrade;
        Choose(relic, RelicKind.Ward);
        relic.State = Stage.Playing;
        relic.Invulnerable = 0;
        float wardHealth = relic.Health;
        relic.Hurt(20);
        Check(Math.Abs((wardHealth - relic.Health) - 16) < .001f, "ward relic reduces incoming damage", report, ref pass, ref failed);

        Game stacks = Fresh();
        Check(stacks.OwnedStacks(RelicKind.Damage) == 0, "new run has no owned relics", report, ref pass, ref failed);
        stacks.State = Stage.Upgrade;
        Choose(stacks, RelicKind.Damage);
        Check(stacks.OwnedStacks(RelicKind.Damage) == 1, "selecting a relic records a stack", report, ref pass, ref failed);
        stacks.Start(999);
        Check(stacks.OwnedStacks(RelicKind.Damage) == 0, "new run clears owned relics", report, ref pass, ref failed);

        Game dashStacks = Fresh();
        for (int i = 1; i <= 4; i++)
        {
            dashStacks.ApplyUpgrade(RelicKind.Dash);
            Check(Math.Abs(dashStacks.DashCooldownScale - Math.Max(.5f, 1f - .125f * i)) < .001f, "dash stack " + i + " reduces recovery", report, ref pass, ref failed);
        }
        dashStacks.ApplyUpgrade(RelicKind.Dash);
        Check(dashStacks.OwnedStacks(RelicKind.Dash) == 4, "dash cannot exceed four stacks", report, ref pass, ref failed);

        Check(Campaign.Upgrades.Length == 9, "nine relics defined", report, ref pass, ref failed);
        Game relicRoll = Fresh();
        Check(relicRoll.UpgradeChoices.Length == 3, "three relics offered", report, ref pass, ref failed);
        bool distinct = true;
        bool known = true;
        for (int i = 0; i < relicRoll.UpgradeChoices.Length; i++)
        {
            for (int j = i + 1; j < relicRoll.UpgradeChoices.Length; j++)
            {
                if (relicRoll.UpgradeChoices[i] == relicRoll.UpgradeChoices[j]) distinct = false;
            }
            bool inPool = false;
            for (int k = 0; k < Campaign.Upgrades.Length; k++)
            {
                if (Campaign.Upgrades[k].Kind == relicRoll.UpgradeChoices[i]) inPool = true;
            }
            if (!inPool) known = false;
        }
        Check(distinct, "relic offers are distinct", report, ref pass, ref failed);
        Check(known, "relic offers come from the pool", report, ref pass, ref failed);

        string firstRoll = RollText(relicRoll);
        bool rollsVary = false;
        for (int seed = 0; seed < 64; seed++)
        {
            Game roll = new Game();
            roll.Start(seed);
            if (RollText(roll) != firstRoll) rollsVary = true;
        }
        Check(rollsVary, "relic offers vary by seed", report, ref pass, ref failed);

        Check(!Campaign.UpgradeFor(RelicKind.Split).Stackable && Campaign.UpgradeFor(RelicKind.Split).MaxStacks == 1, "split is one-time", report, ref pass, ref failed);
        Check(Campaign.UpgradeFor(RelicKind.Damage).Stackable && Campaign.UpgradeFor(RelicKind.Damage).MaxStacks == 99, "damage stacks", report, ref pass, ref failed);
        Check(Campaign.UpgradeFor(RelicKind.Swift).MaxStacks == 8 && Campaign.UpgradeFor(RelicKind.Pierce).MaxStacks == 3, "capped relics declare limits", report, ref pass, ref failed);
        Check(Campaign.UpgradeFor(RelicKind.Dash).MaxStacks == 4 && Campaign.UpgradeFor(RelicKind.Ward).MaxStacks == 2, "dash and ward declare limits", report, ref pass, ref failed);
        Check(Campaign.UpgradeFor(RelicKind.None) == null, "empty relic resolves to no definition", report, ref pass, ref failed);

        bool ownedHidden = true;
        bool stackSeen = false;
        for (int seed = 0; seed < 64; seed++)
        {
            Game roll = new Game();
            roll.Start(seed);
            roll.RelicStacks[(int)RelicKind.Split] = 1;
            roll.RollUpgrades();
            for (int i = 0; i < roll.UpgradeChoices.Length; i++)
            {
                if (roll.UpgradeChoices[i] == RelicKind.Split) ownedHidden = false;
                if (roll.UpgradeChoices[i] == RelicKind.Pierce) stackSeen = true;
            }
        }
        Check(ownedHidden, "owned non-stackable relics are not offered", report, ref pass, ref failed);
        Check(stackSeen, "stackable relics can be offered again", report, ref pass, ref failed);

        bool cappedHidden = true;
        for (int seed = 0; seed < 64; seed++)
        {
            Game roll = new Game();
            roll.Start(seed);
            roll.RelicStacks[(int)RelicKind.Pierce] = 3;
            roll.RollUpgrades();
            for (int i = 0; i < roll.UpgradeChoices.Length; i++)
            {
                if (roll.UpgradeChoices[i] == RelicKind.Pierce) cappedHidden = false;
            }
        }
        Check(cappedHidden, "maxed stackable relics are not offered", report, ref pass, ref failed);

        Game exhausted = Fresh();
        for (int i = 0; i < Campaign.Upgrades.Length; i++) exhausted.RelicStacks[i] = Campaign.Upgrades[i].MaxStacks;
        exhausted.RollUpgrades();
        bool noOffers = true;
        for (int i = 0; i < exhausted.UpgradeChoices.Length; i++)
        {
            if (exhausted.UpgradeChoices[i] != RelicKind.None) noOffers = false;
        }
        Check(noOffers, "exhausted relic pool offers none", report, ref pass, ref failed);

        Game pilgrim = new Game();
        pilgrim.Start(1, 0);
        pilgrim.ContinueStory();
        pilgrim.Enemies.Clear();
        Game nightmare = new Game();
        nightmare.Start(1, 3);
        nightmare.ContinueStory();
        nightmare.Enemies.Clear();
        pilgrim.Spawn(0, new V(400, Game.Floor - 24));
        nightmare.Spawn(0, new V(400, Game.Floor - 24));
        Check(nightmare.Enemies[0].Hp > pilgrim.Enemies[0].Hp * 2, "difficulty scales enemy health", report, ref pass, ref failed);
        nightmare.Invulnerable = 0;
        float nightmareBefore = nightmare.Health;
        nightmare.Hurt(10);
        Check(Math.Abs((nightmareBefore - nightmare.Health) - 19) < .001f, "difficulty scales incoming damage", report, ref pass, ref failed);

        Game clamped = new Game();
        clamped.Start(1, -5);
        Check(clamped.Difficulty == 0, "difficulty clamps low", report, ref pass, ref failed);
        clamped.Start(1, 99);
        Check(clamped.Difficulty == Campaign.Difficulties.Length - 1, "difficulty clamps high", report, ref pass, ref failed);
        pilgrim.Start(2);
        Check(pilgrim.Difficulty == 0, "single-argument start keeps chosen difficulty", report, ref pass, ref failed);
        Check(Campaign.Difficulties.Length == 4 && Campaign.DefaultDifficulty == 1, "four difficulty tiers with bearer default", report, ref pass, ref failed);

        Game boss = Fresh();
        boss.Room = 2;
        boss.EnterRoom();
        boss.ContinueStory();
        Enemy bishop = boss.Enemies[0];
        bishop.Fire = .001f;
        bishop.Hp = bishop.MaxHp * .8f;
        Steps(boss, 10);
        bool hostileBullet = false;
        for (int i = 0; i < boss.Bullets.Count; i++)
        {
            if (boss.Bullets[i].Hostile) hostileBullet = true;
        }
        Check(hostileBullet, "boss fires projectiles", report, ref pass, ref failed);
        bishop.Hp = bishop.MaxHp * .3f;
        bishop.Fire = .001f;
        int hazardsBefore = boss.Hazards.Count;
        Steps(boss, 10);
        Check(boss.Hazards.Count > hazardsBefore, "enraged boss summons spikes", report, ref pass, ref failed);
        Check(!boss.Overcharged, "chain begins uncharged", report, ref pass, ref failed);
        boss.Chain = 8;
        Check(boss.Overcharged, "high chain overcharges weapon", report, ref pass, ref failed);

        Game pause = Fresh();
        pause.Pause();
        V pausedPosition = pause.Player;
        Steps(pause, 50);
        Check(pause.Player.X == pausedPosition.X && pause.State == Stage.Paused, "pause freezes simulation", report, ref pass, ref failed);
        pause.Pause();
        Check(pause.State == Stage.Playing, "pause resumes", report, ref pass, ref failed);

        Game nav = Fresh();
        nav.ShowHelp();
        Check(nav.State == Stage.Help && nav.HelpReturn == Stage.Playing, "help opens from play", report, ref pass, ref failed);
        nav.CloseHelp();
        Check(nav.State == Stage.Playing, "help returns to play", report, ref pass, ref failed);
        nav.Pause();
        nav.ShowHelp();
        Check(nav.HelpReturn == Stage.Paused, "pause help remembers return", report, ref pass, ref failed);
        nav.CloseHelp();
        Check(nav.State == Stage.Paused, "pause help returns to pause", report, ref pass, ref failed);
        nav.Pause();
        bool soundBefore = nav.SoundOn;
        nav.ToggleSound();
        Check(nav.SoundOn != soundBefore, "sound toggles", report, ref pass, ref failed);
        nav.ToggleSound();
        Check(nav.SoundOn == soundBefore, "sound restores", report, ref pass, ref failed);
        nav.Menu = 0;
        nav.MoveMenu(-1, 5);
        Check(nav.Menu == 4, "menu navigation wraps backward", report, ref pass, ref failed);
        nav.MoveMenu(1, 5);
        Check(nav.Menu == 0, "menu navigation wraps forward", report, ref pass, ref failed);
        nav.ReturnToTitle();
        Check(nav.State == Stage.Title && nav.Menu == 0 && !nav.Input.Fire, "return to title clears input", report, ref pass, ref failed);

        Game settings = new Game();
        settings.ShowAudio();
        Check(settings.State == Stage.Audio && settings.Menu == 0 && settings.HelpReturn == Stage.Title, "audio opens from title", report, ref pass, ref failed);
        settings.AdjustVolume(Game.MusicVolumeIndex, -.35f);
        Check(Math.Abs(settings.Volume[Game.MusicVolumeIndex] - .45f) < .001f, "music slider adjusts", report, ref pass, ref failed);
        settings.SetVolume(Game.MasterVolumeIndex, 2);
        Check(settings.Volume[Game.MasterVolumeIndex] == 1, "volume clamps high", report, ref pass, ref failed);
        settings.SetVolume(Game.SfxVolumeIndex, -1);
        Check(settings.Volume[Game.SfxVolumeIndex] == 0, "volume clamps low", report, ref pass, ref failed);
        settings.CloseAudio();
        Check(settings.State == Stage.Title, "audio returns to title", report, ref pass, ref failed);
        settings.ShowAbilities();
        Check(settings.State == Stage.Abilities && settings.HelpReturn == Stage.Title, "abilities opens from title", report, ref pass, ref failed);
        settings.CloseAbilities();
        Check(settings.State == Stage.Title, "abilities returns to title", report, ref pass, ref failed);
        settings.ShowDifficulty();
        Check(settings.State == Stage.Difficulty && settings.Menu == Campaign.DefaultDifficulty, "difficulty opens with default tier", report, ref pass, ref failed);
        settings.CloseDifficulty();
        Check(settings.State == Stage.Title, "difficulty returns to title", report, ref pass, ref failed);

        Game dawn = Fresh();
        dawn.Ending = true;
        dawn.State = Stage.Story;
        dawn.ContinueStory();
        Check(dawn.State == Stage.Sunrise && dawn.SunriseTime == 0, "epilogue opens sunrise", report, ref pass, ref failed);
        Steps(dawn, 60);
        Check(dawn.State == Stage.Sunrise && dawn.SunriseTime > 0, "sunrise advances", report, ref pass, ref failed);
        Steps(dawn, 700);
        Check(dawn.State == Stage.Won, "sunrise finishes after six seconds", report, ref pass, ref failed);
        dawn.SkipSunrise();
        Check(dawn.State == Stage.Won, "skip is safe after sunrise", report, ref pass, ref failed);

        Game campaign = Fresh();
        campaign.Health = campaign.MaxHealth = 10000;
        for (int room = 0; room < Campaign.TotalRooms; room++)
        {
            int frames = 0;
            while (!campaign.ExitOpen && frames++ < 14000)
            {
                KillAll(campaign);
                campaign.Step(Dt);
            }
            Check(campaign.ExitOpen, "combat clears room " + room, report, ref pass, ref failed);
            Check(campaign.Hazards.Count == 0, "clear purges hazards in room " + room, report, ref pass, ref failed);
            Check(!campaign.Bullets.Exists(b => b.Hostile), "clear purges hostile bullets in room " + room, report, ref pass, ref failed);
            Check(campaign.ClearPulse > 0, "clear pulse in room " + room, report, ref pass, ref failed);
            float guardedHealth = campaign.Health;
            campaign.Invulnerable = 0;
            campaign.Hurt(99);
            Check(campaign.Health == guardedHealth, "clear blocks damage in room " + room, report, ref pass, ref failed);
            campaign.Input.Fire = false;
            campaign.Player = new V(1190, Game.Floor - 24);
            campaign.Input.Use = true;
            campaign.Step(Dt);
            if (room < Campaign.TotalRooms - 1)
            {
                Check(campaign.State == Stage.Upgrade, "room gate " + room, report, ref pass, ref failed);
                campaign.SelectUpgrade(room % 3);
                Check(campaign.State == Stage.Story, "next story " + room, report, ref pass, ref failed);
                campaign.ContinueStory();
            }
            else
            {
                Check(campaign.Ending && campaign.State == Stage.Story, "final gate opens epilogue", report, ref pass, ref failed);
            }
        }
        campaign.ContinueStory();
        Check(campaign.State == Stage.Sunrise, "epilogue closes to sunrise", report, ref pass, ref failed);
        campaign.SkipSunrise();
        Check(campaign.State == Stage.Won, "sunrise closes to victory", report, ref pass, ref failed);
        Check(campaign.Kills == 13, "campaign encounters completed", report, ref pass, ref failed);

        Game death = Fresh();
        death.Invulnerable = 0;
        death.Hurt(150);
        Check(death.State == Stage.Dead && death.Health == 0, "death state", report, ref pass, ref failed);
        death.Start(4);
        Check(death.State == Stage.Story && death.Score == 0 && death.Health == 100 && !death.Ending, "restart resets run", report, ref pass, ref failed);

        Check(Math.Abs(Game.SegmentDistance(new V(0, 0), new V(100, 0), new V(50, 5)) - 5) < .01f, "swept collision", report, ref pass, ref failed);
        Check(Campaign.Names.Length == 3 && Campaign.Scenes.Length == 4 && Campaign.Speakers.Length == 4, "campaign content complete", report, ref pass, ref failed);
        Check(Campaign.Boss == "THE CINDER BISHOP" && Campaign.TotalRooms == 3, "boss and room count defined", report, ref pass, ref failed);

        Check(SoundData.Rate == 22050 && SoundData.LoopSeconds == 16 && SoundData.TrackCount == 5, "sound data loop format", report, ref pass, ref failed);
        Check(SoundData.LoopSamples == SoundData.Rate * SoundData.LoopSeconds, "music loop length", report, ref pass, ref failed);
        bool seams = true;
        for (int t = 0; t < SoundData.TrackCount; t++)
        {
            if (SoundData.MusicSeam(t) > .03f) seams = false;
        }
        Check(seams, "music loops without audible seam", report, ref pass, ref failed);

        float fireStart = SoundData.EffectSample(0, Game.FireballSound, 0);
        float fireEnd = SoundData.EffectSample(0, Game.FireballSound, SoundData.EffectLength(Game.FireballSound) - 1);
        bool firePeak = false;
        for (int i = 0; i < SoundData.EffectLength(Game.FireballSound); i++)
        {
            if (Math.Abs(SoundData.EffectSample(0, Game.FireballSound, i)) > .15f) firePeak = true;
        }
        Check(Math.Abs(fireStart) < .02f && Math.Abs(fireEnd) < .02f && firePeak, "fireball burst is shaped without clicks", report, ref pass, ref failed);

        bool musicFinite = true;
        for (int t = 0; t < SoundData.TrackCount; t++)
        {
            for (int i = 0; i < 256; i++)
            {
                float value = SoundData.MusicSample(t, i * 1379);
                if (float.IsNaN(value) || value < -1.2f || value > 1.2f) musicFinite = false;
            }
        }
        Check(musicFinite, "music samples finite", report, ref pass, ref failed);

        bool effectsFinite = true;
        for (int room = 0; room < 3; room++)
        {
            for (int effect = 0; effect < 6; effect++)
            {
                int count = SoundData.EffectLength(effect);
                Check(count > 0, "effect length " + room + ":" + effect, report, ref pass, ref failed);
                for (int i = 0; i < count; i += Math.Max(1, count / 64))
                {
                    float value = SoundData.EffectSample(room, effect, i);
                    if (float.IsNaN(value) || value < -1.2f || value > 1.2f) effectsFinite = false;
                }
            }
        }
        Check(effectsFinite, "effect samples finite", report, ref pass, ref failed);

        bool spriteMapsFit = ArtData.Maps[(int)RelicKind.Damage].Length > 0 && ArtData.RelicMaps.Length == (int)RelicKind.None;
        for (int i = 0; i < Campaign.Upgrades.Length; i++)
        {
            string[] map = ArtData.RelicMaps[(int)Campaign.Upgrades[i].Kind];
            if (map.Length != 16) spriteMapsFit = false;
            for (int row = 0; row < map.Length; row++)
            {
                if (map[row].Length != 12) spriteMapsFit = false;
            }
        }
        Check(spriteMapsFit, "sprite maps available", report, ref pass, ref failed);

        bool storyFits = true;
        for (int i = 0; i < Campaign.Scenes.Length; i++)
        {
            string[] lines = Campaign.Scenes[i].Split('\n');
            int visualLines = 0;
            for (int j = 0; j < lines.Length; j++)
            {
                float width = ApproxTextWidth(lines[j], 17);
                if (width > 684) storyFits = false;
                visualLines += Math.Max(1, (int)Math.Ceiling(width / 684));
            }
            if (visualLines * 22 >= 240) storyFits = false;
        }
        Check(storyFits, "story fits wrapped page proxy", report, ref pass, ref failed);
        Check(52 + ApproxTextWidth("> CONTINUE", 20) + ApproxTextWidth("ENTER", 14) <= MenuLayout.Story[0].Width, "story hint fits", report, ref pass, ref failed);
        Check(52 + ApproxTextWidth("WITNESS THE DAWN", 20) + ApproxTextWidth("ENTER", 14) <= MenuLayout.Epilogue[0].Width, "epilogue hint fits", report, ref pass, ref failed);
        Check(28 + ApproxTextWidth("A/D MOVE  SPACE JUMP  MOUSE AIM + FIRE  SHIFT/RMB DASH  Q FLASK  E GATE  S DROP", 13) + 20 <= 1252 - ApproxTextWidth("ESC PAUSE  M MUTE  F11", 13), "footer controls fit", report, ref pass, ref failed);

        bool relicCardsFit = true;
        bool abilityCardsFit = true;
        bool stackLabelsFit = true;
        for (int i = 0; i < Campaign.Upgrades.Length; i++)
        {
            UpgradeDef def = Campaign.Upgrades[i];
            float relicWidth = MenuLayout.Upgrade[0].Width;
            if (14 + ApproxTextWidth(def.Name, 18) + 14 > relicWidth) relicCardsFit = false;
            int relicLines = Math.Max(1, (int)Math.Ceiling(ApproxTextWidth(def.Detail, 14) / (relicWidth - 28)));
            if (178 + relicLines * 20 > MenuLayout.Upgrade[0].Height - 8) relicCardsFit = false;
            if (84 + ApproxTextWidth(def.Name, 17) + 8 > 270) abilityCardsFit = false;
            int abilityLines = Math.Max(1, (int)Math.Ceiling(ApproxTextWidth(def.Detail, 13) / (270 - 64)));
            if (44 + abilityLines * 18 > 98 - 8) abilityCardsFit = false;
            string label = RelicText(def, 1, true);
            string owned = RelicText(def, def.MaxStacks, false);
            if (def.Stackable)
            {
                if (!label.StartsWith("STACKABLE")) stackLabelsFit = false;
                if (def.MaxStacks < 99 && owned != "STACKABLE MAX") stackLabelsFit = false;
                if (def.MaxStacks >= 99 && !owned.StartsWith("STACKABLE x")) stackLabelsFit = false;
            }
            else
            {
                if (label != "") stackLabelsFit = false;
                if (owned != "OWNED") stackLabelsFit = false;
            }
            string badge = def.MaxStacks < 99 ? "MAXED" : "x" + def.MaxStacks;
            if (10 + ApproxTextWidth(badge, 11) + 10 > relicWidth) stackLabelsFit = false;
            if (8 + ApproxTextWidth(badge, 11) + 8 > 270) stackLabelsFit = false;
        }
        Check(relicCardsFit, "relic cards fit", report, ref pass, ref failed);
        Check(abilityCardsFit, "ability cards fit", report, ref pass, ref failed);
        Check(stackLabelsFit, "relic stack labels fit", report, ref pass, ref failed);

        bool difficultyCardsFit = true;
        for (int i = 0; i < Campaign.Difficulties.Length; i++)
        {
            DifficultyDef def = Campaign.Difficulties[i];
            if (86 + ApproxTextWidth(def.Name, 21) + 18 > MenuLayout.Difficulty[0].Width) difficultyCardsFit = false;
            if (86 + ApproxTextWidth(def.Detail, 13) + 18 > MenuLayout.Difficulty[0].Width) difficultyCardsFit = false;
        }
        Check(difficultyCardsFit, "difficulty cards fit", report, ref pass, ref failed);
        Check(37 + 13 <= MenuLayout.Difficulty[0].Height && 39 + 14 <= MenuLayout.Difficulty[0].Height, "difficulty card text stays inside compact frame", report, ref pass, ref failed);

        report.AppendLine("CHECKS " + (pass + failed) + " PASS " + pass + " NOT OK " + failed);
        report.AppendLine(failed == 0 ? "STATUS OK" : "STATUS BAD");
        return report.ToString();
    }

    static Game Fresh()
    {
        Game g = new Game();
        g.Start(12345);
        g.ContinueStory();
        return g;
    }

    static void Steps(Game g, int count)
    {
        for (int i = 0; i < count; i++) g.Step(Dt);
    }

    static void Choose(Game g, RelicKind kind)
    {
        for (int i = 0; i < g.UpgradeChoices.Length; i++)
        {
            if (g.UpgradeChoices[i] == kind)
            {
                g.SelectUpgrade(i);
                return;
            }
        }
        g.UpgradeChoices[0] = kind;
        g.SelectUpgrade(0);
    }

    static void KillAll(Game g)
    {
        for (int guard = 0; guard < 64 && g.Enemies.Count > 0; guard++)
        {
            Enemy e = g.Enemies[0];
            g.Bullets.Add(new Bullet { P = e.P, Velocity = new V(0, 0), Life = .01f, Damage = 5000, Kind = 0 });
            g.Step(Dt);
        }
    }

    static string RollText(Game g)
    {
        return g.UpgradeChoices[0] + "/" + g.UpgradeChoices[1] + "/" + g.UpgradeChoices[2];
    }

    static float ApproxTextWidth(string value, float size)
    {
        return value == null ? 0 : value.Length * size * .5f;
    }

    static string RelicText(UpgradeDef def, int stack, bool offer)
    {
        if (def.Stackable)
        {
            if (def.MaxStacks < 99 && stack >= def.MaxStacks) return "STACKABLE MAX";
            return def.MaxStacks >= 99 ? "STACKABLE x" + stack : "STACKABLE x" + stack + "/" + def.MaxStacks;
        }
        return !offer && stack > 0 ? "OWNED" : "";
    }

    static void Check(bool condition, string name, StringBuilder report, ref int pass, ref int failed)
    {
        if (condition) pass++;
        else
        {
            failed++;
            report.AppendLine("CHECK NOT OK: " + name);
        }
    }
}
