using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
namespace RiftReference.Release {
public class Manifest {public string product,version,installerUrl,sha256,notes;public long sizeBytes;}
public class Envelope {public string payload,signature;}
public static class ReleaseInfo {
 // Product is the stable updater/install identity; the display brand is Rift Ready.
 public const string Product="RiftReference",Version="0.12.15";
 public static string PublicKey(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("update-public-key.xml"))using(var r=new StreamReader(s)){return r.ReadToEnd();}}
 public static string Hash(string file){using(var h=SHA256.Create())using(var f=File.OpenRead(file)){return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}}
 public static void Https(string url){Uri u;if(!Uri.TryCreate(url,UriKind.Absolute,out u)||u.Scheme!="https"||u.UserInfo!=""||u.Fragment!=""||url.Contains("\""))throw new InvalidDataException("A valid HTTPS release URL is required.");}
 public static Manifest Verify(string envelope,string key){
  if(envelope.Length>131072)throw new InvalidDataException("Release manifest is too large.");
  var json=new JavaScriptSerializer();var e=json.Deserialize<Envelope>(envelope);if(e==null||e.payload==null||e.signature==null)throw new InvalidDataException("Release signature missing.");
  byte[] payload=Convert.FromBase64String(e.payload),sig=Convert.FromBase64String(e.signature);
  using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(key);if(!rsa.VerifyData(payload,CryptoConfig.MapNameToOID("SHA256"),sig))throw new InvalidDataException("Release signature does not match this publisher.");}
  var m=json.Deserialize<Manifest>(Encoding.UTF8.GetString(payload));System.Version version;
  if(m==null||m.product!=Product||!System.Version.TryParse(m.version,out version)||m.sha256==null||!System.Text.RegularExpressions.Regex.IsMatch(m.sha256,"^[a-fA-F0-9]{64}$")||m.sizeBytes<1||m.sizeBytes>268435456)throw new InvalidDataException("Invalid release metadata.");
  Https(m.installerUrl);return m;
 }
 public static void VerifyFile(string file,Manifest m){if(new FileInfo(file).Length!=m.sizeBytes||!string.Equals(Hash(file),m.sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The installer failed its integrity check. Nothing was installed.");}
}
}
