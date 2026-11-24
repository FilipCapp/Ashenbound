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
   game.PlaySound=audio.Play;timer.Interval=8;timer.Tick+=TickGame;timer.Start();
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
   SyncMenuHover();
  }
  void SyncMenuHover(){
   RectangleF[] items=MenuLayout.Items(game.State);
   for(int i=0;i<items.Length;i++)if(items[i].Contains(game.Input.Aim.X,game.Input.Aim.Y)){game.Menu=i;return;}
  }
  void MenuMove(int delta){game.MoveMenu(delta,MenuLayout.Items(game.State).Length);}
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
    else if(index==2)ToggleSound();
    else if(index==3)Application.Exit();
   } else if(state==Stage.Help){game.CloseHelp();ClearInput();}
   else if(state==Stage.Paused){
    if(index==0)game.Pause();
    else if(index==1)game.ShowHelp();
    else if(index==2)ToggleSound();
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
   if(state==Stage.Story){game.ContinueStory();ClearInput();return;}
   if(state==Stage.Playing){game.Input.Fire=true;return;}
   int hit=MenuLayout.Hit(state,game.Input.Aim);
   if(hit>=0)ActivateMenu(hit);
  }
  void MouseReleased(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)game.Input.Fire=false;}
  void NewRun(){ClearInput();game.Start(Environment.TickCount);runActive=true;audio.StartAmbient();}
  void Down(object sender,KeyEventArgs e){
   e.SuppressKeyPress=true;Keys k=e.KeyCode;
   if(k==Keys.F11){ToggleFullscreen();return;}
   if(k==Keys.M){ToggleSound();return;}
   if(k==Keys.F8){renderer.ReduceMotion=!renderer.ReduceMotion;return;}
   Stage state=game.State;
   bool menu=state==Stage.Title||state==Stage.Help||state==Stage.Upgrade||state==Stage.Paused||state==Stage.Dead||state==Stage.Won;
   if(menu){
    if(k==Keys.W||k==Keys.Up){MenuMove(-1);return;}
    if(k==Keys.S||k==Keys.Down){MenuMove(1);return;}
    if(state==Stage.Upgrade&&k>=Keys.D1&&k<=Keys.D3){game.SelectUpgrade(k-Keys.D1);ClearInput();return;}
    if(k==Keys.Enter){ActivateMenu(game.Menu);return;}
    if(k==Keys.Escape){
     if(state==Stage.Help)game.CloseHelp();
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
 static readonly double[] Chime={293.66,349.23,440,587.33};
 readonly SoundPlayer[] sounds=new SoundPlayer[5];readonly MemoryStream[] streams=new MemoryStream[5];
 public bool Muted;bool ambient;
 public Audio(){
  try{
   for(int n=0;n<5;n++){
    byte[] data=n==4?Wave(Rate*8,AmbientSample):Wave(n==0?Rate/4:n==1?Rate/2:n==2?Rate*3/2:Rate,Effect(n));
    MemoryStream ms=new MemoryStream(data);streams[n]=ms;sounds[n]=new SoundPlayer(ms);sounds[n].Load();
   }
  }catch{Muted=true;}
 }
 static double Noise(int i){double x=Math.Sin(i*12.9898)*43758.5453;return (x-Math.Floor(x))*2-1;}
 static Func<int,double> Effect(int n){
  return delegate(int i){
   double t=(double)i/Rate;
   if(n==0){
    double pluck=Math.Sin(2*Math.PI*660*t)+.45*Math.Sin(2*Math.PI*990*t)+.2*Math.Sin(2*Math.PI*1320*t);
    double bell=.22*Math.Sin(2*Math.PI*1760*t)*Math.Exp(-t*14);
    return (pluck*.5+bell+Noise(i)*.05*Math.Exp(-t*30))*Math.Exp(-t*9);
   }
   if(n==1){
    double drum=Math.Sin(2*Math.PI*(130-70*Math.Min(1,t*3))*t);
    double metal=Math.Sin(2*Math.PI*310*t)+.5*Math.Sin(2*Math.PI*465*t);
    return drum*.55*Math.Exp(-t*7)+Noise(i)*.22*Math.Exp(-t*22)+metal*.16*Math.Exp(-t*9);
   }
   if(n==2){
    double v=.1*Math.Sin(2*Math.PI*146.83*t)*Math.Exp(-t*1.6);
    for(int k=0;k<Chime.Length;k++){
     double dt=t-k*.18;if(dt<0)continue;
     v+=.3*Math.Exp(-dt*3.6)*(Math.Sin(2*Math.PI*Chime[k]*dt)+.35*Math.Sin(2*Math.PI*Chime[k]*2.01*dt));
    }
    return v;
   }
   double drone=Math.Sin(2*Math.PI*110*t)+.7*Math.Sin(2*Math.PI*164.81*t)+.4*Math.Sin(2*Math.PI*220*t+.3*Math.Sin(2*Math.PI*5*t));
   return (.42*drone+.24*Math.Sin(2*Math.PI*(180-90*t)*t))*Math.Exp(-t*2.6);
  };
 }
 static double AmbientSample(int i){
  double t=(double)i/Rate;
  double v=.09*Math.Sin(2*Math.PI*55*t)+.06*Math.Sin(2*Math.PI*82.5*t)+.045*Math.Sin(2*Math.PI*110*t);
  v+=.035*Math.Sin(2*Math.PI*165*t+.4*Math.Sin(2*Math.PI*.25*t));
  v+=.025*Math.Sin(2*Math.PI*.25*t)*Math.Sin(2*Math.PI*330*t);
  double fade=Math.Min(1,Math.Min(t/.25,(8-t)/.25));
  return v*fade;
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
 public bool AmbientPlaying{get{return ambient;}}
 public void Play(int n){if(Muted||n<0||n>3||sounds[n]==null)return;try{sounds[n].Play();}catch{Muted=true;}}
 public void StartAmbient(){if(Muted||ambient||sounds[4]==null)return;try{sounds[4].PlayLooping();ambient=true;}catch{Muted=true;}}
 public void StopAmbient(){if(!ambient)return;try{sounds[4].Stop();}catch{}ambient=false;}
 public void Dispose(){StopAmbient();for(int i=0;i<5;i++){if(sounds[i]!=null)sounds[i].Dispose();if(streams[i]!=null)streams[i].Dispose();}}
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
