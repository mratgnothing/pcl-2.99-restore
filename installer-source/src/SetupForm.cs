using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly:AssemblyTitle("Plain Craft Launcher (PCL) 本地功能恢复补丁安装包")]
[assembly:AssemblyDescription("第三方独立本地修复工具，适用于指定构建的 PCL 2.99.0.0")]
[assembly:AssemblyProduct("Plain Craft Launcher (PCL) 本地功能恢复补丁")]
[assembly:AssemblyCompany("第三方独立修改；PCL 原作者：龙腾猫跃")]
[assembly:AssemblyVersion("1.0.0.0")]
[assembly:AssemblyFileVersion("1.0.0.0")]

namespace PclRestore {
 public sealed class SetupForm:Form {
  readonly TextBox executable=new TextBox(),game=new TextBox(),log=new TextBox();
  readonly CheckBox update=new CheckBox(),ack=new CheckBox();
  readonly Button install=new Button(),restore=new Button(),check=new Button(),choose=new Button(),gameChoose=new Button();
  readonly Label status=new Label();bool busy;
  public SetupForm() {
   Text="PCL 2.99 本地功能恢复 · 修复安装包";Font=new Font("Microsoft YaHei UI",9F);AutoScaleMode=AutoScaleMode.Dpi;
   ClientSize=new Size(820,740);MinimumSize=new Size(835,730);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(245,247,251);
   var header=new Panel{Dock=DockStyle.Top,Height=104,BackColor=Color.FromArgb(26,91,170)};
   header.Controls.Add(new Label{Text="Plain Craft Launcher (PCL)\r\n本地功能恢复补丁",ForeColor=Color.White,Font=new Font(Font.FontFamily,18F,FontStyle.Bold),AutoSize=true,Location=new Point(24,14)});
   Controls.Add(header);
   var tabs=new TabControl{Dock=DockStyle.Fill,Padding=new Point(17,7)};
   var repair=new TabPage("安装 / 回退"){BackColor=BackColor,Padding=new Padding(20)};
   var about=new TabPage("说明与来源"){BackColor=Color.White,Padding=new Padding(16)};tabs.TabPages.AddRange(new[]{repair,about});Controls.Add(tabs);tabs.BringToFront();
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=12};
   layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,115));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));
   var heights=new[]{40F,26F,32F,26F,32F,26F,30F,34F,32F,38F,100F,24F};
   for(int i=0;i<heights.Length;i++)layout.RowStyles.Add(new RowStyle(i==10?SizeType.Percent:SizeType.Absolute,heights[i]));
   repair.Controls.Add(layout);
   Add(layout,new Label{Text="适用于通过文件校验的原版 2.99.0.0。请先正常退出 PCL。\r\n选择 EXE 文件后，会在原位置修复，并保存原版和改动前的设置。",AutoSize=true},0,0,3);
   Add(layout,new Label{Text="PCL 程序文件",AutoSize=true,Anchor=AnchorStyles.Left},0,1,3);
   executable.Dock=DockStyle.Fill;choose.Text="选择程序…";choose.Dock=DockStyle.Fill;check.Text="校验文件";check.Dock=DockStyle.Fill;
   Add(layout,executable,0,2);Add(layout,choose,1,2);Add(layout,check,2,2);
   Add(layout,new Label{Text="Minecraft 游戏目录（可选；留空则保留原设置）",AutoSize=true,Anchor=AnchorStyles.Left},0,3,3);
   game.Dock=DockStyle.Fill;gameChoose.Text="选择目录…";gameChoose.Dock=DockStyle.Fill;Add(layout,game,0,4,2);Add(layout,gameChoose,2,4);
   Add(layout,new Label{Text="目录应包含 versions 子目录。修复不会修改游戏、模组、存档或账号资料。",AutoSize=true,ForeColor=Color.DimGray,Anchor=AnchorStyles.Left},0,5,3);
   update.Text="仅提示重大漏洞更新，避免普通自动更新覆盖本地补丁";update.Checked=true;update.AutoSize=true;Add(layout,update,0,6,3);
   ack.Text="我已了解这是第三方修改，并已阅读“说明与来源”中的适用范围。";ack.AutoSize=true;Add(layout,ack,0,7,3);
   status.Text="等待选择程序";status.AutoSize=true;status.ForeColor=Color.FromArgb(26,91,170);Add(layout,status,0,8,3);
   install.Text="备份并修复";install.BackColor=Color.FromArgb(26,91,170);install.ForeColor=Color.White;install.FlatStyle=FlatStyle.Flat;install.Dock=DockStyle.Fill;
   restore.Text="恢复原版";restore.Dock=DockStyle.Fill;Add(layout,install,0,9,2);Add(layout,restore,2,9);
   log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.Dock=DockStyle.Fill;log.BackColor=Color.White;Add(layout,log,0,10,3);
   Add(layout,new Label{Text="离线修复工具 1.0.0 · 不会自动启动游戏或强行关闭进程",AutoSize=true,ForeColor=Color.DimGray,Anchor=AnchorStyles.Left},0,11,3);
   var aboutLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};aboutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));aboutLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));aboutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));about.Controls.Add(aboutLayout);
   var links=new FlowLayoutPanel{Dock=DockStyle.Fill};links.Controls.Add(Link("原作者：龙腾猫跃 / 赞助","https://meloong.com/afd/a/LTCat"));links.Controls.Add(Link("官方项目与许可","https://github.com/Meloong-Git/PCL/blob/2.99.0.0/LICENCE"));aboutLayout.Controls.Add(links,0,0);
   var description=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Color.White,Text=System.Text.Encoding.UTF8.GetString(PatchEngine.Resource("README.txt"))+"\r\n\r\n—— 官方许可与指南原文 ——\r\n"+System.Text.Encoding.UTF8.GetString(PatchEngine.Resource("LICENCE"))};aboutLayout.Controls.Add(description,0,1);
   var footer=new Label{Text="第三方基于 PCL 独立制作。源码随分享压缩包提供；未获得微软或 Mojang 的背书。",AutoSize=true,Anchor=AnchorStyles.Left};aboutLayout.Controls.Add(footer,0,2);
   choose.Click+=(s,e)=>{using(var d=new OpenFileDialog{Filter="PCL 程序 (*.exe)|*.exe",Title="选择你电脑上的原版 PCL 2.99 程序"})if(d.ShowDialog(this)==DialogResult.OK){executable.Text=d.FileName;DoCheck();}};
   gameChoose.Click+=(s,e)=>{using(var d=new FolderBrowserDialog{Description="选择包含 versions 子目录的 Minecraft 目录",ShowNewFolderButton=false})if(d.ShowDialog(this)==DialogResult.OK)game.Text=d.SelectedPath;};
   check.Click+=(s,e)=>DoCheck();
   install.Click+=async(s,e)=>{if(!ack.Checked){MessageBox.Show(this,"请先阅读说明并勾选确认。",Text);return;}await RunOperation(false);};
   restore.Click+=async(s,e)=>{if(MessageBox.Show(this,"恢复所选位置的原版 PCL 2.99？\r\n游戏和存档不会被删除；安装后又改过的设置会保留。",Text,MessageBoxButtons.OKCancel,MessageBoxIcon.Question)==DialogResult.OK)await RunOperation(true);};
   FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MessageBox.Show(this,"正在写入或回退，请等待操作结束。",Text);}};
  }
  static void Add(TableLayoutPanel p,Control c,int col,int row,int span=1){p.Controls.Add(c,col,row);if(span>1)p.SetColumnSpan(c,span);}
  static LinkLabel Link(string text,string url){var link=new LinkLabel{Text=text,AutoSize=true,Margin=new Padding(0,4,22,0)};link.LinkClicked+=(s,e)=>Process.Start(new ProcessStartInfo(url){UseShellExecute=true});return link;}
  void DoCheck(){try{status.Text=PatchEngine.Inspect(executable.Text);Append(status.Text);}catch(Exception e){status.Text="未通过校验";Append(e.Message);}}
  void Append(string s){log.AppendText(DateTime.Now.ToString("HH:mm:ss")+"  "+s+Environment.NewLine);}
  async Task RunOperation(bool rollback){
   string target=executable.Text,folder=game.Text;bool updates=update.Checked;busy=true;
   foreach(var c in new Control[]{executable,game,choose,gameChoose,check,install,restore,update,ack})c.Enabled=false;
   status.Text=rollback?"正在校验并回退…":"正在校验、备份并修复…";
   try{var outcome=await Task.Run(()=>rollback?PatchEngine.Restore(target):PatchEngine.Install(target,updates,folder));Append(outcome.Message);if(outcome.Backup!=null)Append("备份："+outcome.Backup);status.Text=rollback?"已恢复原版":"修复操作完成";}
   catch(Exception e){status.Text="操作未完成";Append(e.Message);MessageBox.Show(this,e.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
   finally{busy=false;foreach(var c in new Control[]{executable,game,choose,gameChoose,check,install,restore,update,ack})c.Enabled=true;}
  }
  [STAThread]static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new SetupForm());}
 }
}
