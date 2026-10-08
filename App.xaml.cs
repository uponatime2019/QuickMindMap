using System;
using Microsoft.UI.Xaml;
using QuickMindMap.Helpers;

namespace QuickMindMap
{
    public partial class App : Application
    {
        public MainWindow? MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();

            AppLogger.Initialize();

            this.UnhandledException += (sender, args) =>
            {
                AppLogger.LogException(args.Exception, "GlobalUnhandledException");
                args.Handled = true; // Prevents process crash for non-fatal UI exceptions
            };
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                MainWindow = new MainWindow();
                MainWindow.Activate();
            }
            catch (Exception ex)
            {
                AppLogger.LogException(ex, "App.OnLaunched");
            }
        }
    }
}
