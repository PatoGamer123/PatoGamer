using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Linq;
using System.Xml.Linq;
[assembly:System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly:System.Reflection.AssemblyInformationalVersion("1.0 Release")]
class PanelPrestamo:Form {
 [DllImport("user32")]static extern bool LockWorkStation();
 readonly TextBox pair=new TextBox();
 readonly Label state=new Label();readonly byte[] key=new byte[24];
 UdpClient udp;string nonce;DateTime until; IPEndPoint peer;
 static string Hex(byte[] data){return BitConverter.ToString(data).Replace("-","").ToLowerInvariant();}
 static string RandomHex(){using(var r=RandomNumberGenerator.Create()){var b=new byte[16];r.GetBytes(b);return Hex(b);}}
 static string Mac(byte[] key,string data){using(var h=new HMACSHA256(key))return Hex(h.ComputeHash(Encoding.UTF8.GetBytes(data)));}
 readonly ComboBox apps=new ComboBox();
 readonly System.Collections.Generic.SortedDictionary<string,string> catalog=new System.Collections.Generic.SortedDictionary<string,string>(StringComparer.CurrentCultureIgnoreCase);
 readonly Color ink=Color.FromArgb(29,47,72), accent=Color.FromArgb(31,90,151);
 void Place(Control parent,Control child,int x,int y,int w,int h){child.SetBounds(x,y,w,h);parent.Controls.Add(child);}
 void Caption(Control p,string text,int x,int y,int w,int h,bool bold){
    var l=new Label{Text=text,ForeColor=ink,Font=new Font("Segoe UI",bold?12:10,bold?FontStyle.Bold:FontStyle.Regular)};
    Place(p,l,x,y,w,h);
 }
 void ActionButton(Control p,string text,int x,int y,int w,Action action,bool primary){
    var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,BackColor=primary?accent:Color.White,ForeColor=primary?Color.White:ink,Cursor=Cursors.Hand};
    b.FlatAppearance.BorderColor=Color.FromArgb(175,196,210);Place(p,b,x,y,w,37);
    b.Click+=delegate{try{action();}catch(Exception ex){state.Text=ex.Message;}};
 }
 sealed class WindowChoice {
    public IntPtr Handle; public int Pid; public string Title;
    public override string ToString(){return Title;}
 }
 delegate bool Visitor(IntPtr h,IntPtr data);
 [DllImport("user32")]static extern bool EnumWindows(Visitor visit,IntPtr data);
 [DllImport("user32")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32")]static extern bool IsWindow(IntPtr h);
 [DllImport("user32")]static extern IntPtr GetWindow(IntPtr h,int relation);
 [DllImport("user32",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder text,int len);
 [DllImport("user32")]static extern uint GetWindowThreadProcessId(IntPtr h,out int pid);
 [DllImport("user32")]static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32")]static extern bool ShowWindowAsync(IntPtr h,int cmd);
 [DllImport("user32")]static extern bool IsZoomed(IntPtr h);
 [DllImport("user32")]static extern bool IsIconic(IntPtr h);
 [DllImport("user32")]static extern bool SetWindowPos(IntPtr h,IntPtr order,int x,int y,int w,int height,uint flags);
 [DllImport("dwmapi")]static extern int DwmGetWindowAttribute(IntPtr h,int key,out int value,int size);
 WindowChoice active;bool alreadyTop;int oldState;
 bool Valid(WindowChoice w){int pid;return w!=null && IsWindow(w.Handle) && GetWindowThreadProcessId(w.Handle,out pid)!=0 && pid==w.Pid;}
 void LoadApps(){
    WindowChoice selected=apps.SelectedItem as WindowChoice;
    var list=new System.Collections.Generic.List<WindowChoice>();int self=Process.GetCurrentProcess().Id;
    EnumWindows(delegate(IntPtr h,IntPtr unused){
        int pid,cloak;GetWindowThreadProcessId(h,out pid);
        if(pid==self||!IsWindowVisible(h)||GetWindow(h,4)!=IntPtr.Zero||(GetWindowLong(h,-20)&128)!=0)return true;
        if(DwmGetWindowAttribute(h,14,out cloak,4)==0 && cloak!=0)return true;
        var title=new StringBuilder(512);GetWindowText(h,title,title.Capacity);
        if(title.Length==0)return true;
        try{using(var proc=Process.GetProcessById(pid)){
            if(proc.ProcessName=="TextInputHost"||proc.ProcessName=="ShellExperienceHost")return true;
            if(proc.ProcessName=="explorer" && h==GetShellWindow())return true;
            list.Add(new WindowChoice{Handle=h,Pid=pid,Title=proc.ProcessName+"  ·  "+title});
        }}catch(SystemException){}
        return true;
    },IntPtr.Zero);
    apps.Items.Clear();apps.Items.AddRange(list.OrderBy(w=>w.Title).Cast<object>().ToArray());
    if(apps.Items.Count>0){int i=list.Count==0?-1:Array.FindIndex(apps.Items.Cast<WindowChoice>().ToArray(),w=>selected!=null&&w.Handle==selected.Handle);apps.SelectedIndex=Math.Max(0,i);}
 }
 [DllImport("user32")]static extern IntPtr GetShellWindow();
 bool BeginFocus(){
    var w=apps.SelectedItem as WindowChoice;
    if(!Valid(w)){state.Text="Abre una ventana y actualiza la lista.";return false;}
    EndFocus();oldState=IsIconic(w.Handle)?2:IsZoomed(w.Handle)?3:9;alreadyTop=(GetWindowLong(w.Handle,-20)&8)!=0;
    if(!SetWindowPos(w.Handle,new IntPtr(-1),0,0,0,0,3)){state.Text="Windows no permitió fijar esta ventana.";return false;}
    active=w;ShowWindowAsync(w.Handle,3);state.Text="Ventana fijada. Para soltarla, usa el mando o vuelve al panel.";return true;
 }
 bool EndFocus(){
    if(!Valid(active)){active=null;return false;}
    if(!alreadyTop)SetWindowPos(active.Handle,new IntPtr(-2),0,0,0,0,19);
    ShowWindowAsync(active.Handle,oldState);active=null;state.Text="Ventana liberada.";return true;
 } public PanelPrestamo(){
    Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Text="Bloqueo de apps · 1.0 Release";ClientSize=new Size(660,510);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;
    StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(242,246,250);
    var header=new Panel{BackColor=ink};Place(this,header,0,0,660,64);
    Place(header,new Label{Text="Bloqueo de apps    ·    1.0",ForeColor=Color.White,Font=new Font("Segoe UI",21,FontStyle.Bold)},20,9,600,49);
    var tabs=new TabControl();Place(this,tabs,18,80,624,358);
    var setup=new TabPage("Ventanas abiertas"){BackColor=Color.White};var remote=new TabPage("Mando Android"){BackColor=Color.White};tabs.TabPages.Add(setup);tabs.TabPages.Add(remote);
    Caption(setup,"¿Qué aplicación podrá usar?",17,14,560,28,true);
    apps.DropDownStyle=ComboBoxStyle.DropDownList;apps.AutoCompleteSource=AutoCompleteSource.ListItems;apps.AutoCompleteMode=AutoCompleteMode.SuggestAppend;
    Place(setup,apps,18,51,572,31);
    Caption(setup,"Fijar en primer plano",17,104,568,33,true);
    Caption(setup,"Maximiza la ventana elegida y la mantiene encima. Las funciones de Windows y las demás pestañas siguen accesibles.",18,145,570,62,false);
    ActionButton(setup,"Fijar ventana",18,220,169,delegate{BeginFocus();},true);
    ActionButton(setup,"Soltar ventana",199,220,169,delegate{EndFocus();},false);
    ActionButton(setup,"Actualizar lista",380,220,169,LoadApps,false);
    apps.DropDown+=delegate{LoadApps();};    Caption(remote,"Conectar el teléfono por Wi-Fi",18,19,565,31,true);
    Caption(remote,"Cuando tengas el teléfono, activa el mando y copia la dirección y la clave en la app Android.",18,63,565,60,false);
    pair.ReadOnly=true;Place(remote,pair,18,140,571,30);
    ActionButton(remote,"Activar mando",18,196,160,Listen,true);ActionButton(remote,"Desactivar",194,196,140,Stop,false);
    Place(this,state,23,452,610,52);state.ForeColor=ink;state.Text="Este modo de presentación no protege una sesión personal frente a quien tenga acceso físico.";
    Shown+=delegate{try{LoadApps();}catch(Exception ex){state.Text="Error al cargar las aplicaciones: "+ex.Message;}};
 } void Stop(){var s=udp;udp=null;if(s!=null)s.Close();pair.Clear();state.Text="Mando desactivado.";}
 void Listen(){
 Stop();using(var r=RandomNumberGenerator.Create())r.GetBytes(key);
 var ip=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up && n.GetIPProperties().GatewayAddresses.Count>0).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork);
 if(ip==null)throw new Exception("Conecta el PC a la red local.");
 var s=new UdpClient(new IPEndPoint(ip,0));udp=s;pair.Text=s.Client.LocalEndPoint+" "+Hex(key);state.Text="Mando activo. La clave cambia al volver a activarlo.";Read(s);
 }
 async void Read(UdpClient s){try{while(udp==s){var packet=await s.ReceiveAsync();if(udp!=s)return;
 string m=Encoding.ASCII.GetString(packet.Buffer);if(m=="HELLO"){peer=packet.RemoteEndPoint;nonce=RandomHex();until=DateTime.UtcNow.AddSeconds(8);var b=Encoding.ASCII.GetBytes(nonce);s.Send(b,b.Length,peer);continue;}
 if(nonce==null||DateTime.UtcNow>until||!packet.RemoteEndPoint.Equals(peer))continue;
 foreach(string cmd in new[]{"LOCK","RETURN"}){if(m!=cmd+" "+Mac(key,nonce+" "+cmd))continue;nonce=null;
 bool ok=cmd=="LOCK"?BeginFocus():EndFocus();var b=Encoding.ASCII.GetBytes(ok?"OK":"ERROR");s.Send(b,b.Length,peer);break;}
 }}catch(ObjectDisposedException){}catch(SocketException){if(udp==s)Stop();}}
 protected override void OnFormClosed(FormClosedEventArgs e){EndFocus();Stop();base.OnFormClosed(e);}
 [STAThread]static void Main(){Application.EnableVisualStyles();Application.Run(new PanelPrestamo());}
}




