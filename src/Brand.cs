using System.Drawing;
using System.Reflection;
namespace RiftReference {
public static class Brand {
 public static readonly Image Logo=LoadLogo();
 public static readonly Icon Icon=Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
 static Image LoadLogo(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("rift-ready.png"))using(var image=Image.FromStream(stream))return new Bitmap(image);}
}
}
