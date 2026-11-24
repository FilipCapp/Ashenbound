using System;
using System.Collections.Generic;
using System.Drawing;
namespace Ashenbound {
public class Enemy {
 public V P;public int Kind;public float Hp,MaxHp,Time,Fire,Hit,Mode;
}
public class Bullet {
 public V P,Velocity;public bool Hostile;public float Damage,Life,Gravity;public int Kind;
}
public class Particle {
 public V P,Velocity;public Color Color;public float Life,Max;
}
public class Hazard {
 public RectangleF Bounds;public int Kind;public float Timer;public bool Active;public float Life;
}
 public class Game {
  public const float Floor=620,Width=1280,Height=720,EnemyMargin=44f;
  public const int FireballSound=0,HitSound=1,ClearSound=2,HurtSound=3,DashSound=4,FlaskSound=5;
  public const int MasterVolumeIndex=0,MusicVolumeIndex=1,SfxVolumeIndex=2;
  public Stage State=Stage.Title,HelpReturn=Stage.Title;
  public int Menu,AudioSlider;
  public bool SoundOn=true,AudioDragging;
  public readonly float[] Volume={1f,.8f,1f};
  public Action<int> RoomEntered;
 public readonly Controls Input=new Controls();
 public V Player,Velocity,Aim=new V(1,0);
 public bool Grounded,ExitOpen,Ending;
 public float Health=100,MaxHealth=100,Damage=26,FireRate=.15f,Speed=315;
 public float Invulnerable,DashTime,DashCooldown,FireCooldown,FlaskCooldown;
 public float ChainTime,MultiWindow,Shake,Elapsed,RoomTime;
 public int Chain,BestChain,Flasks=3;
 public int Room,Score,Kills,Seed,Pending,UpgradeCount;
 public string Toast="";public float ToastTime;
 public readonly List<RectangleF> Platforms=new List<RectangleF>();
 public readonly List<Enemy> Enemies=new List<Enemy>();
 public readonly List<Bullet> Bullets=new List<Bullet>();
 public readonly List<Particle> Particles=new List<Particle>();
 public readonly List<Hazard> Hazards=new List<Hazard>();
 Rng random=new Rng(1);float spawn;public Action<int> PlaySound;
 public bool Overcharged{get{return Chain>=8;}}
 public void Start(int seed){
  Seed=seed;Menu=0;HelpReturn=Stage.Title;random=new Rng(seed);Room=Score=Kills=Chain=BestChain=UpgradeCount=0;
  Health=MaxHealth=100;Damage=26;FireRate=.15f;Speed=315;Flasks=3;Elapsed=0;Ending=false;
  EnterRoom();State=Stage.Story;
 }
 public void ContinueStory(){if(State!=Stage.Story)return;State=Ending?Stage.Won:Stage.Playing;Menu=0;Input.Fire=false;Input.ClearEdges();}
 public void EnterRoom(){
  Platforms.Clear();Enemies.Clear();Bullets.Clear();Particles.Clear();Hazards.Clear();
  Player=new V(95,Floor-24);Velocity=new V();Aim=new V(1,0);Grounded=true;
  FireCooldown=DashTime=DashCooldown=FlaskCooldown=0;Invulnerable=1.2f;
  RoomTime=0;ExitOpen=false;Chain=0;ChainTime=0;MultiWindow=0;Shake=0;ToastTime=3;
  Platforms.Add(new RectangleF(0,Floor,Width,30));
  if(Room==0){
   Platforms.Add(new RectangleF(150,480,250,18));
   Platforms.Add(new RectangleF(505,390,270,18));
   Platforms.Add(new RectangleF(880,470,245,18));
   Pending=5;Toast="ASHEN NAVE / KEEP THE LAST CINDER BURNING";
  } else if(Room==1){
   Platforms.Add(new RectangleF(130,490,240,18));
   Platforms.Add(new RectangleF(480,370,280,18));
   Platforms.Add(new RectangleF(890,455,250,18));
   AddHazard(300,0,0);AddHazard(650,0,0);AddHazard(880,1,0);
   Pending=7;Toast="CINDER CLOISTER / BREAK THE ACOLYTE CHOIR";
  } else {
   Platforms.Add(new RectangleF(220,470,230,18));
   Platforms.Add(new RectangleF(830,470,230,18));
   AddHazard(430,0,0);AddHazard(980,0,0);
   Pending=0;Spawn(3,new V(980,500));
   Toast=Campaign.Boss+" / END THE LONG DUSK";
  }
   spawn=.8f;
   if(RoomEntered!=null)RoomEntered(Room);
  }
 void AddHazard(float x,int kind,float life){
  RectangleF b=kind==0?new RectangleF(x,Floor-16,64,16):new RectangleF(x,180,28,Floor-180);
  Hazards.Add(new Hazard{Bounds=b,Kind=kind,Timer=kind==1?1.8f:0,Active=kind==0,Life=life});
 }
 public void Spawn(int kind,V p){
  float hp=kind==3?1100:kind==0?58:kind==1?44:72;
  Enemies.Add(new Enemy{P=p,Kind=kind,Hp=hp,MaxHp=hp,Fire=kind==3?1.2f:.8f+(float)random.NextDouble()});
 }
 public void Pause(){if(State==Stage.Playing){State=Stage.Paused;Menu=0;}else if(State==Stage.Paused){State=Stage.Playing;Menu=0;}}
 public void MoveMenu(int delta,int count){if(count>0)Menu=((Menu+delta)%count+count)%count;}
 public void ShowHelp(){if(State==Stage.Help)return;HelpReturn=State;State=Stage.Help;Menu=0;Input.Fire=false;Input.ClearEdges();}
 public void CloseHelp(){State=HelpReturn==Stage.Help?Stage.Title:HelpReturn;Menu=0;Input.Fire=false;Input.ClearEdges();}
  public void ToggleSound(){SoundOn=!SoundOn;}
  public void ShowAudio(){if(State==Stage.Audio)return;HelpReturn=State;State=Stage.Audio;Menu=AudioSlider;Input.Fire=false;Input.ClearEdges();}
  public void CloseAudio(){State=HelpReturn==Stage.Audio?Stage.Title:HelpReturn;Menu=0;Input.Fire=false;Input.ClearEdges();}
  public void SetVolume(int index,float value){if(index<0||index>2)return;Volume[index]=Clamp(value,0,1);}
  public void AdjustVolume(int index,float delta){SetVolume(index,Volume[index]+delta);}
  public void ReturnToTitle(){State=Stage.Title;Menu=0;Input.Fire=false;Input.ClearEdges();}
 public void SelectUpgrade(int n){
  if(State!=Stage.Upgrade||n<0||n>2)return;
  if(n==0)Damage*=1.15f;
  else if(n==1){MaxHealth+=30;Health=Math.Min(MaxHealth,Health+55);}
  else{FireRate=Math.Max(.07f,FireRate*.9f);Speed=Math.Min(405,Speed+12);}
  UpgradeCount++;Room++;EnterRoom();State=Stage.Story;Menu=0;Sound(ClearSound);
 }
 public void Hurt(float amount){
  if(State!=Stage.Playing||Invulnerable>0)return;
  Health=Math.Max(0,Health-amount);Invulnerable=.85f;
  Chain=0;ChainTime=0;MultiWindow=0;Shake=9;
   Burst(Player,Palette.Blood,20);Sound(HurtSound);
  if(Health<=0)State=Stage.Dead;
 }
 void Sound(int n){if(PlaySound!=null)PlaySound(n);}
 public void Step(float dt){
  if(State!=Stage.Playing){Input.ClearEdges();return;}
  dt=Math.Min(dt,.025f);
  Elapsed+=dt;RoomTime+=dt;ToastTime-=dt;Invulnerable-=dt;
  DashCooldown-=dt;FireCooldown-=dt;FlaskCooldown-=dt;Shake=Math.Max(0,Shake-dt*28);
  ChainTime-=dt;if(ChainTime<=0){Chain=0;ChainTime=0;MultiWindow=0;}
  MultiWindow-=dt;if(MultiWindow<=0)MultiCountReset();
  Aim=(Input.Aim-(Player+new V(0,-13))).Unit;
  int horizontal=(Input.Right?1:0)-(Input.Left?1:0);
  if(Input.Jump&&Grounded){Velocity.Y=-780;Grounded=false;Burst(Player,Palette.Cyan,8);}
  if(Input.Down&&!Input.Jump&&Grounded&&Player.Y<Floor-26){Player.Y+=20;Grounded=false;}
   if(Input.Dash&&DashCooldown<=0){
    DashTime=.15f;DashCooldown=1.15f;Invulnerable=Math.Max(Invulnerable,DashTime+.5f);
    Velocity=new V((horizontal==0?(Aim.X>=0?1:-1):horizontal)*920,0);
    Burst(Player,Palette.Cyan,18);Sound(DashSound);
   }
  if(DashTime>0){DashTime-=dt;Burst(Player,Palette.Cyan,1);}
  else{Velocity.X=horizontal*Speed;Velocity.Y+=1750*dt;}
  float oldFoot=Player.Y+24;
  Player+=Velocity*dt;Player.X=Clamp(Player.X,22,Width-22);Grounded=false;
  foreach(RectangleF r in Platforms)
   if(Velocity.Y>=0&&oldFoot<=r.Y+1&&Player.Y+24>=r.Y&&Player.X+13>r.Left&&Player.X-13<r.Right){
    Player.Y=r.Y-24;Velocity.Y=0;Grounded=true;
   }
  if(Player.Y>820)Hurt(200);
  float rate=FireRate*(Overcharged?.85f:1f);
  if(Input.Fire&&FireCooldown<=0){
   FireCooldown=rate;
   float damage=Damage*(Overcharged?1.35f:1f);
   Bullets.Add(new Bullet{P=Player+new V(0,-13)+Aim*29,Velocity=Aim*1000,Life=1.3f,Damage=damage,Kind=0});
    Burst(Player+new V(0,-13)+Aim*32,Palette.Gold,3);Sound(FireballSound);
  }
  if(Input.Flask&&FlaskCooldown<=0&&Flasks>0){
   FlaskCooldown=.9f;Flasks--;
    Bullets.Add(new Bullet{P=Player+new V(0,-13)+Aim*24,Velocity=Aim*620+new V(0,-260),Life=2.2f,Damage=70,Gravity=900,Kind=1});
    Sound(FlaskSound);
  }
  if(Pending>0){
   spawn-=dt;
   if(spawn<=0){
    spawn=.8f+(float)random.NextDouble()*.75f;Pending--;
     int kind=Room==0?random.Next(0,2):random.Next(0,3);
     Spawn(kind,new V(random.Next(0,2)==0?65:1215,kind==1?random.Next(230,390):Floor-24));
   }
  }
  StepEnemies(dt);
  StepBullets(dt);
  StepHazards(dt);
  for(int i=Particles.Count-1;i>=0;i--){
   Particle p=Particles[i];p.Life-=dt;p.P+=p.Velocity*dt;p.Velocity*=1-dt*3;
   if(p.Life<=0)Particles.RemoveAt(i);
  }
  if(!ExitOpen&&Pending==0&&Enemies.Count==0){
   ExitOpen=true;Score+=500;Bullets.RemoveAll(delegate(Bullet b){return b.Hostile;});
   Toast="SANCTUM CLEAR / THE GATE IS OPEN";ToastTime=4;Sound(ClearSound);
  }
  if(ExitOpen&&Player.X>1150&&Input.Use){
   if(Room==Campaign.TotalRooms-1){Ending=true;State=Stage.Story;}
   else State=Stage.Upgrade;
  }
  Input.ClearEdges();
 }
 int multiCount;
 void MultiCountReset(){multiCount=0;}
 void StepEnemies(float dt){
  for(int i=Enemies.Count-1;i>=0;i--){
   Enemy e=Enemies[i];e.Time+=dt;e.Fire-=dt;e.Hit-=dt;
   V dir=(Player-e.P).Unit;
   if(e.Kind==0){e.P.X+=Math.Sign(dir.X)*(108+Room*24)*dt;e.P.Y=Floor-24;}
   else if(e.Kind==1){e.P+=dir*122*dt;e.P.Y=Clamp(e.P.Y,170,590);}
   else if(e.Kind==2){
    e.P.X+=Math.Sign(dir.X)*(Math.Abs(dir.X)>.35f?-38:48)*dt;
    e.P.Y=500+(float)Math.Sin(e.Time*1.7f)*26;
   } else {
    float phase=e.Hp/e.MaxHp;
    e.P.X=930+(float)Math.Sin(e.Time*(.55f+(phase<.34f?.35f:0)))*225;
    e.P.Y=phase<.34f?455+(float)Math.Sin(e.Time*2.1f)*62:phase<.67f?480+(float)Math.Sin(e.Time*1.4f)*38:515;
    if(e.Fire<=0){
     if(phase<.34f){
      e.Fire=.58f;Shake=Math.Max(Shake,5);
      AddHazard(300+(float)random.NextDouble()*600,0,1.4f);
      Fan(e.P,dir,5,.19f,315,16);
      } else if(phase<.67f){
       e.Fire=.92f;e.Mode=(e.Mode+1)%2;
       if(e.Mode==0)Fan(e.P,dir,5,.16f,300,15);
       else{Fan(e.P,new V(dir.X,0),2,.06f,520,17);Fan(e.P+new V(0,28),dir,3,.12f,420,15);}
      } else {
       e.Fire=1.25f;Fan(e.P,dir,3,.17f,255,14);
      }
     }
    }
    e.P.X=Clamp(e.P.X,EnemyMargin,Width-EnemyMargin);
    if(e.Kind<3)e.P.Y=Clamp(e.P.Y,170,Floor-24);
    if(e.Kind<3&&e.Fire<=0&&e.Kind==2){
     e.Fire=2.1f;
     Bullets.Add(new Bullet{P=e.P,Velocity=dir*260,Life=5,Hostile=true,Damage=12,Kind=0});
    }
    if((Player-e.P).Length<(e.Kind==3?66:34))Hurt(e.Kind==3?22:e.Kind==2?12:11);
  }
 }
 void Fan(V origin,V dir,int count,float spread,float speed,float damage){
  float angle=(float)Math.Atan2(dir.Y,dir.X);
  for(int n=0;n<count;n++){
   float a=angle+(n-(count-1)*.5f)*spread;
   Bullets.Add(new Bullet{P=origin,Velocity=new V((float)Math.Cos(a),(float)Math.Sin(a))*speed,Life=5,Hostile=true,Damage=damage,Kind=0});
  }
  Sound(1);
 }
 void StepBullets(float dt){
  for(int i=Bullets.Count-1;i>=0;i--){
   Bullet b=Bullets[i];V old=b.P;
   if(b.Gravity>0)b.Velocity.Y+=b.Gravity*dt;
   b.P+=b.Velocity*dt;b.Life-=dt;
   bool remove=b.Life<=0||b.P.X<0||b.P.X>Width||b.P.Y<80||b.P.Y>Floor+20;
   if(b.Kind==1&&b.P.Y>=Floor-6&&b.Velocity.Y>0){Explode(b.P,b.Damage);remove=true;}
   if(b.Hostile){
    if(SegmentDistance(old,b.P,Player)<21){Hurt(b.Damage);remove=true;}
   } else if(b.Kind==0){
    for(int j=Enemies.Count-1;j>=0;j--){
     Enemy e=Enemies[j];
     if(SegmentDistance(old,b.P,e.P)<(e.Kind==3?53:24)){
      e.Hp-=b.Damage;e.Hit=.09f;Burst(b.P,Palette.Ember,6);remove=true;
      if(e.Hp<=0){KillEnemy(j,e);}break;
     }
    }
   }
   if(remove)Bullets.RemoveAt(i);
  }
 }
 void KillEnemy(int index,Enemy e){
  Enemies.RemoveAt(index);Kills++;Score+=100*Math.Min(Chain+1,10);
  Chain++;BestChain=Math.Max(BestChain,Chain);ChainTime=4.2f;
  MultiWindow=.55f;multiCount++;
  if(multiCount>=2){ChainTime+=1.4f;multiCount=0;}
  Health=Math.Min(MaxHealth,Health+3);
  Burst(e.P,e.Kind==3?Palette.Gold:Palette.Ember,32);Shake=4;Sound(1);
 }
 void Explode(V p,float damage){
  Burst(p,Palette.Ember,30);Shake=7;Sound(1);
  for(int j=Enemies.Count-1;j>=0;j--){
   Enemy e=Enemies[j];
   if((e.P-p).Length<(e.Kind==3?105:92)){
    e.Hp-=damage;e.Hit=.12f;
    if(e.Hp<=0)KillEnemy(j,e);
   }
  }
 }
 void StepHazards(float dt){
  for(int i=Hazards.Count-1;i>=0;i--){
   Hazard h=Hazards[i];
   if(h.Life>0){h.Life-=dt;if(h.Life<=0){Hazards.RemoveAt(i);continue;}}
   if(h.Kind==1){
    h.Timer-=dt;
    if(h.Timer<=0){h.Active=!h.Active;h.Timer=h.Active?1.15f:2.05f;if(h.Active)Sound(1);}
   }
   if(h.Active&&Player.X+13>h.Bounds.Left&&Player.X-13<h.Bounds.Right&&Player.Y+24>h.Bounds.Top&&Player.Y-24<h.Bounds.Bottom)
    Hurt(h.Kind==1?18:14);
  }
 }
 public void Burst(V p,Color c,int n){
  for(int i=0;i<n;i++){
   float life=.18f+(float)random.NextDouble()*.4f;
   Particles.Add(new Particle{P=p,Velocity=new V((float)random.NextDouble()*360-180,(float)random.NextDouble()*360-180),Color=c,Life=life,Max=life});
  }
 }
 public static float Clamp(float x,float lo,float hi){return Math.Max(lo,Math.Min(hi,x));}
 public static float SegmentDistance(V a,V b,V p){
  V v=b-a;float d=v.X*v.X+v.Y*v.Y;
  float t=d<.001f?0:Clamp(((p.X-a.X)*v.X+(p.Y-a.Y)*v.Y)/d,0,1);
  return (p-(a+v*t)).Length;
 }
}
}
