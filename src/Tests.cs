using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace Ashenbound {
public static class Tests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks++;}
 static void Steps(Game g,int n){for(int i=0;i<n;i++)g.Step(1f/120);}
 static Game Fresh(){Game g=new Game();g.Start(12345);g.ContinueStory();return g;}
 static void Image(Renderer r,Game g,string name){
  using(Bitmap b=new Bitmap(1280,720))using(Graphics gr=Graphics.FromImage(b)){
   r.Draw(gr,g,1280,720,2.5f);b.Save(Path.Combine("Tests",name+".png"));
   Check(b.GetPixel(20,20).ToArgb()!=Color.Black.ToArgb(),"render "+name);
  }
 }
 static void KillAll(Game g){
  while(g.Enemies.Count>0){
   Enemy e=g.Enemies[0];
   g.Bullets.Add(new Bullet{P=e.P,Velocity=new V(0,0),Life=.01f,Damage=5000,Kind=0});
   g.Step(1f/120);
  }
 }
 public static int Run(){
  checks=0;Directory.CreateDirectory("Tests");
  try{
   Game g=new Game();g.Start(12345);
   Check(g.State==Stage.Story,"run opens story");
   V pos=g.Player;Steps(g,60);
   Check(g.RoomTime==0&&g.Player.X==pos.X,"story freezes combat");
   g.ContinueStory();Check(g.State==Stage.Playing,"story continues to combat");
   Check(g.Room==0&&g.Pending==5&&g.Grounded,"run initializes");
   Check(g.Platforms.Count==4&&g.Hazards.Count==0,"nave layout");
   Game same=new Game();same.Start(12345);same.ContinueStory();Steps(same,120);
   Check(same.Enemies.Count==1,"seeded spawn occurs");
   Game sameAgain=new Game();sameAgain.Start(12345);sameAgain.ContinueStory();Steps(sameAgain,120);
   Check(sameAgain.Enemies.Count==same.Enemies.Count&&sameAgain.Enemies[0].Kind==same.Enemies[0].Kind,"seeded spawn repeatable");
   bool sawWalker=false,sawWisp=false;
   for(int seed=0;seed<32;seed++){
    Game v=new Game();v.Start(seed);v.ContinueStory();Steps(v,120);
    if(v.Enemies.Count>0){if(v.Enemies[0].Kind==0)sawWalker=true;else sawWisp=true;}
   }
   Check(sawWalker&&sawWisp,"seeds vary encounters");

   g=Fresh();g.Input.Right=true;Steps(g,60);g.Input.Right=false;
   Check(g.Player.X>240,"movement");
   g.Input.Jump=true;float startY=g.Player.Y,minY=startY;
   for(int i=0;i<110;i++){g.Step(1f/120);minY=Math.Min(minY,g.Player.Y);}
   Check(startY-minY>165,"jump reaches ledge");
   Check(g.Grounded&&g.Player.Y<570,"landing on ledge");
   g.Input.Down=true;Steps(g,55);g.Input.Down=false;
   Check(g.Player.Y==Game.Floor-24,"drop through ledge");

   g=Fresh();g.Invulnerable=0;g.Input.Dash=true;g.Step(1f/120);
   Check(g.DashCooldown>1&&g.Invulnerable>0,"dash grants invulnerability");
   float hp=g.Health;g.Hurt(20);Check(g.Health==hp,"dash blocks damage");
   Steps(g,40);g.Invulnerable=0;g.Hurt(12);Check(g.Health==88,"damage applied");
   g.Hurt(12);Check(g.Health==88,"damage grace period");

   g=Fresh();g.Pending=0;g.Spawn(0,new V(370,Game.Floor-24));
   g.Health=80;g.Input.Aim=new V(370,Game.Floor-24);g.Input.Fire=true;
   Steps(g,100);g.Input.Fire=false;
   Check(g.Kills==1&&g.Enemies.Count==0,"shooting kills enemy");
   Check(g.Chain==1&&g.BestChain==1&&g.Health==83,"chain heal and score");
   Steps(g,600);Check(g.Chain==0,"chain expires");
   Check(g.ExitOpen,"room clear opens gate");

   g=Fresh();g.Pending=0;g.Spawn(0,new V(370,Game.Floor-24));
   g.Bullets.Add(new Bullet{P=new V(370,Game.Floor-12),Velocity=new V(0,120),Life=1,Damage=70,Gravity=900,Kind=1});
   Steps(g,12);Check(g.Kills==1,"cinder flask explodes");
   Check(g.Flasks==3,"test flask does not consume stock");

   g=Fresh();g.Room=1;g.EnterRoom();g.ContinueStory();g.Invulnerable=0;
   g.Player=new V(302,Game.Floor-24);
   float before=g.Health;Steps(g,5);
   Check(g.Health<before,"spike hazard damages player");

   g=Fresh();g.Player=new V(1190,Game.Floor-24);g.Pending=0;g.Enemies.Clear();g.ExitOpen=true;
   g.Input.Use=true;g.Step(1f/120);
   Check(g.State==Stage.Upgrade,"gate enters upgrade");
   float damage=g.Damage;g.SelectUpgrade(0);
   Check(g.Room==1&&g.Damage>damage&&g.UpgradeCount==1&&g.State==Stage.Story,"damage upgrade advances");
   g.ContinueStory();g.State=Stage.Upgrade;g.Health=30;g.SelectUpgrade(1);
   Check(g.Room==2&&g.MaxHealth==130&&g.Health==85,"health upgrade");
   g.ContinueStory();Check(g.Room==2&&g.Enemies.Count==1&&g.Enemies[0].Kind==3,"boss room");

   g=Fresh();g.Room=2;g.EnterRoom();g.ContinueStory();
   Enemy boss=g.Enemies[0];boss.Fire=.001f;boss.Hp=boss.MaxHp*.8f;Steps(g,10);
   Check(g.Bullets.Exists(delegate(Bullet b){return b.Hostile;}),"boss fires projectiles");
   boss.Hp=boss.MaxHp*.3f;boss.Fire=.001f;int hazards=g.Hazards.Count;Steps(g,10);
   Check(g.Hazards.Count>hazards,"enraged boss summons spikes");
   Check(g.Overcharged==false,"chain begins uncharged");
   g.Chain=8;Check(g.Overcharged,"high chain overcharges weapon");

   g=Fresh();g.Pause();pos=g.Player;Steps(g,50);
   Check(g.Player.X==pos.X&&g.State==Stage.Paused,"pause freezes simulation");
   g.Pause();Check(g.State==Stage.Playing,"pause resumes");

   g=Fresh();g.Health=g.MaxHealth=10000;
   for(int room=0;room<Campaign.TotalRooms;room++){
    int frames=0;
    while(!g.ExitOpen&&frames++<14000){KillAll(g);g.Step(1f/120);}
    Check(g.ExitOpen,"combat clears room "+room);
    g.Input.Fire=false;g.Player=new V(1190,Game.Floor-24);g.Input.Use=true;g.Step(1f/120);
    if(room<Campaign.TotalRooms-1){
     Check(g.State==Stage.Upgrade,"room gate "+room);
     g.SelectUpgrade(room%3);Check(g.State==Stage.Story,"next story "+room);
     g.ContinueStory();
    } else Check(g.Ending&&g.State==Stage.Story,"final gate opens epilogue");
   }
   g.ContinueStory();Check(g.State==Stage.Won,"epilogue closes to victory");
   Check(g.Kills==13,"campaign encounters completed");

   g=Fresh();g.Invulnerable=0;g.Hurt(150);
   Check(g.State==Stage.Dead&&g.Health==0,"death state");
   g.Start(4);Check(g.State==Stage.Story&&g.Score==0&&g.Health==100&&!g.Ending,"restart resets run");
   Check(Math.Abs(Game.SegmentDistance(new V(0,0),new V(100,0),new V(50,5))-5)<.01,"swept collision");
   Check(Campaign.Names.Length==3&&Campaign.Scenes.Length==4&&Campaign.Speakers.Length==4,"campaign content complete");

   using(Font font=new Font("Consolas",19,FontStyle.Regular,GraphicsUnit.Pixel))
   using(Bitmap bitmap=new Bitmap(1,1))using(Graphics graphics=Graphics.FromImage(bitmap)){
    foreach(string scene in Campaign.Scenes)Check(graphics.MeasureString(scene,font,626).Height<324,"story fits page");
   }

   using(Renderer r=new Renderer()){
    g=new Game();Image(r,g,"title");
    g.Start(9);Image(r,g,"story");g.ContinueStory();
    g.Spawn(0,new V(410,Game.Floor-24));g.Spawn(1,new V(810,320));g.Spawn(2,new V(1030,Game.Floor-24));
    g.Chain=5;g.ChainTime=3;Image(r,g,"combat");
    g.State=Stage.Upgrade;Image(r,g,"upgrade");
    for(int room=0;room<3;room++){
     g.Room=room;g.EnterRoom();g.State=Stage.Story;Image(r,g,"room"+room+"-story");
     g.ContinueStory();Image(r,g,"room"+room+"-world");
    }
    g.Room=2;g.EnterRoom();g.State=Stage.Playing;Image(r,g,"boss");
    g.Ending=true;g.State=Stage.Story;Image(r,g,"epilogue");g.ContinueStory();Image(r,g,"victory");
    g.State=Stage.Dead;Image(r,g,"death");g.State=Stage.Paused;Image(r,g,"pause");
    using(Bitmap b=new Bitmap(960,600))using(Graphics gr=Graphics.FromImage(b)){
     r.Draw(gr,g,960,600,2);b.Save("Tests/resized.png");
     Check(b.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"letterboxing");
    }
   }

   using(GameWindow window=new GameWindow()){
    window.Show();Application.DoEvents();
    System.Reflection.MethodInfo key=typeof(Control).GetMethod("OnKeyDown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    System.Reflection.FieldInfo field=typeof(GameWindow).GetField("game",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    Action<Keys> press=delegate(Keys k){key.Invoke(window,new object[]{new KeyEventArgs(k)});};
    press(Keys.Enter);Game model=(Game)field.GetValue(window);
    Check(model.State==Stage.Story,"window starts story");
    System.Reflection.MethodInfo mouse=typeof(Control).GetMethod("OnMouseDown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    mouse.Invoke(window,new object[]{new MouseEventArgs(MouseButtons.Left,1,900,650,0)});
    Check(model.State==Stage.Playing,"mouse turns story page");
    press(Keys.J);Check(model.State==Stage.Story,"journal reopens scene");
    press(Keys.Enter);Check(model.State==Stage.Playing&&model.Room==0,"journal resumes same room");
    press(Keys.Escape);Check(model.State==Stage.Paused,"window pauses");
    press(Keys.Escape);Check(model.State==Stage.Playing,"window resumes");
    window.ClientSize=new Size(960,600);Application.DoEvents();
    using(Bitmap b=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(b,window.ClientRectangle);b.Save("Tests/window.png");}
    window.Close();
   }

   File.WriteAllText("Tests/results.txt","PASS: "+checks+" checks. Procedural pixel rendering, 3 sanctums, story gating, boss phases, combo, hazards, flasks, full combat run and native window controls.\n");
   return 0;
  }catch(Exception e){
   File.WriteAllText("Tests/results.txt","FAIL after "+checks+" checks: "+e);
   Console.WriteLine("FAIL after "+checks+" checks: "+e);
   return 1;
  }
 }
}
}
