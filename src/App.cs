using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace Ashenbound {
public class GameWindow : Form {
 readonly Game game=new Game();readonly Renderer renderer=new Renderer();readonly Timer timer=new Timer();
 readonly Stopwatch watch=Stopwatch.StartNew();readonly Audio audio=new Audio();
   double previous,accumulator;bool fullscreen;public bool CursorHidden;Rectangle oldBounds;FormWindowState oldState;
  public GameWindow():this(true){}
  public GameWindow(bool autoFullscreen){
   Text="Ashenbound | The Last Cinder";ClientSize=new Size(1280,720);MinimumSize=new Size(800,480);
   StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;KeyPreview=true;BackColor=Color.Black;
   Shown+=delegate{Cursor.Hide();CursorHidden=true;if(autoFullscreen)ToggleFullscreen();};
   game.PlaySound=audio.Play;game.RoomEntered=audio.SetRoom;timer.Interval=8;timer.Tick+=TickGame;timer.Start();
   KeyDown+=Down;KeyUp+=Up;MouseMove+=MoveMouse;MouseDown+=MousePressed;MouseUp+=MouseReleased;
   Deactivate+=delegate{if(game.State==Stage.Playing)game.Pause();ClearInput();};
    FormClosed+=delegate{audio.StopMusic();timer.Stop();timer.Dispose();renderer.Dispose();audio.Dispose();Cursor.Show();CursorHidden=false;};
    SyncMusic();
   }
  void ClearInput(){game.Input.Left=game.Input.Right=game.Input.Fire=game.Input.Down=false;game.Input.ClearEdges();}
  public void SyncMusic(){
   if(game.State==Stage.Playing)audio.StartAmbient();
   else if(game.State==Stage.Sunrise||game.State==Stage.Won)audio.StartDawn();
   else audio.StartMenu();
  }
  void TickGame(object sender,EventArgs e){
  double now=watch.Elapsed.TotalSeconds;accumulator+=Math.Min(.12,now-previous);previous=now;
   while(accumulator>=1.0/120){game.Step(1f/120);accumulator-=1.0/120;}
   SyncMusic();
   Invalidate();
 }
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);renderer.Draw(e.Graphics,game,ClientSize.Width,ClientSize.Height,(float)watch.Elapsed.TotalSeconds);}
  void MoveMouse(object sender,MouseEventArgs e){
   float s=Math.Min(ClientSize.Width/Game.Width,ClientSize.Height/Game.Height);
   game.Input.Aim=new V((e.X-(ClientSize.Width-Game.Width*s)/2)/s,(e.Y-(ClientSize.Height-Game.Height*s)/2)/s);
   if(game.State==Stage.Audio&&game.AudioDragging){SetSliderFromAim();return;}
   SyncMenuHover();
  }
  void SyncMenuHover(){
   RectangleF[] items=MenuLayout.Items(game.State,game.Ending);
   for(int i=0;i<items.Length;i++)if(items[i].Contains(game.Input.Aim.X,game.Input.Aim.Y)){game.Menu=i;return;}
  }
  void MenuMove(int delta){game.MoveMenu(delta,MenuLayout.Items(game.State,game.Ending).Length);}
  void SetSliderFromAim(){
   if(game.Menu<0||game.Menu>2)return;
   RectangleF row=MenuLayout.Audio[game.Menu];
   game.SetVolume(game.Menu,(game.Input.Aim.X-(row.X+190))/(row.Width-300));
  }
    void ToggleSound(){
     game.ToggleSound();audio.Muted=!game.SoundOn;
     SyncMusic();
     ClearInput();
    }
   void ReturnToTitle(){game.ReturnToTitle();ClearInput();SyncMusic();}
   void StartDifficulty(int index){ClearInput();game.Start(Environment.TickCount,index);SyncMusic();}
   void ActivateMenu(int index){
    Stage state=game.State;
    if(state==Stage.Title){
     if(index==0)game.ShowDifficulty();
     else if(index==1)game.ShowHelp();
     else if(index==2)game.ShowAudio();
     else if(index==3)game.ShowAbilities();
     else if(index==4)Application.Exit();
     ClearInput();
    } else if(state==Stage.Help){game.CloseHelp();ClearInput();}
    else if(state==Stage.Audio){if(index>=3){game.CloseAudio();audio.ApplyVolumes(game.Volume);ClearInput();}}
    else if(state==Stage.Abilities){game.CloseAbilities();ClearInput();}
    else if(state==Stage.Difficulty){
     if(index>=0&&index<Campaign.Difficulties.Length)StartDifficulty(index);
     else if(index==Campaign.Difficulties.Length){game.CloseDifficulty();ClearInput();}
    }
    else if(state==Stage.Paused){
     if(index==0)game.Pause();
     else if(index==1)game.ShowHelp();
     else if(index==2){game.ShowAudio();ClearInput();}
     else if(index==3)ReturnToTitle();
    } else if(state==Stage.Upgrade){game.SelectUpgrade(index);ClearInput();}
     else if(state==Stage.Dead||state==Stage.Won){
      if(index==0)NewRun();
      else if(index==1)ReturnToTitle();
     }
     SyncMusic();
    }
  void MousePressed(object sender,MouseEventArgs e){
   MoveMouse(sender,e);
   if(e.Button==MouseButtons.Right){game.Input.Dash=true;return;}
   if(e.Button!=MouseButtons.Left)return;
   Stage state=game.State;
   if(state==Stage.Audio){
    int slider=MenuLayout.Hit(Stage.Audio,game.Input.Aim);
    if(slider>=0&&slider<3){game.Menu=slider;game.AudioDragging=true;SetSliderFromAim();}
    else if(slider==3)ActivateMenu(3);
    return;
   }
    if(state==Stage.Sunrise){game.SkipSunrise();ClearInput();SyncMusic();return;}
    if(state==Stage.Story){game.ContinueStory();ClearInput();SyncMusic();return;}
    if(state==Stage.Playing){game.Input.Fire=true;return;}
   int hit=MenuLayout.Hit(state,game.Input.Aim,game.Ending);
   if(hit>=0)ActivateMenu(hit);
  }
  void MouseReleased(object sender,MouseEventArgs e){
    if(game.AudioDragging){game.AudioDragging=false;audio.ApplyVolumes(game.Volume);SyncMusic();ClearInput();return;}
    if(e.Button==MouseButtons.Left)game.Input.Fire=false;
   }
   void NewRun(){ClearInput();game.Start(Environment.TickCount);SyncMusic();}
  void Down(object sender,KeyEventArgs e){
   e.SuppressKeyPress=true;Keys k=e.KeyCode;
   if(k==Keys.F11){ToggleFullscreen();return;}
   if(k==Keys.M){ToggleSound();return;}
    if(k==Keys.F8){renderer.ReduceMotion=!renderer.ReduceMotion;return;}
    if(game.State==Stage.Sunrise&&(k==Keys.Enter||k==Keys.Escape)){game.SkipSunrise();ClearInput();SyncMusic();return;}
    Stage state=game.State;
    bool menu=state==Stage.Title||state==Stage.Help||state==Stage.Audio||state==Stage.Abilities||state==Stage.Difficulty||state==Stage.Upgrade||state==Stage.Paused||state==Stage.Dead||state==Stage.Won;
    if(menu){
     if(k==Keys.W||k==Keys.Up){MenuMove(-1);return;}
     if(k==Keys.S||k==Keys.Down){MenuMove(1);return;}
      if(state==Stage.Upgrade&&k>=Keys.D1&&k<=Keys.D3){game.SelectUpgrade(k-Keys.D1);ClearInput();SyncMusic();return;}
      if(state==Stage.Difficulty&&k>=Keys.D1&&k<=Keys.D4){StartDifficulty(k-Keys.D1);return;}
      if(state==Stage.Audio&&(k==Keys.A||k==Keys.Left||k==Keys.D||k==Keys.Right)){
       game.AdjustVolume(game.Menu,(k==Keys.D||k==Keys.Right) ? .05f : -.05f);
       audio.ApplyVolumes(game.Volume);SyncMusic();return;
      }
     if(k==Keys.Enter){ActivateMenu(game.Menu);return;}
     if(k==Keys.Escape){
      if(state==Stage.Help)game.CloseHelp();
      else if(state==Stage.Audio){game.CloseAudio();audio.ApplyVolumes(game.Volume);}
      else if(state==Stage.Abilities)game.CloseAbilities();
      else if(state==Stage.Difficulty)game.CloseDifficulty();
       else if(state==Stage.Paused)game.Pause();
       ClearInput();SyncMusic();return;
     }
    return;
   }
    if(k==Keys.Enter&&state==Stage.Story){game.ContinueStory();ClearInput();SyncMusic();return;}
    if(k==Keys.J&&state==Stage.Playing){game.State=Stage.Story;game.Menu=0;ClearInput();SyncMusic();return;}
    if(k==Keys.Escape){game.Pause();ClearInput();SyncMusic();return;}
   if(k==Keys.A||k==Keys.Left)game.Input.Left=true;
   if(k==Keys.D||k==Keys.Right)game.Input.Right=true;
   if(k==Keys.S||k==Keys.Down)game.Input.Down=true;
   if(k==Keys.Space||k==Keys.W||k==Keys.Up)game.Input.Jump=true;
   if(k==Keys.ShiftKey)game.Input.Dash=true;
   if(k==Keys.Q)game.Input.Flask=true;
   if(k==Keys.E)game.Input.Use=true;
 }
 void Up(object sender,KeyEventArgs e){
  Keys k=e.KeyCode;
  if(k==Keys.A||k==Keys.Left)game.Input.Left=false;
  if(k==Keys.D||k==Keys.Right)game.Input.Right=false;
  if(k==Keys.S||k==Keys.Down)game.Input.Down=false;
 }
 void ToggleFullscreen(){
  if(!fullscreen){oldBounds=Bounds;oldState=WindowState;WindowState=FormWindowState.Normal;FormBorderStyle=FormBorderStyle.None;Bounds=Screen.FromControl(this).Bounds;}
  else{FormBorderStyle=FormBorderStyle.Sizable;Bounds=oldBounds;WindowState=oldState;}
  fullscreen=!fullscreen;
 }
}
public sealed class Audio : IDisposable {
 public const int RoomTracks=3,MenuTrack=3,DawnTrack=4,TrackCount=5;
 readonly PcmMixer mixer;
 public Audio(){mixer=new PcmMixer();mixer.SetGains(1,.8f,.65f);}
  public bool Available{get{return mixer.Available;}}
  public bool MixerAvailable{get{return mixer.Available;}}
  public bool IsRoom(int n){return n>=0&&n<RoomTracks&&mixer.Room==n;}
  public bool MusicPlaying{get{return mixer.MusicPlaying;}}
 public bool AmbientPlaying{get{return mixer.MusicPlaying&&mixer.ActiveTrack>=0&&mixer.ActiveTrack<RoomTracks;}}
 public bool MenuPlaying{get{return mixer.MusicPlaying&&mixer.ActiveTrack==MenuTrack;}}
 public bool DawnPlaying{get{return mixer.MusicPlaying&&mixer.ActiveTrack==DawnTrack;}}
 public bool Muted{get{return mixer.Muted;}set{mixer.SetMuted(value);}}
 public int ActiveTrack{get{return mixer.ActiveTrack;}}
 public int Theme{get{return mixer.Room;}}
 public int ActiveVoices{get{return mixer.ActiveVoices;}}
 public bool CrossfadeActive{get{return mixer.CrossfadeActive;}}
 public float FadeProgress{get{return mixer.FadeProgress;}}
 public int Position{get{return mixer.Position;}}
 public float MasterGain{get{return mixer.MasterGain;}}
 public float MusicGain{get{return mixer.MusicGain;}}
 public float SfxGain{get{return mixer.SfxGain;}}
 public void ApplyVolumes(float[] volume){
  if(volume==null||volume.Length<3)return;
  mixer.SetGains(volume[0],volume[1],volume[2]);
 }
 public void SetRoom(int index){mixer.SetRoom(index);}
 public void Play(int n){mixer.Play(n);}
 public void StartTheme(int index){if(index<0||index>=TrackCount)return;mixer.SetMusic(index);}
 public void StopMusic(){mixer.SetMusic(-1);}
 public void StartAmbient(){StartTheme(mixer.Room);}
 public void StartMenu(){StartTheme(MenuTrack);}
 public void StartDawn(){StartTheme(DawnTrack);}
 public void StopAmbient(){StopMusic();}
 public void Dispose(){mixer.Dispose();}
}
static class Program {
 [STAThread]
 static int Main(string[] args){
  try{
   if(args.Length>0&&args[0]=="--self-test")return Tests.Run();
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   Application.Run(new GameWindow());return 0;
  }catch(Exception e){
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"crash.log"),e.ToString());
   if(args.Length==0)MessageBox.Show(e.Message,"Ashenbound");return 1;
  }
 }
}
}
