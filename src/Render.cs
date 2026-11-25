using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
namespace Ashenbound {
public sealed class Renderer : IDisposable {
 Graphics g;Game game;float clock;
  public bool ReduceMotion;
  public string FontName{get{return family.Name;}}
  readonly FontFamily family=CreateFamily("Georgia","Serif");
   readonly PixelArt pixels=new PixelArt();
   readonly Bitmap canvas=new Bitmap(320,180);
   readonly StringFormat format=CreateFormat();
   public void Dispose(){family.Dispose();pixels.Dispose();canvas.Dispose();format.Dispose();}
   static FontFamily CreateFamily(params string[] names){
    for(int i=0;i<names.Length;i++){try{return new FontFamily(names[i]);}catch{}}
    return new FontFamily(FontFamily.GenericSerif.Name);
   }
    static StringFormat CreateFormat(){
     StringFormat result=new StringFormat(StringFormatFlags.NoClip|StringFormatFlags.NoWrap);
     result.Alignment=StringAlignment.Near;result.LineAlignment=StringAlignment.Near;result.Trimming=StringTrimming.None;
     return result;
    }
    public static string RelicLabel(UpgradeDef def,int stack,bool offer){
     if(def.Stackable){
      if(def.MaxStacks<99&&stack>=def.MaxStacks)return "STACKABLE MAX";
      return def.MaxStacks>=99?"STACKABLE x"+stack:"STACKABLE x"+stack+"/"+def.MaxStacks;
     }
     return !offer&&stack>0?"OWNED":"";
    }
   void Box(float x,float y,float w,float h,Color c){if(w<=0||h<=0)return;using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
  void Line(float x,float y,float x2,float y2,Color c,float width){using(var p=new Pen(c,width))g.DrawLine(p,x,y,x2,y2);}
  void Text(string s,float x,float y,float size,Color c){using(var f=new Font(family,size,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(c))g.DrawString(s,f,b,x,y,format);}
  void Bold(string s,float x,float y,float size,Color c){using(var f=new Font(family,size,FontStyle.Bold,GraphicsUnit.Pixel))using(var b=new SolidBrush(c))g.DrawString(s,f,b,x,y,format);}
  float Measure(string s,float size,bool bold){using(var f=new Font(family,size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))return g.MeasureString(s,f,new SizeF(10000,10000),format).Width;}
  public float TextWidth(string s,float size,bool bold){
   using(Bitmap b=new Bitmap(1,1))using(Graphics mg=Graphics.FromImage(b))
   using(Font f=new Font(family,size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))
   return mg.MeasureString(s,f,new SizeF(10000,10000),format).Width;
  }
  void TextRight(string s,float right,float y,float size,Color c){Text(s,right-Measure(s,size,false),y,size,c);}
  void TextWrap(string s,float x,float y,float maxWidth,float size,float lineHeight,Color c){
   using(var f=new Font(family,size,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(c)){
    float cy=y;string[] paragraphs=s.Split('\n');
    for(int p=0;p<paragraphs.Length;p++){
     if(paragraphs[p].Length==0){cy+=lineHeight;continue;}
     string[] words=paragraphs[p].Split(' ');string line="";
     for(int n=0;n<words.Length;n++){
      string candidate=line.Length==0?words[n]:line+" "+words[n];
      if(g.MeasureString(candidate,f,new SizeF(10000,10000),format).Width>maxWidth&&line.Length>0){
       g.DrawString(line,f,b,x,cy,format);cy+=lineHeight;line=words[n];
      } else line=candidate;
     }
     if(line.Length>0){g.DrawString(line,f,b,x,cy,format);cy+=lineHeight;}
    }
   }
  }
  void Guide(string key,string value,float x,float y){Bold(key,x,y,15,Palette.Gold);Text(value,x+112,y,15,Palette.Ash);}
  void Circle(float x,float y,float r,Color c){using(var b=new SolidBrush(c))g.FillEllipse(b,x-r,y-r,r*2,r*2);}
 public void Draw(Graphics graphics,Game model,int width,int height,float seconds){
  game=model;clock=seconds;graphics.Clear(Color.Black);
  using(Graphics low=Graphics.FromImage(canvas)){
   g=low;g.Clear(Palette.Ink);g.ScaleTransform(.25f,.25f);g.SmoothingMode=SmoothingMode.None;
    pixels.Background(g,game.Room,clock,ReduceMotion);
    if(game.State!=Stage.Title&&game.State!=Stage.Sunrise){
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
   g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
   g.SmoothingMode=SmoothingMode.AntiAlias;
   if(game.State==Stage.Title){Title();CursorMark();return;}
   if(game.State==Stage.Sunrise){Sunrise();CursorMark();return;}
   Hud();
   if(game.State==Stage.Story)StoryPage();
  else if(game.State==Stage.Upgrade)Upgrades();
  else if(game.State==Stage.Paused)Pause();
    else if(game.State==Stage.Help)Help();
    else if(game.State==Stage.Audio)AudioSettings();
    else if(game.State==Stage.Abilities)Abilities();
    else if(game.State==Stage.Difficulty)Difficulty();
    else if(game.State==Stage.Won||game.State==Stage.Dead)End();
  CursorMark();
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
    if(!b.Hostile&&b.Kind==0){Fireball(b);continue;}
    Circle(b.P.X,b.P.Y,9,Color.FromArgb(48,Palette.Blood));Circle(b.P.X,b.P.Y,5,Palette.Blood);Circle(b.P.X,b.P.Y,2,Color.White);
   }
   foreach(Particle p in game.Particles)Box(p.P.X,p.P.Y,4,4,Color.FromArgb((int)(255*Game.Clamp(p.Life/p.Max,0,1)),p.Color));
   if(game.Invulnerable<=0||((int)(clock*24))%2==0)Actor(game.Player,game.Aim,1,0);
  }
  void Fireball(Bullet b){
   V dir=b.Velocity.Unit;float flicker=(float)Math.Sin(clock*38+b.P.X*.11f)*1.5f;
   Circle(b.P.X,b.P.Y,11+flicker,Color.FromArgb(58,196,58,22));
   Circle(b.P.X,b.P.Y,8+flicker*.4f,Palette.Ember);
   for(int i=1;i<=3;i++){
    V p=b.P-dir*(8+i*7)+new V((float)Math.Sin(clock*22+i)*2,(float)Math.Cos(clock*19+i)*2);
    Circle(p.X,p.Y,5-i,Color.FromArgb(210-i*45,255,139,74));
   }
   Circle(b.P.X-dir.X*4,b.P.Y-dir.Y*4,5,Palette.Gold);
   Circle(b.P.X+dir.X*2,b.P.Y+dir.Y*2,2,Color.White);
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
   void Grain(float x,float y,float w,float h,int count,int seed){
    int spanX=Math.Max(1,(int)w),spanY=Math.Max(1,(int)h);
    for(int i=0;i<count;i++){
     float px=x+(i*73+seed*37)%spanX;
     float py=y+(i*41+seed*19)%spanY;
     Color c=i%4==0?Color.FromArgb(26,255,205,116):Color.FromArgb(18,228,222,208);
     Box(px,py,i%7==0?2:1,1,c);
    }
   }
   void Panel(RectangleF r,bool active,int seed){
    Box(r.X,r.Y,r.Width,r.Height,Color.FromArgb(active?52:20,9,9,18));
    Box(r.X+3,r.Y+3,r.Width-6,r.Height-6,Color.FromArgb(active?38:16,20,20,32));
    Box(r.X,r.Y,r.Width,2,active?Palette.Gold:Color.FromArgb(92,255,139,74));
    Box(r.X,r.Y+r.Height-2,r.Width,2,Color.FromArgb(active?130:70,255,139,74));
    Box(r.X,r.Y,2,r.Height,Color.FromArgb(active?150:80,255,205,116));
    Box(r.X+r.Width-2,r.Y,2,r.Height,Color.FromArgb(active?110:60,255,139,74));
    Grain(r.X+6,r.Y+6,r.Width-12,r.Height-12,18,seed);
   }
   void Frame(float x,float y,float w,float h,Color edge){
    Box(x,y,w,h,Color.FromArgb(238,8,8,16));
    Box(x,y,w,2,edge);Box(x,y+h-2,w,2,Color.FromArgb(90,edge.R,edge.G,edge.B));
    Box(x,y,2,h,edge);Box(x+w-2,y,2,h,Color.FromArgb(90,edge.R,edge.G,edge.B));
    Box(x,y,20,2,Palette.Gold);Box(x,y,2,20,Palette.Gold);
    Box(x+w-20,y+h-2,20,2,Palette.Gold);Box(x+w-2,y+h-20,2,20,Palette.Gold);
    Box(x+7,y+7,w-14,1,Color.FromArgb(28,255,205,116));
    Box(x+7,y+h-8,w-14,1,Color.FromArgb(24,255,139,74));
    Grain(x+9,y+9,w-18,h-18,42,(int)x+(int)y);
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
     Panel(r,active,index+3);
     Box(r.X+8,r.Y+8,10,r.Height-16,active?Palette.Gold:Color.FromArgb(90,255,139,74));
     Box(r.X+11,r.Y+r.Height/2-5,4,4,active?Color.White:Palette.Ember);
     Box(r.X+24,r.Bottom-9,r.Width-48,1,Color.FromArgb(active?90:40,255,139,74));
     string shown=(active?"> ":"  ")+label;float labelX=r.X+32;
     Bold(shown,labelX,r.Y+12,21,active?Color.White:Palette.Ash);
     if(hint.Length>0){
      float hintWidth=Measure(hint,14,false);float hintX=r.Right-18-hintWidth;
      if(hintX>=labelX+Measure(shown,21,true)+12)Text(hint,hintX,r.Y+16,14,active?Palette.Gold:Palette.Muted);
     }
    }
     void MenuCard(string key,string title,string detail,RectangleF r,int index,string meta="",bool enabled=true){
      bool hover=r.Contains(game.Input.Aim.X,game.Input.Aim.Y);
      bool active=enabled&&(hover||game.Menu==index);
      bool compact=r.Height<72;
      Panel(r,active,index+13);
      Box(r.X+8,r.Y+8,8,r.Height-16,active?Palette.Gold:Palette.Ember);
      Box(r.X+72,r.Y+10,1,r.Height-20,Color.FromArgb(50,255,205,116));
      float keySize=compact?25:32,titleSize=compact?21:24,detailSize=compact?13:16;
      float keyY=compact?r.Y+13:r.Y+29,titleY=compact?r.Y+13:r.Y+19,detailY=compact?r.Y+37:r.Y+58;
      Bold(key,r.X+24,keyY,keySize,active?Palette.Gold:Palette.Muted);
      Bold(title,r.X+86,titleY,titleSize,active?Color.White:Palette.Ash);
      Text(detail,r.X+86,detailY,detailSize,active?Color.FromArgb(220,255,205,116):Palette.Muted);
      if(meta.Length>0){
       float metaSize=compact?12:13,metaWidth=Measure(meta,metaSize,false);
       float metaX=r.Right-18-metaWidth,metaY=compact?r.Y+8:r.Y+16;
       float titleEnd=r.X+86+Measure(title,titleSize,true);
       if(metaX>=titleEnd+12)Text(meta,metaX,metaY,metaSize,enabled?(active?Palette.Gold:Color.FromArgb(180,255,205,116)):Palette.Muted);
      }
      if(active){
       float badgeWidth=Measure("SELECTED",14,false);float badgeX=r.Right-18-badgeWidth;
       float badgeY=compact?r.Y+39:r.Y+64;
       if(badgeX>=r.X+86+Measure(detail,detailSize,false)+12)Text("SELECTED",badgeX,badgeY,14,Palette.Gold);
      }
     }
   void ContextFooter(string message){Box(0,670,Game.Width,50,Palette.Ink);Text(message,28,678,14,Palette.Muted);}
   void Footer(){
    Box(0,670,Game.Width,50,Palette.Ink);
    Text("A/D MOVE  SPACE JUMP  MOUSE AIM + FIRE  SHIFT/RMB DASH  Q FLASK  E GATE  S DROP",28,678,13,Palette.Muted);
    TextRight("ESC PAUSE  M SOUND  F11",1252,678,13,Palette.Muted);
   }
  void Title(){
   Box(0,0,Game.Width,Game.Height,Color.FromArgb(84,6,6,14));
   Sparks(0,40,1280,620);
   Frame(58,54,600,590,Palette.Ember);
   Text("AN ORIGINAL DARK FANTASY RUN-AND-GUN",88,88,15,Palette.Ember);
   Sigil(556,112);
    Bold("ASHENBOUND",86,150,68,Color.White);
    Bold("THE LAST CINDER",88,226,48,Palette.Ember);
    Box(90,300,520,2,Color.FromArgb(110,255,139,74));
    Text("THE SUN HAS BEEN DEAD FOR THREE WINTERS.",90,324,19,Palette.Gold);
    TextWrap("Carry the Last Cinder through three ruined sanctums. Break the Cinder Bishop's choir. Give the world one more dawn.",90,360,520,17,24,Color.FromArgb(200,204,208));
   Actor(new V(360,548),new V(1,0),3,0);
   Box(58,600,600,3,Palette.Ember);
   Text("VEYRA / EMBER BEARER",88,612,16,Palette.Muted);
    Frame(700,160,510,470,Palette.Gold);
    Text("MENU",740,190,16,Palette.Gold);
    RectangleF[] items=MenuLayout.Title;
    MenuItem("NEW VIGIL","ENTER",items[0],0);
    MenuItem("FIELD MANUAL","",items[1],1);
     MenuItem("AUDIO SETTINGS","",items[2],2);
     MenuItem("ABILITIES MANUAL","",items[3],3);
     MenuItem("RETURN TO DESKTOP","",items[4],4);
    ContextFooter("W/S SELECT   ENTER CONFIRM   M MUTE   F11 FULLSCREEN   F8 MOTION");
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
  Footer();
 }
   void StoryPage(){
    if(game.Ending){EpiloguePage();return;}
    Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
    Frame(248,124,784,526,Palette.Gold);
    Header("SANCTUM "+(game.Room+1)+" / "+Campaign.TotalRooms,"THE LAST CINDER",Campaign.Speakers[Math.Min(game.Room,3)],294,152);
     Sparks(248,300,784,260);
     TextWrap(Campaign.Scenes[Math.Min(game.Room,3)],298,276,724,17,24,Color.FromArgb(214,214,216));
     MenuItem("CONTINUE","ENTER",MenuLayout.Story[0],0);
    ContextFooter("ENTER / CLICK TO CONTINUE   J REOPENS THE PAGE IN PLAY");
   }
   void EpiloguePage(){
    Box(0,88,Game.Width,632,Color.FromArgb(242,7,7,15));
    Frame(210,124,860,526,Palette.Gold);
    Header("VIGIL COMPLETE","ONE MORE MORNING","The Cinder has left Veyra's hands.",254,150);
    Sparks(250,300,780,150);
    Circle(640,338,98,Color.FromArgb(22,255,205,116));
    Circle(640,338,66,Color.FromArgb(32,255,139,74));
    Box(370,370,540,3,Palette.Gold);
    for(int i=0;i<9;i++){
     float x=400+i*60;float y=356-(float)Math.Sin(i/8f*Math.PI)*34;
     Box(x,y,3,44-(int)(i/3f)*8,Color.FromArgb(180-i*12,255,205,116));
    }
    Bold("THE BISHOP BREAKS",270,398,19,Palette.Gold);
    TextWrap("A dried host. A silenced choir. The long dusk loses its voice.",270,430,220,15,22,Palette.Ash);
    Bold("THE CINDER FLIES",530,398,19,Palette.Gold);
    TextWrap("It rises through the vaulted dark and takes flight beyond the nave.",530,430,220,15,22,Palette.Ash);
    Bold("ONE MORE MORNING",790,398,19,Palette.Gold);
    TextWrap("First a red line, then a river. Veyra carries the rest.",790,430,220,15,22,Palette.Ash);
    MenuItem("WITNESS THE DAWN","ENTER",MenuLayout.Epilogue[0],0);
    ContextFooter("ENTER / CLICK TO SEE THE VICTORY RECORD");
   }
   static UpgradeDef UpgradeFor(RelicKind kind){return Campaign.UpgradeFor(kind);}
   void Upgrades(){
    Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
    Frame(220,132,840,500,Palette.Ember);
    Header("SANCTUM CLEAR","A BREATH BETWEEN BELLS","Choose one relic. The next sanctum is waiting.",266,152);
    for(int i=0;i<game.UpgradeChoices.Length;i++){
     RelicKind kind=game.UpgradeChoices[i];
     UpgradeDef def=UpgradeFor(kind);
     if(def==null){MenuCard((i+1).ToString(),"NO RELIC","ALL RELICS CLAIMED",MenuLayout.Upgrade[i],i,"",false);continue;}
     MenuCard((i+1).ToString(),def.Name,def.Detail,MenuLayout.Upgrade[i],i,RelicLabel(def,game.OwnedStacks(kind),true));
    }
    ContextFooter("1/2/3 OR W/S + ENTER   SELECT A RELIC");
   }
   void Triangle(float x1,float y1,float x2,float y2,float x3,float y3,Color c){
    using(SolidBrush b=new SolidBrush(c))g.FillPolygon(b,new PointF[]{new PointF(x1,y1),new PointF(x2,y2),new PointF(x3,y3)});
   }
   void GothicArch(float cx,float baseY,float w,float h,Color fill,Color edge){
    float left=cx-w/2,right=cx+w/2,top=baseY-h;
    Triangle(cx,top-38,left,top+34,right,top+34,fill);
    Box(left,top+34,w,h-34,fill);
    Color opening=Color.FromArgb(255,34,20,32);
    Triangle(cx,top-14,left+26,top+48,right-26,top+48,opening);
    Box(left+26,top+48,w-52,h-48,opening);
    Line(left,baseY,left,top+34,edge,2);
    Line(right,baseY,right,top+34,edge,2);
    Line(left,top+34,cx,top-38,edge,2);
    Line(cx,top-38,right,top+34,edge,2);
    Line(left+26,baseY,left+26,top+48,Color.FromArgb(60,255,139,74),2);
    Line(right-26,baseY,right-26,top+48,Color.FromArgb(60,255,139,74),2);
   }
   void Ruins(float horizon){
    Color far=Color.FromArgb(255,28,18,34),dark=Color.FromArgb(255,9,8,15),edge=Color.FromArgb(90,255,139,74);
    for(int i=0;i<9;i++){
     float x=i*162-60,peak=horizon-70-((i*37)%66);
     Triangle(x,horizon+8,x+80,peak,x+170,horizon+8,far);
    }
    for(int i=0;i<7;i++){
     float x=i*205-80,h=58+((i*61)%82);
     Box(x,horizon-h,116,h+10,dark);
     Triangle(x,horizon-h,x+58,horizon-h-22-((i*17)%30),x+116,horizon-h,dark);
     Box(x+18,horizon-h+8,12,18,Color.FromArgb(180,92,28,36));
     Box(x+74,horizon-h+14,10,12,Color.FromArgb(150,92,28,36));
     Line(x,horizon-h,x,horizon,edge,1);
    }
    GothicArch(704,horizon,244,206,dark,edge);
    GothicArch(1042,horizon,150,142,dark,Color.FromArgb(60,255,139,74));
    for(int i=0;i<5;i++){
     float x=180+i*84,h=104+((i*29)%70);
     Box(x,horizon-h,18,h,dark);
     Box(x-8,horizon-h,34,10,dark);
     Box(x-6,horizon-8,30,8,dark);
     Line(x+9,horizon-h,x+9,horizon,edge,1);
    }
    Box(0,horizon-6,Game.Width,10,Color.FromArgb(255,24,12,20));
    for(int i=0;i<10;i++){
     float x=(i*151+83)%1280,y=horizon-24-((i*43)%80);
     Box(x,y,3,12,Color.FromArgb(180,255,139,74));
    }
   }
   void Sunrise(){
    float progress=ReduceMotion?1:Game.Clamp(game.SunriseTime/Game.SunriseDuration,0,1);
    using(LinearGradientBrush sky=new LinearGradientBrush(new Rectangle(0,0,(int)Game.Width,(int)Game.Height),Color.FromArgb(255,7,7,17),Color.FromArgb(255,104,20,36),LinearGradientMode.Vertical))
     g.FillRectangle(sky,0,0,Game.Width,Game.Height);
    float horizon=548-progress*96;
    float sunY=520-progress*310;
    float sunX=826;
    int glow=(int)(40+progress*52);
    Circle(sunX,sunY,glow*3,Color.FromArgb((int)(8+progress*10),190,32,44));
    Circle(sunX,sunY,glow*2,Color.FromArgb((int)(16+progress*16),206,44,52));
    Circle(sunX,sunY,glow,Color.FromArgb((int)(130+progress*90),154,24,36));
    Circle(sunX,sunY,Math.Max(6,glow/2),Color.FromArgb(255,12,7,14));
    Ring(sunX,sunY,glow+4,Color.FromArgb((int)(70+progress*90),255,92,72),3);
    if(!ReduceMotion){
     for(int i=0;i<14;i++){
      float a=i*.4488f;
      float length=210+((i*47)%170)*progress;
      Line(sunX,sunY,sunX+(float)Math.Cos(a)*length,sunY+(float)Math.Sin(a)*length,Color.FromArgb((int)(12+progress*18),255,92,72),2);
     }
    }
    Box(0,horizon,Game.Width,Game.Height-horizon,Color.FromArgb(255,12,8,17));
    Ruins(horizon);
    if(!ReduceMotion){
     for(int i=0;i<28;i++){
      float t=(clock*.28f+i*.179f)%1f;
      float px=(i*97+(int)(clock*18))%1280;
      float py=horizon-180+t*260;
      Box(px,py,2,5,Color.FromArgb((int)(120*(1-t)),255,139,74));
     }
    }
   Text("THE LONG DUSK BREAKS",58,58,16,Palette.Ember);
   Bold("ONE MORE MORNING",54,88,50,Color.White);
   Box(58,158,520,2,Color.FromArgb(120,255,205,116));
   TextWrap("First a red line, then a river. The Cinder leaves Veyra's hands, and the world remembers the sun.",60,178,590,17,24,Color.FromArgb(230,235,226,214));
   string phase=progress<.34f?"I  THE RED LINE":progress<.72f?"II  THE RIVER":"III  THE MORNING";
   Bold(phase,60,276,24,Palette.Gold);
   if(!ReduceMotion){
    float line=600-progress*480;
    Box(line,horizon-2,1280-line,3,Color.FromArgb((int)(90+progress*120),255,205,116));
   }
   Box(440,628,400,6,Color.FromArgb(44,228,222,208));
   Box(440,628,400*progress,6,Palette.Gold);
   Text("WITNESSING THE DAWN",440,602,14,Palette.Muted);
   TextRight("ENTER / ESC / CLICK TO SKIP",840,602,14,Palette.Ash);
  }
  void Pause(){
   Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
   Frame(360,150,560,420,Palette.Cyan);
   Header("TIME SUSPENDED","THE ASH HOLDS STILL","Take a breath. The choir can wait.",406,170);
   RectangleF[] items=MenuLayout.Pause;
   MenuItem("RESUME VIGIL","ESC",items[0],0);
    MenuItem("FIELD MANUAL","",items[1],1);
    MenuItem("AUDIO SETTINGS","",items[2],2);
    MenuItem("RETURN TO TITLE","",items[3],3);
    ContextFooter("W/S SELECT   ENTER CONFIRM   ESC RESUME   M MUTE");
  }
  void Help(){
   Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
   Frame(220,100,840,525,Palette.Cyan);
   Header("FIELD MANUAL","HOW TO SURVIVE THE ASH","Controls and cinder-lore for the bearer.",266,132);
    Text("MOVEMENT",266,264,16,Palette.Gold);
    Guide("A / D","MOVE",266,292);
    Guide("SPACE / W","JUMP",266,320);
    Guide("S / DOWN","DROP THROUGH LEDGES",266,348);
    Guide("SHIFT / RMB","DASH WITH I-FRAMES",266,376);
    Text("COMBAT",650,264,16,Palette.Gold);
    Guide("MOUSE","AIM",650,292);
    Guide("LEFT CLICK","FIRE",650,320);
    Guide("Q","CINDER FLASK",650,348);
    Guide("E","OPEN CLEARED GATE",650,376);
    Guide("J","REOPEN SCENE",650,404);
    Box(266,430,724,2,Color.FromArgb(90,255,139,74));
    Text("CINDER RULES",266,448,16,Palette.Gold);
    TextWrap("Chain kills quickly to overcharge the Trigger and heal on every takedown. Break every foe before the gate opens. Walls hold them inside the sanctum. Dashes ignore hits. Flasks explode on impact. Stay ahead of the Bishop's choir.",266,474,724,15,22,Palette.Ash);
   MenuItem("BACK","ESC",MenuLayout.Help[0],0);
   ContextFooter("ESC / ENTER TO RETURN");
  }
    void Slider(string label,float value,RectangleF r,int index){
     bool active=game.Menu==index||r.Contains(game.Input.Aim.X,game.Input.Aim.Y);
     Panel(r,active,60+index);
     Box(r.X+8,r.Y+8,8,r.Height-16,active?Palette.Gold:Palette.Ember);
     Bold(label,r.X+28,r.Y+14,21,active?Color.White:Palette.Ash);
     float barX=r.X+190,barY=r.Y+24,barW=r.Width-300;
     Box(barX-2,barY-2,barW+4,14,Color.FromArgb(active?70:40,255,205,116));
     Box(barX,barY,barW,10,Color.FromArgb(70,34,45));
     Box(barX,barY,barW*value,10,active?Palette.Gold:Palette.Ember);
     Box(barX,barY,barW*value,2,Color.FromArgb(active?180:110,255,205,116));
     for(int i=0;i<=10;i++)Box(barX+barW*i/10-1,barY-3,2,16,Color.FromArgb(70,12,12,18));
     TextRight(Math.Round(value*100)+"%",r.Right-24,r.Y+18,17,active?Palette.Gold:Palette.Muted);
    }
    void AudioSettings(){
     Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
     Frame(250,130,780,470,Palette.Gold);
     Header("AUDIO RITES","TUNE THE ASH","Master, music, and effects.",296,148);
     Slider("MASTER",game.Volume[Game.MasterVolumeIndex],MenuLayout.Audio[0],0);
     Slider("MUSIC",game.Volume[Game.MusicVolumeIndex],MenuLayout.Audio[1],1);
     Slider("EFFECTS",game.Volume[Game.SfxVolumeIndex],MenuLayout.Audio[2],2);
     MenuItem("BACK","ESC",MenuLayout.Audio[3],3);
     ContextFooter("A/D ADJUST   W/S SELECT   ENTER / ESC BACK   M MUTE");
    }
    void Abilities(){
     Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
     Frame(180,104,920,560,Palette.Gold);
     Header("CINDER LEDGER","ABILITIES MANUAL","Nine relics can change the shape of a vigil.",226,126);
      for(int i=0;i<Campaign.Upgrades.Length;i++){
       float x=226+(i%3)*290,y=246+(i/3)*114;
       RectangleF card=new RectangleF(x,y,270,98);
       UpgradeDef def=Campaign.Upgrades[i];int stack=game.OwnedStacks(def.Kind);
        bool active=card.Contains(game.Input.Aim.X,game.Input.Aim.Y);
        Panel(card,active,40+i);
        Box(card.X+54,card.Y+10,1,card.Height-20,Color.FromArgb(50,255,205,116));
        Bold((i+1).ToString("00"),card.X+16,card.Y+20,17,active?Palette.Gold:Palette.Muted);
        Bold(def.Name,card.X+68,card.Y+16,17,active?Color.White:Palette.Ash);
        Text(def.Detail,card.X+18,card.Y+56,14,active?Palette.Gold:Palette.Muted);
        string meta=RelicLabel(def,stack,false);
        if(meta.Length>0){float metaWidth=Measure(meta,12,false);Text(meta,card.Right-14-metaWidth,card.Y+77,12,stack>0?Palette.Gold:Palette.Muted);}
      }
     MenuItem("BACK","ESC",MenuLayout.Abilities[0],0);
     ContextFooter("ESC / ENTER TO RETURN");
    }
    void Difficulty(){
     Box(0,88,Game.Width,632,Color.FromArgb(238,8,8,16));
     Frame(250,110,780,510,Palette.Gold);
     Header("NEW VIGIL","DIFFICULTY","Foe strength and incoming damage change. Rooms and relics do not.",296,130);
     for(int i=0;i<Campaign.Difficulties.Length;i++){
      DifficultyDef d=Campaign.Difficulties[i];
      MenuCard((i+1).ToString(),d.Name,d.Detail,MenuLayout.Difficulty[i],i);
     }
     MenuItem("BACK","ESC",MenuLayout.Difficulty[4],4);
     ContextFooter("W/S SELECT   ENTER / NUMBER 1-4 CONFIRM   ESC BACK");
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
   void CursorMark(){
    V a=game.Input.Aim;Color c=game.State==Stage.Playing?Palette.Cyan:Palette.Gold;
    Ring(a.X,a.Y,13,c,2);Ring(a.X,a.Y,5,Palette.Gold,1);
    Line(a.X-20,a.Y,a.X-8,a.Y,c,2);Line(a.X+8,a.Y,a.X+20,a.Y,c,2);
    Line(a.X,a.Y-20,a.X,a.Y-8,c,2);Line(a.X,a.Y+8,a.X,a.Y+20,c,2);
    Circle(a.X,a.Y,2,Color.White);
   }
 }
}
