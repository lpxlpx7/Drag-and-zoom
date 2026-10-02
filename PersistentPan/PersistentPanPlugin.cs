using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using vatsys.Plugin;

namespace vatSys.PersistentPan
{
    /// <summary>
    /// Keeps the centre reached by a middle-button pan instead of allowing the
    /// built-in temporary-pan behaviour to restore the previous centre.
    ///
    /// vatSys does not expose ASD mouse events through IPlugin, so the plugin
    /// uses reflection only for the ASD control and its public SetDisplayCenter
    /// API. This keeps it compatible with the normal SDK loading mechanism.
    /// </summary>
    public sealed class PersistentPanPlugin : IPlugin
    {
        private readonly System.Windows.Forms.Timer discoveryTimer;
        private readonly HashSet<Control> hookedControls = new HashSet<Control>();
        private readonly Dictionary<Control, object> lastPanCentres = new Dictionary<Control, object>();
        private readonly HashSet<Control> middleButtonDown = new HashSet<Control>();

        public PersistentPanPlugin()
        {
            discoveryTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            discoveryTimer.Tick += DiscoveryTimer_Tick;
            discoveryTimer.Start();
        }

        public string Name
        {
            get { return "Persistent Middle-Button Pan"; }
        }

        public void OnFDRUpdate(vatsys.FDP2.FDR fdr)
        {
        }

        public void OnRadarTrackUpdate(vatsys.RDP.RadarTrack radarTrack)
        {
        }

        private void DiscoveryTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    DiscoverControls(form);
                }
            }
            catch
            {
                // Plugin discovery must never interfere with vatSys startup.
            }
        }

        private void DiscoverControls(Control parent)
        {
            if (parent == null)
                return;

            if (parent.GetType().FullName == "vatsys.ASDControlDX" && hookedControls.Add(parent))
            {
                parent.MouseDown += AsdMouseDown;
                parent.MouseMove += AsdMouseMove;
                parent.MouseUp += AsdMouseUp;
                parent.MouseLeave += AsdMouseLeave;
            }

            foreach (Control child in parent.Controls)
            {
                DiscoverControls(child);
            }
        }

        private void AsdMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle)
                return;

            Control asd = sender as Control;
            if (asd == null)
                return;

            middleButtonDown.Add(asd);
            object centre = ReadCurrentCentre(asd);
            if (centre != null)
                lastPanCentres[asd] = centre;
        }

        private void AsdMouseMove(object sender, MouseEventArgs e)
        {
            Control asd = sender as Control;
            if (asd == null || !middleButtonDown.Contains(asd))
                return;

            // Capture while the built-in temporary-pan centre is still active.
            object centre = ReadCurrentCentre(asd);
            if (centre != null)
                lastPanCentres[asd] = centre;
        }

        private void AsdMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle)
                return;

            Control asd = sender as Control;
            if (asd == null || asd.IsDisposed)
                return;

            middleButtonDown.Remove(asd);
            object lastCentre;
            if (!lastPanCentres.TryGetValue(asd, out lastCentre))
                lastCentre = ReadCurrentCentre(asd);

            lastPanCentres.Remove(asd);

            // Run after vatSys's own MouseUp handler has finished. The built-in
            // handler may restore the temporary pan centre during MouseUp, so we
            // apply the centre captured during MouseMove afterwards.
            try
            {
                object centreToRestore = lastCentre;
                asd.BeginInvoke((MethodInvoker)(() => RestoreCentre(asd, centreToRestore)));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void AsdMouseLeave(object sender, EventArgs e)
        {
            Control asd = sender as Control;
            if (asd == null || !middleButtonDown.Contains(asd))
                return;

            object centre = ReadCurrentCentre(asd);
            if (centre != null)
                lastPanCentres[asd] = centre;
        }

        private static object ReadCurrentCentre(Control asd)
        {
            try
            {
                MethodInfo getRenderParams = asd.GetType().GetMethod(
                    "GetRenderParams",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(bool) },
                    null);
                MethodInfo setDisplayCenter = asd.GetType().GetMethod(
                    "SetDisplayCenter",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    null,
                    null);

                if (getRenderParams == null || setDisplayCenter == null)
                    return null;

                object renderParams = getRenderParams.Invoke(asd, new object[] { false });
                if (renderParams == null)
                    return null;

                PropertyInfo centreProperty = renderParams.GetType().GetProperty("ScreenCentre");
                object centre = centreProperty == null ? null : centreProperty.GetValue(renderParams, null);
                return centre;
            }
            catch
            {
                return null;
            }
        }

        private static void RestoreCentre(Control asd, object centre)
        {
            if (centre == null || asd == null || asd.IsDisposed)
                return;

            try
            {
                MethodInfo setDisplayCenter = asd.GetType().GetMethod(
                    "SetDisplayCenter",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    null,
                    null);

                if (setDisplayCenter == null)
                    return;

                // SetDisplayCenter(Coordinate, redraw, preserveRange).
                setDisplayCenter.Invoke(asd, new[] { centre, (object)true, (object)false });
            }
            catch
            {
            }
        }
    }
}
