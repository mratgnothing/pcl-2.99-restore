using System;using System.IO;using System.Drawing;using System.Reflection;using System.Windows.Forms;
class RenderSetup {
 [STAThread]static void Main(string[] args) {
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  var assembly=Assembly.LoadFrom(args[0]);var type=assembly.GetType("PclRestore.SetupForm",true);
  using(var form=(Form)Activator.CreateInstance(type)) {
   form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.ShowInTaskbar=false;form.Show();Application.DoEvents();
   using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);}
   var text=(TextBox)type.GetField("executable",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
   var button=(Button)type.GetField("check",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
   var status=(Label)type.GetField("status",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
   text.Text=args[2];button.PerformClick();Application.DoEvents();
   if(!status.Text.Contains("校验通过"))throw new Exception("Native form check button did not validate supported original");
   form.Close();Console.WriteLine("Native WinForms render and file-check button: PASS");
  }
 }
}
