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
  static void Choose(Game g,RelicKind kind){
   for(int i=0;i<g.UpgradeChoices.Length;i++)if(g.UpgradeChoices[i]==kind){g.SelectUpgrade(i);return;}
   g.UpgradeChoices[0]=kind;g.SelectUpgrade(0);
  }
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
    Check(SoundData.Rate==22050&&SoundData.LoopSeconds==16&&SoundData.TrackCount==5,"pcm loop format");
     Check(PcmMixer.WaveFormatBytes==18,"wasapi pcm format layout");
     Check(PcmMixer.WasapiBufferDuration<500000,"wasapi buffer stays under half second");
    bool seams=true;
    for(int t=0;t<SoundData.TrackCount;t++)if(SoundData.MusicSeam(t)>.03f)seams=false;
    Check(seams,"pcm music loops without audible seam");
    float fireStart=SoundData.EffectSample(0,Game.FireballSound,0);
    float fireEnd=SoundData.EffectSample(0,Game.FireballSound,SoundData.EffectLength(Game.FireballSound)-1);
    bool firePeak=false;
    for(int i=0;i<SoundData.EffectLength(Game.FireballSound);i++)if(Math.Abs(SoundData.EffectSample(0,Game.FireballSound,i))>.15f)firePeak=true;
    Check(Math.Abs(fireStart)<.02f&&Math.Abs(fireEnd)<.02f&&firePeak,"fireball burst is shaped without clicks");
    using(PcmMixer mixer=new PcmMixer(false)){
     mixer.SetMusic(Audio.MenuTrack);float[] buffer=new float[512];mixer.Render(buffer);
     Check(mixer.MusicPlaying&&mixer.ActiveTrack==Audio.MenuTrack&&!mixer.CrossfadeActive,"pcm music starts");
     mixer.SetMusic(Audio.DawnTrack);mixer.Render(buffer);
     Check(mixer.CrossfadeActive&&mixer.ActiveTrack==Audio.DawnTrack&&mixer.FadeProgress>0&&mixer.FadeProgress<1,"pcm track change crossfades");
     mixer.Play(Game.FireballSound);mixer.Render(buffer);
     Check(mixer.ActiveVoices==1,"pcm fireball overlaps music");
     float[] tail=new float[128];int renders=SoundData.EffectLength(Game.FireballSound)/128+4;
     for(int i=0;i<renders;i++)mixer.Render(tail);
     Check(mixer.ActiveVoices==0,"pcm voices expire");
      mixer.SetMusic(-1);mixer.Render(buffer);
      Check(!mixer.MusicPlaying&&mixer.ActiveTrack==-1,"pcm music stops cleanly");
      mixer.SetMusic(Audio.MenuTrack);mixer.Render(buffer);
      mixer.SetMusic(-1);mixer.Render(buffer);
      float stopFade=mixer.FadeProgress;
      mixer.SetMusic(-1);mixer.SetMusic(-1);mixer.Render(buffer);
      Check(mixer.FadeProgress>stopFade&&mixer.CrossfadeActive,"repeated stop keeps fading out");
      mixer.SetGains(1,.5f,.25f);mixer.SetMuted(true);
     Check(mixer.MasterGain==0&&mixer.MusicGain==0&&mixer.SfxGain==0,"pcm mute silences buses");
    }
    Game pilgrim=new Game();pilgrim.Start(1,0);pilgrim.ContinueStory();pilgrim.Enemies.Clear();
    Game nightmare=new Game();nightmare.Start(1,3);nightmare.ContinueStory();nightmare.Enemies.Clear();
    pilgrim.Spawn(0,new V(400,Game.Floor-24));nightmare.Spawn(0,new V(400,Game.Floor-24));
    Check(nightmare.Enemies[0].Hp>pilgrim.Enemies[0].Hp*2,"difficulty scales enemy health");
    nightmare.Invulnerable=0;float nightmareBefore=nightmare.Health;nightmare.Hurt(10);
    Check(Math.Abs((nightmareBefore-nightmare.Health)-19)<.001f,"difficulty scales incoming damage");
    Game clamped=new Game();clamped.Start(1,-5);Check(clamped.Difficulty==0,"difficulty clamps low");
    clamped.Start(1,99);Check(clamped.Difficulty==Campaign.Difficulties.Length-1,"difficulty clamps high");
    pilgrim.Start(2);Check(pilgrim.Difficulty==0,"single-argument start keeps chosen difficulty");
    bool sawWalker=false,sawWisp=false;
   for(int seed=0;seed<32;seed++){
    Game v=new Game();v.Start(seed);v.ContinueStory();Steps(v,120);
    if(v.Enemies.Count>0){if(v.Enemies[0].Kind==0)sawWalker=true;else sawWisp=true;}
   }
     Check(sawWalker&&sawWisp,"seeds vary encounters");
     Check(Campaign.Upgrades.Length==9,"nine relics defined");
     Game relicRoll=Fresh();
     Check(relicRoll.UpgradeChoices.Length==3,"three relics offered");
     bool distinct=true,known=true;
     for(int i=0;i<relicRoll.UpgradeChoices.Length;i++){
      for(int j=i+1;j<relicRoll.UpgradeChoices.Length;j++)if(relicRoll.UpgradeChoices[i]==relicRoll.UpgradeChoices[j])distinct=false;
      bool inPool=false;
      for(int k=0;k<Campaign.Upgrades.Length;k++)if(Campaign.Upgrades[k].Kind==relicRoll.UpgradeChoices[i])inPool=true;
      if(!inPool)known=false;
     }
     Check(distinct,"relic offers are distinct");Check(known,"relic offers come from the pool");
     string firstRoll=relicRoll.UpgradeChoices[0]+"/"+relicRoll.UpgradeChoices[1]+"/"+relicRoll.UpgradeChoices[2];
     bool rollsVary=false;
     for(int seed=0;seed<64;seed++){
      Game roll=new Game();roll.Start(seed);
      string set=roll.UpgradeChoices[0]+"/"+roll.UpgradeChoices[1]+"/"+roll.UpgradeChoices[2];
      if(set!=firstRoll)rollsVary=true;
     }
      Check(rollsVary,"relic offers vary by seed");
      Check(!Campaign.UpgradeFor(RelicKind.Split).Stackable&&Campaign.UpgradeFor(RelicKind.Split).MaxStacks==1,"split is one-time");
      Check(Campaign.UpgradeFor(RelicKind.Damage).Stackable&&Campaign.UpgradeFor(RelicKind.Damage).MaxStacks==99,"damage stacks");
      Check(Campaign.UpgradeFor(RelicKind.Swift).MaxStacks==8&&Campaign.UpgradeFor(RelicKind.Pierce).MaxStacks==3,"capped relics declare limits");
      Check(Campaign.UpgradeFor(RelicKind.Dash).MaxStacks==4&&Campaign.UpgradeFor(RelicKind.Ward).MaxStacks==2,"dash and ward declare limits");
      Check(Campaign.UpgradeFor(RelicKind.None)==null,"empty relic resolves to no definition");
      bool ownedHidden=true,stackSeen=false;
      for(int seed=0;seed<64;seed++){
       Game roll=new Game();roll.Start(seed);roll.RelicStacks[(int)RelicKind.Split]=1;roll.RollUpgrades();
       for(int i=0;i<roll.UpgradeChoices.Length;i++){
        if(roll.UpgradeChoices[i]==RelicKind.Split)ownedHidden=false;
        if(roll.UpgradeChoices[i]==RelicKind.Pierce)stackSeen=true;
       }
      }
      Check(ownedHidden,"owned non-stackable relics are not offered");Check(stackSeen,"stackable relics can be offered again");
      Game capped=Fresh();capped.RelicStacks[(int)RelicKind.Pierce]=3;
      bool cappedHidden=true;
      for(int seed=0;seed<64;seed++){
       Game roll=new Game();roll.Start(seed);roll.RelicStacks[(int)RelicKind.Pierce]=3;roll.RollUpgrades();
       for(int i=0;i<roll.UpgradeChoices.Length;i++)if(roll.UpgradeChoices[i]==RelicKind.Pierce)cappedHidden=false;
      }
      Check(cappedHidden,"maxed stackable relics are not offered");
      Game dawn=Fresh();dawn.Ending=true;dawn.State=Stage.Story;dawn.ContinueStory();
     Check(dawn.State==Stage.Sunrise&&dawn.SunriseTime==0,"epilogue opens sunrise");
     Steps(dawn,60);Check(dawn.State==Stage.Sunrise&&dawn.SunriseTime>0,"sunrise advances");
     Steps(dawn,700);Check(dawn.State==Stage.Won,"sunrise finishes after six seconds");
     dawn.SkipSunrise();Check(dawn.State==Stage.Won,"skip is safe after sunrise");

    g=Fresh();g.Pending=0;g.Invulnerable=100000;g.Player=new V(Game.Width-60,Game.Floor-24);
    for(int kind=0;kind<3;kind++){
     g.Enemies.Clear();g.ExitOpen=false;
     g.Spawn(kind,new V(Game.Width+160,kind==1?320:Game.Floor-24));
     Steps(g,2);
     Check(g.Enemies.Count==1&&g.Enemies[0].P.X>=Game.EnemyMargin&&g.Enemies[0].P.X<=Game.Width-Game.EnemyMargin,"enemy "+kind+" stays on screen");
     Check(g.Enemies[0].P.Y>=170&&g.Enemies[0].P.Y<=Game.Floor-24,"enemy "+kind+" stays in arena");
     KillAll(g);
     Check(g.ExitOpen,"bounded enemy "+kind+" can be cleared");
    }
    g.Enemies.Clear();g.ExitOpen=false;g.Player=new V(1240,Game.Floor-24);
    g.Spawn(2,new V(1215,500));Steps(g,900);
    Check(g.Enemies.Count==1&&g.Enemies[0].P.X<=Game.Width-Game.EnemyMargin,"backing acolyte held inside right edge");
    KillAll(g);Check(g.ExitOpen,"edge acolyte soft-lock cleared");

    g=Fresh();g.Input.Right=true;Steps(g,60);g.Input.Right=false;
   Check(g.Player.X>240,"movement");
   g.Input.Jump=true;float startY=g.Player.Y,minY=startY;
   for(int i=0;i<110;i++){g.Step(1f/120);minY=Math.Min(minY,g.Player.Y);}
   Check(startY-minY>165,"jump reaches ledge");
   Check(g.Grounded&&g.Player.Y<570,"landing on ledge");
   g.Input.Down=true;Steps(g,55);g.Input.Down=false;
   Check(g.Player.Y==Game.Floor-24,"drop through ledge");

    g=Fresh();g.Invulnerable=0;g.Input.Dash=true;g.Step(1f/120);
    Check(g.DashCooldown>1&&g.Invulnerable>=.64f,"dash grants invulnerability");
    float hp=g.Health;g.Hurt(20);Check(g.Health==hp,"dash blocks damage");
    Steps(g,60);g.Hurt(20);Check(g.Health==hp,"dash grace outlasts dash");
    g.Invulnerable=0;g.Hurt(12);Check(g.Health==88,"damage applied");
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
    float damage=g.Damage;Choose(g,RelicKind.Damage);
    Check(g.Room==1&&g.Damage>damage&&g.UpgradeCount==1&&g.State==Stage.Story,"damage upgrade advances");
    g.ContinueStory();g.State=Stage.Upgrade;g.Health=30;Choose(g,RelicKind.Vitality);
    Check(g.Room==2&&g.MaxHealth==130&&g.Health==85,"health upgrade");
    g.ContinueStory();Check(g.Room==2&&g.Enemies.Count==1&&g.Enemies[0].Kind==3,"boss room");

    Game relic=Fresh();relic.State=Stage.Upgrade;relic.Pending=0;relic.Enemies.Clear();
    float baseRate=relic.FireRate,baseSpeed=relic.Speed;Choose(relic,RelicKind.Swift);
    Check(relic.FireRate<baseRate&&relic.Speed>baseSpeed,"swift relic improves fire and movement");

    relic=Fresh();relic.State=Stage.Upgrade;Choose(relic,RelicKind.Pierce);
    Check(relic.Pierce==1,"pierce relic is applied");
    relic.State=Stage.Playing;relic.Pending=0;relic.Enemies.Clear();relic.Invulnerable=100;
    relic.Spawn(0,new V(400,Game.Floor-24));relic.Spawn(0,new V(500,Game.Floor-24));
    relic.Bullets.Add(new Bullet{P=new V(380,Game.Floor-24),Velocity=new V(1000,0),Life=1,Damage=70,Kind=0,Pierce=relic.Pierce});
    Steps(relic,24);Check(relic.Kills==2&&relic.Enemies.Count==0,"pierce fireball hits multiple foes");

    relic=Fresh();relic.State=Stage.Upgrade;Choose(relic,RelicKind.Split);
    relic.State=Stage.Playing;relic.Pending=0;relic.Bullets.Clear();relic.Input.Fire=true;relic.Step(1f/120);relic.Input.Fire=false;
    Check(relic.Bullets.Count==3,"split relic fires three embers");

    relic=Fresh();relic.State=Stage.Upgrade;Choose(relic,RelicKind.Flask);
    Check(relic.Flasks==4&&relic.FlaskRadius>1,"flask relic adds stock and blast radius");

    relic=Fresh();relic.State=Stage.Upgrade;Choose(relic,RelicKind.Dash);
    relic.State=Stage.Playing;relic.Input.Dash=true;relic.Step(1f/120);relic.Input.Dash=false;
    Check(relic.DashCooldown>0&&relic.DashCooldown<1.15f,"dash relic shortens recovery");

    relic=Fresh();relic.State=Stage.Upgrade;Choose(relic,RelicKind.Siphon);
    relic.State=Stage.Playing;relic.Health=50;relic.Pending=0;relic.Enemies.Clear();relic.Invulnerable=100;
    relic.Spawn(0,new V(370,Game.Floor-24));KillAll(relic);
    Check(relic.Health==57,"siphon relic increases takedown healing");

    relic=Fresh();relic.State=Stage.Upgrade;Choose(relic,RelicKind.Ward);
    relic.State=Stage.Playing;relic.Invulnerable=0;float wardHp=relic.Health;relic.Hurt(20);
    Check(Math.Abs((wardHp-relic.Health)-16)<.001f,"ward relic reduces incoming damage");

    Game stacks=Fresh();Check(stacks.OwnedStacks(RelicKind.Damage)==0,"new run has no owned relics");
    stacks.State=Stage.Upgrade;Choose(stacks,RelicKind.Damage);Check(stacks.OwnedStacks(RelicKind.Damage)==1,"selecting a relic records a stack");
    stacks.Start(999);Check(stacks.OwnedStacks(RelicKind.Damage)==0,"new run clears owned relics");
    Game dashStacks=Fresh();
    for(int i=1;i<=4;i++){dashStacks.ApplyUpgrade(RelicKind.Dash);Check(Math.Abs(dashStacks.DashCooldownScale-Math.Max(.5f,1f-.125f*i))<.001f,"dash stack "+i+" reduces recovery");}
    dashStacks.ApplyUpgrade(RelicKind.Dash);Check(dashStacks.OwnedStacks(RelicKind.Dash)==4,"dash cannot exceed four stacks");

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
    g.ContinueStory();Check(g.State==Stage.Sunrise,"epilogue closes to sunrise");
    g.SkipSunrise();Check(g.State==Stage.Won,"sunrise closes to victory");
    Check(g.Kills==13,"campaign encounters completed");

   g=Fresh();g.Invulnerable=0;g.Hurt(150);
   Check(g.State==Stage.Dead&&g.Health==0,"death state");
    g.Start(4);Check(g.State==Stage.Story&&g.Score==0&&g.Health==100&&!g.Ending,"restart resets run");
    g=new Game();g.ShowAudio();
    Check(g.State==Stage.Audio&&g.Menu==0&&g.HelpReturn==Stage.Title,"audio opens from title");
    g.AdjustVolume(Game.MusicVolumeIndex,-.35f);
    Check(Math.Abs(g.Volume[Game.MusicVolumeIndex]-.45f)<.001f,"music slider adjusts");
    g.SetVolume(Game.MasterVolumeIndex,2);Check(g.Volume[Game.MasterVolumeIndex]==1,"volume clamps high");
    g.SetVolume(Game.SfxVolumeIndex,-1);Check(g.Volume[Game.SfxVolumeIndex]==0,"volume clamps low");
    g.CloseAudio();Check(g.State==Stage.Title,"audio returns to title");
    g.ShowAbilities();Check(g.State==Stage.Abilities&&g.HelpReturn==Stage.Title,"abilities opens from title");
    g.CloseAbilities();Check(g.State==Stage.Title,"abilities returns to title");
    g.ShowDifficulty();Check(g.State==Stage.Difficulty&&g.Menu==Campaign.DefaultDifficulty,"difficulty opens with default tier");
    g.CloseDifficulty();Check(g.State==Stage.Title,"difficulty returns to title");
    Check(Game.FireballSound==0&&Game.HitSound==1&&Game.ClearSound==2&&Game.HurtSound==3&&Game.DashSound==4&&Game.FlaskSound==5,"sound identifiers defined");
    Check(Math.Abs(new Game().Volume[Game.SfxVolumeIndex]-.65f)<.001f,"sfx defaults to 65 percent");
    Check(Math.Abs(Game.SegmentDistance(new V(0,0),new V(100,0),new V(50,5))-5)<.01,"swept collision");
   Check(Campaign.Names.Length==3&&Campaign.Scenes.Length==4&&Campaign.Speakers.Length==4,"campaign content complete");

   using(Font font=new Font("Georgia",17,FontStyle.Regular,GraphicsUnit.Pixel))
   using(Bitmap bitmap=new Bitmap(1,1))using(Graphics graphics=Graphics.FromImage(bitmap)){
    foreach(string scene in Campaign.Scenes)Check(graphics.MeasureString(scene,font,724).Height<240,"story fits wrapped page");
   }

   using(Renderer r=new Renderer()){
    g=new Game();Image(r,g,"title");
    Check(r.FontName=="Georgia","serif interface font");
    float storyLabel=r.TextWidth("> CONTINUE",20,true),storyHint=r.TextWidth("ENTER",14,false);
    Check(storyLabel+storyHint+52<=MenuLayout.Story[0].Width,"story hint fits");
    float endingLabel=r.TextWidth("WITNESS THE DAWN",20,true),endingHint=r.TextWidth("ENTER",14,false);
    Check(endingLabel+endingHint+52<=MenuLayout.Epilogue[0].Width,"epilogue hint fits");
    Check(MenuLayout.Items(Stage.Story,true)[0]==MenuLayout.Epilogue[0]&&MenuLayout.Items(Stage.Story,false)[0]==MenuLayout.Story[0],"epilogue owns its hit target");
    Check(MenuLayout.Items(Stage.Audio).Length==4,"audio menu has sliders and back");
    Check(MenuLayout.Title.Length==5&&MenuLayout.Abilities.Length==1&&MenuLayout.Difficulty.Length==5,"expanded menu hit targets");
    Check(MenuLayout.Items(Stage.Abilities).Length==1&&MenuLayout.Items(Stage.Difficulty).Length==5,"new menu states dispatch hit targets");
    Check(Campaign.Difficulties.Length==4&&Campaign.DefaultDifficulty==1,"four difficulty tiers with bearer default");
    float footerLeft=r.TextWidth("A/D MOVE  SPACE JUMP  MOUSE AIM + FIRE  SHIFT/RMB DASH  Q FLASK  E GATE  S DROP",13,false);
    float footerRight=r.TextWidth("ESC PAUSE  M MUTE  F11",13,false);
    Check(28+footerLeft+20<=1252-footerRight,"footer controls fit");
    g.Menu=1;Image(r,g,"title-selected");
    g.ShowHelp();Image(r,g,"help");g.CloseHelp();
    g.ShowAbilities();Image(r,g,"abilities");g.CloseAbilities();
    g.ShowDifficulty();Image(r,g,"difficulty");g.CloseDifficulty();
    g.Start(9);Image(r,g,"story");g.ContinueStory();
    g.Spawn(0,new V(410,Game.Floor-24));g.Spawn(1,new V(810,320));g.Spawn(2,new V(1030,Game.Floor-24));
     g.Chain=5;g.ChainTime=3;Image(r,g,"combat");
     Game fire=new Game();fire.Start(77);fire.ContinueStory();fire.Invulnerable=100;
     using(Bitmap blank=new Bitmap(1280,720),lit=new Bitmap(1280,720))
     using(Graphics blankGraphics=Graphics.FromImage(blank),litGraphics=Graphics.FromImage(lit)){
      r.Draw(blankGraphics,fire,1280,720,2.5f);
      fire.Bullets.Add(new Bullet{P=new V(640,300),Velocity=new V(800,0),Life=1,Damage=10,Kind=0});
      r.Draw(litGraphics,fire,1280,720,2.5f);
      Check(blank.GetPixel(640,300).ToArgb()!=lit.GetPixel(640,300).ToArgb(),"player fireball renders distinctly");
     }
      g.State=Stage.Upgrade;Image(r,g,"upgrade");
      foreach(UpgradeDef def in Campaign.Upgrades)Check(86+r.TextWidth(def.Name,24,true)+18<=MenuLayout.Upgrade[0].Width&&86+r.TextWidth(def.Detail,16,false)+18<=MenuLayout.Upgrade[0].Width,"relic card fits: "+def.Name);
      foreach(UpgradeDef def in Campaign.Upgrades)Check(68+r.TextWidth(def.Name,17,true)+18<=270&&18+r.TextWidth(def.Detail,14,false)+18<=270,"ability card fits: "+def.Name);
       foreach(UpgradeDef def in Campaign.Upgrades){
        string label=Renderer.RelicLabel(def,1,true),owned=Renderer.RelicLabel(def,def.MaxStacks,false);
        Check(!def.Stackable||label.StartsWith("STACKABLE"),"stackable relic label: "+def.Name);
        Check(def.Stackable||owned=="OWNED","owned relic label: "+def.Name);
        Check(18+r.TextWidth(label,13,false)+18<=MenuLayout.Upgrade[0].Width,"relic stack label fits: "+def.Name);
        Check(14+r.TextWidth(label,12,false)+14<=270,"ability stack label fits: "+def.Name);
       }
      foreach(DifficultyDef d in Campaign.Difficulties)Check(86+r.TextWidth(d.Name,21,true)+18<=MenuLayout.Difficulty[0].Width&&86+r.TextWidth(d.Detail,13,false)+18<=MenuLayout.Difficulty[0].Width,"difficulty card fits: "+d.Name);
      Check(37+13<=MenuLayout.Difficulty[0].Height&&39+14<=MenuLayout.Difficulty[0].Height,"difficulty card text stays inside compact frame");
    for(int room=0;room<3;room++){
     g.Room=room;g.EnterRoom();g.State=Stage.Story;Image(r,g,"room"+room+"-story");
     g.ContinueStory();Image(r,g,"room"+room+"-world");
    }
    g.Room=2;g.EnterRoom();g.State=Stage.Playing;Image(r,g,"boss");
     g.Ending=true;g.State=Stage.Story;Image(r,g,"epilogue");g.ContinueStory();
     Check(g.State==Stage.Sunrise,"epilogue render opens sunrise");Image(r,g,"sunrise");
     r.ReduceMotion=true;Image(r,g,"sunrise-still");r.ReduceMotion=false;
     g.SkipSunrise();Check(g.State==Stage.Won,"sunrise render completes");Image(r,g,"victory");
    g.State=Stage.Dead;Image(r,g,"death");g.State=Stage.Paused;g.Menu=0;Image(r,g,"pause");g.Menu=1;Image(r,g,"pause-selected");
    using(Bitmap b=new Bitmap(960,600))using(Graphics gr=Graphics.FromImage(b)){
     r.Draw(gr,g,960,600,2);b.Save("Tests/resized.png");
     Check(b.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"letterboxing");
    }
   }

   using(GameWindow window=new GameWindow(false)){
    window.Show();Application.DoEvents();
    Check(window.CursorHidden,"window hides system cursor");
    System.Reflection.MethodInfo key=typeof(Control).GetMethod("OnKeyDown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    System.Reflection.FieldInfo field=typeof(GameWindow).GetField("game",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
     System.Reflection.FieldInfo audioField=typeof(GameWindow).GetField("audio",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
     System.Reflection.MethodInfo sync=typeof(GameWindow).GetMethod("SyncMusic");
     System.Reflection.MethodInfo mouse=typeof(Control).GetMethod("OnMouseDown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
     System.Reflection.MethodInfo mouseMove=typeof(Control).GetMethod("OnMouseMove",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
     System.Reflection.MethodInfo mouseUp=typeof(Control).GetMethod("OnMouseUp",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
     Action<Keys> press=delegate(Keys k){key.Invoke(window,new object[]{new KeyEventArgs(k)});};
     Action<int,int,MouseButtons> click=delegate(int x,int y,MouseButtons button){mouse.Invoke(window,new object[]{new MouseEventArgs(button,1,x,y,0)});};
     Action<int,int> drag=delegate(int x,int y){mouseMove.Invoke(window,new object[]{new MouseEventArgs(MouseButtons.Left,0,x,y,0)});};
     Action release=delegate{mouseUp.Invoke(window,new object[]{new MouseEventArgs(MouseButtons.Left,1,0,0,0)});};
    Game model=(Game)field.GetValue(window);
    Audio audio=(Audio)audioField.GetValue(window);
    Check(model.State==Stage.Title&&model.Menu==0,"window opens title menu");
     Check(!audio.AmbientPlaying&&audio.MenuPlaying&&audio.ActiveTrack==Audio.MenuTrack,"menu music plays at title");
     Check(Math.Abs(audio.SfxGain-.65f)<.001f,"default sfx gain");
    bool sound=model.SoundOn;press(Keys.M);Check(model.SoundOn!=sound,"window sound toggle");press(Keys.M);Check(model.SoundOn==sound,"window sound restored");
    press(Keys.S);Check(model.Menu==1,"menu keyboard navigation");
    press(Keys.W);Check(model.Menu==0,"menu navigation wraps");
    click(900,650,MouseButtons.Left);Check(model.State==Stage.Title,"click outside menu keeps title");
    click(900,341,MouseButtons.Left);Check(model.State==Stage.Help,"mouse activates field manual");
     press(Keys.Escape);Check(model.State==Stage.Title,"field manual returns to title");
      press(Keys.S);press(Keys.S);press(Keys.Enter);Check(model.State==Stage.Audio,"audio settings open");
      Check(audio.MenuPlaying,"menu music plays in audio settings");
      float musicBefore=model.Volume[Game.MusicVolumeIndex];
     press(Keys.S);press(Keys.A);
     Check(model.Volume[Game.MusicVolumeIndex]<musicBefore&&audio.MusicGain<musicBefore,"music slider lowers gain");
     press(Keys.D);
     click(900,370,MouseButtons.Left);Check(model.AudioDragging,"slider drag begins");
     drag(580,370);release();
     Check(!model.AudioDragging&&model.Volume[Game.MusicVolumeIndex]<.5f,"slider drag applies volume");
      press(Keys.Escape);Check(model.State==Stage.Title,"audio returns to title");
      press(Keys.S);press(Keys.S);press(Keys.S);press(Keys.Enter);
      Check(model.State==Stage.Abilities&&audio.MenuPlaying,"abilities manual opens");
      press(Keys.Escape);Check(model.State==Stage.Title,"abilities manual returns to title");
      press(Keys.Enter);Check(model.State==Stage.Difficulty&&model.Menu==Campaign.DefaultDifficulty,"new vigil opens difficulty");
      Check(audio.MenuPlaying&&!audio.AmbientPlaying,"menu music plays in difficulty");
      press(Keys.Escape);Check(model.State==Stage.Title,"difficulty returns to title");
      press(Keys.Enter);press(Keys.D1);
      Check(model.State==Stage.Story&&model.Difficulty==0,"number key starts pilgrim vigil");
      Check(audio.MenuPlaying&&!audio.AmbientPlaying,"menu music continues through story");
      click(900,650,MouseButtons.Left);Check(model.State==Stage.Playing,"mouse turns story page");
      Check(audio.AmbientPlaying&&audio.Theme==0&&audio.ActiveTrack==0,"nave ambient theme");
     model.Room=1;model.EnterRoom();Check(audio.Theme==1,"cloister ambient theme");
     model.Room=2;model.EnterRoom();Check(audio.Theme==2,"reliquary ambient theme");
      model.Room=0;model.EnterRoom();Check(audio.Theme==0,"ambient theme returns");
      model.State=Stage.Sunrise;sync.Invoke(window,null);
      Check(audio.DawnPlaying&&!audio.AmbientPlaying&&audio.ActiveTrack==Audio.DawnTrack,"dawn music plays at sunrise");
      model.State=Stage.Won;sync.Invoke(window,null);Check(audio.DawnPlaying,"dawn music continues into victory record");
      model.State=Stage.Playing;sync.Invoke(window,null);Check(audio.AmbientPlaying,"combat music resumes after sunrise");
      audio.Play(Game.FireballSound);Check(audio.SfxGain>=0,"fireball sound prepared");
     press(Keys.J);Check(model.State==Stage.Story,"journal reopens scene");
    press(Keys.Enter);Check(model.State==Stage.Playing&&model.Room==0,"journal resumes same room");
     press(Keys.M);Check(audio.Muted&&audio.MusicGain==0&&audio.AmbientPlaying,"mute silences ambient without stopping it");
     press(Keys.M);Check(!audio.Muted&&audio.AmbientPlaying&&audio.MusicGain>0,"unmute restores ambient gain");
    press(Keys.Escape);Check(model.State==Stage.Paused&&model.Menu==0,"window pauses");
    press(Keys.S);press(Keys.Enter);Check(model.State==Stage.Help&&model.HelpReturn==Stage.Paused,"pause opens field manual");
    press(Keys.Escape);Check(model.State==Stage.Paused,"field manual returns to pause");
    press(Keys.Escape);Check(model.State==Stage.Playing,"window resumes");
    window.ClientSize=new Size(960,600);Application.DoEvents();
    using(Bitmap b=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(b,window.ClientRectangle);b.Save("Tests/window.png");}
    window.Close();Check(!window.CursorHidden,"window restores system cursor");
   }

     File.WriteAllText("Tests/results.txt","PASS: "+checks+" checks. Procedural pixel rendering, 3 sanctums, dark-fantasy ruins, volume sliders, seamless mute-safe PCM crossfades and layered fireballs, dash grace, bounded enemies, measured Georgia menus, abilities manual, four difficulty tiers, story gating, boss phases, combo, hazards, flasks, full combat run, per-room dark-fantasy audio, fullscreen-ready native window and custom cursor.\n");
   return 0;
  }catch(Exception e){
   File.WriteAllText("Tests/results.txt","FAIL after "+checks+" checks: "+e);
   Console.WriteLine("FAIL after "+checks+" checks: "+e);
   return 1;
  }
 }
}
}
