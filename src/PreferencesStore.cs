using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace RiftReference {
public static class PreferencesStore {
 public static Preferences Read(string root){
  string path=Path.Combine(root,"preferences.json");
  if(!File.Exists(path))return new Preferences();
  if(new FileInfo(path).Length>65536)throw new IOException("Preferences are too large; the existing file was preserved.");
  var value=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(path,Encoding.UTF8));
  if(value==null)throw new IOException("Preferences are invalid; the existing file was preserved.");
  return value;
 }
 public static void Save(string root,Preferences value){
  Read(root); // Never replace unreadable user settings with defaults or a partial draft.
  string path=Path.Combine(root,"preferences.json"),temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{
   File.WriteAllText(temp,new JavaScriptSerializer().Serialize(value),new UTF8Encoding(false));
   if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
  }finally{if(File.Exists(temp))File.Delete(temp);}
 }
}
}
