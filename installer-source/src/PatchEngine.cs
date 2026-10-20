using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace PclRestore {
 public sealed class Receipt {
  public int Format = 1;
  public string Target, State, CreatedUtc, BeforeConfigHash, AfterConfigHash;
  public bool ConfigExisted, ConfigChanged;
 }
 public sealed class Outcome {
  public string Message, Backup;
  public Outcome(string message, string backup) { Message=message; Backup=backup; }
 }
 public static class PatchEngine {
  public const string OriginalHash="6b6e1260cb5a1d6a303855f1c60cab3d0bc1aebba7e40a964180188b271e7386";
  public const string PatchedHash="6be10780d1f1471bb5bddefdfbcb3966dbc90cc2f00d2dbce579fa75190932b9";
  public const string Product="Plain Craft Launcher (PCL) 本地功能恢复补丁";
  const int MaxExe=16*1024*1024;
  public static string Hash(byte[] b) { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant(); }
  public static string HashFile(string p) { return Hash(File.ReadAllBytes(p)); }
  public static byte[] Resource(string name) {
   using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("PclRestore."+name)) {
    if(s==null)throw new InvalidDataException("安装包资源不完整："+name);
    using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}
   }
  }
  public static string Inspect(string path) {
   path=FullTarget(path); var size=new FileInfo(path).Length;
   if(size<=0||size>MaxExe)throw new InvalidDataException("文件大小不符。请选择原版 PCL 2.99.0.0 的程序文件。");
   string h=HashFile(path);
   if(h==OriginalHash)return "支持：原版 2.99.0.0，文件校验通过。";
   if(h==PatchedHash)return "此文件已经是本补丁对应的恢复版，无需再次安装。";
   throw new InvalidDataException("不支持这个程序文件。版本号相同也可能不是同一构建；为避免损坏，不会修改。请使用说明中指定 SHA-256 的原版 2.99.0.0。");
  }
  static string FullTarget(string p) {
   if(String.IsNullOrWhiteSpace(p))throw new ArgumentException("请先选择 PCL 程序文件。");
   p=Path.GetFullPath(p);
   if(!File.Exists(p)||!p.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new FileNotFoundException("没有找到所选 EXE 文件。");
   if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new IOException("程序文件本身是链接，请选择链接指向的实际 EXE。");
   return p;
  }
  static void CheckDirectory(string dir) {
   if(Directory.Exists(dir)&&(File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)throw new IOException("备份或配置目录是链接，未进行修改。请在实际目录操作。");
  }
  static void AssertUnlocked(string path) {
   if(File.Exists(path))using(var f=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){}
  }
  public static byte[] Apply(byte[] original, byte[] compressed) {
   if(Hash(original)!=OriginalHash)throw new InvalidDataException("原程序校验失败。");
   using(var gz=new GZipStream(new MemoryStream(compressed,false),CompressionMode.Decompress))
   using(var r=new BinaryReader(gz,Encoding.UTF8))
   using(var result=new MemoryStream()) {
    if(Encoding.ASCII.GetString(r.ReadBytes(8))!="PCLR2991")throw new InvalidDataException("补丁格式错误。");
    if(BitConverter.ToString(r.ReadBytes(32)).Replace("-","").ToLowerInvariant()!=OriginalHash||BitConverter.ToString(r.ReadBytes(32)).Replace("-","").ToLowerInvariant()!=PatchedHash)throw new InvalidDataException("补丁版本错误。");
    int length=r.ReadInt32(),count=r.ReadInt32();
    if(length!=5976064||count<1||count>200000)throw new InvalidDataException("补丁长度无效。");
    for(int i=0;i<count;i++) {
     byte op=r.ReadByte(); int offset=0,n;
     if(op==0){offset=r.ReadInt32();n=r.ReadInt32();if(offset<0||n<0||(long)offset+n>original.Length)throw new InvalidDataException("补丁引用越界。");}
     else if(op==1)n=r.ReadInt32();else throw new InvalidDataException("补丁操作无效。");
     if(n<0||result.Length+n>length)throw new InvalidDataException("补丁数据越界。");
     if(op==0)result.Write(original,offset,n);
     else {var b=r.ReadBytes(n);if(b.Length!=n)throw new EndOfStreamException("补丁不完整。");result.Write(b,0,n);}
    }
    if(result.Length!=length||gz.ReadByte()!=-1)throw new InvalidDataException("补丁结果长度错误。");
    var output=result.ToArray();if(Hash(output)!=PatchedHash)throw new InvalidDataException("修复结果校验失败，未安装。");return output;
   }
  }
  static byte[] Config(byte[] original,bool updates,string game) {
   Encoding enc=new UTF8Encoding(false,true); int start=0; byte[] prefix=new byte[0];
   if(original.Length>=3&&original[0]==239&&original[1]==187&&original[2]==191){start=3;prefix=new byte[]{239,187,191};}
   else if(original.Length>=2&&original[0]==255&&original[1]==254){start=2;prefix=new byte[]{255,254};enc=new UnicodeEncoding(false,false,true);}
   else if(original.Length>=2&&original[0]==254&&original[1]==255){start=2;prefix=new byte[]{254,255};enc=new UnicodeEncoding(true,false,true);}
   var text=enc.GetString(original,start,original.Length-start);
   if(updates)text=SetKey(text,"SystemSystemUpdate","2");
   if(!String.IsNullOrWhiteSpace(game))text=SetKey(text,"LaunchFolderSelect",game);
   return prefix.Concat(enc.GetBytes(text)).ToArray();
  }
  static string SetKey(string text,string key,string value) {
   var rx=new System.Text.RegularExpressions.Regex(@"(?m)^"+key+@":[^\r\n]*");
   if(rx.IsMatch(text))return rx.Replace(text,m=>key+":"+value);
   return text+(text.Length==0||text.EndsWith("\n")?"":"\r\n")+key+":"+value+"\r\n";
  }
  static void SaveReceipt(string dir,Receipt rec) {
   string path=Path.Combine(dir,"receipt.xml"),tmp=path+".new";
   using(var f=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(Receipt)).Serialize(f,rec);f.Flush(true);}
   if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);
  }
  static void WriteNew(string path,byte[] b) {
   using(var f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(b,0,b.Length);f.Flush(true);}
  }
  static void Atomic(string path,byte[] b) {
   string tmp=path+".pclrestore-"+Guid.NewGuid().ToString("N")+".tmp";
   try {WriteNew(tmp,b);if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);}
   finally {if(File.Exists(tmp))File.Delete(tmp);}
  }
  static string ConfigPath(string target) {return Path.Combine(Path.GetDirectoryName(target),"PCL","Setup.ini");}
  static string BackupRoot(string target) {return Path.Combine(Path.GetDirectoryName(target),"PCL","LocalRestoreBackups");}
  // failpoint is internal test instrumentation; it is never exposed through installer arguments.
  internal static Outcome InstallCore(string target,bool updates,string game,byte[] patch,Action<string> failpoint) {
   return Locked(target,()=>InstallUnlocked(target,updates,game,patch,failpoint));
  }
  static Outcome Locked(string target,Func<Outcome> action) {
   target=FullTarget(target);
   using(var mutex=new System.Threading.Mutex(false,"Local\\PCLRestore-"+Hash(Encoding.UTF8.GetBytes(target.ToUpperInvariant())))) {
    bool acquired=false;
    try {try{acquired=mutex.WaitOne(0);}catch(System.Threading.AbandonedMutexException){acquired=true;}
     if(!acquired)throw new IOException("另一个修复或回退操作正在处理同一程序，请稍后再试。");return action();
    }finally{if(acquired)mutex.ReleaseMutex();}
   }
  }
  static Outcome InstallUnlocked(string target,bool updates,string game,byte[] patch,Action<string> failpoint) {
   target=FullTarget(target);Inspect(target);
   if(HashFile(target)==PatchedHash)return new Outcome("已经安装，无需重复修复。",null);
   if(!String.IsNullOrWhiteSpace(game)) {
    game=Path.GetFullPath(game).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
    if(!Directory.Exists(game)||!Directory.Exists(Path.Combine(game,"versions")))throw new IOException("请选择包含 versions 子目录的 Minecraft 游戏根目录。");
    if(game.IndexOfAny(new[]{'\r','\n'})>=0)throw new IOException("游戏路径无效。");
   }
   string config=ConfigPath(target),backupRoot=BackupRoot(target);
   if(File.Exists(config)&&(File.GetAttributes(config)&FileAttributes.ReparsePoint)!=0)throw new IOException("Setup.ini 是链接，未进行修改。");
   CheckDirectory(Path.GetDirectoryName(config));CheckDirectory(backupRoot);AssertUnlocked(target);AssertUnlocked(config);
   bool had=File.Exists(config);var before=had?File.ReadAllBytes(config):new byte[0];
   var after=(updates||!String.IsNullOrWhiteSpace(game))?Config(before,updates,game):before;
   var candidate=Apply(File.ReadAllBytes(target),patch); // validate before any write
   string backup=Path.Combine(backupRoot,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
   Directory.CreateDirectory(backup);
   File.Copy(target,Path.Combine(backup,"original.exe"),false);
   if(HashFile(Path.Combine(backup,"original.exe"))!=OriginalHash)throw new IOException("原程序备份校验失败，未安装。");
   if(had)WriteNew(Path.Combine(backup,"Setup.ini.before"),before);
   var rec=new Receipt{Target=target,State="Prepared",CreatedUtc=DateTime.UtcNow.ToString("o"),ConfigExisted=had,ConfigChanged=!before.SequenceEqual(after),BeforeConfigHash=Hash(before),AfterConfigHash=Hash(after)};
   SaveReceipt(backup,rec);
   try {
    if(HashFile(target)!=OriginalHash||(File.Exists(config)!=had)||(had&&HashFile(config)!=rec.BeforeConfigHash))throw new IOException("文件已被其他程序改变，停止安装。");
    Atomic(target,candidate);if(failpoint!=null)failpoint("exe-written");
    if(rec.ConfigChanged)Atomic(config,after);if(failpoint!=null)failpoint("config-written");
    if(HashFile(target)!=PatchedHash||(rec.ConfigChanged&&HashFile(config)!=rec.AfterConfigHash))throw new IOException("安装后校验失败。");
    rec.State="Installed";SaveReceipt(backup,rec);
    return new Outcome("修复完成。原程序和需要改动的设置已备份。\r\n可正常打开 PCL；需要回退时使用本安装包的“恢复原版”。",backup);
   } catch(Exception first) {
    try {Rollback(target,backup,rec);rec.State="RolledBackAfterFailure";SaveReceipt(backup,rec);}
    catch(Exception rollback){throw new IOException("安装中断，自动回退未完成。请保留备份目录："+backup+"。原因："+rollback.Message,first);}
    throw new IOException("安装未完成，已回退此次写入。原因："+first.Message,first);
   }
  }
  public static Outcome Install(string target,bool updates,string game) {return InstallCore(target,updates,game,Resource("restore.patch.gz"),null);}
  static bool Rollback(string target,string backup,Receipt rec) {
   if(HashFile(Path.Combine(backup,"original.exe"))!=OriginalHash)throw new IOException("原版备份损坏，拒绝恢复。");
   string current=HashFile(target);if(current!=OriginalHash&&current!=PatchedHash)throw new IOException("当前程序已被其他更新改变，拒绝覆盖。");
   string config=ConfigPath(target);bool skipped=false;
   if(rec.ConfigChanged) {
    if(File.Exists(config)&&HashFile(config)==rec.AfterConfigHash) {
     AssertUnlocked(config);
     if(rec.ConfigExisted) {var b=File.ReadAllBytes(Path.Combine(backup,"Setup.ini.before"));if(Hash(b)!=rec.BeforeConfigHash)throw new IOException("设置备份校验失败。");Atomic(config,b);}
     else File.Delete(config);
    } else skipped=File.Exists(config)||rec.ConfigExisted;
   }
   if(current==PatchedHash){AssertUnlocked(target);Atomic(target,File.ReadAllBytes(Path.Combine(backup,"original.exe")));}
   return skipped;
  }
  public static Outcome Restore(string target) {return Locked(target,()=>RestoreUnlocked(target));}
  static Outcome RestoreUnlocked(string target) {
   target=FullTarget(target);AssertUnlocked(target);
   string root=BackupRoot(target);CheckDirectory(Path.GetDirectoryName(root));CheckDirectory(root);
   if(!Directory.Exists(root))throw new IOException("没有找到由本安装包创建的备份。");
   foreach(var dir in Directory.GetDirectories(root).OrderByDescending(x=>x,StringComparer.Ordinal)) {
    CheckDirectory(dir);string rp=Path.Combine(dir,"receipt.xml");if(!File.Exists(rp))continue;
    Receipt rec;using(var f=File.OpenRead(rp)){var settings=new System.Xml.XmlReaderSettings{DtdProcessing=System.Xml.DtdProcessing.Prohibit,XmlResolver=null};using(var reader=System.Xml.XmlReader.Create(f,settings))rec=(Receipt)new XmlSerializer(typeof(Receipt)).Deserialize(reader);}
    if(rec.Format!=1||!String.Equals(Path.GetFullPath(rec.Target),target,StringComparison.OrdinalIgnoreCase)||rec.State.StartsWith("RolledBack"))continue;
    bool skipped=Rollback(target,dir,rec);rec.State="RolledBack";SaveReceipt(dir,rec);
    return new Outcome("已恢复原版 2.99.0.0。"+(skipped?"\r\n设置在安装后有变化，已保留当前设置。":"\r\n此次修改的设置已恢复。"),dir);
   }
   throw new IOException("未找到与所选程序对应的可恢复备份。");
  }
 }
}
