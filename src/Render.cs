using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
namespace Ashenbound {
public sealed class Renderer : IDisposable {
 Graphics g;Game game;float clock;
 public bool ReduceMotion;
 readonly FontFamily family=new FontFamily("Consolas");
 readonly PixelArt pixels=new PixelArt();
 readonly Bitmap canvas=new Bitmap(320,180);
 public void Dispose(){family.Dispose();pixels.Dispose();canvas.Dispose();}
 void Box(float x,float y,float w,float h,Color c){if(w<=0||h<=0)return;using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
 void Line(float x,float y,float x2,float y2,Color c,float width){using(var p=new Pen(c,width))g.DrawLine(p,x,y,x2,y2);}
 void Text(string s,float x,float y,float size,Color c){using(var f=new Font(family,size,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(c))g.DrawString(s,f,b,x,y,StringFormat.GenericTypographic);}
 void Bold(string s,float x,float y,float size,Color c){using(var f=new Font(family,size,FontStyle.Bold,GraphicsUnit.Pixel))using(var b=new SolidBrush(c))g.DrawString(s,f,b,x,y,StringFormat.GenericTypographic);}
 void Circle(float x,float y,float r,Color c){using(var b=new SolidBrush(c))g.FillEllipse(b,x-r,y-r,r*2,r*2);}
 public void Draw(Graphics graphics,Game model,int width,int height,float seconds){
  game=model;clock=seconds;graphics.Clear(Color.Black);
  using(Graphics low=Graphics.FromImage(canvas)){
   g=low;g.Clear(Palette.Ink);g.ScaleTransform(.25f,.25f);g.SmoothingMode=SmoothingMode.None;
   pixels.Background(g,game.Room,clock,ReduceMotion);
   if(game.State!=Stage.Title){
    var saved=g.Save();
    if(!ReduceMotion)g.TranslateTransform((int)(Math.Sin(clock*110)*game.Shake/4)*4,(int)(Math.Cos(clock*130)*game.Shake/8)*4);
    World();g.Restore(saved);
   }
  }
  g=graphics;
  float scale=Math.Min(width/Game.Width,height/Game.Height);
  g.TranslateTransform((width-Game.Width*scale)/2,(height-Game.Height*scale)/2);
  g.ScaleTransform(scale,scale);
  g.SetClip(new Rectangle(0,0,(int)Game.Width,(int)Game.Height));
  g.InterpolationMode=InterpolationMode.NearestNeighbor;
  g.PixelOffsetMode=PixelOffsetMode.Half;
  g.DrawImage(canvas,new Rectangle(0,0,(int)Game.Width,(int)Game.Height));
  g.TextRenderingHint=TextRenderingHint.SingleBitPerPixelGridFit;
  g.SmoothingMode=SmoothingMode.None;
  if(game.State==Stage.Title){Title();return;}
  Hud();
  if(game.State==Stage.Story)StoryPage();
  if(game.State==Stage.Upgrade)Upgrades();
  if(game.State==Stage.Paused)Pause();
  if(game.State==Stage.Won||game.State==Stage.Dead)End();
 }
 void World(){
  foreach(RectangleF r in game.Platforms){
   Box(r.X,r.Y,r.Width,r.Height,Color.FromArgb(30,29,42));
   Box(r.X,r.Y,r.Width,3,Campaign.Accent(game.Room));
   for(float x=r.X+12;x<r.Right-14;x+=34)Box(x,r.Y+9,17,2,Color.FromArgb(56,53,66));
   if(r.Y<Game.Floor-20){Box(r.X+12,r.Y+18,8,24,Color.FromArgb(26,25,37));Box(r.Right-20,r.Y+18,8,24,Color.FromArgb(26,25,37));}
  }
  foreach(Hazard h in game.Hazards){
   if(h.Kind==0){
    for(float x=h.Bounds.Left;x<h.Bounds.Right-4;x+=12){
     Line(x,h.Bounds.Bottom,x+6,h.Bounds.Top,Palette.Blood,3);
     Line(x+6,h.Bounds.Top,x+12,h.Bounds.Bottom,Palette.Blood,3);
    }
   } else {
    Color c=h.Active?Palette.Ember:Color.FromArgb(70,55,60);
    Box(h.Bounds.X,h.Bounds.Y,h.Bounds.Width,h.Bounds.Height,Color.FromArgb(h.Active?70:24,c));
    Box(h.Bounds.X+h.Bounds.Width/2-1,h.Bounds.Y,2,h.Bounds.Height,c);
   }
  }
  if(game.ExitOpen){
   float a=.5f+(float)Math.Sin(clock*3)*.15f;
   Box(1177,493,70,129,Color.FromArgb((int)(a*100),112,214,205));
   Box(1177,493,4,129,Palette.Cyan);Box(1243,493,4,129,Palette.Cyan);Box(1177,493,70,4,Palette.Cyan);
   Bold("E",1198,534,32,Color.White);
  }
  foreach(Enemy e in game.Enemies)EnemyDraw(e);
  foreach(Bullet b in game.Bullets){
   if(b.Kind==1){Circle(b.P.X,b.P.Y,8,Palette.Ember);Circle(b.P.X,b.P.Y,4,Palette.Gold);continue;}
   Color c=b.Hostile?Palette.Blood:Palette.Gold;
   if(b.Hostile){Circle(b.P.X,b.P.Y,9,Color.FromArgb(48,c));Circle(b.P.X,b.P.Y,5,c);Circle(b.P.X,b.P.Y,2,Color.White);}
   else{V trail=b.P-b.Velocity.Unit*17;Line(trail.X,trail.Y,b.P.X,b.P.Y,c,4);Circle(b.P.X,b.P.Y,3,Color.White);}
  }
  foreach(Particle p in game.Particles)Box(p.P.X,p.P.Y,4,4,Color.FromArgb((int)(255*Game.Clamp(p.Life/p.Max,0,1)),p.Color));
  if(game.Invulnerable<=0||((int)(clock*24))%2==0)Actor(game.Player,game.Aim,1,0);
  if(game.State==Stage.Playing){
   V a=game.Input.Aim;
   Line(a.X-9,a.Y,a.X-4,a.Y,Palette.Cyan,1);Line(a.X+4,a.Y,a.X+9,a.Y,Palette.Cyan,1);
   Line(a.X,a.Y-9,a.X,a.Y-4,Palette.Cyan,1);Line(a.X,a.Y+4,a.X,a.Y+9,Palette.Cyan,1);
  }
 }
 void Actor(V p,V aim,float size,int type){
  int frame=game.State==Stage.Playing&&Math.Abs(game.Velocity.X)>10?(int)(clock*9)%2:0;
  pixels.Sprite(g,type,p,size,aim.X<0,frame);
  float step=4*size;
  V hand=p+new V(aim.X*28*size,-12*size+aim.Y*22*size);
  Box((int)(hand.X/step)*step,(int)(hand.Y/step)*step,16*size,4*size,Palette.Gold);
 }
 void EnemyDraw(Enemy e){
  int type=e.Kind==3?4:e.Kind+1;
  float size=e.Kind==3?2.5f:1;
  pixels.Sprite(g,type,e.P,size,game.Player.X<e.P.X,(int)(clock*7)%2);
  Color c=e.Kind==3?Campaign.Accent(game.Room):Palette.Blood;
  if(e.Hit>0)Box(e.P.X-8,e.P.Y-12,16,8,Color.White);
  if(e.Kind!=2){Box(e.P.X-22,e.P.Y-44,44,4,Color.FromArgb(66,36,48));Box(e.P.X-22,e.P.Y-44,44*Math.Max(0,e.Hp/e.MaxHp),4,c);}
 }
 void Title(){
  Box(0,0,Game.Width,Game.Height,Color.FromArgb(70,6,6,12));
  Box(66,70,5,514,Palette.Ember);
  Text("A N   O R I G I N A L   D A R K   F A N T A S Y   R U N - A N D - G U N",94,78,16,Palette.Ember);
  Bold("ASHENBOUND",88,143,88,Color.White);
  Bold("THE LAST CINDER",88,228,64,Palette.Ember);
  Text("THE SUN HAS BEEN DEAD FOR THREE WINTERS.",96,346,22,Palette.Gold);
  Text("Carry the Last Cinder through three ruined sanctums.\nBreak the Cinder Bishop's choir. Give the world one more dawn.",98,398,21,Color.FromArgb(200,204,208));
  Button("BEGIN THE VIGIL","ENTER",new RectangleF(98,524,405,62));
  Text("Move fast. Chain kills. Make every second count.",98,606,17,Palette.Muted);
  Actor(new V(1010,457),new V(1,0),3,0);
  Box(848,551,320,3,Palette.Ember);Text("VEYRA / EMBER BEARER",859,575,16,Palette.Muted);
  Footer();Text("M SOUND  /  F11 FULLSCREEN",961,678,14,Palette.Muted);
 }
 public void Button(string title,string key,RectangleF r){
  bool hover=r.Contains(game.Input.Aim.X,game.Input.Aim.Y);
  Box(r.X,r.Y,r.Width,r.Height,hover?Color.FromArgb(32,74,70):Color.FromArgb(22,36,45));
  Box(r.X,r.Y,3,r.Height,Palette.Ember);
  Bold(title,r.X+20,r.Y+17,20,Color.White);
  Text(key,r.Right-90,r.Y+20,15,Palette.Ember);
 }
 void Footer(){Box(0,670,Game.Width,50,Palette.Ink);Text("A/D MOVE  SPACE JUMP  MOUSE AIM + FIRE  SHIFT/RMB DASH  Q FLASK  E GATE  S DROP",28,678,14,Palette.Muted);}
 void Hud(){
  Box(0,0,Game.Width,88,Color.FromArgb(245,9,10,20));
  Text("ASHENBOUND",28,16,17,Palette.Ember);
  Box(28,50,230,11,Color.FromArgb(65,34,45));
  Box(28,50,230*game.Health/game.MaxHealth,11,Palette.Blood);
  Text(((int)game.Health)+" / "+game.MaxHealth,274,44,16,Color.White);
  Text("SANCTUM "+(game.Room+1)+" / "+Campaign.TotalRooms+"  -  "+Campaign.Names[game.Room],411,15,17,Color.White);
  Text(game.ExitOpen?"SANCTUM CLEAR / GATE OPEN ON THE RIGHT":"HOSTILES  "+(game.Enemies.Count+game.Pending),434,49,14,game.ExitOpen?Palette.Cyan:Palette.Muted);
  Bold(game.Score.ToString("D6"),1080,11,29,Palette.Gold);
  Text(game.DashCooldown<=0?"DASH READY":"DASH  "+game.DashCooldown.ToString("0.0"),1090,51,14,Palette.Ember);
  Text("FLASKS "+game.Flasks,28,92,14,Palette.Gold);
  if(game.Chain>0){
   Bold(game.Chain+" x CHAIN"+(game.Overcharged?" / OVERCHARGE":""),30,112,27,game.Overcharged?Palette.Gold:Palette.Ash);
   Box(32,154,190*Game.Clamp(game.ChainTime/4.2f,0,1),4,game.Overcharged?Palette.Gold:Palette.Ember);
  }
  if(game.ToastTime>0&&game.Room<2)Text(game.Toast,410,103,16,Palette.Cyan);
  foreach(Enemy e in game.Enemies)if(e.Kind==3){
   Text(Campaign.Boss+(e.Hp<e.MaxHp*.34f?" / ENRAGED":e.Hp<e.MaxHp*.67f?" / CHOIR":""),410,105,18,Palette.Gold);
   Box(350,133,580,8,Color.FromArgb(68,36,48));
   Box(350,133,580*Math.Max(0,e.Hp/e.MaxHp),8,Campaign.Accent(game.Room));
  }
  Footer();Text("ESC PAUSE / M SOUND / F11",995,678,14,Palette.Muted);
 }
 void Overlay(string title,string sub){
  Box(0,88,Game.Width,632,Color.FromArgb(242,9,10,20));
  Box(260,160,4,390,Palette.Ember);
  Bold(title,294,172,45,Palette.Ember);
  Text(sub,298,237,19,Color.FromArgb(198,204,208));
 }
 void StoryPage(){
  Overlay("THE LAST CINDER",Campaign.Speakers[Math.Min(game.Room,3)]);
  Text(Campaign.Scenes[Math.Min(game.Room,3)],298,285,19,Color.FromArgb(214,214,216));
  Bold("ENTER / CLICK TO CONTINUE",298,594,18,Palette.Gold);
 }
 void Upgrades(){
  Overlay("A BREATH BETWEEN BELLS","Choose one relic. The next sanctum is waiting.");
  Button("CINDER EDGE  /  +15% DAMAGE","1",new RectangleF(300,300,680,62));
  Button("ASHEN HEART  /  +30 MAX HP, HEAL 55","2",new RectangleF(300,382,680,62));
  Button("SWIFT PILGRIM  /  FASTER FIRE + MOVEMENT","3",new RectangleF(300,464,680,62));
 }
 void Pause(){
  Overlay("THE ASH HOLDS STILL","Take a breath. The choir can wait.");
  Button("RESUME THE VIGIL","ESC",new RectangleF(300,315,680,62));
  Button("RETURN TO TITLE","",new RectangleF(300,400,680,62));
  Text("M toggles sound. F11 toggles fullscreen. F8 reduces motion.",300,504,17,Palette.Muted);
 }
 void End(){
  bool won=game.State==Stage.Won;
  Overlay(won?"DAWN RETURNS":"THE DUSK KEEPS YOU",won?"The Cinder takes flight. A thin red line becomes a river.":"The choir keeps the night. Another bearer will answer.");
  Bold("SCORE "+game.Score+"     BEST CHAIN "+game.BestChain+"     SILENCED "+game.Kills,300,308,23,Palette.Gold);
  Text("VIGIL "+(game.Seed&65535).ToString("X4")+" / "+TimeSpan.FromSeconds(game.Elapsed).ToString(@"mm\:ss"),300,354,18,Palette.Muted);
  Button("NEW VIGIL","ENTER",new RectangleF(300,420,680,62));
  Button("RETURN TO TITLE","",new RectangleF(300,505,680,62));
 }
}
}
