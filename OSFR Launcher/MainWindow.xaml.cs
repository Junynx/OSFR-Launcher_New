using DiscordRPC;
using IWshRuntimeLibrary;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
namespace OSFRLauncher
{
    enum LauncherStatus
    {
        ready,
        installing,
        installingfailed,
        extracting,
        extractingfailed
    }

    public partial class MainWindow : Window
    {
        private string path;
        private string clientzip;
        private string serverzip;
        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.installing:
                        StatusInfo.Text = "Installing...";
                        break;
                    case LauncherStatus.installingfailed:
                        StatusInfo.Text = "Installing Failed...";
                        break;
                    case LauncherStatus.extracting:
                        StatusInfo.Text = "Extracting...";
                        break;
                    case LauncherStatus.extractingfailed:
                        StatusInfo.Text = "Extracting Failed...";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            Initialize();
            CreateShortcut();
            InitializeComponent();
            path = Directory.GetCurrentDirectory();
            clientzip = Path.Combine(path, "Client.zip");
            serverzip = Path.Combine(path, "Server.zip");
        }

        private async void Download(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            Installbutton.Visibility = Visibility.Hidden;
            try
            {
                // Downloads client and server files
                Status = LauncherStatus.installing;
                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(ExtractFiles);
                await webClient.DownloadFileTaskAsync(new Uri("https://osfr.editz.dev/Server.zip"), serverzip);
                await webClient.DownloadFileTaskAsync(new Uri("https://osfr.editz.dev/Client.zip"), clientzip);
            }
            catch (Exception error)
            {
                // Catch error when installing the game files has failed
                Status = LauncherStatus.installingfailed;
                taskbar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                MessageBox.Show($"Error installing game files: {error}");
            }
        }

        private async void ExtractFiles(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (System.IO.File.Exists(serverzip))
                {
                    // Extracts server files
                    ZipFile.ExtractToDirectory(serverzip, path);
                    System.IO.File.Delete(serverzip);
                }
                if (System.IO.File.Exists(clientzip))
                {
                    // Extracts client files
                    Status = LauncherStatus.extracting;
                    await Task.Run(() => ZipFile.ExtractToDirectory(clientzip, path));
                    System.IO.File.Delete(clientzip);
                    Installbutton.Visibility = Visibility.Visible;
                    progressBar.Visibility = Visibility.Hidden;
                    StatusInfo.Visibility = Visibility.Hidden;
                    Installbutton.Visibility = Visibility.Hidden;
                    PlayButton.Visibility = Visibility.Visible;
                    taskbar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                }
            }
            catch (Exception error)
            {
                // Catch error if extracting has failed
                Status = LauncherStatus.extractingfailed;
                taskbar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                MessageBox.Show($"Error extracting game files: {error}");
            }
        }

        private void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        // Discord RPC
        public DiscordRpcClient client;
        void Initialize()
        {
            client = new DiscordRpcClient("1223728876199608410");

            // Connects to the RPC
            client.Initialize();

            // Set the rich presence
            client.SetPresence(new RichPresence()
            {
                Details = "Hanging out in the Launcher",
                Assets = new Assets()
                {
                    LargeImageKey = "osfr",
                },
                Timestamps = new Timestamps()
                {
                    // Starts a timer
                    Start = DateTime.UtcNow,
                },
            });
        }

        private void CreateShortcut()
        {
            // Creates a shortcut on the desktop
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\OSFR Launcher.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "A Launcher For Open Source Free Realms";
            shortcut.TargetPath = Environment.CurrentDirectory + @"\OSFR Launcher.exe";
            shortcut.Save();
        }

        private void StartGame(object sender, RoutedEventArgs e)
        {
            // TODO
        }
    }
}