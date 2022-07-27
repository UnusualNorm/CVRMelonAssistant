using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace CoVRMelonAssistant
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string ChilloutVRInstallDirectory;
        public static string ChilloutVRInstallType;

        public static bool CloseWindowOnFinish;
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static MainWindow window;
        public static string Arguments;
        public static bool Update = true;
        public static bool GUI = true;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Set SecurityProtocol to prevent crash with TLS
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            if (CoVRMelonAssistant.Properties.Settings.Default.UpgradeRequired)
            {
                CoVRMelonAssistant.Properties.Settings.Default.Upgrade();
                CoVRMelonAssistant.Properties.Settings.Default.UpgradeRequired = false;
                CoVRMelonAssistant.Properties.Settings.Default.Save();
            }

            Pages.Options options = Pages.Options.Instance;
            options.InstallDirectory =
                ChilloutVRInstallDirectory = Utils.GetInstallDir();

            Languages.LoadLanguages();

            while (string.IsNullOrEmpty(ChilloutVRInstallDirectory))
            {
                string title = (string)Current.FindResource("App:InstallDirDialog:Title");
                string body = (string)Current.FindResource("App:InstallDirDialog:OkCancel");

                if (System.Windows.Forms.MessageBox.Show(body, title, System.Windows.Forms.MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
                {
                    ChilloutVRInstallDirectory = Utils.GetManualDir();
                }
                else
                {
                    Environment.Exit(0);
                }
            }

            options.InstallType =
                ChilloutVRInstallType = CoVRMelonAssistant.Properties.Settings.Default.StoreType;
            options.CloseWindowOnFinish =
                CloseWindowOnFinish = CoVRMelonAssistant.Properties.Settings.Default.CloseWindowOnFinish;

            await ArgumentHandler(e.Args);
            await Init();
        }

        private async Task Init()
        {
            if (Update)
            {
                try
                {
                    await Task.Run(async () => await Updater.Run());
                }
                catch (UnauthorizedAccessException)
                {
                    Utils.StartAsAdmin(Arguments, true);
                }
            }

            if (GUI)
            {
                window = new MainWindow();
                window.Show();
            }
            else
            {
                //Application.Current.Shutdown();
            }
        }

        private async Task ArgumentHandler(string[] args)
        {
            Arguments = string.Join(" ", args);
            while (args.Length > 0)
            {
                switch (args[0])
                {
                    case "--install":
                        if (args.Length < 2 || string.IsNullOrEmpty(args[1]))
                        {
                            Utils.SendNotify(string.Format((string)Current.FindResource("App:InvalidArgument"), "--install"));
                        }

                        if (CloseWindowOnFinish)
                        {
                            await Task.Delay(5 * 1000);
                            Current.Shutdown();
                        }

                        Update = false;
                        GUI = false;
                        args = Shift(args, 2);
                        break;

                    case "--no-update":
                        Update = false;
                        args = Shift(args);
                        break;

                    case "--language":
                        if (args.Length < 2 || string.IsNullOrEmpty(args[1]))
                        {
                            Utils.SendNotify(string.Format((string)Current.FindResource("App:InvalidArgument"), "--language"));
                        }
                        else
                        {
                            if (Languages.LoadLanguage(args[1]))
                            {
                                CoVRMelonAssistant.Properties.Settings.Default.LanguageCode = args[1];
                                CoVRMelonAssistant.Properties.Settings.Default.Save();
                                Languages.UpdateUI(args[1]);
                            }
                        }

                        args = Shift(args, 2);
                        break;

                    case "--register":
                        if (args.Length < 3 || string.IsNullOrEmpty(args[1]))
                        {
                            Utils.SendNotify(string.Format((string)Current.FindResource("App:InvalidArgument"), "--register"));
                        }

                        Update = false;
                        GUI = false;
                        args = Shift(args, 3);
                        break;

                    case "--unregister":
                        if (args.Length < 2 || string.IsNullOrEmpty(args[1]))
                        {
                            Utils.SendNotify(string.Format((string)Current.FindResource("App:InvalidArgument"), "--unregister"));
                        }

                        Update = false;
                        GUI = false;
                        args = Shift(args, 2);
                        break;

                    case "--runforever":
                        while (true)
                        {

                        }

                    default:
                        Utils.SendNotify((string)Current.FindResource("App:UnrecognizedArgument"));
                        args = Shift(args);
                        break;
                }
            }
        }

        private static string[] Shift(string[] array, int places = 1)
        {
            if (places >= array.Length) return Array.Empty<string>();
            string[] newArray = new string[array.Length - places];
            for (int i = places; i < array.Length; i++)
            {
                newArray[i - places] = array[i];
            }

            return newArray;
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string title = (string)Current.FindResource("App:Exception");
            string body = (string)Current.FindResource("App:UnhandledException");
            MessageBox.Show($"{body}: {e.Exception}", title, MessageBoxButton.OK, MessageBoxImage.Warning);

            e.Handled = true;
            Current.Shutdown();
        }
    }
}
