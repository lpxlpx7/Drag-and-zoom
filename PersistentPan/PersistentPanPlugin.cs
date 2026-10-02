using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private const int WmLButtonUp = 0x0202;
        private const int WmMButtonDown = 0x0207;
        private const int WmMButtonUp = 0x0208;
        private const int WmMouseWheel = 0x020A;
        private const int MkMButton = 0x0010;

        private readonly Dictionary<Control, object> lastCentres = new Dictionary<Control, object>();
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
                        SaveCentre(asd);
                        Log("Middle down");
                        break;

                    case WmMouseMove:
                        if (panning.Contains(asd) && (((long)message.WParam.ToInt64() & MkMButton) != 0))
                            SaveCentre(asd);
                        break;

                    case WmMButtonUp:
                        if (panning.Remove(asd))
                        {
                            object finalCentre;
                            if (!lastCentres.TryGetValue(asd, out finalCentre))
                                finalCentre = ReadCentre(asd);
                            lastCentres.Remove(asd);
                            RestoreAfterVatSys(asd, finalCentre);
                            Log("Middle up; restore captured centre");
                        }
                        break;

                    case WmMouseWheel:
                        ZoomWithWheel(asd, SignedHighWord(message.WParam.ToInt64()));
                        break;
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

        private void SaveCentre(Control asd)
        {
            object centre = ReadCentre(asd);
            if (centre != null)
                lastCentres[asd] = centre;
        }

        private static object ReadCentre(Control asd)
        {
            MethodInfo getRenderParams = asd.GetType().GetMethod(
                "GetRenderParams", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(bool) }, null);
            if (getRenderParams == null)
                return null;

            object renderParams = getRenderParams.Invoke(asd, new object[] { false });
            PropertyInfo centre = renderParams == null ? null : renderParams.GetType().GetProperty("ScreenCentre");
            return centre == null ? null : centre.GetValue(renderParams, null);
        }

        private static void RestoreAfterVatSys(Control asd, object centre)
        {
            if (centre == null || asd.IsDisposed)
                return;

            try
            {
                asd.BeginInvoke((MethodInvoker)(() =>
                {
                    MethodInfo setDisplayCenter = asd.GetType().GetMethod(
                        "SetDisplayCenter", BindingFlags.Instance | BindingFlags.Public);
                    if (setDisplayCenter != null)
                        setDisplayCenter.Invoke(asd, new[] { centre, (object)true, (object)false });
                }));
            }
            catch (InvalidOperationException) { }
        }

        private static void ZoomWithWheel(Control asd, int delta)
        {
            if (delta == 0)
                return;

            MethodInfo getRenderParams = asd.GetType().GetMethod(
                "GetRenderParams", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(bool) }, null);
            MethodInfo setZoom = asd.GetType().GetMethod(
                "SetZoom", BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(double), typeof(bool), typeof(bool), typeof(bool) }, null);
            if (getRenderParams == null || setZoom == null)
                return;

            object renderParams = getRenderParams.Invoke(asd, new object[] { false });
            PropertyInfo zoom = renderParams == null ? null : renderParams.GetType().GetProperty("Zoom");
            if (zoom == null)
                return;

            double current = Convert.ToDouble(zoom.GetValue(renderParams, null));
            // End/PgDn change the range in 20% steps. Wheel uses the same
            // direction: wheel up zooms in, wheel down zooms out.
            double next = delta > 0 ? current * 0.8 : current * 1.25;
            setZoom.Invoke(asd, new object[] { next, true, false, true });
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
