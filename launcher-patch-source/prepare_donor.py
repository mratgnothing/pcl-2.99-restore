from pathlib import Path
import xml.etree.ElementTree as E
root=Path(__file__).resolve().parent
source=root/'source'/'PCL-2.99.0.0'/'Plain Craft Launcher 2'
text=(source/'Modules'/'Minecraft'/'ModModpack.vb').read_text(encoding='utf-8-sig')
assert text.count('RestrictMcDownload')==2
text=text.replace('RestrictMcDownload','False')
donor=root/'ModModpack.restored.vb';donor.write_text(text,encoding='utf-8-sig')
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
imports=['PCL']+[x.attrib['Include'] for x in E.parse(source/'Plain Craft Launcher 2.vbproj').findall('.//m:Import[@Include]',ns)]
refs=root/'tools'/'net48'/'build'/'.NETFramework'/'v4.8'
lines=['/nologo','/target:library','/rootnamespace:PCL','/optionexplicit+','/optioninfer+','/optionstrict-','/optimize+','/debug-','/define:RELEASE=-1','/sdkpath:"'+str(refs)+'"','/imports:'+','.join(imports),'/out:"'+str(root/'donor.dll')+'"']
for p in list(refs.glob('*.dll'))+list((refs/'Facades').glob('*.dll')):
    if p.name.lower() not in ('microsoft.visualbasic.dll','system.enterpriseservices.wrapper.dll','system.enterpriseservices.thunk.dll'):lines.append('/reference:"'+str(p)+'"')
for p in (root/'reference'/'exposed').glob('*'):
    if p.suffix.lower() in ('.exe','.dll') and p.name!='libwebp64.dll':lines.append('/reference:"'+str(p)+'"')
lines.append('"'+str(donor)+'"')
(root/'donor.rsp').write_text('\n'.join(lines)+'\n',encoding='utf-8-sig')
print('Prepared same-version module donor; public-source changes: 2 download guards set to False.')
