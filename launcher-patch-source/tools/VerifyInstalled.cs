using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using System.Security.Cryptography;using Mono.Cecil;
class VerifyInstalled {
 static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> ts){foreach(var t in ts){yield return t;foreach(var n in All(t.NestedTypes))yield return n;}}
 static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static void Main(string[] a){try{Run(a);}catch(Exception e){Console.Error.WriteLine(e.Message);Environment.ExitCode=1;}}
 static void Run(string[] a){
  var fingerprint=Assembly.LoadFrom(a[3]).GetType("RestorePcl").GetMethod("Fingerprint",BindingFlags.Static|BindingFlags.NonPublic);
  Func<MethodDefinition,string> fp=m=>(string)fingerprint.Invoke(null,new object[]{m});
  var r=new DefaultAssemblyResolver();r.AddSearchDirectory(a[4]);r.AddSearchDirectory(a[5]);var rp=new ReaderParameters{InMemory=true,AssemblyResolver=r};
  using(var old=ModuleDefinition.ReadModule(a[0],rp))using(var current=ModuleDefinition.ReadModule(a[1],rp)){
   var om=All(old.Types).SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName);var nm=All(current.Types).SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName);
   if(!om.Keys.OrderBy(x=>x).SequenceEqual(nm.Keys.OrderBy(x=>x)))throw new Exception("Method inventory changed");
   var changed=nm.Values.Where(m=>fp(m)!=fp(om[m.FullName])).Select(m=>m.FullName).OrderBy(x=>x).ToArray();
   var expected=File.ReadAllLines(a[2]).Where(x=>x.StartsWith("System.Void PCL.")||x.StartsWith("PCL.ModLoader/")).OrderBy(x=>x).ToArray();
   if(!changed.SequenceEqual(expected)||changed.Length!=8)throw new Exception("Changed methods do not match audited whitelist");
   var ores=old.Resources.OfType<EmbeddedResource>().ToDictionary(x=>x.Name,x=>Hash(x.GetResourceData()));var nres=current.Resources.OfType<EmbeddedResource>().ToDictionary(x=>x.Name,x=>Hash(x.GetResourceData()));
   if(ores.Count!=nres.Count||ores.Any(x=>!nres.ContainsKey(x.Key)||nres[x.Key]!=x.Value))throw new Exception("Embedded resource mismatch");
   var of=All(old.Types).SelectMany(t=>t.Fields).ToDictionary(x=>x.FullName);var nf=All(current.Types).SelectMany(t=>t.Fields).ToDictionary(x=>x.FullName);
   if(!of.Keys.OrderBy(x=>x).SequenceEqual(nf.Keys.OrderBy(x=>x)))throw new Exception("Field inventory changed");
   var fields=nf.Values.Where(x=>x.HasConstant!=of[x.FullName].HasConstant||!Object.Equals(x.Constant,of[x.FullName].Constant)).Select(x=>x.FullName).ToArray();
   if(fields.Length!=1||fields[0]!="System.Boolean PCL.ModSecret::RestrictMcDownload"||!Object.Equals(nf[fields[0]].Constant,false))throw new Exception("Unexpected constant change");
   if(!old.AssemblyReferences.Select(x=>x.FullName).SequenceEqual(current.AssemblyReferences.Select(x=>x.FullName)))throw new Exception("Assembly references changed");
   string originalHash=Hash(File.ReadAllBytes(a[0])),installedHash=Hash(File.ReadAllBytes(a[1]));
   if(originalHash!="6b6e1260cb5a1d6a303855f1c60cab3d0bc1aebba7e40a964180188b271e7386"||installedHash!="6be10780d1f1471bb5bddefdfbcb3966dbc90cc2f00d2dbce579fa75190932b9")throw new Exception("Unexpected executable hash");
   Console.WriteLine("Installed binary static verification: PASS\nOriginal SHA256: "+originalHash+"\nInstalled SHA256: "+installedHash+"\nMethod inventory unchanged: "+nm.Count+"\nChanged method whitelist matched: "+changed.Length+"\nAll other method bodies unchanged: true\nEmbedded resource blobs unchanged: "+nres.Count+"\nChanged constants: RestrictMcDownload true -> false\nAssembly references unchanged: true");
  }
 }
}
