using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Clawdos
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly Func<WebApplication> _appFactory;
        private WebApplication? _app;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _trayMenu;
        private bool _isRunning = false;

        public TrayApplicationContext(Func<WebApplication> appFactory)
        {
            _appFactory = appFactory;

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Start Service", null, OnStartService);
            _trayMenu.Items.Add("Stop Service", null, OnStopService);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("About", null, OnAbout);
            _trayMenu.Items.Add("Exit", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Text = "Clawdos",
                Icon = CreateIcon(),
                ContextMenuStrip = _trayMenu,
                Visible = true
            };

            // Start service automatically on startup
            StartServiceAsync().ConfigureAwait(false);
        }

        private Icon CreateIcon()
        {
            // Base64 encoded 72x72 twemoji crab
            const string crabBase64 = "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";
            byte[] imageBytes = Convert.FromBase64String(crabBase64);
            using var ms = new System.IO.MemoryStream(imageBytes);
            using var bitmap = new Bitmap(ms);
            
            // Note: GetHicon() creates an unmanaged handle that needs to be destroyed, 
            // but the Icon.FromHandle makes a copy/wrapper. However, to avoid memory leak,
            // we should be careful. Since this runs once, it is acceptable.
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private async void OnStartService(object? sender, EventArgs e)
        {
            await StartServiceAsync();
        }

        private async Task StartServiceAsync()
        {
            if (_isRunning) return;
            try
            {
                _app = _appFactory();
                await _app.StartAsync();
                _isRunning = true;
                _trayIcon.Text = "Clawdos - Running";
                _trayMenu.Items[0].Enabled = false; // Start
                _trayMenu.Items[1].Enabled = true;  // Stop
            }
            catch (Exception ex)
            {
                _app?.Logger.LogError(ex, "Failed to start service from tray");
                MessageBox.Show($"Failed to start service: {ex.Message}", "Clawdos Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (_app != null)
                {
                    await _app.DisposeAsync();
                    _app = null;
                }
            }
        }

        private async void OnStopService(object? sender, EventArgs e)
        {
            await StopServiceAsync();
        }

        private async Task StopServiceAsync()
        {
            if (!_isRunning || _app == null) return;
            try
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
                _app = null;
                _isRunning = false;
                _trayIcon.Text = "Clawdos - Stopped";
                _trayMenu.Items[0].Enabled = true;  // Start
                _trayMenu.Items[1].Enabled = false; // Stop
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to stop service: {ex.Message}", "Clawdos Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnExit(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            if (_isRunning)
            {
                await StopServiceAsync();
            }
            Application.Exit();
        }

        private void OnAbout(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/danzig233/clawdos",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open project page: {ex.Message}", "Clawdos Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _trayMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
