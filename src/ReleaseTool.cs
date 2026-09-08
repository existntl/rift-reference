using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using RiftReference.Release;
class ReleaseTool {
 static int Main(string[] args){try{
  if(args.Length==3&&args[0]=="keygen"){
   if(File.Exists(args[1])||File.Exists(args[2]))throw new Exception("A signing key already exists; refusing to replace it.");
   using(var rsa=new RSACryptoServiceProvider(3072)){rsa.PersistKeyInCsp=false;File.WriteAllText(args[1],rsa.ToXmlString(true));File.WriteAllText(args[2],rsa.ToXmlString(false));}
   Console.WriteLine("Publisher key created. Keep the private key local and back it up securely; never publish it.");return 0;
  }
  if(args.Length>=6&&args[0]=="sign"){
   ReleaseInfo.Https(args[4]);System.Version.Parse(args[3]);
   var m=new Manifest{product=ReleaseInfo.Product,version=args[3],installerUrl=args[4],sha256=ReleaseInfo.Hash(args[2]),sizeBytes=new FileInfo(args[2]).Length,notes=args.Length>6?File.ReadAllText(args[6]):""};
   var json=new JavaScriptSerializer();byte[] payload=Encoding.UTF8.GetBytes(json.Serialize(m));string key=File.ReadAllText(args[1]);
   using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(key);File.WriteAllText(args[5],json.Serialize(new Envelope{payload=Convert.ToBase64String(payload),signature=Convert.ToBase64String(rsa.SignData(payload,CryptoConfig.MapNameToOID("SHA256")))}));}
   Console.WriteLine("Signed manifest created: "+args[5]);return 0;
  }
  Console.WriteLine("keygen PRIVATE.xml PUBLIC.xml | sign PRIVATE.xml INSTALLER VERSION HTTPS_URL OUTPUT.json [NOTES.txt]");return 2;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
