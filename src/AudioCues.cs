using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Speech.Synthesis;
namespace RiftReference {
public enum AttentionCue { None, SecondClick, TenSeconds, ThirtySeconds, Minute, JungleCheck }
public class RespawnWatch {
 Snapshot previous;
 public void Reset(){previous=null;}
 public string Observe(Snapshot next,DataStore data,bool laneOnly,int maxGap){
  var before=previous;previous=next;
  if(next.Demo||next.Phase!="In game"){Reset();return null;}
  if(before==null||before.Phase!="In game"||next.Time<before.Time||next.Time-before.Time>maxGap||next.Account!=before.Account)return null;
  var self=next.Players.FirstOrDefault(p=>p.Self);if(self==null)return null;
  var eligible=laneOnly?LaneView.Select(next).Enemies:next.Players.Where(p=>p.Team!=self.Team).ToList();
  var names=eligible.Where(p=>p.Dead==false&&before.Players.Any(old=>old.Team==p.Team&&old.Champion==p.Champion&&old.Account==p.Account&&old.Dead==true)).Select(p=>data.Name(p.Champion)).Distinct().Take(5).ToArray();
  if(names.Length==0)return null;
  return names.Length==1?names[0]+" is back alive.":string.Join(", ",names.Take(names.Length-1))+" and "+names[names.Length-1]+" are back alive.";
 }
}
public class AttentionSchedule {
 double previous=-1;
 public void Reset(){previous=-1;}
 public AttentionCue Observe(double seconds,bool active,bool tones,bool voice,int first,int interval,int end,bool ten=true,bool thirty=true,bool minute=true,bool second=false){
  if(!active || double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0){Reset();return AttentionCue.None;}
  double before=previous;previous=seconds;
  // No catch-up sounds on attach, reconnect, rewind or a stale clock jump.
  if(before<0||seconds<before||seconds-before>3)return AttentionCue.None;
  first=Math.Max(30,first);interval=Math.Max(30,interval);end=Math.Max(first,end);
  int nextVoice=before<first?first:first+((int)Math.Floor((before-first)/interval)+1)*interval;
  if(voice&&nextVoice<=end&&seconds>=nextVoice)return AttentionCue.JungleCheck;
  int boundary=(int)Math.Floor(seconds);
  if(!tones||boundary==0||before>=boundary)return AttentionCue.None;
  if(minute&&boundary%60==0)return AttentionCue.Minute;
  if(thirty&&boundary%30==0)return AttentionCue.ThirtySeconds;
  if(ten&&boundary%10==0)return AttentionCue.TenSeconds;
  return second?AttentionCue.SecondClick:AttentionCue.None;
 }
}
public class AttentionAudio : IDisposable {
 SpeechSynthesizer speech; SoundPlayer player; MemoryStream stream; bool disposed;
 public string Status="";string voiceFailure="";
 public static byte[] Wave(AttentionCue cue,int volume){
  double[] notes=cue==AttentionCue.SecondClick?new[]{1800.0}:cue==AttentionCue.Minute?new[]{523.25,659.25,880.0}:cue==AttentionCue.ThirtySeconds?new[]{659.25,880.0}:new[]{659.25};
  const int rate=22050;int duration=cue==AttentionCue.SecondClick?18:cue==AttentionCue.Minute?110:75;int silence=cue==AttentionCue.SecondClick?0:55;
  int per=(duration+silence)*rate/1000,total=per*notes.Length;
  using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory)){
   writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+total*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(total*2);
   for(int n=0;n<notes.Length;n++)for(int i=0;i<per;i++){
    double t=(double)i/rate,limit=(double)duration/1000;
    double envelope=t<limit?Math.Min(1,Math.Min(t/0.012,(limit-t)/0.025)):0;
    short sample=(short)(Math.Sin(2*Math.PI*notes[n]*t)*Math.Max(0,envelope)*(cue==AttentionCue.SecondClick?5000:10000)*Math.Max(0,Math.Min(100,volume))/100.0);writer.Write(sample);
   }return memory.ToArray();
  }
 }
 public void Tone(AttentionCue cue,int volume,bool preview){if(disposed)return;try{
  if(preview)Stop();else if(speech!=null&&speech.State==SynthesizerState.Speaking)return;
  if(player!=null){player.Stop();player.Dispose();}if(stream!=null)stream.Dispose();
  stream=new MemoryStream(Wave(cue,volume));player=new SoundPlayer(stream);player.Load();player.Play();Status=voiceFailure;
 }catch{Status="Sound output unavailable; check the Windows audio device.";}}
 public void Speak(string text,int volume){if(disposed)return;try{
  if(player!=null)player.Stop();
  if(speech==null){speech=new SpeechSynthesizer();var voice=speech.GetInstalledVoices().FirstOrDefault(v=>v.Enabled&&v.VoiceInfo.Culture.TwoLetterISOLanguageName=="en");if(voice!=null)speech.SelectVoice(voice.VoiceInfo.Name);speech.SetOutputToDefaultAudioDevice();speech.Rate=-1;}
  speech.SpeakAsyncCancelAll();speech.Volume=Math.Max(0,Math.Min(100,volume));speech.SpeakAsync(text);voiceFailure="";Status="";
 }catch{var failed=speech;speech=null;try{if(failed!=null)failed.Dispose();}catch{}voiceFailure="Voice unavailable: check the Windows voice and audio output. Tones can still work.";Status=voiceFailure;}}
 public static string JungleMessage(Snapshot s){return "Jungle check.";}
 public void Stop(){try{if(player!=null)player.Stop();if(speech!=null)speech.SpeakAsyncCancelAll();}catch{}}
 public void Dispose(){if(disposed)return;Stop();disposed=true;if(player!=null)player.Dispose();if(stream!=null)stream.Dispose();if(speech!=null)speech.Dispose();}
}
}
