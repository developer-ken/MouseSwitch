using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static MouseSwitch.HotKeys;

namespace MouseSwitch
{
    public class Form1 : Form
    {
        private HotKeys h = new HotKeys();

        private int NowAt = 1;

        private IContainer components;

        private Label label1;

        private Label number;

        private Timer autohide;
        private NotifyIcon notifyIcon1;
        private Timer CurrentPositionCheck;

        public HotkeyModifiers modifier;
        public Keys previous, next;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem 设置SToolStripMenuItem;
        private ToolStripMenuItem 关于AToolStripMenuItem;
        private ToolStripMenuItem 退出QToolStripMenuItem;
        public bool shownotifi = false;

        public Form1()
        {
            InitializeComponent();
            var isfirst = LoadConfig();

            if (RegKeys())
            {
                var count = MouseGoto();
                if (isfirst)
                {
                    if (shownotifi)
                        notifyIcon1.ShowBalloonTip(2000, "屏幕切换程序", "欢迎使用屏幕切换程序！", ToolTipIcon.None);
                    SaveConfig();
                }
                else
                {
                    if (shownotifi)
                        notifyIcon1.ShowBalloonTip(2000, "屏幕切换程序", "检测到" + count + "个显示器\n" +
                        "上一个：" + GetKeyName(modifier) + "+" + GetKeyName(previous) + "\n" +
                        "下一个：" + GetKeyName(modifier) + "+" + GetKeyName(next) + "\n"
                        , ToolTipIcon.None);
                }
            }
        }

        private bool RegKeys(bool reporterror = false)
        {
            try
            {
                h.Regist(base.Handle, modifier, previous, GoMinus);
                h.Regist(base.Handle, modifier, next, GoPlus);
                return true;
            }
            catch (Exception err)
            {
                if (reporterror)
                {
                    ErrorReport.ReportError(err);
                }
                else
                {
                    previous = Keys.Left;
                    next = Keys.Right;
                    modifier = HotkeyModifiers.Alt;
                    shownotifi = true;
                    notifyIcon1.ShowBalloonTip(2000, "屏幕切换程序", "无法使用您自定义的快捷键，已恢复默认设置。\n" +
                            "上一个：" + GetKeyName(modifier) + "+" + GetKeyName(previous) + "\n" +
                            "下一个：" + GetKeyName(modifier) + "+" + GetKeyName(next) + "\n"
                            , ToolTipIcon.None);
                    RegKeys(true);
                    SaveConfig();
                }
            }
            return false;
        }

        private void UnRegKeys()
        {
            h.UnRegist(Handle, GoMinus);
            h.UnRegist(Handle, GoPlus);
            h.keyid = 10;
        }

        private bool LoadConfig()
        {
            if (!File.Exists("config.json"))
            {
                previous = Keys.Left;
                next = Keys.Right;
                modifier = HotkeyModifiers.Alt;
                shownotifi = true;
                return true;
            }
            else
            {
                var confstr = File.ReadAllText("config.json");
                var jb = JObject.Parse(confstr);
                previous = (Keys)jb.Value<int>("previous_screen");
                next = (Keys)jb.Value<int>("next_screen");
                modifier = (HotkeyModifiers)jb.Value<int>("modifier");
                shownotifi = jb.Value<bool>("notify_when_boot");
                return false;
            }
        }

        public void SaveConfig()
        {
            if (File.Exists("config.json"))
                File.Delete("config.json");
            {
                var jb = new JObject();
                jb.Add("previous_screen", (int)previous);
                jb.Add("next_screen", (int)next);
                jb.Add("modifier", (int)modifier);
                jb.Add("notify_when_boot", shownotifi);
                File.WriteAllText("config.json", jb.ToString());
            }
        }

        [DllImport("user32")]
        private static extern int SetCursorPos(int x, int y);

        public void MouseToPoint(Point p)
        {
            SetCursorPos(p.X, p.Y);
        }

        public void SetMouseAtCenterScreen(Screen screen)
        {
            int x = screen.WorkingArea.X;
            int y = screen.WorkingArea.Y;
            int height = screen.WorkingArea.Height;
            int width = screen.WorkingArea.Width;
            Point p = new Point(x + width / 2, y + height / 2);
            MouseToPoint(p);
        }

