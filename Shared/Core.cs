using System;
using System.Drawing;
namespace Ashenbound {
public enum Stage { Title, Story, Playing, Upgrade, Paused, Help, Audio, Abilities, Difficulty, Dead, Sunrise, Won }
public struct V {
 public float X,Y;
 public V(float x,float y){X=x;Y=y;}
 public static V operator +(V a,V b){return new V(a.X+b.X,a.Y+b.Y);}
 public static V operator -(V a,V b){return new V(a.X-b.X,a.Y-b.Y);}
 public static V operator *(V a,float n){return new V(a.X*n,a.Y*n);}
 public float Length{get{return (float)Math.Sqrt(X*X+Y*Y);}}
  public V Unit{get{float l=Length;return l<.001f?new V(1,0):this*(1/l);}}
 }
 public static class MenuLayout {
  public static readonly RectangleF[] None=new RectangleF[0];
   public static readonly RectangleF[] Title={
    new RectangleF(740,238,430,62),new RectangleF(740,310,430,62),
    new RectangleF(740,382,430,62),new RectangleF(740,454,430,62),
    new RectangleF(740,526,430,62)
   };
  public static readonly RectangleF[] Story={new RectangleF(760,586,270,54)};
   public static readonly RectangleF[] Epilogue={new RectangleF(450,604,380,50)};
   public static readonly RectangleF[] Upgrade={
    new RectangleF(286,264,216,316),new RectangleF(532,264,216,316),new RectangleF(778,264,216,316)
   };

