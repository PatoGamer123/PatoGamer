using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Acuario Studio")]
[assembly: System.Reflection.AssemblyProduct("Acuario Studio")]
[assembly: System.Reflection.AssemblyVersion("1.1.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.1.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("1.1 Release")]
class Tank : Control {
    readonly Timer timer = new Timer();
    readonly Font fishFont = new Font("Consolas",25,FontStyle.Bold);
    readonly Random random = new Random();
    readonly List<PointF> crumbs = new List<PointF>();
    readonly List<PointF> fish = new List<PointF> { new PointF(.68f,.56f),new PointF(.43f,.28f),new PointF(.22f,.68f),new PointF(.80f,.38f) };
    readonly List<float> dir=new List<float>{1,-1,1,-1}, target=new List<float>{.54f,.30f,.68f,.38f};
    readonly Brush[] colors={Brushes.PaleTurquoise,Brushes.PaleGreen,Brushes.LightPink,Brushes.Khaki,Brushes.Plum};
    struct Guest { public int Photo; public PointF Position; public long Until; }
    readonly List<Guest> guests=new List<Guest>();
    readonly Bitmap[] photos=new Bitmap[2];
    void RollGuests(){
        for(int photo=0;photo<2;photo++){
            if(random.Next(photo==0?10:50)!=0)continue;
            if(photos[photo]==null){
                using(var stream=typeof(Tank).Assembly.GetManifestResourceStream("bunny"+photo+".png"))
                using(var source=new Bitmap(stream))photos[photo]=new Bitmap(source);
            }
            guests.Add(new Guest{Photo=photo,Position=new PointF(.13f+.74f*(float)random.NextDouble(),.20f+.57f*(float)random.NextDouble()),Until=clock.ElapsedMilliseconds+60000});
        }
    }
    public void RemoveFish(){
        int last=fish.Count-1;
        if(last<0){Say("El acuario está vacío.");return;}
        fish.RemoveAt(last);dir.RemoveAt(last);target.RemoveAt(last);
        Say(fish.Count==1?"Queda un pez.":"Quedan "+fish.Count+" peces.");
        Invalidate();
    }
    public void AddFish(){
        float y=.18f+.55f*(float)random.NextDouble();
        fish.Add(new PointF(.16f+.68f*(float)random.NextDouble(),y));
        dir.Add(random.Next(2)==0?-1:1);target.Add(y);
        RollGuests();
        Say("Ahora hay "+fish.Count+" peces en el acuario.");
        Invalidate();
    }
    float time, ripple, hook;
    PointF tap;
    Bitmap scenery;
    public Action<string> Say;
    readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    long nextVisibilityCheck,lastFrame,remainder;
    bool visibleWater=true,precise;
    int frameInterval=100;
    [DllImport("winmm")] static extern uint timeBeginPeriod(uint period);
    [DllImport("winmm")] static extern uint timeEndPeriod(uint period);
    public void SetRate(int fps){
        frameInterval=(int)Math.Round(1000.0/fps);
        if(fps>=30 && !precise)precise=timeBeginPeriod(1)==0;
        if(fps<30 && precise){timeEndPeriod(1);precise=false;}
        timer.Interval=frameInterval;
    }
    public Tank(){

        DoubleBuffered=true;
        ResizeRedraw=true;
        timer.Interval=100;
        timer.Tick+=delegate {
            long now=clock.ElapsedMilliseconds;
            for(int i=guests.Count-1;i>=0;i--)if(now>=guests[i].Until)guests.RemoveAt(i);
            if(now>=nextVisibilityCheck){
                visibleWater=Desktop.WaterVisible(this);
                nextVisibilityCheck=now+700;
            }
            timer.Interval=visibleWater?frameInterval:700;
            if(visibleWater){
                remainder+=Math.Min(125,now-lastFrame);
                while(remainder>=25){Step();remainder-=25;}
                Invalidate();
            }
            lastFrame=now;
        };        timer.Start();
        MouseDown+=delegate(object s, MouseEventArgs e){Tap(e.X,e.Y);};
    }
    public void Tap(float x,float y){
        tap=new PointF(x,y); ripple=1;
        for(int i=0;i<fish.Count;i++){dir[i]=fish[i].X*Width>x?1:-1; target[i]=Math.Max(.16f,Math.Min(.75f,fish[i].Y+.12f));}
        Say("Los peces sintieron el toque en el cristal.");
    }
    public void Feed(){
        for(int i=0;i<9 && crumbs.Count<45;i++)crumbs.Add(new PointF(.27f+(float)random.NextDouble()*.46f,.05f+(float)random.NextDouble()*.07f));
        Say("¡A comer! Los peces buscan las miguitas.");
    }
    public void Fish(){hook=1;Say("Los peces persiguen el señuelo.");}
    void Step(){
        time+=.025f; ripple=Math.Max(0,ripple-.018f);hook=Math.Max(0,hook-.004f);
        for(int j=crumbs.Count-1;j>=0;j--){PointF c=crumbs[j];c.Y+=.00085f;if(c.Y>.85f)crumbs.RemoveAt(j);else crumbs[j]=c;}
        for(int i=0;i<fish.Count;i++){
            PointF f=fish[i];float speed=.0011f*(i==0?1:.82f);
            float y=target[i]+.055f*(float)Math.Sin(time*.7+i);
            if(ripple<=0 && (crumbs.Count>0 || hook>0)){
                int nearest=-1; double dist=10;
                for(int j=0;j<crumbs.Count;j++){double d=Math.Pow(f.X-crumbs[j].X,2)+Math.Pow(f.Y-crumbs[j].Y,2);if(d<dist){nearest=j;dist=d;}}
                PointF goal=nearest>=0?crumbs[nearest]:new PointF(.5f,.36f);
                y=goal.Y; if(Math.Abs(goal.X-f.X)>.008)dir[i]=Math.Sign(goal.X-f.X);
                speed=Math.Min(.003f,Math.Abs(goal.X-f.X));
                if(nearest>=0 && dist<.001)crumbs.RemoveAt(nearest);
            } else if(ripple>0)speed*=3;
            f.X+=dir[i]*speed; f.Y+=Math.Max(-.003f,Math.Min(.003f,y-f.Y));
            if(f.X<.13f || f.X>.87f){dir[i]=f.X<.13f?1:-1;target[i]=.2f+.5f*(float)random.NextDouble();}
            fish[i]=f;
        }
    }
    protected override void OnPaint(PaintEventArgs e){
        int w=Width,h=Height;if(w<1||h<1)return;
        Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        if(scenery==null || scenery.Size!=ClientSize){
            if(scenery!=null)scenery.Dispose();
            scenery=new Bitmap(w,h,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using(var background=Graphics.FromImage(scenery))DrawScenery(background,w,h);
        }
        g.DrawImageUnscaled(scenery,0,0);
        for(int j=0;j<18 && Scene!=1;j++){
            float r=3+(j*17%27), x=((j*137%997)/997f)*w+5*(float)Math.Sin(time+j),y=(1-((time*(.024f+j%4*.006f)+j*.137f)%1))*h;
            using(var path=new GraphicsPath()){path.AddEllipse(x-r,y-r,r*2,r*2);using(var shine=new PathGradientBrush(path)){shine.CenterPoint=new PointF(x-r*.4f,y-r*.48f);shine.CenterColor=Color.FromArgb(130,245,253,255);shine.SurroundColors=new[]{Color.FromArgb(12,210,237,255)};g.FillPath(shine,path);}}
            using(var rim=new Pen(Color.FromArgb(100,200,234,255)))g.DrawEllipse(rim,x-r,y-r,r*2,r*2);
        }
        foreach(var c in crumbs)g.FillEllipse(Brushes.SandyBrown,c.X*w,c.Y*h,6,5);
        if(hook>0){g.DrawLine(Pens.Silver,w/2,0,w/2,h*.36f);g.FillEllipse(Brushes.Coral,w/2-5,h*.36f,10,7);}
        for(int i=0;i<fish.Count;i++){
            string glyph=dir[i]>0?"><(((o>":"<o)))><";
            SizeF size=g.MeasureString(glyph,fishFont);
            float x=fish[i].X*w-size.Width/2,y=fish[i].Y*h-size.Height/2;
            g.DrawString(glyph,fishFont,Brushes.DarkSlateBlue,x+2,y+2);
            g.DrawString(glyph,fishFont,colors[i%colors.Length],x,y);
        }
        foreach(var guest in guests){
            var photo=photos[guest.Photo];
            float width=Math.Min(140,w*.18f),height=width*photo.Height/photo.Width;
            g.DrawImage(photo,guest.Position.X*w-width/2,guest.Position.Y*h-height/2+4*(float)Math.Sin(time),width,height);
        }
        if(ripple>0){float r=4+(1-ripple)*68;using(var pen=new Pen(Color.FromArgb((int)(180*ripple),Color.White),2))g.DrawEllipse(pen,tap.X-r,tap.Y-r,2*r,2*r);}
    }
    public int Scene;
    public void SetScene(int scene){
        Scene=scene;
        if(scenery!=null){scenery.Dispose();scenery=null;}
        Invalidate();
    }
    void DrawScenery(Graphics g,int w,int h){
        if(Scene==1){g.Clear(Color.Magenta);return;}
        if(Scene>=2){
            bool night=Scene==2;
            Color top=night?Color.FromArgb(42,40,96):Color.FromArgb(134,216,177);
            Color bottom=night?Color.FromArgb(8,16,42):Color.FromArgb(10,73,76);
            using(var water=new LinearGradientBrush(ClientRectangle,top,bottom,90f))g.FillRectangle(water,ClientRectangle);
            if(night){
                for(int j=0;j<96;j++)g.FillEllipse(Brushes.LightCyan,(j*179%997)*w/997f,(j*97%239)*h/300f,2+j%3,2+j%3);
            }
            using(var kelp=new SolidBrush(night?Color.FromArgb(34,40,70):Color.FromArgb(21,90,78))){
                for(int j=0;j<29;j++){
                    float x=(j*133%997)*w/997f, y=h*(.67f+.23f*(j%7)/7);
                    using(var blade=new GraphicsPath()){
                        blade.StartFigure();blade.AddBezier(x,h,x+56,y+40,x-30,y+60,x+10,y);
                        blade.AddBezier(x+10,y,x-14,y+75,x+40,y+66,x+18,h);
                        blade.CloseFigure();g.FillPath(kelp,blade);
                    }
                }
            }
            return;
        }
        using(var sea=new LinearGradientBrush(ClientRectangle,Color.FromArgb(91,162,209),Color.FromArgb(19,47,89),90f))g.FillRectangle(sea,ClientRectangle);
        using(var ray=new SolidBrush(Color.FromArgb(13,230,250,255)))
        for(int j=0;j<9;j++){int x=j*w/7;g.FillPolygon(ray,new Point[]{new Point(x,0),new Point(x+w/19,0),new Point(x-w/12,h),new Point(x-w/7,h)});}
        for(int j=0;j<3;j++)for(int k=0;k<4;k++)g.DrawString((k%2==0?"(":")"),Font,Brushes.LightGreen,w*(.16f+.31f*j)+3*(float)Math.Sin(time+k),h*.88f-13*(k+1));
        var sand=new Rectangle(0,(int)(h*.88),w,Math.Max(1,h-(int)(h*.88)));
        using(var bed=new LinearGradientBrush(sand,Color.BurlyWood,Color.FromArgb(124,87,50),90f))g.FillRectangle(bed,sand);
        for(int j=0;j<145;j++)g.FillEllipse(Brushes.Wheat,(j*223%997)*w/997f,sand.Y+(j*71%101)*sand.Height/101f,2+j%3,2);
        for(int x=0;x<w;x+=19)g.DrawString("~",Font,Brushes.LightCyan,x,2+(float)Math.Sin(time+x));
    }
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();foreach(var photo in photos)if(photo!=null)photo.Dispose();if(precise)timeEndPeriod(1);fishFont.Dispose();if(scenery!=null)scenery.Dispose();}base.Dispose(disposing);}
}
class Desktop {
    delegate bool Visitor(IntPtr window,IntPtr data);
    [DllImport("user32")] static extern bool EnumWindows(Visitor visit,IntPtr data);
    [DllImport("user32",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string title);
    [DllImport("user32",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
    [DllImport("user32")] static extern IntPtr SendMessageTimeout(IntPtr h,uint msg,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
    [DllImport("user32")] static extern IntPtr SetParent(IntPtr child,IntPtr parent);
    [DllImport("user32")] static extern IntPtr GetParent(IntPtr child);
    [DllImport("user32")] static extern bool GetClientRect(IntPtr h,out Rect rect);
    [DllImport("user32")] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32")] public static extern bool SetProcessDPIAware();
    [DllImport("user32")] static extern int GetWindowLong(IntPtr h,int index);
    [DllImport("user32")] static extern int SetWindowLong(IntPtr h,int index,int value);
    [DllImport("user32")] static extern bool SetLayeredWindowAttributes(IntPtr h,uint key,byte alpha,uint flags);
    static IntPtr iconView,iconList;
    [DllImport("user32")] static extern IntPtr WindowFromPoint(Point p);
    static bool OnDesktop(Point p){
        var h=WindowFromPoint(p);
        return h!=IntPtr.Zero && (h==iconList || h==iconView);
    }
    public static bool WaterVisible(Control tank){
        // Sample each monitor so a covered monitor does not pause its neighbour.
        foreach(var monitor in Screen.AllScreens){
            var r=monitor.WorkingArea;
            for(int x=1;x<=3;x++)for(int y=1;y<=3;y++)
                if(OnDesktop(new Point(r.Left+r.Width*x/4,r.Top+r.Height*y/4)))return true;
        }
        return false;
    }
    internal sealed class Taps : IDisposable {
        delegate IntPtr MouseProc(int code,IntPtr message,IntPtr data);
        [DllImport("user32")] static extern IntPtr SetWindowsHookEx(int id,MouseProc callback,IntPtr module,uint thread);
        [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32")] static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32")] static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        readonly MouseProc callback;
        readonly IntPtr hook;
        readonly Tank tank;
        public Taps(Tank target){
            tank=target; callback=Observe;
            hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);
            if(hook==IntPtr.Zero)throw new InvalidOperationException("No se pudo activar el toque del cristal.");
        }
        IntPtr Observe(int code,IntPtr message,IntPtr data){
            if(code>=0 && message.ToInt32()==0x202){
                var point=new Point(Marshal.ReadInt32(data),Marshal.ReadInt32(data,4));
                if(OnDesktop(point) && !tank.IsDisposed)tank.BeginInvoke((Action)delegate{CheckTap(point);});
            }
            return CallNextHookEx(hook,code,message,data);
        }
        void CheckTap(Point screen){
            try{
                var element=System.Windows.Automation.AutomationElement.FromPoint(new System.Windows.Point(screen.X,screen.Y));
                if(element==null || element.Current.ControlType==System.Windows.Automation.ControlType.ListItem)return;
                Point local=tank.PointToClient(screen);
                tank.Tap(local.X,local.Y);
                tank.Invalidate();
            }catch(System.Windows.Automation.ElementNotAvailableException){}
        }
        public void Dispose(){UnhookWindowsHookEx(hook);}
    }
    public static void ChangeScene(IntPtr window,bool transparent){
        SetWindowLong(window,-20,GetWindowLong(window,-20)|0x80000);
        SetLayeredWindowAttributes(window,0x00ff00ff,255,transparent?3u:2u);
    }
    struct Rect { public int left,top,right,bottom; }
    public static bool Attach(IntPtr window){
        IntPtr zero=IntPtr.Zero, prog=FindWindow("Progman",null),result;
        if(prog==zero)return false;
        SendMessageTimeout(prog,0x052c,new IntPtr(13),new IntPtr(1),2,1000,out result);
        IntPtr host=zero,after=zero;
        EnumWindows(delegate(IntPtr top,IntPtr unused){
            IntPtr icons=FindWindowEx(top,zero,"SHELLDLL_DefView",null);
            if(icons==zero)return true;
            iconView=icons;iconList=FindWindowEx(icons,zero,"SysListView32",null);
            if(top==prog && FindWindowEx(prog,zero,"WorkerW",null)!=zero){host=prog;after=icons;}
            else host=FindWindowEx(zero,top,"WorkerW",null);
            return host==zero;
        },zero);
        Rect rect;
        if(host==zero || !GetClientRect(host,out rect) || rect.right<=0 || rect.bottom<=0)return false;
        SetWindowLong(window,-16,(GetWindowLong(window,-16) & 0x7fffffff) | 0x40000000); SetParent(window,host);
        if(host==prog){
            SetWindowLong(window,-20,GetWindowLong(window,-20) | 0x80000);
            SetLayeredWindowAttributes(window,0,255,2);
        }
        return GetParent(window)==host && SetWindowPos(window,after,0,0,rect.right,rect.bottom,0x50);
    }
}
class BackgroundForm : Form {
    protected override CreateParams CreateParams {
        get {var p=base.CreateParams;p.ExStyle|=0x08000080;return p;}
    }
    protected override bool ShowWithoutActivation {get{return true;}}
}
class Studio : Form {
    readonly Label state=new Label(), meter=new Label();
    readonly Button apply=new Button(), remove=new Button(), feed=new Button();
    readonly Button fishing=new Button(), addFish=new Button(), removeFish=new Button();
    readonly NotifyIcon tray=new NotifyIcon();
    readonly ContextMenuStrip trayMenu=new ContextMenuStrip();
    bool quitting;
    readonly string[] names={"Acuario azul","Mi escritorio","Mar nocturno","Bosque de algas"};
    readonly Button[] scenes=new Button[4];
    int scene=1;
    readonly ComboBox fps=new ComboBox();
    readonly Timer monitor=new Timer{Interval=1000};
    readonly System.Diagnostics.Process process=System.Diagnostics.Process.GetCurrentProcess();
    readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    readonly System.Threading.EventWaitHandle reopen;
    TimeSpan previousCpu;
    double previousSeconds;
    BackgroundForm wallpaper;
    Tank tank;
    Desktop.Taps taps;
    public Studio(System.Threading.EventWaitHandle signal,int command){
        reopen=signal;
        Text="Acuario Studio"; ClientSize=new Size(606,429);
        Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.FixedSingle;
        MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor=Color.FromArgb(245,248,246);ForeColor=Color.FromArgb(23,52,62);
        var header=new Panel{BackColor=Color.FromArgb(14,46,57)};
        Place(header,0,0,606,72);
        header.Paint+=delegate(object sender,PaintEventArgs e){e.Graphics.DrawIcon(Icon,new Rectangle(20,12,48,48));};
        header.Controls.Add(new Label{Text="1.1 Release",ForeColor=Color.PaleTurquoise,Bounds=new Rectangle(495,27,105,26)});
        header.Controls.Add(new Label{Text="Acuario Studio",ForeColor=Color.White,Font=new Font("Segoe UI",19,FontStyle.Bold),Bounds=new Rectangle(80,14,398,45)});
        Place(new Label{Text="Fondo de pantalla",Font=new Font("Segoe UI",11,FontStyle.Bold)},20,86,450,25);
        Color[] palette={Color.FromArgb(42,98,153),Color.FromArgb(56,71,95),Color.FromArgb(60,47,103),Color.FromArgb(30,107,86)};
        for(int j=0;j<names.Length;j++){
            int index=j;
            var tile=new Button{Text=names[j],FlatStyle=FlatStyle.Flat,BackColor=palette[j],ForeColor=Color.White,Cursor=Cursors.Hand};
            tile.FlatAppearance.BorderColor=Color.FromArgb(67,220,178);
            scenes[j]=tile;Place(tile,20+j*143,117,137,58);
            tile.Click+=delegate{SelectScene(index);};
        }
        SelectScene(scene);
        Place(state,21,192,565,26);        apply.Text="Poner fondo";remove.Text="Quitar fondo";feed.Text="Alimentar";fishing.Text="Pescar";
        addFish.Text="Añadir pez";
        addFish.Click+=delegate{RunCommand(3);};
        removeFish.Text="Quitar pez";
        removeFish.Click+=delegate{RunCommand(4);};
        Button[] buttons={apply,remove,feed,fishing,addFish,removeFish};
        for(int i=0;i<buttons.Length;i++){
            var button=buttons[i];button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;
            button.BackColor=i==0?Color.FromArgb(31,119,116):Color.FromArgb(208,229,227);
            if(i==0)button.ForeColor=Color.White;
            Place(button,17+i*96,226,92,38);
        }
        apply.Click+=delegate{StartWater();};remove.Click+=delegate{StopWater("Fondo anterior restaurado.");};
        feed.Click+=delegate{RunCommand(1);};fishing.Click+=delegate{RunCommand(2);};
        Place(new Label{Text="Fotogramas por segundo"},22,290,250,28);
        fps.DropDownStyle=ComboBoxStyle.DropDownList;
        fps.Items.AddRange(new object[]{10,15,30,45,60});fps.SelectedItem=60;
        Place(fps,459,285,123,32);
        fps.SelectedIndexChanged+=delegate{if(tank!=null)tank.SetRate((int)fps.SelectedItem);};
        Place(meter,22,339,555,29);
        Place(new Label{Text="El fondo se detiene si la app alcanza ambos límites a la vez: 10 % de CPU y 100 MB de RAM."},22,377,560,48);        tray.Text="Acuario Studio";
        tray.Icon=Icon;
        trayMenu.Items.Add("Abrir panel",null,delegate{OpenPanel();});
        trayMenu.Items.Add("Cerrar Acuario Studio",null,delegate{Quit();});
        tray.ContextMenuStrip=trayMenu;
        tray.DoubleClick+=delegate{OpenPanel();};
        tray.Visible=true;
        monitor.Tick+=delegate{Sample();};
        Shown+=delegate{StartWater();RunCommand(command);monitor.Start();};
    }
    void SelectScene(int index){
        scene=index;
        for(int j=0;j<scenes.Length;j++)scenes[j].FlatAppearance.BorderSize=j==index?4:0;
        if(tank!=null){tank.SetScene(index);Desktop.ChangeScene(wallpaper.Handle,index==1);state.Text="Fondo activo · "+names[index];}
    }
    void OpenPanel(){Show();WindowState=FormWindowState.Normal;Activate();}
    void Quit(){quitting=true;Close();}
    protected override void OnFormClosing(FormClosingEventArgs e){
        if(!quitting && e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}
        base.OnFormClosing(e);
    }
    void RunCommand(int command){
        if(command==99){Quit();return;}
        if(command==4){if(tank!=null)tank.RemoveFish();return;}
        if(command>=1 && command<=3){
            StartWater();
            if(tank!=null){if(command==1)tank.Feed();else if(command==2)tank.Fish();else tank.AddFish();}
        }else if(fps.Items.Contains(command))fps.SelectedItem=command;
    }
    protected override void WndProc(ref Message m){
        if(m.Msg==Program.CommandMessage){RunCommand(m.WParam.ToInt32());return;}
        base.WndProc(ref m);
    }
    void Place(Control c,int x,int y,int w,int h){c.SetBounds(x,y,w,h);Controls.Add(c);}
    void StartWater(){
        if(wallpaper!=null)return;
        try{
            wallpaper=new BackgroundForm{FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=Point.Empty};
            tank=new Tank{Dock=DockStyle.Fill};tank.Say=delegate(string message){state.Text=message;};tank.SetRate((int)fps.SelectedItem);
            wallpaper.Controls.Add(tank);
            if(!Desktop.Attach(wallpaper.Handle))throw new InvalidOperationException("Windows no pudo mostrar el acuario.");
            SelectScene(scene);wallpaper.Show();taps=new Desktop.Taps(tank);
            apply.Enabled=false;remove.Enabled=removeFish.Enabled=addFish.Enabled=fishing.Enabled=feed.Enabled=true;
            state.Text="Fondo activo · "+names[scene];ResetSample();
        }catch(Exception ex){StopWater(ex.Message);}
    }
    void StopWater(string reason){
        if(taps!=null){taps.Dispose();taps=null;}
        if(wallpaper!=null){wallpaper.Dispose();wallpaper=null;tank=null;}
        apply.Enabled=true;remove.Enabled=removeFish.Enabled=addFish.Enabled=fishing.Enabled=feed.Enabled=false;state.Text=reason;
    }
    void ResetSample(){process.Refresh();previousCpu=process.TotalProcessorTime;previousSeconds=clock.Elapsed.TotalSeconds;}
    void Sample(){
        if(reopen.WaitOne(0))OpenPanel();
        process.Refresh();
        double now=clock.Elapsed.TotalSeconds;
        if(now-previousSeconds<0.9)return;
        TimeSpan used=process.TotalProcessorTime;
        double cpu=100*(used-previousCpu).TotalSeconds/(Math.Max(.001,now-previousSeconds)*Environment.ProcessorCount);
        previousCpu=used;previousSeconds=now;
        double mb=process.WorkingSet64/1000000.0;
        meter.Text=String.Format("CPU   {0:F1} %       RAM   {1:F1} MB",cpu,mb);
        if(tank!=null && (cpu>=10 && mb>=100)){
            StopWater(String.Format("Detenido · CPU {0:F1} % y RAM {1:F1} MB",cpu,mb));
        }
    }
    protected override void Dispose(bool disposing){
        if(disposing){tray.Visible=false;tray.Dispose();trayMenu.Dispose();monitor.Dispose();StopWater("");process.Dispose();}
        base.Dispose(disposing);
    }
}
class Program {
    [DllImport("user32",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string title);
    [DllImport("user32",CharSet=CharSet.Unicode)] static extern int RegisterWindowMessage(string name);
    [DllImport("user32")] static extern bool PostMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
    internal static readonly int CommandMessage=RegisterWindowMessage("AcuarioStudio.DesktopCommand.v1");
    static void RegisterDesktopMenu(){
        const string path=@"Software\Classes\DesktopBackground\Shell\AcuarioStudio";
        string exe=Application.ExecutablePath;
        using(var root=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path)){
            root.SetValue("MUIVerb","Acuario Studio");root.SetValue("SubCommands","");root.SetValue("Icon",exe);
            string[] keys={"01Panel","02Feed","03Fish","04Add","05Remove","fps10","fps15","fps30","fps45","fps60"};
            string[] labels={"Abrir panel","Alimentar a los peces","Pescar","Añadir pez","Quitar pez","10 FPS","15 FPS","30 FPS","45 FPS","60 FPS"};
            int[] commands={0,1,2,3,4,10,15,30,45,60};
            for(int i=0;i<keys.Length;i++){
                using(var item=root.CreateSubKey(@"shell\"+keys[i]))
                using(var action=item.CreateSubKey("command")){
                    item.SetValue("MUIVerb",labels[i]);action.SetValue("",String.Format("\"{0}\" {1}",exe,commands[i]));
                }
            }
        }
    }
    [STAThread] static void Main(string[] args){
        int command=0;
        if(args.Length==1)int.TryParse(args[0],out command);
        bool first;
        using(var signal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\AcuarioStudioPanel",out first)){
            if(!first){
                if(command==0)signal.Set();
                else for(int attempt=0;attempt<50;attempt++){
                    var panel=FindWindow(null,"Acuario Studio");
                    if(panel!=IntPtr.Zero){PostMessage(panel,CommandMessage,(IntPtr)command,IntPtr.Zero);break;}
                    System.Threading.Thread.Sleep(50);
                }
                return;
            }
            if(command==99)return;
            try{
                using(var startup=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    startup.SetValue("AcuarioStudio","\""+Application.ExecutablePath+"\"");
                RegisterDesktopMenu();
            }
            catch(UnauthorizedAccessException){}
            catch(System.Security.SecurityException){}
            Application.EnableVisualStyles();
            using(var studio=new Studio(signal,command))Application.Run(studio);
        }
    }
}








