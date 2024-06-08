﻿using Hurl.BrowserSelector.Globals;
using Hurl.BrowserSelector.Helpers;
using Hurl.BrowserSelector.Windows;
using SingleInstanceCore;
using System.Text.Json;
using System.Windows;
using Windows.Win32;
using Windows.Win32.System.Pipes;
using Windows.Win32.Storage.FileSystem;
using System;
using Windows.Win32.Foundation;
using System.Diagnostics;


namespace Hurl.BrowserSelector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstance
    {
        private MainWindow _mainWindow;

        public App()
        {
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string ErrorMsgBuffer;
            string ErrorWndTitle;
            switch (e.Exception?.InnerException)
            {
                case JsonException:
                    ErrorMsgBuffer = "The UserSettings.json file is in invalid JSON format. \n";
                    ErrorWndTitle = "Invalid JSON";
                    break;
                default:
                    ErrorMsgBuffer = "An unknown error has occurred. \n";
                    ErrorWndTitle = "Unknown Error";
                    break;

            }
            string errorMessage = string.Format("{0}\n{1}\n\n{2}", ErrorMsgBuffer, e.Exception?.InnerException?.Message, e.Exception.Message);
            MessageBox.Show(errorMessage, ErrorWndTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected unsafe override void OnStartup(StartupEventArgs e)
        {
            var PIPE_NAME = "\\\\.\\pipe\\HurlNamedPipe";
            var pipeHandle = PInvoke.CreateNamedPipe(PIPE_NAME,
                FILE_FLAGS_AND_ATTRIBUTES.PIPE_ACCESS_INBOUND,
                NAMED_PIPE_MODE.PIPE_TYPE_BYTE,
                2,
                0,
                16000,
                0,
                null);

            while (PInvoke.WaitNamedPipe(PIPE_NAME, 0xffffffff))
            {
                Debug.WriteLine("Pipe is ready for connection");
                var connected = PInvoke.ConnectNamedPipe(pipeHandle, null);
                Debug.WriteLine($"Pipe is connected {connected}");
                if (connected && pipeHandle != null)
                {
                    var buffer = new byte[16000];
                    fixed (byte* pBuffer = buffer)
                    {
                        var readSuccess = PInvoke.ReadFile(new HANDLE(pipeHandle.DangerousGetHandle()), pBuffer, 16000);
                        if (readSuccess)
                        {
                            var args = System.Text.Encoding.UTF8.GetString(buffer);

                            MessageBox.Show(args);
                        }
                    }
                }
            }




            bool isFirstInstance = this.InitializeAsFirstInstance("HurlTray");
            if (isFirstInstance)
            {
                var cliArgs = CliArgs.GatherInfo(e.Args, false);
                UriGlobal.Value = cliArgs.Url;

                _mainWindow = new();
                _mainWindow.Init(cliArgs);
            }
            else
            {
                Current.Shutdown();
            }
        }

        public void OnInstanceInvoked(string[] args)
        {
            Current.Dispatcher.Invoke(() =>
            {
                var cliArgs = CliArgs.GatherInfo(args, true);
                var IsTimedSet = TimedBrowserSelect.CheckAndLaunch(cliArgs.Url);

                if (!IsTimedSet)
                {
                    UriGlobal.Value = cliArgs.Url;
                    _mainWindow.Init(cliArgs);
                }

            });
        }

        protected override void OnExit(ExitEventArgs e) => SingleInstance.Cleanup();
    }
}