  public static readonly RectangleF[] Pause={
   new RectangleF(430,292,420,58),new RectangleF(430,356,420,58),
   new RectangleF(430,420,420,58),new RectangleF(430,484,420,58)
  };
  public static readonly RectangleF[] Help={new RectangleF(520,558,240,52)};
   public static readonly RectangleF[] Audio={
    new RectangleF(320,262,640,60),new RectangleF(320,340,640,60),
    new RectangleF(320,418,640,60),new RectangleF(520,518,240,52)
   };
   public static readonly RectangleF[] Abilities={new RectangleF(520,600,240,52)};
   public static readonly RectangleF[] Difficulty={
    new RectangleF(290,250,700,58),new RectangleF(290,320,700,58),
    new RectangleF(290,390,700,58),new RectangleF(290,460,700,58),
    new RectangleF(520,540,240,52)
   };
  public static readonly RectangleF[] End={
   new RectangleF(430,396,420,58),new RectangleF(430,464,420,58)
  };
  public static RectangleF[] Items(Stage state){
   if(state==Stage.Title)return Title;
   if(state==Stage.Story)return Story;
   if(state==Stage.Upgrade)return Upgrade;
   if(state==Stage.Paused)return Pause;
    if(state==Stage.Help)return Help;
    if(state==Stage.Audio)return Audio;
    if(state==Stage.Abilities)return Abilities;
    if(state==Stage.Difficulty)return Difficulty;
    if(state==Stage.Dead||state==Stage.Won)return End;
   return None;
  }
  public static RectangleF[] Items(Stage state,bool ending){
   if(state==Stage.Story&&ending)return Epilogue;
   return Items(state);
  }
  public static int Hit(Stage state,V point){return Hit(state,point,false);}
  public static int Hit(Stage state,V point,bool ending){
   RectangleF[] items=Items(state,ending);
   for(int i=0;i<items.Length;i++)if(items[i].Contains(point.X,point.Y))return i;
   return -1;
  }
 }
 public class Controls {
 public bool Left,Right,Jump,Dash,Fire,Down,Use,Flask;
 public V Aim=new V(900,400);
 public void ClearEdges(){Jump=Dash=Use=Flask=false;}
}
public static class Palette {
 public static readonly Color Ash=Color.FromArgb(228,222,208);
 public static readonly Color Ember=Color.FromArgb(255,139,74);
 public static readonly Color Blood=Color.FromArgb(201,54,72);
 public static readonly Color Gold=Color.FromArgb(255,205,116);
 public static readonly Color Cyan=Color.FromArgb(112,214,205);
 public static readonly Color Ink=Color.FromArgb(12,12,20);
 public static readonly Color Muted=Color.FromArgb(137,139,151);
 public static readonly Color Violet=Color.FromArgb(126,84,143);
}
public enum RelicKind { Damage, Vitality, Swift, Pierce, Split, Flask, Dash, Siphon, Ward, None }

public sealed class UpgradeDef {
 public readonly RelicKind Kind;public readonly string Name;public readonly string Detail;
 public readonly bool Stackable;public readonly int MaxStacks;
 public UpgradeDef(RelicKind kind,string name,string detail,int maxStacks){Kind=kind;Name=name;Detail=detail;MaxStacks=maxStacks;Stackable=maxStacks>1;}
}
public sealed class DifficultyDef {
 public readonly string Name;public readonly string Detail;
 public readonly float EnemyHp;public readonly float IncomingDamage;
 public DifficultyDef(string name,string detail,float enemyHp,float incomingDamage){Name=name;Detail=detail;EnemyHp=enemyHp;IncomingDamage=incomingDamage;}
}

public static class Campaign {
 public static readonly string[] Names={"Ashen Nave","Cinder Cloister","The Reliquary"};
 public static readonly string[] Speakers={"VEYRA","VEYRA","VEYRA","VEYRA"};
 public static readonly string[] Scenes={
  "VEYRA\n\nThe bell tower burned three winters ago and the sun has not returned.\nI carry the Last Cinder under my ribs, wrapped in a dead abbot's psalter.\nThe Bishop's ashbound hunt me through the Nave.\nIf I fall, the dusk becomes forever.",
  "VEYRA\n\nThe Cloister still rings with the Names of the extinguished.\nEvery acolyte I silence whispers the Bishop's liturgy:\n'Let the dark be merciful. Let the flame forget.'\nI bind my wounds with cinder thread and keep moving.\nThe Cinder is warm. That is enough.",
  "VEYRA\n\nIn the Reliquary, the Bishop waits on a throne of cold incense.\nHe offers me a crown of ash and a quiet end.\nI answer with the only prayer I know: the sound of a Trigger\nand a heart that refuses to stop.\nThe Last Cinder is not a gift. It is a debt.",
  "VEYRA\n\nThe Bishop breaks like a dried host.\nThe Cinder lifts from my hands, hangs in the vaulted dark, and takes flight.\nDawn does not arrive all at once; it comes as a thin red line, then a river.\nI am still bound to the ash, but the world has one more morning.\nI will carry the rest."
 };
  public const string Boss="THE CINDER BISHOP";
  public const int TotalRooms=3;
  public const int DefaultDifficulty=1;
  public static readonly DifficultyDef[] Difficulties={
   new DifficultyDef("PILGRIM","A gentler vigil. Fewer foes stand in your way and their strikes are softened.",.8f,.65f),
   new DifficultyDef("BEARER","The vigil as it was written. Reckonable foes and honest wounds.",1f,1f),
   new DifficultyDef("ASHBOUND","The order sends its strongest. Foes endure more and every hit bites deeper.",1.35f,1.4f),
   new DifficultyDef("NIGHTMARE","The dusk has learned your name. Foes are relentless and no wound is small.",1.8f,1.9f)
  };
  public static readonly UpgradeDef[] Upgrades={
   new UpgradeDef(RelicKind.Damage,"CINDER EDGE","+15% DAMAGE",99),
   new UpgradeDef(RelicKind.Vitality,"ASHEN HEART","+30 MAX HP / HEAL 55",99),
   new UpgradeDef(RelicKind.Swift,"SWIFT PILGRIM","FASTER FIRE AND MOVEMENT",8),
   new UpgradeDef(RelicKind.Pierce,"EMBER LANCE","FIREBALLS PIERCE FOES",3),
   new UpgradeDef(RelicKind.Split,"FORKED FLAME","FIRE TWO ANGLED EMBERS",1),
   new UpgradeDef(RelicKind.Flask,"CINDER FLASK","EXTRA FLASK / WIDER BLAST",99),
   new UpgradeDef(RelicKind.Dash,"ASHEN STEP","FASTER DASH RECOVERY",4),
   new UpgradeDef(RelicKind.Siphon,"PYRE BLOOD","MORE HEAL ON TAKEDOWNS",99),
   new UpgradeDef(RelicKind.Ward,"CINDER WARD","TAKE 20% LESS DAMAGE",2)
  };
  public static UpgradeDef UpgradeFor(RelicKind kind){
   for(int i=0;i<Upgrades.Length;i++)if(Upgrades[i].Kind==kind)return Upgrades[i];
   return null;
  }
  public static Color Accent(int room){return room==0?Palette.Cyan:room==1?Palette.Ember:Palette.Violet;}
}
public sealed class Rng {
 uint state;
 public Rng(int seed){state=(uint)seed;if(state==0)state=0x9E3779B9u;}
 public int Next(int lo,int hi){return lo+(int)(NextDouble()*(hi-lo));}
 public double NextDouble(){state^=state<<13;state^=state>>17;state^=state<<5;return (state&0x00FFFFFF)/(double)0x01000000;}
}
}
