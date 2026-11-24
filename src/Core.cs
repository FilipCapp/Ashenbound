using System;
using System.Drawing;
namespace Ashenbound {
public enum Stage { Title, Story, Playing, Upgrade, Paused, Dead, Won }
public struct V {
 public float X,Y;
 public V(float x,float y){X=x;Y=y;}
 public static V operator +(V a,V b){return new V(a.X+b.X,a.Y+b.Y);}
 public static V operator -(V a,V b){return new V(a.X-b.X,a.Y-b.Y);}
 public static V operator *(V a,float n){return new V(a.X*n,a.Y*n);}
 public float Length{get{return (float)Math.Sqrt(X*X+Y*Y);}}
 public V Unit{get{float l=Length;return l<.001f?new V(1,0):this*(1/l);}}
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
 public static Color Accent(int room){return room==0?Palette.Cyan:room==1?Palette.Ember:Palette.Violet;}
}
public sealed class Rng {
 uint state;
 public Rng(int seed){state=(uint)seed;if(state==0)state=0x9E3779B9u;}
 public int Next(int lo,int hi){return lo+(int)(NextDouble()*(hi-lo));}
 public double NextDouble(){state^=state<<13;state^=state>>17;state^=state<<5;return (state&0x00FFFFFF)/(double)0x01000000;}
}
}
