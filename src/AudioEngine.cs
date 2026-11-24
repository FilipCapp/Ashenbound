using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
namespace Ashenbound {
 internal static class SoundData {
   public const int Rate=22050,LoopSeconds=16,LoopSamples=Rate*LoopSeconds,NoisePeriod=4410,TrackCount=5;
  static readonly float[][] music=BuildMusic();
  static readonly float[][][] effects=BuildEffects();
  static double Noise(int i){double x=Math.Sin(i*12.9898)*43758.5453;return (x-Math.Floor(x))*2-1;}
  static double AmbientNoise(int i){int n=i%NoisePeriod;return Noise(n)*Math.Sin(Math.PI*n/NoisePeriod);}
  static double Bell(double t,double f){return Math.Exp(-t*4.2)*(Math.Sin(2*Math.PI*f*t)+.42*Math.Sin(2*Math.PI*f*2.01*t)+.18*Math.Sin(2*Math.PI*f*3.02*t));}
  static double Drum(double t,double f,double decay){return Math.Sin(2*Math.PI*(f-35*Math.Min(1,t*4))*t)*Math.Exp(-t*decay);}
  static double Chord(double t,double a,double b,double c){return (Math.Sin(2*Math.PI*a*t)+Math.Sin(2*Math.PI*b*t)+Math.Sin(2*Math.PI*c*t)+.3*Math.Sin(2*Math.PI*a*2*t))*.25;}
  static double Ambient(int r,int i){
   double t=(double)i/Rate,v;
   if(r==0){
    v=.075*Math.Sin(2*Math.PI*55*t)+.05*Math.Sin(2*Math.PI*82.5*t)+.03*Math.Sin(2*Math.PI*110*t);
    v+=.06*Bell(t%2,293.66)+.04*Bell((t+1)%2,440);
    v+=.025*Math.Sin(2*Math.PI*.5*t)*Math.Sin(2*Math.PI*220*t);
    v+=.012*AmbientNoise(i)*Math.Exp(-(t%1)*3);
   } else if(r==1){
    v=.07*Math.Sin(2*Math.PI*73.42*t)+.045*Math.Sin(2*Math.PI*110*t+.5*Math.Sin(2*Math.PI*.5*t));
    double beat=t%1.6;v+=.09*Drum(beat,96,6)*(beat<1?1:0);
    double bell=t%3.2;v+=.045*Bell(bell,196)+.03*Bell((bell+1.6)%3.2,261.63);
    v+=.018*AmbientNoise(i)*(.5+.5*Math.Sin(2*Math.PI*3.2*t));
   } else if(r==2){
    double chord=t%4,toll=t%4;
    v=.085*Math.Sin(2*Math.PI*49*t)+.05*Math.Sin(2*Math.PI*73.5*t)+.035*Math.Sin(2*Math.PI*98*t);
    v+=.06*Chord(chord,110,130.81,164.81)+.04*Chord((chord+2)%4,98,123.47,146.83);
    v+=.05*Bell(toll,146.83)+.03*Bell((toll+2)%4,155.56);
    v+=.01*AmbientNoise(i);
   } else if(r==3){
    double arp=t%3.2;
    v=.045*Math.Sin(2*Math.PI*110*t)+.03*Math.Sin(2*Math.PI*164.81*t)+.022*Math.Sin(2*Math.PI*220*t+.3*Math.Sin(2*Math.PI*.25*t));
    v+=.036*Bell(arp,220)+.027*Bell((arp+1.0667)%3.2,261.63)+.021*Bell((arp+2.1333)%3.2,329.63);
    v+=.008*AmbientNoise(i)*Math.Exp(-(t%1)*2);
   } else {
    double swell=t%4,bell=t%2;
    v=.06*Math.Sin(2*Math.PI*130.81*t)+.045*Math.Sin(2*Math.PI*196*t)+.03*Math.Sin(2*Math.PI*261.63*t);
    v+=.05*Chord(swell,261.63,329.63,392)*(.35+.65*swell/4);
    v+=.04*Bell(bell,523.25)+.025*Bell((bell+1)%2,659.25);
    v+=.006*AmbientNoise(i);
   }
   return v;
  }
  public static int EffectLength(int n){
   if(n==0||n==1||n==5)return Rate/2;
   if(n==2)return Rate*3/2;
   if(n==3)return Rate*3/5;
   return Rate*3/10;
  }
  static double Effect(int n,int r,int i){
   double t=(double)i/Rate,f=r==0?1:r==1?1.12:.88,v;
   if(n==0){
    double duration=.5,rise=Game.Clamp((float)(t/.03),0,1),fall=Game.Clamp((float)((duration-t)/.08),0,1);
    double whoosh=.36*Noise(i%NoisePeriod)*Math.Exp(-Math.Pow((t-.13)/.15,2));
    double sweep=.2*Math.Sin(2*Math.PI*(420+520*t)*f*t)*Math.Exp(-t*9);
    double crackle=.17*Noise(i*7+11)*Math.Exp(-t*12)*(.5+.5*Math.Sin(2*Math.PI*31*t));
    double boom=.28*Math.Sin(2*Math.PI*(92-24*t)*f*t)*Math.Exp(-t*7);
    v=(whoosh+sweep+crackle+boom)*rise*fall;
   } else if(n==1){
    v=.5*Drum(t,130*f,7)+.24*Noise(i)*Math.Exp(-t*20);
    v+=.18*(Math.Sin(2*Math.PI*310*f*t)+.5*Math.Sin(2*Math.PI*465*f*t))*Math.Exp(-t*9);
   } else if(n==2){
    v=.1*Math.Sin(2*Math.PI*146.83*f*t)*Math.Exp(-t*1.6);
    double[] notes={293.66*f,349.23*f,440*f,587.33*f};
    for(int k=0;k<notes.Length;k++){
     double dt=t-k*.18;if(dt<0)continue;
     v+=.3*Math.Exp(-dt*3.6)*(Math.Sin(2*Math.PI*notes[k]*dt)+.35*Math.Sin(2*Math.PI*notes[k]*2.01*dt));
    }
   } else if(n==3){
    v=.34*Math.Sin(2*Math.PI*(92*f-28*t)*t)*Math.Exp(-t*5);
    v+=.22*Math.Sin(2*Math.PI*138.6*f*t)*Math.Exp(-t*7)+.16*Noise(i)*Math.Exp(-t*14);
   } else if(n==4){
    v=.3*Noise(i)*Math.Exp(-t*11)+.2*Math.Sin(2*Math.PI*(180+620*t)*f*t)*Math.Exp(-t*12);
   } else {
    v=.28*Bell(t,880*f)+.18*Math.Sin(2*Math.PI*1320*f*t)*Math.Exp(-t*12)+.14*Noise(i)*Math.Exp(-t*18);
   }
   return v;
  }
  static float[][] BuildMusic(){
   float[][] result=new float[TrackCount][];
   for(int r=0;r<result.Length;r++){
    float[] source=new float[LoopSamples+Rate];
    for(int i=0;i<source.Length;i++)source[i]=(float)Ambient(r,i);
    result[r]=new float[LoopSamples];
    for(int i=0;i<LoopSamples;i++)result[r][i]=source[i];
    for(int i=0;i<Rate;i++){
     float weight=1f-i/(float)Rate;
     result[r][i]=source[LoopSamples+i]*weight+source[i]*(1-weight);
    }
   }
   return result;
  }
  static float[][][] BuildEffects(){
   float[][][] result=new float[3][][];
   for(int r=0;r<result.Length;r++){
    result[r]=new float[6][];
    for(int n=0;n<result[r].Length;n++){
     int count=EffectLength(n);result[r][n]=new float[count];
     for(int i=0;i<count;i++)result[r][n][i]=(float)Effect(n,r,i);
    }
   }
   return result;
  }
  public static float MusicSample(int track,int index){return music[track][index%LoopSamples];}
  public static float EffectSample(int room,int effect,int index){return effects[room][effect][index];}
  public static float MusicSeam(int track){return Math.Abs(music[track][0]-music[track][LoopSamples-1]);}
 }
 public sealed class PcmMixer : IDisposable {
  [StructLayout(LayoutKind.Sequential)]
  struct WaveFormatEx {public ushort FormatTag,Channels;public uint SamplesPerSecond,AverageBytesPerSecond;public ushort BlockAlign,BitsPerSample,Size;}
  [StructLayout(LayoutKind.Sequential)]
  struct WaveHeader {public IntPtr Data;public uint BufferLength,BytesRecorded,User,Flags,Loops;public IntPtr Next;public uint Reserved;}
  [DllImport("winmm.dll")] static extern uint waveOutOpen(out IntPtr handle,uint deviceId,ref WaveFormatEx format,IntPtr callback,IntPtr instance,uint flags);
  [DllImport("winmm.dll")] static extern uint waveOutPrepareHeader(IntPtr handle,IntPtr header,uint size);
  [DllImport("winmm.dll")] static extern uint waveOutWrite(IntPtr handle,IntPtr header,uint size);
  [DllImport("winmm.dll")] static extern uint waveOutUnprepareHeader(IntPtr handle,IntPtr header,uint size);
  [DllImport("winmm.dll")] static extern uint waveOutReset(IntPtr handle);
  [DllImport("winmm.dll")] static extern uint waveOutClose(IntPtr handle);
  const uint CallbackEvent=0x00050000,BufferSamples=2048,BufferCount=8;
  static readonly int HeaderSize=Marshal.SizeOf(typeof(WaveHeader));
  readonly object sync=new object();readonly List<Voice> voices=new List<Voice>();
  readonly AutoResetEvent ready=new AutoResetEvent(false);readonly ManualResetEvent stop=new ManualResetEvent(false);
  readonly WaitHandle[] waits;readonly float[] frame=new float[BufferSamples];
  byte[][] buffers;GCHandle[] pins;IntPtr[] headers;IntPtr device=IntPtr.Zero;Thread thread;
  int writeIndex,position,fromTrack=-1,toTrack=-1,fade,fadeLength;
  float master=1,musicVolume=.8f,sfxVolume=1;
  bool musicPlaying,muted,running,disposed,available;
   struct Voice {public int Effect;public int Position;}
   public PcmMixer():this(true){}
   public PcmMixer(bool stream){
    waits=new WaitHandle[]{ready,stop};fadeLength=SoundData.Rate*2;
    if(stream)Start();
   }
   public bool Available{get{return available;}}
   public int Room{get{lock(sync)return room;}}
   public bool Muted{get{lock(sync)return muted;}}

