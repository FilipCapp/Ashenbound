using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
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
   [StructLayout(LayoutKind.Sequential,Pack=1)]
   struct WaveFormatEx {public ushort FormatTag,Channels;public uint SamplesPerSecond,AverageBytesPerSecond;public ushort BlockAlign,BitsPerSample,Size;}
   [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
   interface IMMDeviceEnumerator {
    [PreserveSig] int EnumAudioEndpoints(int dataFlow,uint stateMask,IntPtr devices);
    [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow,int role,out IMMDevice endpoint);
   }
   [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
   interface IMMDevice {
    [PreserveSig] int Activate(ref Guid iid,uint context,IntPtr parameters,out IAudioClient client);
   }
   [ComImport,Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
   interface IAudioClient {
    [PreserveSig] int Initialize(int shareMode,uint flags,long bufferDuration,long periodicity,ref WaveFormatEx format,IntPtr session);
    [PreserveSig] int GetBufferSize(out uint frames);
    [PreserveSig] int GetStreamLatency(out long latency);
    [PreserveSig] int GetCurrentPadding(out uint padding);
    [PreserveSig] int IsFormatSupported(int shareMode,ref WaveFormatEx format,out IntPtr closest);
    [PreserveSig] int GetMixFormat(out IntPtr format);
    [PreserveSig] int GetDevicePeriod(out long defaultPeriod,out long minimumPeriod);
    [PreserveSig] int Start();
    [PreserveSig] int Stop();
    [PreserveSig] int Reset();
    [PreserveSig] int SetEventHandle(IntPtr handle);
    [PreserveSig] int GetService(ref Guid iid,out IAudioRenderClient service);
   }
   [ComImport,Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
   interface IAudioRenderClient {
    [PreserveSig] int GetBuffer(uint frames,out IntPtr data);
    [PreserveSig] int ReleaseBuffer(uint frames,uint flags);
   }
   [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved,uint context);
   [DllImport("ole32.dll")] static extern void CoUninitialize();
   [DllImport("ole32.dll")] static extern int CoCreateInstance(ref Guid clsid,IntPtr outer,uint context,ref Guid iid,out IntPtr instance);
   const uint EventCallback=0x00040000,AutoConvertPcm=0x80000000,SrcDefaultQuality=0x08000000;
    const uint ClassContextAll=0x00000017,CoInitMultiThreaded=0x00000000;
    const int RpcEChangedMode=unchecked((int)0x80010106);
   const int DataFlowRender=0,RoleMultimedia=1,ShareModeShared=0;
   const long BufferDuration=200000;
   const int BufferSamples=512;

    internal static bool Diagnostics;
    static readonly StringBuilder DiagnosticLog=new StringBuilder();
    internal static string DiagnosticText{get{lock(DiagnosticLog)return DiagnosticLog.ToString();}}
    static void Diag(string message){if(Diagnostics)lock(DiagnosticLog)DiagnosticLog.AppendLine(message);}
    static int Hr(string name,int result){Diag(name+" 0x"+result.ToString("X8"));return result;}
    readonly object sync=new object();readonly List<Voice> voices=new List<Voice>();
   readonly AutoResetEvent ready=new AutoResetEvent(false);readonly ManualResetEvent stop=new ManualResetEvent(false);
   readonly ManualResetEvent initialized=new ManualResetEvent(false);readonly WaitHandle[] waits;
   float[] frame=new float[BufferSamples];byte[] renderBytes;Thread thread;
   IMMDeviceEnumerator enumerator;IMMDevice endpoint;IAudioClient audioClient;IAudioRenderClient renderClient;
   volatile bool running,available;
   bool musicPlaying,muted,disposed,comInitialized,comMustUninitialize;
    int bufferFrames,position,fromTrack=-1,toTrack=-1,fade,fadeLength,renderDiagCount;
   float master=1,musicVolume=.8f,sfxVolume=1;
    struct Voice {public int Effect;public int Position;public int Room;}
    public PcmMixer():this(true){}
    public PcmMixer(bool stream){
     waits=new WaitHandle[]{ready,stop};fadeLength=SoundData.Rate*2;
     if(stream)Start();
    }
    public bool Available{get{return available;}}
    internal static int WaveFormatBytes{get{return Marshal.SizeOf(typeof(WaveFormatEx));}}
    internal static long WasapiBufferDuration{get{return BufferDuration;}}

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
    voices.Add(new Voice{Effect=effect,Position=0,Room=room});
   }
  }
   void Mix(float[] target,int count){
    lock(sync){
     for(int i=0;i<count;i++){

     float musicSample=0;
     float weight=fadeLength<=0?1:Math.Min(1,(float)fade/fadeLength);
     if(fromTrack>=0&&weight<1)musicSample+=SoundData.MusicSample(fromTrack,position)*(1-weight);
     if(toTrack>=0)musicSample+=SoundData.MusicSample(toTrack,position)*weight;
     float sfxSample=0;
      for(int v=voices.Count-1;v>=0;v--){
       Voice voice=voices[v];
       sfxSample+=SoundData.EffectSample(voice.Room,voice.Effect,voice.Position);
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
     Mix(target,target.Length);
    }

   int room;
   public void SetRoom(int index){lock(sync)room=(int)Game.Clamp(index,0,2);}
   void Start(){
    thread=new Thread(Run);thread.IsBackground=true;
    running=true;thread.Start();initialized.WaitOne();
   }
    bool InitializeAudio(){
     int result=Hr("CoInitializeEx",CoInitializeEx(IntPtr.Zero,CoInitMultiThreaded));
     if(result<0&&result!=RpcEChangedMode)return false;
     if(result==RpcEChangedMode)Diag("CoInitializeEx already initialized on this thread");
     comInitialized=true;comMustUninitialize=result==0;
     Guid clsid=new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
     Guid iidEnumerator=new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
     IntPtr raw=IntPtr.Zero;
     result=Hr("CoCreateInstance",CoCreateInstance(ref clsid,IntPtr.Zero,ClassContextAll,ref iidEnumerator,out raw));
     if(result<0||raw==IntPtr.Zero)return false;
     try{enumerator=(IMMDeviceEnumerator)Marshal.GetTypedObjectForIUnknown(raw,typeof(IMMDeviceEnumerator));}
     finally{if(raw!=IntPtr.Zero)Marshal.Release(raw);}
     if(enumerator==null){Diag("GetTypedObjectForIUnknown null");return false;}
     result=Hr("GetDefaultAudioEndpoint",enumerator.GetDefaultAudioEndpoint(DataFlowRender,RoleMultimedia,out endpoint));
     if(result<0||endpoint==null)return false;
     Guid iidClient=new Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
     WaveFormatEx format=new WaveFormatEx();
     format.FormatTag=1;format.Channels=1;format.SamplesPerSecond=SoundData.Rate;
     format.AverageBytesPerSecond=(uint)(SoundData.Rate*2);format.BlockAlign=2;format.BitsPerSample=16;format.Size=0;
     Diag("WaveFormatBytes "+WaveFormatBytes+" channels "+format.Channels+" rate "+format.SamplesPerSecond);
     uint[] attempts=new uint[]{EventCallback|AutoConvertPcm|SrcDefaultQuality,EventCallback|AutoConvertPcm,EventCallback};
     for(int i=0;i<attempts.Length;i++){
      IAudioClient attempt=null;
      result=Hr("Activate["+i+"]",endpoint.Activate(ref iidClient,ClassContextAll,IntPtr.Zero,out attempt));
      if(result<0||attempt==null)continue;
      result=Hr("Initialize["+i+"] flags=0x"+attempts[i].ToString("X8"),attempt.Initialize(ShareModeShared,attempts[i],BufferDuration,0,ref format,IntPtr.Zero));
      if(result<0){ReleaseCom(attempt);continue;}
      audioClient=attempt;break;
     }
     if(audioClient==null){Diag("No usable IAudioClient");return false;}
     uint frames;
     result=Hr("GetBufferSize",audioClient.GetBufferSize(out frames));
     if(result<0||frames==0)return false;
     result=Hr("SetEventHandle",audioClient.SetEventHandle(ready.SafeWaitHandle.DangerousGetHandle()));
     if(result<0)return false;
     Guid iidRender=new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
     result=Hr("GetService",audioClient.GetService(ref iidRender,out renderClient));
     if(result<0||renderClient==null)return false;
     bufferFrames=(int)frames;
     Diag("bufferFrames "+bufferFrames);
     frame=new float[bufferFrames];renderBytes=new byte[bufferFrames*2];
     if(!RenderBuffer(bufferFrames)){Diag("Prefill RenderBuffer failed");return false;}
     result=Hr("Start",audioClient.Start());
     if(result<0)return false;
     available=true;
     Diag("available true");
     return true;
    }
    void Run(){
     bool ok=false;
     try{ok=InitializeAudio();}
     catch(Exception e){Diag("InitializeAudio exception "+e);}
     initialized.Set();
     if(ok){try{Loop();}catch(Exception e){Diag("Loop exception "+e);}}
     Cleanup();
    }
    void Loop(){
     int errors=0;
     while(running){
      if(WaitHandle.WaitAny(waits)==1)break;
      if(audioClient==null||renderClient==null)break;
      uint padding;
      int result=audioClient.GetCurrentPadding(out padding);
      if(result<0){if(errors++<16)Hr("GetCurrentPadding loop",result);continue;}
      int count=bufferFrames-(int)padding;
      if(count<=0)continue;
      if(count>bufferFrames)count=bufferFrames;
      if(!RenderBuffer(count)&&errors++<16)Diag("RenderBuffer loop failed frames="+count);
     }
    }
    bool RenderBuffer(int frames){
     if(frames<=0||frames>bufferFrames||renderClient==null||renderBytes==null||frame==null)return false;
     IntPtr data=IntPtr.Zero;
     int result=renderClient.GetBuffer((uint)frames,out data);
     if(Diagnostics&&renderDiagCount++<8)Hr("GetBuffer",result);
     if(result<0||data==IntPtr.Zero)return false;
     Mix(frame,frames);
     int bytes=frames*2;
     for(int i=0;i<frames;i++){
      short sample=(short)(frame[i]*32767);
      renderBytes[i*2]=(byte)(sample&255);renderBytes[i*2+1]=(byte)((sample>>8)&255);
     }
     Marshal.Copy(renderBytes,0,data,bytes);
     result=renderClient.ReleaseBuffer((uint)frames,0);
     if(Diagnostics&&renderDiagCount<8)Hr("ReleaseBuffer",result);
     return result>=0;
    }
   static void ReleaseCom(object value){
    if(value==null)return;
    try{Marshal.ReleaseComObject(value);}catch{}
   }
   void Cleanup(){
    try{if(audioClient!=null){audioClient.Stop();audioClient.Reset();}}catch{}
    ReleaseCom(renderClient);renderClient=null;
    ReleaseCom(audioClient);audioClient=null;
    ReleaseCom(endpoint);endpoint=null;
    ReleaseCom(enumerator);enumerator=null;
    if(comInitialized&&comMustUninitialize)CoUninitialize();
    comInitialized=false;comMustUninitialize=false;
    available=false;running=false;
   }
   public void Dispose(){
    if(disposed)return;disposed=true;
    lock(sync){running=false;}
    stop.Set();if(thread!=null){thread.Join();thread=null;}
    initialized.Dispose();stop.Dispose();ready.Dispose();
   }

 }
}
