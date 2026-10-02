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
                parent.MouseUp += AsdMouseUp;
            }

            foreach (Control child in parent.Controls)
            {
                DiscoverControls(child);
            }
        }

        private void AsdMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle)
                return;

            Control asd = sender as Control;
            if (asd == null || asd.IsDisposed)
                return;

            // Run after vatSys's own MouseUp handler has finished. The built-in
            // handler may restore the temporary pan centre during MouseUp.
            try
            {
                asd.BeginInvoke((MethodInvoker)(() => PreserveCurrentCentre(asd)));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void PreserveCurrentCentre(Control asd)
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
                    return;

                object renderParams = getRenderParams.Invoke(asd, new object[] { false });
                if (renderParams == null)
                    return;

                PropertyInfo centreProperty = renderParams.GetType().GetProperty("ScreenCentre");
                object centre = centreProperty == null ? null : centreProperty.GetValue(renderParams, null);
                if (centre == null)
                    return;

                // SetDisplayCenter(Coordinate, redraw, preserveRange).
                setDisplayCenter.Invoke(asd, new[] { centre, (object)true, (object)false });
            }
            catch (TargetInvocationException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentException)
            {
            }
        }
    }
}
