using System;using RiftReference;
class UpdateProbe {static int Main(string[] args){try{var u=new UpdateManager(args[0]);u.Check().GetAwaiter().GetResult();Console.WriteLine(u.Status);return 0;}catch(Exception e){Console.WriteLine(e.Message);return 1;}}}