        private int MouseGoto(int ScreenID = 1)
        {
            int num = 0;
            Screen[] allScreens = Screen.AllScreens;
            foreach (Screen screen in allScreens)
            {
                num++;
                if (num == ScreenID)
                {
                    SetMouseAtCenterScreen(screen);
                    number.Text = num.ToString();
                    base.Left = screen.WorkingArea.X;
                    base.Top = screen.WorkingArea.Y;
                    base.Opacity = 0.75;
                    autohide.Start();
                    break;
                }
            }
            return allScreens.Count();
        }

        protected override void WndProc(ref Message m)
        {
            h.ProcessHotKey(m);
            base.WndProc(ref m);
        }

        public void GoMinus()
        {
            NowAt--;
            if (NowAt < 1)
            {
                NowAt = Screen.AllScreens.Count();
            }
            MouseGoto(NowAt);
        }

        public void GoPlus()
        {
            NowAt++;
            if (NowAt > Screen.AllScreens.Count())
            {
                NowAt = 1;
            }
            MouseGoto(NowAt);
        }

        private void Form1_MouseEnter(object sender, EventArgs e)
        {
            base.Opacity = 0.0;
        }

        private void autohide_Tick(object sender, EventArgs e)
        {
            base.Opacity = 0.0;
        }

        private void CurrentPositionCheck_Tick(object sender, EventArgs e)
        {
            int num = 0;
            Screen[] allScreens = Screen.AllScreens;
            foreach (Screen obj in allScreens)
            {
                num++;
                if (obj.WorkingArea.Contains(Control.MousePosition.X, Control.MousePosition.Y))
                {
                    NowAt = num;
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private Config configwindow;

        private void 设置SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (configwindow != null) configwindow.Show();
            else
            {
                UnRegKeys();
                configwindow = new Config(this);
                configwindow.ShowDialog();
                RegKeys();
                configwindow = null;
            }
        }

        private void 退出QToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenuStrip1.Show();
        }

        private void 关于AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutPage().ShowDialog();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label1 = new System.Windows.Forms.Label();
            this.number = new System.Windows.Forms.Label();
            this.autohide = new System.Windows.Forms.Timer(this.components);
            this.CurrentPositionCheck = new System.Windows.Forms.Timer(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.设置SToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.关于AToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.退出QToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 12);
            this.label1.TabIndex = 0;
            // 
            // number
            // 
            this.number.AutoSize = true;
            this.number.BackColor = System.Drawing.Color.Black;
            this.number.Font = new System.Drawing.Font("宋体", 72F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.number.ForeColor = System.Drawing.Color.White;
            this.number.Location = new System.Drawing.Point(49, 41);
            this.number.Name = "number";
            this.number.Size = new System.Drawing.Size(91, 97);
            this.number.TabIndex = 1;
            this.number.Text = "1";
            // 
            // autohide
            // 
            this.autohide.Interval = 2000;
            this.autohide.Tick += new System.EventHandler(this.autohide_Tick);
            // 
            // CurrentPositionCheck
            // 
            this.CurrentPositionCheck.Enabled = true;
            this.CurrentPositionCheck.Interval = 500;
            this.CurrentPositionCheck.Tick += new System.EventHandler(this.CurrentPositionCheck_Tick);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "屏幕切换程序";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.设置SToolStripMenuItem,
            this.关于AToolStripMenuItem,
            this.退出QToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(181, 92);
            // 
            // 设置SToolStripMenuItem
            // 
            this.设置SToolStripMenuItem.Name = "设置SToolStripMenuItem";
            this.设置SToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.设置SToolStripMenuItem.Text = "设置(&S)";
            this.设置SToolStripMenuItem.Click += new System.EventHandler(this.设置SToolStripMenuItem_Click);
            // 
            // 关于AToolStripMenuItem
            // 
            this.关于AToolStripMenuItem.Name = "关于AToolStripMenuItem";
            this.关于AToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.关于AToolStripMenuItem.Text = "关于(&A)";
            this.关于AToolStripMenuItem.Click += new System.EventHandler(this.关于AToolStripMenuItem_Click);
            // 
            // 退出QToolStripMenuItem
            // 
            this.退出QToolStripMenuItem.Name = "退出QToolStripMenuItem";
            this.退出QToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.退出QToolStripMenuItem.Text = "退出(&Q)";
            this.退出QToolStripMenuItem.Click += new System.EventHandler(this.退出QToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(201, 189);
            this.Controls.Add(this.number);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Opacity = 0.75D;
            this.ShowInTaskbar = false;
            this.Text = "屏幕切换助手 后台驻留程序";
            this.TopMost = true;
            this.MouseEnter += new System.EventHandler(this.Form1_MouseEnter);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
