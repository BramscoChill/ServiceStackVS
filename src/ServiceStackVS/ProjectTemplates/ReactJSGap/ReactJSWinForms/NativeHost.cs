﻿using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms.Internals;
using ServiceStack.Configuration;

namespace $safeprojectname$
{
    public class NativeHost
    {
        private readonly FormMain formMain;

        public NativeHost(FormMain formMain)
        {
            this.formMain = formMain;
            //Enable Chrome Dev Tools when debugging WinForms
#if DEBUG
            formMain.ChromiumBrowser.KeyboardHandler = new KeyboardHandler();
#endif
        }

        public string Platform
        {
            get { return "winforms"; }
        }

        public void Quit()
        {
            formMain.InvokeOnUiThreadIfRequired(() =>
            {
                formMain.Close();
            });
        }

        public void ShowAbout()
        {
            MessageBox.Show(@"ServiceStack Winforms with CefSharp + React", @"$safeprojectname$", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ToggleFormBorder()
        {
            formMain.InvokeOnUiThreadIfRequired(() =>
            {
                formMain.FormBorderStyle = formMain.FormBorderStyle == FormBorderStyle.None
                    ? FormBorderStyle.Sizable
                    : FormBorderStyle.None;
            });
        }

        public void Ready()
        {
            formMain.InvokeOnUiThreadIfRequired(() =>
            {
                //Invoke on DOM ready
                var appSettings = new AppSettings();
                var checkForUpdates = appSettings.Get<bool>("EnableAutoUpdate");
                if (!checkForUpdates)
                    return;

                var releaseFolderUrl = appSettings.GetString("UpdateManagerUrl");
                var updatesAvailableTask = SquirrelHelpers.CheckForUpdates(releaseFolderUrl);
                updatesAvailableTask.Wait(5000);
                bool updatesAvailable = updatesAvailableTask.Result;
                if (updatesAvailable)
                {
                    // Notify web client updates are available.
                    formMain.ChromiumBrowser.GetMainFrame().ExecuteJavaScriptAsync("window.updateAvailable();");
                }
            });
        }
		
		public void PerformUpdate()
        {
            SquirrelHelpers.ApplyUpdates(new AppSettings().GetString("UpdateManagerUrl")).ContinueWith(t =>
            {
                var version = t.Result.Version.Version;
                var versionStr = version.Major + "." + version.Minor + "." + version.Build + "." + version.Revision;
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(Path.Combine(Application.StartupPath, "..\\app-" + versionStr + "\\" + exeName));
                formMain.Close();
            });
        }
    }

#if DEBUG
    public class KeyboardHandler : CefSharp.IKeyboardHandler
    {
        public bool OnPreKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode,
            CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            if (windowsKeyCode == (int)Keys.F12)
            {
                Program.Form.ChromiumBrowser.ShowDevTools();
            }
            return false;
        }

        public bool OnKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode,
            CefEventFlags modifiers, bool isSystemKey)
        {
            return false;
        }
    }
#endif
}
