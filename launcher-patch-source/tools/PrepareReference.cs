using System;using System.IO;using System.IO.Compression;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Resources;using Mono.Cecil;
class PrepareReference {
 static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> ts){foreach(var t in ts){yield return t;foreach(var n in All(t.NestedTypes))yield return n;}}
 static void Main(string[] a){try{Run(a);}catch(Exception e){Console.WriteLine("ERROR TYPE "+e.GetType().FullName);try{Console.WriteLine("ERROR MESSAGE "+e.Message);}catch{} Environment.ExitCode=1;}}
 static void Run(string[] a){
  string raw=Path.Combine(a[1],"raw"),exposed=Path.Combine(a[1],"exposed");Directory.CreateDirectory(raw);Directory.CreateDirectory(exposed);var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(Path.GetFullPath(raw));resolver.AddSearchDirectory(a[2]);var reader=new ReaderParameters{AssemblyResolver=resolver,InMemory=true};var module=ModuleDefinition.ReadModule(a[0],reader);int deps=0;
  foreach(var r in module.Resources.OfType<EmbeddedResource>()){
   if(r.Name.EndsWith(".g.resources"))File.WriteAllBytes(Path.Combine(raw,"PclBusinessProbe.g.resources"),r.GetResourceData());
   if(r.Name=="PCL.Resources.resources")using(var rr=new ResourceReader(r.GetResourceStream()))foreach(DictionaryEntry item in rr){var bytes=item.Value as byte[];if(bytes!=null&&bytes.Length>2&&bytes[0]==77&&bytes[1]==90){File.WriteAllBytes(Path.Combine(raw,(string)item.Key+".dll"),bytes);deps++;}}
   if(!r.Name.StartsWith("costura.")||!r.Name.EndsWith(".dll.compressed"))continue;
   string file=r.Name.Substring(8);file=file.Substring(0,file.Length-11);
   using(var src=r.GetResourceStream())using(var zip=new DeflateStream(src,CompressionMode.Decompress))using(var dst=File.Create(Path.Combine(a[1],file)))zip.CopyTo(dst);
   deps++;
  }
  foreach(var t in All(module.Types)){
   if(t.Name=="<Module>")continue;
   t.Attributes=(t.Attributes&~TypeAttributes.VisibilityMask)|(t.IsNested?TypeAttributes.NestedPublic:TypeAttributes.Public);
   foreach(var f in t.Fields){f.Attributes=(f.Attributes&~FieldAttributes.FieldAccessMask)|FieldAttributes.Public;if(f.Name=="RestrictMcDownload")f.Constant=false;}
   foreach(var m in t.Methods)m.Attributes=(m.Attributes&~MethodAttributes.MemberAccessMask)|MethodAttributes.Public;
  }
  module.Write(Path.Combine(exposed,"PCL-compile-reference.exe"));
  foreach(var file in Directory.GetFiles(raw,"*.dll")){if(Path.GetFileName(file)=="libwebp64.dll")continue;var dep=ModuleDefinition.ReadModule(file,reader);foreach(var t in All(dep.Types)){if(t.Name=="<Module>")continue;t.Attributes=(t.Attributes&~TypeAttributes.VisibilityMask)|(t.IsNested?TypeAttributes.NestedPublic:TypeAttributes.Public);foreach(var f in t.Fields)f.Attributes=(f.Attributes&~FieldAttributes.FieldAccessMask)|FieldAttributes.Public;foreach(var method in t.Methods)method.Attributes=(method.Attributes&~MethodAttributes.MemberAccessMask)|MethodAttributes.Public;}dep.Write(Path.Combine(exposed,Path.GetFileName(file)));dep.Dispose();}
  Console.WriteLine("Prepared isolated compilation reference and "+deps+" embedded dependencies; no authentication values displayed.");
 }
}
