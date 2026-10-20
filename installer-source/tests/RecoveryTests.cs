using System;using System.IO;using System.Text;using System.Xml.Serialization;using System.Threading;using PclRestore;
class RecoveryTests {
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Reject(Action action){bool fail=false;try{action();}catch{fail=true;}Assert(fail,"Expected operation to be rejected");}
 static void Main(string[] a){try{
  string root=Path.GetFullPath(a[1]);Assert(!Directory.Exists(root),"Use a fresh test root");Directory.CreateDirectory(root);
  string exe=Path.Combine(root,"Plain Craft Launcher 2.exe");File.Copy(a[0],exe);Assert(PatchEngine.HashFile(exe)==PatchEngine.OriginalHash,"Wrong input");
  Directory.CreateDirectory(Path.Combine(root,"PCL"));File.WriteAllText(Path.Combine(root,"PCL","Setup.ini"),"OriginalSetting:keep\r\nSystemSystemUpdate:0\r\n");
  byte[] before=File.ReadAllBytes(Path.Combine(root,"PCL","Setup.ini"));var installed=PatchEngine.Install(exe,true,null);
  string rp=Path.Combine(installed.Backup,"receipt.xml");Receipt rec;using(var f=File.OpenRead(rp))rec=(Receipt)new XmlSerializer(typeof(Receipt)).Deserialize(f);
  rec.State="Prepared";using(var f=File.Create(rp))new XmlSerializer(typeof(Receipt)).Serialize(f,rec);
  File.WriteAllBytes(Path.Combine(root,"PCL","Setup.ini"),before); // simulate interruption after EXE commit, before configuration commit
  PatchEngine.Restore(exe);Assert(PatchEngine.HashFile(exe)==PatchEngine.OriginalHash,"Prepared transaction did not restore original");Assert(PatchEngine.HashFile(Path.Combine(root,"PCL","Setup.ini"))==PatchEngine.Hash(before),"Prepared transaction lost configuration");
  Console.WriteLine("PASS prepared transaction recovery after simulated interrupted write");
  var ready=new ManualResetEvent(false);var release=new ManualResetEvent(false);
  string mutexName="Local\\PCLRestore-"+PatchEngine.Hash(Encoding.UTF8.GetBytes(Path.GetFullPath(exe).ToUpperInvariant()));
  var thread=new Thread(()=>{using(var mutex=new Mutex(false,mutexName)){mutex.WaitOne();ready.Set();release.WaitOne();mutex.ReleaseMutex();}});thread.Start();ready.WaitOne();
  try{Reject(()=>PatchEngine.Install(exe,true,null));Assert(PatchEngine.HashFile(exe)==PatchEngine.OriginalHash,"Concurrent transaction changed executable");}finally{release.Set();thread.Join();}
  Console.WriteLine("PASS simultaneous transaction guard rejects second operation");
  installed=PatchEngine.Install(exe,true,null);File.WriteAllText(Path.Combine(installed.Backup,"original.exe"),"corrupted backup");
  Reject(()=>PatchEngine.Restore(exe));Assert(PatchEngine.HashFile(exe)==PatchEngine.PatchedHash,"Corrupted backup replaced executable");
  Console.WriteLine("PASS corrupted backup rejected without overwriting installed executable");
  Console.WriteLine("RESULT PASS: 3 recovery/concurrency tests");
 }catch(Exception e){Console.WriteLine("FAIL "+e.Message);Environment.ExitCode=1;}}
}
