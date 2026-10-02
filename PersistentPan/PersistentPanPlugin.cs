using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using vatsys.Plugin;

namespace vatSys.PersistentPan
{
    /// <summary>
    /// Makes the ASD middle-button pan persistent and adds mouse-wheel zoom.
    /// The ASD is a Direct2D control and its mouse handlers are registered in a
    /// way that does not reliably raise normal WinForms mouse events. Therefore
    /// this plugin observes the WinForms message pump instead.
    /// </summary>
    [Export(typeof(IPlugin))]
    public sealed class PersistentPanPlugin : IPlugin, IMessageFilter
    {
        private const int WmMouseMove = 0x0200;
        private const int WmMButtonDown = 0x0207;
        private const int WmMButtonUp = 0x0208;
        private const int WmMouseWheel = 0x020A;

        private readonly HashSet<Control> panning = new HashSet<Control>();
        private readonly Dictionary<IntPtr, Control> controls = new Dictionary<IntPtr, Control>();
        private readonly string logPath;

        public PersistentPanPlugin()
        {
            logPath = Path.Combine(Path.GetDirectoryName(typeof(PersistentPanPlugin).Assembly.Location), "PersistentPan.log");
            Application.AddMessageFilter(this);
            Log("Plugin loaded");
        }

        public string Name
        {
            get { return "Persistent Middle-Button Pan + Wheel Zoom"; }
        }

        public void OnFDRUpdate(vatsys.FDP2.FDR fdr) { }

        public void OnRadarTrackUpdate(vatsys.RDP.RadarTrack radarTrack) { }

        public bool PreFilterMessage(ref Message message)
        {
            try
            {
                Control asd = FindAsd(message.HWnd);
                if (asd == null || asd.IsDisposed)
                    return false;

                switch (message.Msg)
                {
                    case WmMButtonDown:
                        panning.Add(asd);
                        Log("Middle down " + MouseX(message).ToString() + "," + MouseY(message).ToString());
                        break;

                    case WmMouseMove:
                        // The native ASD handler performs the actual pan here.
                        // We only need the final coordinates at MouseUp.
                        break;

                    case WmMButtonUp:
                        if (panning.Remove(asd))
                        {
                            Point finalPoint = new Point(MouseX(message), MouseY(message));
                            RestorePanAfterVatSys(asd, finalPoint);
                            Log("Middle up " + finalPoint.X.ToString() + "," + finalPoint.Y.ToString() + "; replay final move");
                        }
                        break;

                    case WmMouseWheel:
                        int delta = SignedHighWord(message.WParam.ToInt64());
                        ZoomWithWheel(asd, delta);
                        Log("Wheel " + delta.ToString());
                        return true;
                }
            }
            catch (Exception exception)
            {
                Log(exception.ToString());
            }

            // Do not consume messages. vatSys still receives the original input.
            return false;
        }

        private Control FindAsd(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return null;

            Control cached;
            if (controls.TryGetValue(handle, out cached) && !cached.IsDisposed)
                return cached;

            Control control = Control.FromHandle(handle);
            while (control != null)
            {
                if (control.GetType().FullName == "vatsys.ASDControlDX")
                {
                    controls[handle] = control;
                    return control;
                }
                control = control.Parent;
            }
            return null;
        }

        private static void RestorePanAfterVatSys(Control asd, Point finalPoint)
        {
            if (asd.IsDisposed)
                return;

            try
            {
                asd.BeginInvoke((MethodInvoker)(() =>
                {
                    // vatSys's own MouseUp resets QuickPan. Re-enable only its
                    // temporary-pan flag and call its own MouseMove routine at
                    // the release point. This reproduces the native calculation
                    // without duplicating its map projection math.
                    FieldInfo quickPan = asd.GetType().GetField(
                        "QuickPan", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo mouseMove = asd.GetType().GetMethod(
                        "ASD_MouseMove", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (quickPan == null || mouseMove == null)
                        return;

                    quickPan.SetValue(asd, true);
                    mouseMove.Invoke(asd, new object[] {
                        asd,
                        new MouseEventArgs(MouseButtons.Middle, 0, finalPoint.X, finalPoint.Y, 0)
                    });
                    quickPan.SetValue(asd, false);
                }));
            }
            catch (InvalidOperationException) { }
        }

        private static void ZoomWithWheel(Control asd, int delta)
        {
            if (delta == 0)
                return;

            MethodInfo getRange = asd.GetType().GetMethod(
                "GetRange", BindingFlags.Instance | BindingFlags.Public,
                null, Type.EmptyTypes, null);
            MethodInfo setZoom = asd.GetType().GetMethod(
                "SetZoom", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(double), typeof(bool), typeof(bool), typeof(bool) }, null);
            if (getRange == null || setZoom == null)
                return;

            double current = Convert.ToDouble(getRange.Invoke(asd, null));
            // End/PgDn change the range in 20% steps. Wheel uses the same
            // direction: wheel up zooms in, wheel down zooms out.
            double next = delta > 0 ? current * 0.8 : current * 1.25;
            setZoom.Invoke(asd, new object[] { next, true, false, true });
        }

        private static int MouseX(Message message)
        {
            return (short)(message.LParam.ToInt64() & 0xffff);
        }

        private static int MouseY(Message message)
        {
            return (short)((message.LParam.ToInt64() >> 16) & 0xffff);
        }

        private static int SignedHighWord(long value)
        {
            int word = (int)((value >> 16) & 0xffff);
            return word >= 0x8000 ? word - 0x10000 : word;
        }

        private void Log(string text)
        {
            try
            {
                File.AppendAllText(logPath, DateTime.Now.ToString("s") + " " + text + Environment.NewLine);
            }
            catch { }
        }
    }
}
