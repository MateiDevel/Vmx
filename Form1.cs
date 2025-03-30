/*
Code by MateiDev 2025.
With help from Github Copilot :)
*/

using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VolumeMxr
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WM_MOUSEACTIVATE = 0x21;
        private const int MA_ACTIVATE = 1;
        private const int VK_CONTROL = 0x11;

        private MMDevice device;
        private TrackBar slider;
        private bool isUpdatingFromEvent = false; // prevents recursive updates

        private bool IsCtrlHeld() => (GetKeyState(VK_CONTROL) & 0x8000) != 0;

        private void UpdateTransparency()
        {
            int style = GetWindowLong(Handle, GWL_EXSTYLE);
            if (IsCtrlHeld())
                SetWindowLong(Handle, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
            else
                SetWindowLong(Handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE && IsCtrlHeld())
            {
                m.Result = (IntPtr)MA_ACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }

        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(400, 10);
            TopMost = true;
            Opacity = 0.35;
            BackColor = Color.Black;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - Width, 0);
            Icon = new Icon(Path.Combine(Application.StartupPath, "icon\\icon2.ico"));
            ShowInTaskbar = false;

            slider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                ForeColor = Color.LightBlue,
                TickStyle = TickStyle.None,
                Width = (int)(ClientSize.Width * 0.8),
                Location = new Point((ClientSize.Width - (int)(ClientSize.Width * 0.8)) / 2, 0)
            };

            //gets default device
            device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            slider.Value = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            slider.Scroll += slider_scroll;

            Controls.Add(slider);

            device.AudioEndpointVolume.OnVolumeNotification += OnVolumeChanged;

            Timer checkKeyTimer = new Timer { Interval = 100 };
            checkKeyTimer.Tick += (s, e) => UpdateTransparency();
            checkKeyTimer.Start();

            this.Load += Form1_Load;

            UpdateTransparency();
        }

        private void slider_scroll(object sender, EventArgs e)
        {
            if (!isUpdatingFromEvent)
            {
                float volume = slider.Value / 100f;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
            }
        }

        private void OnVolumeChanged(AudioVolumeNotificationData data)
        {
            isUpdatingFromEvent = true;
            Invoke((MethodInvoker)(() => slider.Value = (int)(data.MasterVolume * 100)));
            isUpdatingFromEvent = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.IsFirstRun)
            {
                DialogResult result = MessageBox.Show(
                    "Thank you for choosing VMX!" +
                    "\nThe app by default is click-through, meaning you can't click on it. To change the volume, you must hold CTRL and slide the volume slider." +
                    "\nWould you like the app to autostart when Windows boots?",
                    "First Run",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );
                

                if (result == DialogResult.Yes)
                {
                    SetAutoStart(true); 
                }
                else
                {
                    SetAutoStart(false); 
                }
                Properties.Settings.Default.IsFirstRun = false;
                Properties.Settings.Default.Save();
            }
        }
        private void SetAutoStart(bool enable)
        {
            string appName = "VMX"; 
            string appPath = Application.ExecutablePath;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                {
                    key.SetValue(appName, appPath);
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
        }
    }
}
