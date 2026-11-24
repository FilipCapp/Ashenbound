using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace Ashenbound {
public sealed class PixelArt : IDisposable {
 readonly Bitmap[,] sprites=new Bitmap[5,2];
 static readonly string[] bearer={
  "....oooo....","...ohhhho...","...ohgggo...","...ohgggo...","....orrro...",
  "..oocttco...",".obbcctcco..",".obbcttcco..",".obbcctcco..","..oocctcco..",
  "...occtcco..","...occttcco.","..occtttcco.","...oddoddo..","...oddoddo..","..owwoowwo.."
 };
 static readonly string[] hulk={
  "..oooooooo..",".ohhhhhhho..","ohgghhggho..","ohhhwwhhho..",".ohhwwhho...",
  "..owwwwo....",".oppssppo...","oppssssppo..","oppssssppo..",".oppssppo...",
  "..oppssppo..",".oppssppo...",".orrrrrrro..","..odooddo...","..odooddo...","..oddoooddo."
 };
 static readonly string[] wisp={
  ".....tt.....","....tttt....","...tgggtt...","..ttggggtt..","..tggwwggt..",
  "...twwwwt...","..ttwrwwtt..",".ttwrrrrwtt.","..ttwwwwtt..","...ttwwtt...",
  "....tttt....","...t..t.....","..t....t....","............","............","............"
 };
 static readonly string[] acolyte={
  "....oooo....","...oppppo...","..opggggpo..","..opggggpo..","...oppppo...",
  "..oowwwwoo..",".oppsssppo..","oppssssssppo","oppssgsssppo","oppssssssppo",
  ".oppsssppo..","..oppssppo..",".oppssssppo..","..orrrrrro...","..odooddo....","..oddoooddo.."
 };
 static readonly string[] bishop={
  ".....gg.....","....gggg....","...oggggo...","..oggggggo..",".oggggggggo.",
  "..ohhhhho...","..ohgggho...","...orrro....","..oppsppo...",".oppsssppo..",
  ".opsggsspo..",".opsggsspo..",".opsssssppo.","opsssssssppo","orrrrrrrrrro","..oddoooddo."
 };
 public PixelArt(){
  string[][] maps={bearer,hulk,wisp,acolyte,bishop};
  for(int n=0;n<5;n++)for(int frame=0;frame<2;frame++){
   Bitmap b=new Bitmap(12,16);
   for(int y=0;y<16;y++)for(int x=0;x<12;x++){
    char ch=maps[n][y][x];int xx=x;
    if(frame==1&&n!=2&&y>=13)xx=Math.Max(0,Math.Min(11,x+(x<6?-1:1)));
    Color c=ColorFor(ch);
    if(c.A>0)b.SetPixel(xx,y,c);
   }
   sprites[n,frame]=b;
  }
 }
 Color ColorFor(char c){
  switch(c){
   case 'o':return Color.FromArgb(14,15,25);
   case 'h':return Color.FromArgb(58,58,72);
   case 'g':return Palette.Gold;
   case 'r':return Palette.Blood;
   case 'c':return Color.FromArgb(28,92,101);
   case 't':return Palette.Ember;
   case 'b':return Palette.Violet;
   case 'w':return Palette.Ash;
   case 'd':return Color.FromArgb(38,38,52);
   case 'p':return Color.FromArgb(91,60,104);
   case 's':return Color.FromArgb(172,82,91);
   default:return Color.Transparent;
  }
 }
 public void Sprite(Graphics g,int type,V p,float size,bool left,int frame){
  var state=g.Save();g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
  float pixel=4*size;
  g.TranslateTransform((float)Math.Round(p.X/4)*4,(float)Math.Round(p.Y/4)*4);
  if(left)g.ScaleTransform(-1,1);
  g.DrawImage(sprites[type,frame%2],new RectangleF(-6*pixel,-10*pixel,12*pixel,16*pixel));
  g.Restore(state);
 }
 static void Rect(Graphics g,int x,int y,int w,int h,Color c){using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
 public void Background(Graphics graphics,int room,float clock,bool reduced){
  var saved=graphics.Save();graphics.ScaleTransform(4,4);
  Graphics g=graphics;Color accent=Campaign.Accent(room);
  Rect(g,0,0,320,180,Color.FromArgb(13,12,21));
  for(int y=24;y<156;y+=4)for(int x=0;x<320;x+=4)if((x+y)%14==0)Rect(g,x,y,1,1,Color.FromArgb(22,20,32));
  for(int a=0;a<6;a++){
   int cx=34+a*54;
   Rect(g,cx-22,44,44,96,Color.FromArgb(27,25,39));
   for(int y=48;y<140;y+=3){
    int span=(int)(18*Math.Sqrt(Math.Max(0,1-(double)(y-94)*(y-94)/2401)));
    if(span>0)Rect(g,cx-span,y,span*2,1,Color.FromArgb(16,15,26));
   }
   Rect(g,cx-25,138,50,5,Color.FromArgb(36,33,47));
   Rect(g,cx-23,143,4,13,Color.FromArgb(43,39,54));
   Rect(g,cx+19,143,4,13,Color.FromArgb(43,39,54));
  }
  if(room==0){
   for(int i=0;i<9;i++){int x=i*40-8;Rect(g,x,96,5,60,Color.FromArgb(38,35,49));Rect(g,x-2,94,9,3,Color.FromArgb(52,48,63));}
   Rect(g,0,102,320,2,Color.FromArgb(39,36,51));
   for(int x=0;x<320;x+=8)Rect(g,x,155,5,2,Color.FromArgb(58,54,67));
   Rect(g,283,108,18,30,Color.FromArgb(32,29,44));Rect(g,288,113,8,8,accent);
  } else if(room==1){
   for(int i=0;i<8;i++){
    int x=i*42-6;Rect(g,x,120,38,36,Color.FromArgb(48,33,39));
    for(int t=0;t<8;t++)Rect(g,x+t*5,116,5,8,t%2==0?Palette.Ember:Palette.Blood);
    Rect(g,x+3,136,32,2,Color.FromArgb(92,58,54));
    for(int j=0;j<5;j++)Rect(g,x+6+j*6,139,3,9,Color.FromArgb(76+j*12,48,55));
   }
   Rect(g,0,66,320,1,Color.FromArgb(84,57,57));
   for(int x=16;x<320;x+=40){Rect(g,x+3,67,1,12,Palette.Muted);Rect(g,x,78,8,12,Color.FromArgb(103,51,58));Rect(g,x+2,79,4,10,Palette.Gold);Rect(g,x+3,90,2,4,Palette.Ember);}
  } else {
   for(int i=0;i<8;i++){
    int x=i*43;Rect(g,x,68,35,88,Color.FromArgb(39,31,52));
    for(int row=0;row<4;row++){
     Rect(g,x+2,84+row*19,31,3,Color.FromArgb(88,68,91));
     for(int j=0;j<6;j++){
      int h=7+(i+j+row)%5;
      Rect(g,x+4+j*5,84+row*19-h,3,h,(i+j)%2==0?Palette.Violet:Color.FromArgb(58,86,98));
     }
    }
   }
   Rect(g,0,149,320,7,Color.FromArgb(23,37,48));
   for(int x=0;x<320;x+=19)Rect(g,x,151,10,1,Color.FromArgb(51,71,84));
   Rect(g,145,36,30,66,Color.FromArgb(30,24,40));Rect(g,151,42,18,18,Palette.Gold);
  }
  Rect(g,0,156,320,24,Color.FromArgb(11,11,18));
  for(int x=0;x<320;x+=8){Rect(g,x,164,7,1,Color.FromArgb(30,29,40));if(x%24==0)Rect(g,x,169,2,4,Color.FromArgb(24,22,33));}
  if(!reduced)for(int i=0;i<45;i++){
   int x=(int)(i*37+clock*(room==2?3:9))%320;
   int y=24+(int)(i*19+clock*(room==2?7:65))%130;
   Rect(g,x,y,1,room==2?1:3,Color.FromArgb(52,48,62));
  }
  g.Restore(saved);
 }
 public void Dispose(){foreach(Bitmap b in sprites)b.Dispose();}
}
}
