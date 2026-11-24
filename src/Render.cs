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
  else if(game.State==Stage.Upgrade)Upgrades();
  else if(game.State==Stage.Paused)Pause();
  else if(game.State==Stage.Help)Help();
  else if(game.State==Stage.Won||game.State==Stage.Dead)End();
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
  void Ring(float x,float y,float r,Color c,float width){using(Pen p=new Pen(c,width))g.DrawEllipse(p,x-r,y-r,r*2,r*2);}
  void Frame(float x,float y,float w,float h,Color edge){
   Box(x,y,w,h,Color.FromArgb(238,8,8,16));
   Box(x,y,w,2,edge);Box(x,y+h-2,w,2,Color.FromArgb(90,edge.R,edge.G,edge.B));
   Box(x,y,2,h,edge);Box(x+w-2,y,2,h,Color.FromArgb(90,edge.R,edge.G,edge.B));
   Box(x,y,20,2,Palette.Gold);Box(x,y,2,20,Palette.Gold);
   Box(x+w-20,y+h-2,20,2,Palette.Gold);Box(x+w-2,y+h-20,2,20,Palette.Gold);
  }
  void Sparks(float x,float y,float w,float h){
   if(ReduceMotion)return;
   for(int i=0;i<16;i++){
    float t=(clock*.22f+i*.137f)%1f;
    float px=x+((i*137)%820)+(float)Math.Sin(clock+i)*4;
    float py=y+h-t*h;
    Box(px,py,2,2,Color.FromArgb((int)(140*(1-t)),255,139,74));
   }
  }
  void Sigil(float x,float y){
   Ring(x,y,40,Color.FromArgb(70,255,139,74),2);
   Ring(x,y,26,Palette.Gold,2);
   Line(x,y-32,x,y+32,Palette.Ember,2);
   Line(x-23,y+19,x,y-27,Palette.Ember,2);
   Line(x+23,y+19,x,y-27,Palette.Ember,2);
   Circle(x,y,4,Palette.Gold);
  }
  void Header(string kicker,string title,string sub,float x,float y){
   Text(kicker,x,y,15,Palette.Ember);
   Bold(title,x,y+24,44,Color.White);
   Text(sub,x,y+78,18,Color.FromArgb(198,204,208));
   Box(x,y+108,560,2,Color.FromArgb(90,255,139,74));
  }
  void MenuItem(string label,string hint,RectangleF r,int index){
   bool hover=r.Contains(game.Input.Aim.X,game.Input.Aim.Y);
   bool active=hover||game.Menu==index;
   Box(r.X,r.Y,r.Width,r.Height,Color.FromArgb(active?62:24,18,18,30));
   Box(r.X,r.Y,r.Width,2,active?Palette.Gold:Color.FromArgb(90,255,139,74));
   Box(r.X,r.Y,5,r.Height,active?Palette.Gold:Palette.Ember);
   Bold((active?"> ":"  ")+label,r.X+22,r.Y+16,22,active?Color.White:Palette.Ash);
   if(hint.Length>0)Text(hint,r.Right-104,r.Y+20,16,active?Palette.Gold:Palette.Muted);
  }
  void MenuCard(string key,string title,string detail,RectangleF r,int index){
   bool hover=r.Contains(game.Input.Aim.X,game.Input.Aim.Y);
   bool active=hover||game.Menu==index;
   Box(r.X,r.Y,r.Width,r.Height,Color.FromArgb(active?68:26,18,18,30));
   Box(r.X,r.Y,r.Width,2,active?Palette.Gold:Color.FromArgb(80,112,214,205));
   Box(r.X,r.Y,6,r.Height,active?Palette.Gold:Palette.Ember);
   Bold(key,r.X+24,r.Y+29,32,active?Palette.Gold:Palette.Muted);
   Bold(title,r.X+86,r.Y+19,24,active?Color.White:Palette.Ash);
   Text(detail,r.X+86,r.Y+58,17,active?Color.FromArgb(220,255,205,116):Palette.Muted);
   if(active)Text("SELECTED",r.Right-124,r.Y+64,15,Palette.Gold);
  }
  void ContextFooter(string message){Box(0,670,Game.Width,50,Palette.Ink);Text(message,28,678,14,Palette.Muted);}
  void Footer(){ContextFooter("A/D MOVE  SPACE JUMP  MOUSE AIM + FIRE  SHIFT/RMB DASH  Q FLASK  E GATE  S DROP");}
  void Title(){
   Box(0,0,Game.Width,Game.Height,Color.FromArgb(84,6,6,14));
   Sparks(0,40,1280,620);
   Frame(58,54,600,590,Palette.Ember);
   Text("AN ORIGINAL DARK FANTASY RUN-AND-GUN",88,88,15,Palette.Ember);
   Sigil(556,112);
   Bold("ASHENBOUND",86,150,78,Color.White);
   Bold("THE LAST CINDER",88,226,54,Palette.Ember);
   Box(90,300,520,2,Color.FromArgb(110,255,139,74));
   Text("THE SUN HAS BEEN DEAD FOR THREE WINTERS.",90,324,20,Palette.Gold);
   Text("Carry the Last Cinder through three ruined sanctums.\nBreak the Cinder Bishop's choir. Give the world one more dawn.",90,364,19,Color.FromArgb(200,204,208));
   Actor(new V(360,548),new V(1,0),3,0);
   Box(58,600,600,3,Palette.Ember);
   Text("VEYRA / EMBER BEARER",88,612,16,Palette.Muted);
   Frame(700,160,510,380,Palette.Gold);
   Text("MENU",740,190,16,Palette.Gold);
   RectangleF[] items=MenuLayout.Title;
   MenuItem("NEW VIGIL","ENTER",items[0],0);
   MenuItem("FIELD MANUAL","",items[1],1);
   MenuItem(game.SoundOn?"SOUND ON":"SOUND OFF","M",items[2],2);
   MenuItem("RETURN TO DESKTOP","",items[3],3);
   ContextFooter("W/S SELECT   ENTER CONFIRM   M SOUND   F11 FULLSCREEN   F8 MOTION");
  }
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
  void StoryPage(){
   Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
   Frame(248,124,784,526,Palette.Gold);
   Header("SANCTUM "+(game.Room+1)+" / "+Campaign.TotalRooms,"THE LAST CINDER",Campaign.Speakers[Math.Min(game.Room,3)],294,152);
   Sparks(248,300,784,260);
   Text(Campaign.Scenes[Math.Min(game.Room,3)],298,282,19,Color.FromArgb(214,214,216));
   MenuItem("CONTINUE","ENTER",MenuLayout.Story[0],0);
   ContextFooter("ENTER / CLICK TO CONTINUE   J REOPENS THE PAGE IN PLAY");
  }
  void Upgrades(){
   Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
   Frame(220,132,840,500,Palette.Ember);
   Header("SANCTUM CLEAR","A BREATH BETWEEN BELLS","Choose one relic. The next sanctum is waiting.",266,152);
   MenuCard("1","CINDER EDGE","+15% DAMAGE",MenuLayout.Upgrade[0],0);
   MenuCard("2","ASHEN HEART","+30 MAX HP / HEAL 55",MenuLayout.Upgrade[1],1);
   MenuCard("3","SWIFT PILGRIM","FASTER FIRE AND MOVEMENT",MenuLayout.Upgrade[2],2);
   ContextFooter("1/2/3 OR W/S + ENTER   SELECT A RELIC");
  }
  void Pause(){
   Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
   Frame(360,150,560,420,Palette.Cyan);
   Header("TIME SUSPENDED","THE ASH HOLDS STILL","Take a breath. The choir can wait.",406,170);
   RectangleF[] items=MenuLayout.Pause;
   MenuItem("RESUME VIGIL","ESC",items[0],0);
   MenuItem("FIELD MANUAL","",items[1],1);
   MenuItem(game.SoundOn?"SOUND ON":"SOUND OFF","M",items[2],2);
   MenuItem("RETURN TO TITLE","",items[3],3);
   ContextFooter("W/S SELECT   ENTER CONFIRM   ESC RESUME");
  }
  void Help(){
   Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
   Frame(220,100,840,525,Palette.Cyan);
   Header("FIELD MANUAL","HOW TO SURVIVE THE ASH","Controls and cinder-lore for the bearer.",266,132);
   Text("MOVEMENT",266,264,16,Palette.Gold);
   Text("A / D  or  LEFT / RIGHT      MOVE\nSPACE / W / UP               JUMP\nS / DOWN                     DROP THROUGH LEDGES\nSHIFT / RIGHT MOUSE          DASH WITH BRIEF I-FRAMES",266,290,17,Color.FromArgb(206,204,208));
   Text("COMBAT",650,264,16,Palette.Gold);
   Text("MOUSE                        AIM\nLEFT CLICK                   FIRE\nQ                            THROW A CINDER FLASK\nE                            OPEN A CLEARED GATE\nJ                            REOPEN THE CURRENT SCENE",650,290,17,Color.FromArgb(206,204,208));
   Box(266,430,724,2,Color.FromArgb(90,255,139,74));
   Text("CINDER RULES",266,448,16,Palette.Gold);
   Text("Chain kills quickly to overcharge the Trigger and heal on every takedown.\nBreak every foe before the gate opens. Walls hold them inside the sanctum.\nDashes ignore hits. Flasks explode on impact. Stay ahead of the Bishop's choir.",266,474,17,Palette.Ash);
   MenuItem("BACK","ESC",MenuLayout.Help[0],0);
   ContextFooter("ESC / ENTER TO RETURN");
  }
  void End(){
   bool won=game.State==Stage.Won;
   Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
   Frame(300,160,680,410,won?Palette.Gold:Palette.Blood);
   Header(won?"VIGIL COMPLETE":"VIGIL ENDED",won?"DAWN RETURNS":"THE DUSK KEEPS YOU",won?"The Cinder takes flight. A thin red line becomes a river.":"The choir keeps the night. Another bearer will answer.",346,190);
   Box(346,310,588,76,Color.FromArgb(28,20,24));
   Bold("SCORE "+game.Score,364,322,23,Palette.Gold);
   Text("BEST CHAIN "+game.BestChain+"     SILENCED "+game.Kills+"     VIGIL "+(game.Seed&65535).ToString("X4"),364,360,17,Palette.Muted);
   MenuItem("NEW VIGIL","ENTER",MenuLayout.End[0],0);
   MenuItem("RETURN TO TITLE","",MenuLayout.End[1],1);
   ContextFooter("W/S SELECT   ENTER CONFIRM");
  }
}
}
