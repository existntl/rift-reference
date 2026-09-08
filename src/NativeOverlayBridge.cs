using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace RiftReference {
// Experimental renderer transport. Opt-in only; no game process access occurs here.
public sealed class NativeOverlayBridge : IDisposable {
 public const int HeaderSize=64, MaxWidth=4096, MaxHeight=2160;
 public const long Capacity=HeaderSize+(long)MaxWidth*MaxHeight*4;
 public const uint Magic=0x52465252;
 [DllImport("kernel32.dll")] static extern ulong GetTickCount64();
 readonly MemoryMappedFile mapping; readonly MemoryMappedViewAccessor view; readonly Mutex gate;
 uint sequence; bool disposed;
 public static string MapName(string session){return "Local\\RiftReady.Render."+ValidSession(session);}
 public static string MutexName(string session){return "Local\\RiftReady.RenderLock."+ValidSession(session);}
 static string ValidSession(string value){Guid id;if(!Guid.TryParseExact(value,"N",out id))throw new ArgumentException("Renderer session must be a GUID in N format.");return id.ToString("N");}
 public NativeOverlayBridge(string session){
  var sid=WindowsIdentity.GetCurrent().User;
  var ms=new MutexSecurity();ms.SetAccessRuleProtection(true,false);ms.AddAccessRule(new MutexAccessRule(sid,MutexRights.FullControl,AccessControlType.Allow));
  bool created;gate=new Mutex(false,MutexName(session),out created,ms);
  if(!created){gate.Dispose();throw new InvalidOperationException("Renderer session already exists.");}
  try{
   var fs=new MemoryMappedFileSecurity();fs.SetAccessRuleProtection(true,false);fs.AddAccessRule(new AccessRule<MemoryMappedFileRights>(sid,MemoryMappedFileRights.FullControl,AccessControlType.Allow));
   mapping=MemoryMappedFile.CreateNew(MapName(session),Capacity,MemoryMappedFileAccess.ReadWrite,MemoryMappedFileOptions.None,fs,System.IO.HandleInheritability.None);
   view=mapping.CreateViewAccessor(0,Capacity,MemoryMappedFileAccess.ReadWrite);
  }catch{if(view!=null)view.Dispose();if(mapping!=null)mapping.Dispose();gate.Dispose();throw;}
 }
 public static NativeOverlayBridge FromEnvironment(){var session=Environment.GetEnvironmentVariable("RIFT_READY_RENDER_SESSION");if(String.IsNullOrEmpty(session))return null;try{return new NativeOverlayBridge(session);}catch(Exception e){System.Diagnostics.Trace.WriteLine("Renderer bridge unavailable: "+e.GetType().Name);return null;}}
 bool Enter(){try{return gate.WaitOne(0);}catch(AbandonedMutexException){return true;}}
 public bool Publish(byte[] pixels,int width,int height,bool visible){
  if(disposed)throw new ObjectDisposedException("NativeOverlayBridge");
  if(width<1||height<1||width>MaxWidth||height>MaxHeight||pixels==null||pixels.Length!=(long)width*height*4)throw new ArgumentException("Invalid BGRA frame.");
  if(!Enter())return false;
  try{view.Write(0,Magic);view.Write(4,1U);view.Write(12,(uint)width);view.Write(16,(uint)height);view.Write(20,(uint)(width*4));view.Write(24,(uint)pixels.Length);view.Write(28,visible?1U:0U);view.WriteArray(HeaderSize,pixels,0,pixels.Length);view.Write(32,GetTickCount64());view.Write(8,++sequence);return true;}finally{gate.ReleaseMutex();}
 }
 public void Heartbeat(bool visible){if(disposed||!Enter())return;try{view.Write(28,visible?1U:0U);view.Write(32,GetTickCount64());}finally{gate.ReleaseMutex();}}
 public static byte[] Pixels(Bitmap image){
  var area=new Rectangle(0,0,image.Width,image.Height);var bits=image.LockBits(area,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  try{var bytes=new byte[image.Width*image.Height*4];for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),bytes,y*image.Width*4,image.Width*4);return bytes;}finally{image.UnlockBits(bits);}
 }
 public static Bitmap Compose(Size viewport,OverlayWindow[] panels,bool[] shown){return Compose(viewport,panels,shown,Point.Empty);}
 public static Bitmap Compose(Size viewport,OverlayWindow[] panels,bool[] shown,Point origin){
  if(viewport.Width<1||viewport.Height<1||viewport.Width>MaxWidth||viewport.Height>MaxHeight||panels.Length!=shown.Length)throw new ArgumentException("Invalid renderer viewport.");
  var image=new Bitmap(viewport.Width,viewport.Height,PixelFormat.Format32bppArgb);
  try{using(var canvas=Graphics.FromImage(image)){canvas.Clear(Color.Transparent);for(int n=0;n<panels.Length;n++){
   var p=panels[n];if(!shown[n])continue;
   using(var layer=new Bitmap(p.Width,p.Height,PixelFormat.Format32bppArgb)){
    using(var g=Graphics.FromImage(layer))p.Draw(g);
    if(p.TransparencyKey!=Color.Empty)layer.MakeTransparent(p.TransparencyKey);
    using(var attrs=new ImageAttributes()){var matrix=new ColorMatrix();matrix.Matrix33=(float)p.Opacity;attrs.SetColorMatrix(matrix);var dest=p.Bounds;dest.Offset(-origin.X,-origin.Y);canvas.DrawImage(layer,dest,0,0,layer.Width,layer.Height,GraphicsUnit.Pixel,attrs);}
   }
  }}return image;}catch{image.Dispose();throw;}
 }
 public void Dispose(){if(disposed)return;Heartbeat(false);disposed=true;view.Dispose();mapping.Dispose();gate.Dispose();}
}
}
