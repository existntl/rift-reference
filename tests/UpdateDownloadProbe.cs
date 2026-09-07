using System;
using System.IO;
using RiftReference;
class UpdateDownloadProbe {
    static int Main(string[] args) {
        try {
            var updater=new UpdateManager(AppDomain.CurrentDomain.BaseDirectory);
            updater.Check().GetAwaiter().GetResult();
            if(updater.Available==null || updater.Available.version!=args[0])throw new Exception("Expected update was not offered: "+updater.Status);
            var file=updater.Prepare().GetAwaiter().GetResult();
            if(!File.Exists(file))throw new Exception("Verified installer missing");
            Console.WriteLine("PASS: older app discovered, downloaded and verified "+args[0]+" without installing it.");
            return 0;
        } catch(Exception ex) {Console.WriteLine(ex.Message);return 1;}
    }
}
