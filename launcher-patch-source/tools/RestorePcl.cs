using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using Mono.Cecil;using Mono.Cecil.Cil;
class RestorePcl {
 static ModuleDefinition target;static Dictionary<string,TypeDefinition> types;static readonly List<string> changes=new List<string>();
 static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> ts){foreach(var t in ts){yield return t;foreach(var n in All(t.NestedTypes))yield return n;}}
 static TypeReference T(TypeReference s){
  if(s==null)return null;if(s is GenericParameter)return s;
  if(s is GenericInstanceType){var x=(GenericInstanceType)s;var r=new GenericInstanceType(T(x.ElementType));foreach(var a in x.GenericArguments)r.GenericArguments.Add(T(a));return r;}
  if(s is ArrayType){var x=(ArrayType)s;return new ArrayType(T(x.ElementType),x.Rank);}
  if(s is ByReferenceType)return new ByReferenceType(T(((ByReferenceType)s).ElementType));
  if(s is PointerType)return new PointerType(T(((PointerType)s).ElementType));
  if(s is OptionalModifierType){var x=(OptionalModifierType)s;return new OptionalModifierType(T(x.ModifierType),T(x.ElementType));}
  if(s is RequiredModifierType){var x=(RequiredModifierType)s;return new RequiredModifierType(T(x.ModifierType),T(x.ElementType));}
  TypeDefinition d;if(types.TryGetValue(s.FullName,out d))return d;
  if(s.Scope!=null&&s.Scope.Name=="donor")throw new Exception("Unmapped donor type: "+s.FullName);
  return target.ImportReference(s);
 }
 static MethodReference M(MethodReference s){
  if(s is GenericInstanceMethod){var x=(GenericInstanceMethod)s;var r=new GenericInstanceMethod(M(x.ElementMethod));foreach(var a in x.GenericArguments)r.GenericArguments.Add(T(a));return r;}
  TypeDefinition d;
  if(types.TryGetValue(s.DeclaringType.GetElementType().FullName,out d)){
   var matches=d.Methods.Where(m=>m.Name==s.Name&&m.Parameters.Count==s.Parameters.Count&&m.GenericParameters.Count==s.GenericParameters.Count&&m.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(s.Parameters.Select(p=>p.ParameterType.FullName))).ToList();
   if(matches.Count!=1)throw new Exception("Expected exact target method: "+s.FullName+"; matches="+matches.Count);
   var md=matches[0];if(!(s.DeclaringType is GenericInstanceType))return md;
   var r=new MethodReference(md.Name,T(s.ReturnType),T(s.DeclaringType)){HasThis=s.HasThis,ExplicitThis=s.ExplicitThis,CallingConvention=s.CallingConvention};foreach(var gp in s.GenericParameters)r.GenericParameters.Add(new GenericParameter(gp.Name,r));foreach(var p in s.Parameters)r.Parameters.Add(new ParameterDefinition(T(p.ParameterType)));return r;
  }
  var ext=new MethodReference(s.Name,T(s.ReturnType),T(s.DeclaringType)){HasThis=s.HasThis,ExplicitThis=s.ExplicitThis,CallingConvention=s.CallingConvention};foreach(var gp in s.GenericParameters)ext.GenericParameters.Add(new GenericParameter(gp.Name,ext));foreach(var p in s.Parameters)ext.Parameters.Add(new ParameterDefinition(T(p.ParameterType)));return ext;
 }
 static FieldReference F(FieldReference s){TypeDefinition d;if(types.TryGetValue(s.DeclaringType.GetElementType().FullName,out d)){var f=d.Fields.Single(x=>x.Name==s.Name);if(!(s.DeclaringType is GenericInstanceType))return f;return new FieldReference(f.Name,T(s.FieldType),T(s.DeclaringType));}return new FieldReference(s.Name,T(s.FieldType),T(s.DeclaringType));}
 static void Clone(MethodDefinition s,MethodDefinition d){
  var b=new MethodBody(d){InitLocals=s.Body.InitLocals,MaxStackSize=s.Body.MaxStackSize};var ins=new Dictionary<Instruction,Instruction>();var vars=new Dictionary<VariableDefinition,VariableDefinition>();
  foreach(var v in s.Body.Variables){var n=new VariableDefinition(T(v.VariableType));vars[v]=n;b.Variables.Add(n);}
  foreach(var i in s.Body.Instructions){var n=Instruction.Create(OpCodes.Nop);n.OpCode=i.OpCode;ins[i]=n;b.Instructions.Add(n);}
  foreach(var i in s.Body.Instructions){var n=ins[i];object o=i.Operand;
   if(o is Instruction)n.Operand=ins[(Instruction)o];else if(o is Instruction[])n.Operand=((Instruction[])o).Select(x=>ins[x]).ToArray();else if(o is VariableDefinition)n.Operand=vars[(VariableDefinition)o];else if(o is ParameterDefinition)n.Operand=d.Parameters[((ParameterDefinition)o).Index];else if(o is MethodReference)n.Operand=M((MethodReference)o);else if(o is FieldReference)n.Operand=F((FieldReference)o);else if(o is TypeReference)n.Operand=T((TypeReference)o);else n.Operand=o;
  }
  Func<Instruction,Instruction> map=x=>x==null?null:ins[x];
  foreach(var e in s.Body.ExceptionHandlers)b.ExceptionHandlers.Add(new ExceptionHandler(e.HandlerType){TryStart=map(e.TryStart),TryEnd=map(e.TryEnd),HandlerStart=map(e.HandlerStart),HandlerEnd=map(e.HandlerEnd),FilterStart=map(e.FilterStart),CatchType=T(e.CatchType)});
  d.Body=b;changes.Add(d.FullName);
 }
 static MethodDefinition Method(string type,string name){return types[type].Methods.Single(m=>m.Name==name);}
 static bool Calls(Instruction i,string name){var r=i.Operand as MethodReference;return r!=null&&r.Name==name;}
 static string Fingerprint(MethodDefinition m){if(!m.HasBody)return "NO BODY";var a=m.Body.Instructions.ToArray();Func<Instruction,int> idx=x=>x==null?-1:Array.IndexOf(a,x);var lines=a.Select(i=>i.OpCode.Name+" "+(i.Operand is Instruction?idx((Instruction)i.Operand).ToString():i.Operand is Instruction[]?String.Join(",",((Instruction[])i.Operand).Select(x=>idx(x))):i.Operand==null?"":i.Operand.ToString())).ToList();lines.Add(String.Join(";",m.Body.Variables.Select(v=>v.VariableType.FullName)));lines.AddRange(m.Body.ExceptionHandlers.Select(e=>e.HandlerType+":"+idx(e.TryStart)+":"+idx(e.TryEnd)+":"+idx(e.HandlerStart)+":"+idx(e.HandlerEnd)+":"+idx(e.FilterStart)+":"+(e.CatchType==null?"":e.CatchType.FullName)));return Hash(System.Text.Encoding.UTF8.GetBytes(String.Join("\n",lines)));}
 static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
 static void LongBranches(MethodDefinition m){if(!m.HasBody)return;foreach(var i in m.Body.Instructions){if(i.OpCode.OperandType!=OperandType.ShortInlineBrTarget)continue;var name=i.OpCode.Name;var field=typeof(OpCodes).GetFields().Single(f=>f.FieldType==typeof(OpCode)&&((OpCode)f.GetValue(null)).Name==name.Substring(0,name.Length-2));i.OpCode=(OpCode)field.GetValue(null);}}
 static void Main(string[] a){try{Run(a);}catch(Exception e){Console.WriteLine("PATCH ERROR "+e.GetType().Name+": "+e.Message);Environment.ExitCode=1;}}
 static void Run(string[] a){
  if(Hash(File.ReadAllBytes(a[0]))!="6b6e1260cb5a1d6a303855f1c60cab3d0bc1aebba7e40a964180188b271e7386")throw new Exception("Source SHA256 is not the audited original 2.99 executable; refusing patch");
  var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(a[3]);resolver.AddSearchDirectory(a[4]);var reader=new ReaderParameters{AssemblyResolver=resolver,InMemory=true};target=ModuleDefinition.ReadModule(a[0],reader);types=All(target.Types).ToDictionary(t=>t.FullName);var before=All(target.Types).SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName,Fingerprint);var resources=target.Resources.OfType<EmbeddedResource>().ToDictionary(r=>r.Name,r=>Hash(r.GetResourceData()));
  var donor=ModuleDefinition.ReadModule(a[1],reader);var source=donor.GetType("PCL.ModModpack");
  foreach(var s in source.Methods.Where(m=>m.Name=="ModpackInstall")){var d=types["PCL.ModModpack"].Methods.Single(m=>m.Name==s.Name&&m.Parameters.Count==s.Parameters.Count);Clone(s,d);}
  // Restore the existing version-list loader and retain its original finish handler.
  var init=Method("PCL.PageDownloadInstall","LoaderInit");var old=init.Body.Instructions.ToArray();var body=new MethodBody(init);var il=body.GetILProcessor();Action<OpCode,object> emit=(op,o)=>{var i=Instruction.Create(OpCodes.Nop);i.OpCode=op;i.Operand=o;il.Append(i);};Action<OpCode> op0=op=>emit(op,null);
  op0(OpCodes.Ldarg_0);emit(OpCodes.Call,old.Single(i=>Calls(i,"ControlInit")).Operand);op0(OpCodes.Ldarg_0);
  foreach(var n in new[]{"get_LoadMinecraft","get_PanLoad","get_PanAllBack"}){op0(OpCodes.Ldarg_0);emit(OpCodes.Callvirt,old.Single(i=>Calls(i,n)).Operand);}op0(OpCodes.Ldnull);emit(OpCodes.Ldsfld,types["PCL.ModDownload"].Fields.Single(f=>f.Name=="DlClientListLoader"));op0(OpCodes.Ldarg_0);emit(OpCodes.Ldftn,old.Last(i=>i.OpCode==OpCodes.Ldftn).Operand);emit(OpCodes.Newobj,old.Last(i=>i.OpCode==OpCodes.Newobj).Operand);op0(OpCodes.Ldnull);op0(OpCodes.Ldc_I4_1);emit(OpCodes.Call,old.Single(i=>Calls(i,"PageLoaderInit")).Operand);op0(OpCodes.Ret);init.Body=body;changes.Add(init.FullName);
  var modify=Method("PCL.PageDownloadModify","LoadInstanceFinished");var card=modify.Body.Instructions.Single(i=>Calls(i,"get_CardMinecraft"));if(card.Next.OpCode!=OpCodes.Ldc_I4_2)throw new Exception("Unexpected Minecraft card visibility");card.Next.OpCode=OpCodes.Ldc_I4_0;changes.Add(modify.FullName);
  var launch=Method("PCL.PageLaunchLeft","RefreshButtonsUI");var marker=launch.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldstr&&((string)i.Operand).Contains("无 Minecraft 版本，下载已禁用"));marker.Operand="启动按钮：无 Minecraft 版本，可下载（本地功能恢复）";var text=marker.Next;while(!(text.OpCode==OpCodes.Ldstr&&(string)text.Operand=="启动游戏"))text=text.Next;text.Operand="下载游戏";var enabled=text.Next;while(!Calls(enabled,"set_IsEnabled"))enabled=enabled.Next;if(enabled.Previous.OpCode!=OpCodes.Ldc_I4_0)throw new Exception("Unexpected launch disabled flag");enabled.Previous.OpCode=OpCodes.Ldc_I4_1;changes.Add(launch.FullName);
  // Restore install action for modpacks while preserving save-only clicks for other resources.
  var detail=Method("PCL.PageDownloadResourceDetail","InitializeStackResourceVersions");var savePtr=detail.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldftn&&((MethodReference)i.Operand).Name=="Save_Click");var begin=savePtr.Previous;var saveCtor=savePtr.Next;var common=saveCtor.Next.Next;var proc=detail.Body.GetILProcessor();var inserts=new List<Instruction>();Action<OpCode,object> add=(op,o)=>{var i=Instruction.Create(OpCodes.Nop);i.OpCode=op;i.Operand=o;inserts.Add(i);};
  add(OpCodes.Ldarg_0,null);add(OpCodes.Ldfld,types["PCL.PageDownloadResourceDetail"].Fields.Single(f=>f.Name=="TargetResourceType"));add(OpCodes.Ldc_I4_2,null);add(OpCodes.Bne_Un,begin);add(OpCodes.Ldarg_0,null);add(OpCodes.Ldftn,Method("PCL.PageDownloadResourceDetail","Install_Click"));add(OpCodes.Newobj,saveCtor.Operand);add(OpCodes.Ldarg_0,null);add(OpCodes.Ldftn,savePtr.Operand);add(OpCodes.Newobj,types["PCL.MyIconButton/ClickEventHandler"].Methods.Single(m=>m.IsConstructor));add(OpCodes.Br,common);foreach(var i in inserts)proc.InsertBefore(begin,i);changes.Add(detail.FullName);
  var flag=types["PCL.ModSecret"].Fields.Single(f=>f.Name=="RestrictMcDownload");flag.Constant=false;
  var about=Method("PCL.PageOtherAbout","PageOtherAbout_Loaded");var aboutVersion=about.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldstr&&((string)i.Operand).Contains("2.99.0.0"));aboutVersion.Operand="Plain Craft Launcher (PCL) 本地功能恢复版 2.99.0.0";changes.Add(about.FullName);
  var updates=Method("PCL.FormMain","ShowUpdateLog");foreach(var i in updates.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ldstr&&((string)i.Operand).StartsWith("删除：为遵守相关法律法规")))i.Operand="本地恢复：MC 下载、安装、整合包安装与版本组件修改（非官方修改）";changes.Add(updates.FullName);
  // Clearly identify the private local modification without changing version ordering or login behavior.
  var display=types["PCL.ModBase"].Methods.SingleOrDefault(m=>m.Name=="get_VersionDisplay");if(display!=null&&display.HasBody){var ret=display.Body.Instructions.Last(i=>i.OpCode==OpCodes.Ret);var concat=new MethodReference("Concat",target.TypeSystem.String,target.ImportReference(typeof(string))){HasThis=false};concat.Parameters.Add(new ParameterDefinition(target.TypeSystem.String));concat.Parameters.Add(new ParameterDefinition(target.TypeSystem.String));var p=display.Body.GetILProcessor();p.InsertBefore(ret,Instruction.Create(OpCodes.Ldstr," [本地功能恢复版]"));p.InsertBefore(ret,Instruction.Create(OpCodes.Call,concat));changes.Add(display.FullName);}
  var title=target.Assembly.CustomAttributes.SingleOrDefault(c=>c.AttributeType.FullName=="System.Reflection.AssemblyTitleAttribute");if(title!=null)title.ConstructorArguments[0]=new CustomAttributeArgument(target.TypeSystem.String,"Plain Craft Launcher (PCL) 本地功能恢复版");
  foreach(var m in All(target.Types).SelectMany(t=>t.Methods).Where(m=>changes.Contains(m.FullName)))LongBranches(m);
  var changed=All(target.Types).SelectMany(t=>t.Methods).Where(m=>before[m.FullName]!=Fingerprint(m)).Select(m=>m.FullName).ToList();if(changed.Except(changes).Any())throw new Exception("Unexpected method change");if(resources.Any(r=>Hash(((EmbeddedResource)target.Resources.Single(x=>x.Name==r.Key)).GetResourceData())!=r.Value))throw new Exception("Embedded resources changed");
  var unexpectedRefs=target.AssemblyReferences.Where(r=>r.Name=="donor"||r.Name.Contains("compile-reference")).ToList();if(unexpectedRefs.Any())throw new Exception("Donor assembly leaked into patched references");
  target.Write(a[2]);var report=new List<string>{"Source SHA256: "+Hash(File.ReadAllBytes(a[0])),"Patched SHA256: "+Hash(File.ReadAllBytes(a[2])),"Modified method count: "+changed.Count,"Embedded resources unchanged: "+resources.Count,"Authentication method bodies unchanged: true","Restriction metadata flag: false"};report.AddRange(changed);File.WriteAllLines(a[2]+".audit.txt",report,System.Text.Encoding.UTF8);Console.WriteLine(String.Join("\n",report));
 }
}