  public bool MusicPlaying{get{lock(sync)return musicPlaying;}}
  public int ActiveTrack{get{lock(sync)return activeTrack();}}
  public int ActiveVoices{get{lock(sync)return voices.Count;}}
  public int Position{get{lock(sync)return position;}}
  public bool CrossfadeActive{get{lock(sync)return fade<fadeLength&&(fromTrack>=0||toTrack>=0);}}
  public float FadeProgress{get{lock(sync)return Math.Min(1,(float)fade/fadeLength);}}
  public float MasterGain{get{lock(sync)return muted?0:master;}}
  public float MusicGain{get{lock(sync)return muted?0:master*musicVolume;}}
  public float SfxGain{get{lock(sync)return muted?0:master*sfxVolume;}}
  int activeTrack(){return musicPlaying?toTrack:-1;}
  public void SetMuted(bool value){lock(sync)muted=value;}
  public void SetGains(float masterValue,float musicValue,float sfxValue){
   lock(sync){master=Game.Clamp(masterValue,0,1);musicVolume=Game.Clamp(musicValue,0,1);sfxVolume=Game.Clamp(sfxValue,0,1);}
  }
  public void SetMusic(int track){
   lock(sync){
    if(track>=SoundData.TrackCount)return;
     if(track<0){
      if(!musicPlaying)return;
      fromTrack=toTrack>=0?toTrack:fromTrack;toTrack=-1;fade=0;fadeLength=SoundData.Rate;musicPlaying=false;
      return;
     }
    if(musicPlaying&&toTrack==track)return;
    if(!musicPlaying&&fromTrack<0&&toTrack<0){fadeLength=SoundData.Rate*2;fade=fadeLength;toTrack=track;}
    else{fromTrack=toTrack>=0?toTrack:fromTrack;toTrack=track;fade=0;fadeLength=SoundData.Rate*2;}
    musicPlaying=true;
   }
  }
  public void Play(int effect){
   lock(sync){
    if(muted||effect<0||effect>=6)return;
    if(voices.Count>=16)voices.RemoveAt(0);
    voices.Add(new Voice{Effect=effect,Position=0});
   }
  }
  void Mix(float[] target){
   lock(sync){
    for(int i=0;i<target.Length;i++){
     float musicSample=0;
     float weight=fadeLength<=0?1:Math.Min(1,(float)fade/fadeLength);
     if(fromTrack>=0&&weight<1)musicSample+=SoundData.MusicSample(fromTrack,position)*(1-weight);
     if(toTrack>=0)musicSample+=SoundData.MusicSample(toTrack,position)*weight;
     float sfxSample=0;
      for(int v=voices.Count-1;v>=0;v--){
       Voice voice=voices[v];
       sfxSample+=SoundData.EffectSample(room,voice.Effect,voice.Position);
       voice.Position++;
       if(voice.Position>=SoundData.EffectLength(voice.Effect))voices.RemoveAt(v);else voices[v]=voice;
      }
     float value=(musicSample*musicVolume+sfxSample*sfxVolume)*master;
     if(muted)value=0;
     target[i]=Game.Clamp(value,-1,1);
     position=(position+1)%SoundData.LoopSamples;
     if(fade<fadeLength)fade++;
     else if(fromTrack>=0)fromTrack=-1;
    }
   }
   }
   public void Render(float[] target){
    if(target==null)throw new ArgumentNullException("target");
    Mix(target);
   }
   int room;
   public void SetRoom(int index){lock(sync)room=(int)Game.Clamp(index,0,2);}
   void Start(){
   WaveFormatEx format=new WaveFormatEx();
   format.FormatTag=1;format.Channels=1;format.SamplesPerSecond=SoundData.Rate;
   format.AverageBytesPerSecond=(uint)(SoundData.Rate*2);format.BlockAlign=2;format.BitsPerSample=16;format.Size=0;
   if(waveOutOpen(out device,0,ref format,ready.SafeWaitHandle.DangerousGetHandle(),IntPtr.Zero,CallbackEvent)!=0){
    device=IntPtr.Zero;available=false;return;
   }
   buffers=new byte[BufferCount][];pins=new GCHandle[BufferCount];headers=new IntPtr[BufferCount];
   for(int i=0;i<BufferCount;i++){
    buffers[i]=new byte[BufferSamples*2];pins[i]=GCHandle.Alloc(buffers[i],GCHandleType.Pinned);
    headers[i]=Marshal.AllocHGlobal(HeaderSize);
    WaveHeader header=new WaveHeader();header.Data=pins[i].AddrOfPinnedObject();header.BufferLength=(uint)buffers[i].Length;
    Marshal.StructureToPtr(header,headers[i],false);
    if(waveOutPrepareHeader(device,headers[i],(uint)HeaderSize)!=0){available=false;return;}
   }
   available=true;running=true;thread=new Thread(Run);thread.IsBackground=true;thread.Start();
   for(int i=0;i<BufferCount;i++)Write(i);
  }
  void Write(int index){
   if(device==IntPtr.Zero||headers==null)return;
   Mix(frame);byte[] data=buffers[index];
   for(int i=0;i<BufferSamples;i++){
    short sample=(short)(frame[i]*32767);data[i*2]=(byte)(sample&255);data[i*2+1]=(byte)((sample>>8)&255);
   }
   WaveHeader header=(WaveHeader)Marshal.PtrToStructure(headers[index],typeof(WaveHeader));
   header.BufferLength=(uint)data.Length;header.Flags=0;
   Marshal.StructureToPtr(header,headers[index],false);
   waveOutWrite(device,headers[index],(uint)HeaderSize);
  }
  void Run(){
   while(running){
    int signal=WaitHandle.WaitAny(waits);
    if(signal==1||!running)return;
     Write(writeIndex);writeIndex=(writeIndex+1)%(int)BufferCount;
   }
  }
  public void Dispose(){
   if(disposed)return;disposed=true;
   lock(sync){running=false;}
   stop.Set();if(thread!=null){thread.Join(1000);thread=null;}
   if(device!=IntPtr.Zero){
    waveOutReset(device);
    if(headers!=null)for(int i=0;i<headers.Length;i++)if(headers[i]!=IntPtr.Zero)waveOutUnprepareHeader(device,headers[i],(uint)HeaderSize);
    waveOutClose(device);device=IntPtr.Zero;
   }
   if(headers!=null)for(int i=0;i<headers.Length;i++)if(headers[i]!=IntPtr.Zero){Marshal.FreeHGlobal(headers[i]);headers[i]=IntPtr.Zero;}
   if(pins!=null)for(int i=0;i<pins.Length;i++)if(pins[i].IsAllocated)pins[i].Free();
   stop.Dispose();ready.Dispose();
  }
 }
}
