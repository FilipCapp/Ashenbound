using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;
namespace Ashenbound {
public class GameWindow : Form {
 readonly Game game=new Game();readonly Renderer renderer=new Renderer();readonly Timer timer=new Timer();
 readonly Stopwatch watch=Stopwatch.StartNew();readonly Audio audio=new Audio();
  double previous,accumulator;bool fullscreen,runActive;public bool CursorHidden;Rectangle oldBounds;FormWindowState oldState;
  public GameWindow():this(true){}
  public GameWindow(bool autoFullscreen){
   Text="Ashenbound | The Last Cinder";ClientSize=new Size(1280,720);MinimumSize=new Size(800,480);
   StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;KeyPreview=true;BackColor=Color.Black;
   Shown+=delegate{Cursor.Hide();CursorHidden=true;if(autoFullscreen)ToggleFullscreen();};
   game.PlaySound=audio.Play;game.RoomEntered=audio.SetRoom;timer.Interval=8;timer.Tick+=TickGame;timer.Start();
   KeyDown+=Down;KeyUp+=Up;MouseMove+=MoveMouse;MouseDown+=MousePressed;MouseUp+=MouseReleased;
   Deactivate+=delegate{if(game.State==Stage.Playing)game.Pause();ClearInput();};
   FormClosed+=delegate{audio.StopAmbient();timer.Stop();timer.Dispose();renderer.Dispose();audio.Dispose();Cursor.Show();CursorHidden=false;};
  }
 void ClearInput(){game.Input.Left=game.Input.Right=game.Input.Fire=game.Input.Down=false;game.Input.ClearEdges();}
 void TickGame(object sender,EventArgs e){
  double now=watch.Elapsed.TotalSeconds;accumulator+=Math.Min(.12,now-previous);previous=now;
  while(accumulator>=1.0/120){game.Step(1f/120);accumulator-=1.0/120;}
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
   if(audio.Muted)audio.StopAmbient();
   else if(runActive)audio.StartAmbient();
   ClearInput();
  }
  void ReturnToTitle(){game.ReturnToTitle();runActive=false;audio.StopAmbient();ClearInput();}
  void ActivateMenu(int index){
   Stage state=game.State;
   if(state==Stage.Title){
    if(index==0)NewRun();
    else if(index==1)game.ShowHelp();
    else if(index==2){game.ShowAudio();ClearInput();}
    else if(index==3)Application.Exit();
   } else if(state==Stage.Help){game.CloseHelp();ClearInput();}
   else if(state==Stage.Audio){if(index>=3){game.CloseAudio();audio.ApplyVolumes(game.Volume);ClearInput();}}
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
   if(state==Stage.Story){game.ContinueStory();ClearInput();return;}
   if(state==Stage.Playing){game.Input.Fire=true;return;}
   int hit=MenuLayout.Hit(state,game.Input.Aim,game.Ending);
   if(hit>=0)ActivateMenu(hit);
  }
  void MouseReleased(object sender,MouseEventArgs e){
   if(game.AudioDragging){game.AudioDragging=false;audio.ApplyVolumes(game.Volume);ClearInput();return;}
   if(e.Button==MouseButtons.Left)game.Input.Fire=false;
  }
  void NewRun(){ClearInput();game.Start(Environment.TickCount);runActive=true;audio.StartAmbient();}
  void Down(object sender,KeyEventArgs e){
   e.SuppressKeyPress=true;Keys k=e.KeyCode;
   if(k==Keys.F11){ToggleFullscreen();return;}
   if(k==Keys.M){ToggleSound();return;}
   if(k==Keys.F8){renderer.ReduceMotion=!renderer.ReduceMotion;return;}
   Stage state=game.State;
   bool menu=state==Stage.Title||state==Stage.Help||state==Stage.Audio||state==Stage.Upgrade||state==Stage.Paused||state==Stage.Dead||state==Stage.Won;
   if(menu){
    if(k==Keys.W||k==Keys.Up){MenuMove(-1);return;}
    if(k==Keys.S||k==Keys.Down){MenuMove(1);return;}
    if(state==Stage.Upgrade&&k>=Keys.D1&&k<=Keys.D3){game.SelectUpgrade(k-Keys.D1);ClearInput();return;}
    if(state==Stage.Audio&&(k==Keys.A||k==Keys.Left||k==Keys.D||k==Keys.Right)){
     game.AdjustVolume(game.Menu,(k==Keys.D||k==Keys.Right) ? .05f : -.05f);
     audio.ApplyVolumes(game.Volume);return;
    }
    if(k==Keys.Enter){ActivateMenu(game.Menu);return;}
    if(k==Keys.Escape){
     if(state==Stage.Help)game.CloseHelp();
     else if(state==Stage.Audio){game.CloseAudio();audio.ApplyVolumes(game.Volume);}
     else if(state==Stage.Paused)game.Pause();
     ClearInput();return;
    }
    return;
   }
   if(k==Keys.Enter&&state==Stage.Story){game.ContinueStory();ClearInput();return;}
   if(k==Keys.J&&state==Stage.Playing){game.State=Stage.Story;game.Menu=0;ClearInput();return;}
   if(k==Keys.Escape){game.Pause();ClearInput();return;}
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
 const int Rate=22050;
 readonly SoundPlayer[] effects=new SoundPlayer[6];readonly MemoryStream[] effectStreams=new MemoryStream[6];
 readonly SoundPlayer[] ambiences=new SoundPlayer[3];readonly MemoryStream[] ambientStreams=new MemoryStream[3];
  public bool Muted;bool ambient;int room,activeRoom=-1;
  float master=1,music=.8f,sfx=1;
  public Audio(){Rebuild();}
  static double Noise(int i){double x=Math.Sin(i*12.9898)*43758.5453;return (x-Math.Floor(x))*2-1;}
  static double Loop(double t,double length){double fade=.22;return Math.Max(0,Math.Min(1,Math.Min(t/fade,(length-t)/fade)));}
  static double Bell(double t,double f){return Math.Exp(-t*4.2)*(Math.Sin(2*Math.PI*f*t)+.42*Math.Sin(2*Math.PI*f*2.01*t)+.18*Math.Sin(2*Math.PI*f*3.02*t));}
  static double Drum(double t,double f,double decay){return Math.Sin(2*Math.PI*(f-35*Math.Min(1,t*4))*t)*Math.Exp(-t*decay);}
  static double Chord(double t,double a,double b,double c){return (Math.Sin(2*Math.PI*a*t)+Math.Sin(2*Math.PI*b*t)+Math.Sin(2*Math.PI*c*t)+.3*Math.Sin(2*Math.PI*a*2*t))*.25;}
  static double Ambient(int r,int i,double gain){
   double t=(double)i/Rate,v;
   if(r==0){
    v=.075*Math.Sin(2*Math.PI*55*t)+.05*Math.Sin(2*Math.PI*82.5*t)+.03*Math.Sin(2*Math.PI*110*t);
    v+=.06*Bell(t%2,293.66)+.04*Bell((t+1)%2,440);
    v+=.025*Math.Sin(2*Math.PI*.5*t)*Math.Sin(2*Math.PI*220*t);
    v+=.012*Noise(i)*Math.Exp(-(t%1)*3);
   } else if(r==1){
    v=.07*Math.Sin(2*Math.PI*73.42*t)+.045*Math.Sin(2*Math.PI*110*t+.5*Math.Sin(2*Math.PI*.5*t));
    double beat=t%1.6;v+=.09*Drum(beat,96,6)*(beat<1?1:0);
    double bell=t%3.2;v+=.045*Bell(bell,196)+.03*Bell((bell+1.6)%3.2,261.63);
    v+=.018*Noise(i)*(.5+.5*Math.Sin(2*Math.PI*3.1*t));
   } else {
    double chord=t%4,toll=t%5.3;
    v=.085*Math.Sin(2*Math.PI*49*t)+.05*Math.Sin(2*Math.PI*73.5*t)+.035*Math.Sin(2*Math.PI*98*t);
    v+=.06*Chord(chord,110,130.81,164.81)+.04*Chord((chord+2)%4,98,123.47,146.83);
    v+=.05*Bell(toll,146.83)+.03*Bell((toll+2.65)%5.3,155.56);
    v+=.01*Noise(i);
   }
   return v*Loop(t,8)*gain;
  }
  static int EffectLength(int n){
   if(n==0||n==4)return Rate*3/10;
   if(n==1||n==5)return Rate/2;
   if(n==2)return Rate*3/2;
   return Rate*3/5;
  }
  static double Effect(int n,int r,int i,double gain){
   double t=(double)i/Rate,f=r==0?1:r==1?1.12:.88,v;
   if(n==0){
    v=.28*Noise(i)*Math.Exp(-t*8)+.22*Math.Sin(2*Math.PI*620*f*(1+.35*t)*t)*Math.Exp(-t*10);
    v+=.18*Noise(i*7+11)*Math.Exp(-t*18)*(.5+.5*Math.Sin(2*Math.PI*23*t));
   } else if(n==1){
    v=.5*Drum(t,130*f,7)+.24*Noise(i)*Math.Exp(-t*20);
    v+=.18*(Math.Sin(2*Math.PI*310*f*t)+.5*Math.Sin(2*Math.PI*465*f*t))*Math.Exp(-t*9);
   } else if(n==2){
    v=.1*Math.Sin(2*Math.PI*146.83*f*t)*Math.Exp(-t*1.6);
    double[] notes={293.66*f,349.23*f,440*f,587.33*f};
    for(int k=0;k<notes.Length;k++){
     double dt=t-k*.18;if(dt<0)continue;
     v+=.3*Math.Exp(-dt*3.6)*(Math.Sin(2*Math.PI*notes[k]*dt)+.35*Math.Sin(2*Math.PI*notes[k]*2.01*dt));
    }
   } else if(n==3){
    v=.34*Math.Sin(2*Math.PI*(92*f-28*t)*t)*Math.Exp(-t*5);
    v+=.22*Math.Sin(2*Math.PI*138.6*f*t)*Math.Exp(-t*7)+.16*Noise(i)*Math.Exp(-t*14);
   } else if(n==4){
    v=.3*Noise(i)*Math.Exp(-t*11)+.2*Math.Sin(2*Math.PI*(180+620*t)*f*t)*Math.Exp(-t*12);
   } else {
    v=.28*Bell(t,880*f)+.18*Math.Sin(2*Math.PI*1320*f*t)*Math.Exp(-t*12)+.14*Noise(i)*Math.Exp(-t*18);
   }
   return v*gain;
  }
  static byte[] Wave(int count,Func<int,double> sample){
   MemoryStream ms=new MemoryStream();BinaryWriter w=new BinaryWriter(ms);
   w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);
   w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);
   w.Write(Rate);w.Write(Rate*2);w.Write((short)2);w.Write((short)16);
   w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
   for(int i=0;i<count;i++){
    double v=sample(i);if(v>1)v=1;if(v<-1)v=-1;
    w.Write((short)(v*26000));
   }
   w.Flush();return ms.ToArray();
  }
  void Release(){
   for(int i=0;i<effects.Length;i++){if(effects[i]!=null){effects[i].Dispose();effects[i]=null;}if(effectStreams[i]!=null){effectStreams[i].Dispose();effectStreams[i]=null;}}
   for(int i=0;i<ambiences.Length;i++){if(ambiences[i]!=null){ambiences[i].Dispose();ambiences[i]=null;}if(ambientStreams[i]!=null){ambientStreams[i].Dispose();ambientStreams[i]=null;}}
  }
  void Rebuild(){
   bool was=ambient;StopAmbient();Release();
   try{
    for(int r=0;r<ambiences.Length;r++){
     double gain=Muted?0:master*music;
     MemoryStream ms=new MemoryStream(Wave(Rate*8,delegate(int i){return Ambient(r,i,gain);}));
     ambientStreams[r]=ms;ambiences[r]=new SoundPlayer(ms);ambiences[r].Load();
    }
    for(int n=0;n<effects.Length;n++){
     double gain=Muted?0:master*sfx;
     MemoryStream ms=new MemoryStream(Wave(EffectLength(n),delegate(int i){return Effect(n,room,i,gain);}));
     effectStreams[n]=ms;effects[n]=new SoundPlayer(ms);effects[n].Load();
    }
   }catch{Muted=true;}
   if(was&&!Muted)StartAmbient();
  }
  public bool AmbientPlaying{get{return ambient;}}
  public int Theme{get{return room;}}
  public float MasterGain{get{return Muted?0:master;}}
  public float MusicGain{get{return Muted?0:master*music;}}
  public float SfxGain{get{return Muted?0:master*sfx;}}
  public void ApplyVolumes(float[] volume){
   if(volume==null||volume.Length<3)return;
   float m=Game.Clamp(volume[0],0,1),mus=Game.Clamp(volume[1],0,1),eff=Game.Clamp(volume[2],0,1);
   if(m==master&&mus==music&&eff==sfx)return;
   master=m;music=mus;sfx=eff;Rebuild();
  }
  public void SetRoom(int index){
   index=(int)Game.Clamp(index,0,2);
   if(index==room)return;
   bool was=ambient;if(was)StopAmbient();room=index;Rebuild();if(was&&!Muted)StartAmbient();
  }
  public void Play(int n){if(Muted||n<0||n>=effects.Length||effects[n]==null)return;try{effects[n].Play();}catch{}}
  public void StartAmbient(){if(Muted||ambient||ambiences[room]==null)return;try{ambiences[room].PlayLooping();ambient=true;activeRoom=room;}catch{}}
  public void StopAmbient(){if(!ambient)return;if(activeRoom>=0&&activeRoom<ambiences.Length&&ambiences[activeRoom]!=null){try{ambiences[activeRoom].Stop();}catch{}}ambient=false;activeRoom=-1;}
  public void Dispose(){StopAmbient();Release();}
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
