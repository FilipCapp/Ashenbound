using System;
using Godot;

namespace Ashenbound;

public partial class AudioDirector : Node
{
    const int EffectsPerRoom = 6;

    readonly AudioStreamWav[] music = new AudioStreamWav[SoundData.TrackCount];
    readonly AudioStreamWav[] effects = new AudioStreamWav[3 * EffectsPerRoom];
    readonly AudioStreamPlayer[] sfx = new AudioStreamPlayer[16];

    AudioStreamPlayer musicPlayer;
    int musicBus;
    int sfxBus;
    int room;
    int currentTrack = -1;
    int cursor;
    bool soundOn = true;
    bool headless;
    readonly float[] lastVolume = { -1, -1, -1 };

    public override void _Ready()
    {
        headless = DisplayServer.GetName() == "headless";
        if (headless) return;

        musicBus = EnsureBus("Music");
        sfxBus = EnsureBus("Sfx");

        for (int i = 0; i < music.Length; i++)
        {
            music[i] = MakeStream(null, SoundData.LoopSamples, true, i);
        }
        float[] samples = new float[SoundData.LoopSamples];
        for (int i = 0; i < music.Length; i++)
        {
            for (int n = 0; n < samples.Length; n++) samples[n] = SoundData.MusicSample(i, n);
            music[i].Data = Pcm(samples);
        }

        int effectIndex = 0;
        for (int r = 0; r < 3; r++)
        {
            for (int n = 0; n < EffectsPerRoom; n++)
            {
                int count = SoundData.EffectLength(n);
                float[] data = new float[count];
                for (int i = 0; i < count; i++) data[i] = SoundData.EffectSample(r, n, i);
                effects[effectIndex++] = MakeStream(data, count, false, 0);
            }
        }

        musicPlayer = new AudioStreamPlayer();
        musicPlayer.Bus = "Music";
        AddChild(musicPlayer);

        for (int i = 0; i < sfx.Length; i++)
        {
            AudioStreamPlayer player = new AudioStreamPlayer();
            player.Bus = "Sfx";
            AddChild(player);
            sfx[i] = player;
        }
    }

    public override void _ExitTree()
    {
        if (musicPlayer != null)
        {
            musicPlayer.Stop();
            musicPlayer.Stream = null;
            musicPlayer = null;
        }
        for (int i = 0; i < sfx.Length; i++)
        {
            AudioStreamPlayer player = sfx[i];
            if (player == null) continue;
            player.Stop();
            player.Stream = null;
            sfx[i] = null;
        }
        for (int i = 0; i < music.Length; i++)
        {
            music[i]?.Dispose();
            music[i] = null;
        }
        for (int i = 0; i < effects.Length; i++)
        {
            effects[i]?.Dispose();
            effects[i] = null;
        }
    }

    static int EnsureBus(string name)
    {
        int index = AudioServer.GetBusIndex(name);
        if (index >= 0) return index;
        AudioServer.AddBus();
        index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, name);
        return index;
    }

    static AudioStreamWav MakeStream(float[] source, int count, bool loop, int track)
    {
        AudioStreamWav stream = new AudioStreamWav();
        stream.MixRate = SoundData.Rate;
        stream.Stereo = false;
        stream.Format = (AudioStreamWav.FormatEnum)1;
        stream.LoopMode = loop ? (AudioStreamWav.LoopModeEnum)1 : (AudioStreamWav.LoopModeEnum)0;
        stream.LoopBegin = 0;
        stream.LoopEnd = loop ? count : 0;
        float[] data = new float[count];
        if (source != null && source.Length >= count)
        {
            Array.Copy(source, data, count);
        }
        else if (loop)
        {
            for (int i = 0; i < count; i++) data[i] = SoundData.MusicSample(track, i);
        }
        stream.Data = Pcm(data);
        return stream;
    }

    static byte[] Pcm(float[] samples)
    {
        byte[] result = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            float value = Mathf.Clamp(samples[i], -1f, 1f);
            int pcm = (int)Mathf.Round(value * 32767f);
            result[i * 2] = (byte)(pcm & 0xff);
            result[i * 2 + 1] = (byte)((pcm >> 8) & 0xff);
        }
        return result;
    }

    public void Attach(Game game)
    {
        game.PlaySound = Play;
        game.RoomEntered = SetRoom;
    }

    public void SetRoom(int value)
    {
        room = Mathf.Clamp(value, 0, 2);
    }

    public void Play(int effect)
    {
        if (headless) return;
        if (effect < 0 || effect >= EffectsPerRoom) return;
        int index = room * EffectsPerRoom + effect;
        if (index < 0 || index >= effects.Length) return;
        AudioStreamPlayer player = sfx[cursor];
        cursor = (cursor + 1) % sfx.Length;
        player.Stream = effects[index];
        player.Play();
    }

    public void Sync(Game game)
    {
        if (headless || game == null || musicPlayer == null) return;
        bool changed = game.SoundOn != soundOn;
        soundOn = game.SoundOn;
        for (int i = 0; i < lastVolume.Length; i++)
        {
            if (Mathf.Abs(game.Volume[i] - lastVolume[i]) > .0001f) changed = true;
            lastVolume[i] = game.Volume[i];
        }
        if (changed) ApplyVolumes(game);

        int desired;
        if (game.State == Stage.Playing) desired = Mathf.Clamp(game.Room, 0, 2);
        else if (game.State == Stage.Sunrise || game.State == Stage.Won) desired = 4;
        else desired = 3;

        if (desired != currentTrack)
        {
            musicPlayer.Stream = music[desired];
            currentTrack = desired;
        }
        if (!musicPlayer.Playing) musicPlayer.Play();
    }

    public void ApplyVolumes(Game game)
    {
        if (headless || game == null) return;
        AudioServer.SetBusMute(0, !game.SoundOn);
        AudioServer.SetBusVolumeLinear(0, game.Volume[Game.MasterVolumeIndex]);
        AudioServer.SetBusVolumeLinear(musicBus, game.Volume[Game.MusicVolumeIndex]);
        AudioServer.SetBusVolumeLinear(sfxBus, game.Volume[Game.SfxVolumeIndex]);
    }
}
