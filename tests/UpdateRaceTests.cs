using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using RiftReference;
using RiftReference.Release;
class UpdateRaceTests {
 static int Main(){string root=AppDomain.CurrentDomain.BaseDirectory;string helper=Path.Combine(root,"update_fetch.py");string original=File.ReadAllText(helper);try{
  // A private test key, never the publisher's key. Fake downloader uses no network.
  using(var rsa=new RSACryptoServiceProvider(2048)){rsa.PersistKeyInCsp=false;
   byte[] file=Encoding.UTF8.GetBytes("Offline update race fixture");string fixture=Path.Combine(root,"race-fixture.bin");File.WriteAllBytes(fixture,file);
   File.Copy(Path.Combine(root,"update-race-helper.py"),helper,true);
   var json=new JavaScriptSerializer();var release=new Manifest{product=ReleaseInfo.Product,version="99.0.0",installerUrl="https://example.invalid/update.exe",sha256=ReleaseInfo.Hash(fixture),sizeBytes=file.Length};
   byte[] payload=Encoding.UTF8.GetBytes(json.Serialize(release));string signed=json.Serialize(new Envelope{payload=Convert.ToBase64String(payload),signature=Convert.ToBase64String(rsa.SignData(payload,CryptoConfig.MapNameToOID("SHA256")))});
   var manager=new UpdateManager(root){Available=release};typeof(UpdateManager).GetField("key",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,rsa.ToXmlString(false));typeof(UpdateManager).GetField("signedManifest",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,signed);
   Task<string> pending=manager.Prepare();manager.Available=null;typeof(UpdateManager).GetField("signedManifest",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,"A subsequent check changed the feed");
   string installer=pending.GetAwaiter().GetResult();var preserved=ReleaseInfo.Verify(File.ReadAllText(Path.Combine(Path.GetDirectoryName(installer),"release.json")),rsa.ToXmlString(false));ReleaseInfo.VerifyFile(installer,preserved);
   Console.WriteLine("PASS update download keeps its own signed release during a concurrent check; no installer launched");return 0;
  }
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{File.WriteAllText(helper,original,Encoding.UTF8);}}
}
