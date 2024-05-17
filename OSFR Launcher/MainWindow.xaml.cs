using DiscordRPC;
using IWshRuntimeLibrary;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Octokit;
using System.Diagnostics;

namespace OSFRLauncher
{
    public partial class MainWindow : Window
    {
        private string path;
        private string clientexe;
        private string clientzip;
        private string serverzip;
        private string directx9exe;

        public MainWindow()
        {
            Initialize();
            CreateShortcut();
            InitializeComponent();
            path = Directory.GetCurrentDirectory();
            clientzip = Path.Combine(path, "Client.zip");
            serverzip = Path.Combine(path, "Server.zip");
            clientexe = Path.Combine(path, "Client", "FreeRealms.bat");
        }

        private void CheckFiles(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(clientexe))
            {
                PlayButton.Visibility = Visibility.Visible;
                Installbutton.Visibility = Visibility.Hidden;
                Update.Visibility = Visibility.Visible;
                Version.Visibility = Visibility.Visible;
            }
            else
            {
                Installbutton.Visibility = Visibility.Visible;
            }
        }

        private async void Download(object sender, RoutedEventArgs e)
        {
            try
            {
                string systemfolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string systempath = systemfolder + @"\Windows\System32\d3dx9_31.dll";
                if (!System.IO.File.Exists(systempath))
                {
                    // Downloads client and server files
                    progressBar.Visibility = Visibility.Visible;
                    Installbutton.Visibility = Visibility.Hidden;
                    StopVideo.Visibility = Visibility.Hidden;
                    Version.Visibility = Visibility.Hidden;
                    WebClient webClient = new WebClient();
                    webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(ExtractFiles);
                    StatusInfo.Text = "Installing Server Files";
                    await webClient.DownloadFileTaskAsync(new Uri("https://osfr.editz.dev/Server.zip"), serverzip);
                    StatusInfo.Text = "Installing Client Files";
                    await webClient.DownloadFileTaskAsync(new Uri("https://osfr.editz.dev/Client.zip"), clientzip);
                }
                else
                {
                    Directx9Installer();
                }
            }
            catch (Exception error)
            {
                // Catch error when installing the game files has failed
                StatusInfo.Text = "Failed Installing Game Files";
                MessageBox.Show($"Failed Installing Game Files: {error}");
            }
        }

        private async void Directx9Installer()
        {
            try
            {
                StatusInfo.Text = "installing DirectX9";
                progressBar.Visibility = Visibility.Visible;
                Installbutton.Visibility = Visibility.Hidden;
                WebClient webClient = new WebClient();
                string directx9folder = Path.Combine(path, "directx9");
                Directory.CreateDirectory(directx9folder);
                directx9exe = Path.Combine(directx9folder, "dxwebsetup.exe");
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(RunDirectx9Installer);
                await webClient.DownloadFileTaskAsync(new Uri("https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe"), directx9exe);
            }
            catch (Exception error)
            {
                // Catch error when installing directx9 has failed
                StatusInfo.Text = "Failed Installing DirectX9";
                MessageBox.Show($"Failed Installing DirectX9: {error}");
            }
        }

        private void RunDirectx9Installer(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                progressBar.Visibility = Visibility.Hidden;
                StatusInfo.Visibility = Visibility.Hidden;
                ProcessStartInfo start = new ProcessStartInfo(directx9exe);
                start.WorkingDirectory = Path.Combine(path, "dxwebsetup.exe");
                Process.Start(start);
                Close();
            }
            catch (Exception error)
            {
                // Catch error when launching directx9 installer has failed
                MessageBox.Show($"Failed Running DirectX9 Installer: {error}");
            }
        }

        private async void ExtractFiles(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (System.IO.File.Exists(serverzip))
                {
                    // Extracts server files
                    StatusInfo.Text = "Extracting Server Files";
                    ZipFile.ExtractToDirectory(serverzip, path);
                    System.IO.File.Delete(serverzip);
                }
                if (System.IO.File.Exists(clientzip))
                {
                    // Extracts client files
                    StatusInfo.Text = "Extracting Client Files";
                    await Task.Run(() => ZipFile.ExtractToDirectory(clientzip, path));
                    System.IO.File.Delete(clientzip);

                    // Relaunches the app after everything is finished downloading & extracting
                    StatusInfo.Text = "Re-Launching Now";
                    await Task.Delay(800);
                    ProcessStartInfo relaunchapp = new ProcessStartInfo();
                    Directory.GetCurrentDirectory();
                    relaunchapp.FileName = "OSFR Launcher.exe";
                    Process.Start(relaunchapp);
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch (Exception error)
            {
                // Catch error if extracting has failed
                StatusInfo.Text = "Failed Extracting Game Files";
                MessageBox.Show($"Failed Extracting Game Files: {error}");
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
            shortcut.WorkingDirectory = Environment.CurrentDirectory;
            shortcut.TargetPath = Environment.CurrentDirectory + @"\OSFR Launcher.exe";
            shortcut.IconLocation = Environment.CurrentDirectory + @"\images\" + "icon.ico";
            shortcut.Save();
        }

        private void StartClient(object sender, RoutedEventArgs e)
        {
            try
            {
                if (System.IO.File.Exists(clientexe))
                {
                    // Starts the client bat
                    ProcessStartInfo start = new ProcessStartInfo(clientexe);
                    start.WorkingDirectory = Path.Combine(path, "Client");
                    Process.Start(start);
                    Process.GetProcessesByName("OSFR Launcher.exe");
                    WindowState = WindowState.Minimized;
                }
            }
            catch (Exception error)
            {
                // Catch error
                MessageBox.Show($"Error occured while launching the client: {error}");
            }
        }

        private void BackgroundPlay(object sender, RoutedEventArgs e)
        {
            PlayVideo.Visibility = Visibility.Hidden;
            StopVideo.Visibility = Visibility.Visible;    
            background.Play();
        }

        private void BackgroundStop(object sender, RoutedEventArgs e)
        {
            PlayVideo.Visibility = Visibility.Visible;
            StopVideo.Visibility = Visibility.Hidden;
            background.Pause();
        }

        private void BackgroundLoop(object sender, RoutedEventArgs e)
        {
            background.Position = TimeSpan.FromSeconds(0);
        }

        private async void CheckForUpdate(object sender, RoutedEventArgs e)
        {
            try
            {
                string workspacename = "Junynx";
                string repositoryname = "OSFRLauncher";
                string filename = "release.zip";

                var client = new GitHubClient(new ProductHeaderValue(repositoryname));
                Release latestRelease = client.Repository.Release.GetLatest(workspacename, repositoryname).Result;
                var asset = latestRelease.Assets[0];

                // Setup the versions
                Version latestGitHubVersion = new Version(latestRelease.TagName);
                Version localVersion = new Version("1.0.0");

                // Compare versions
                int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                if (versionComparison < 0)
                {
                    // Downloads the files
                    Update.Content = "Updating";
                    var webClient = new WebClient();
                    webClient.Headers.Add(HttpRequestHeader.UserAgent, "user-agent");
                    await webClient.DownloadFileTaskAsync(asset.BrowserDownloadUrl, filename);

                    // Extracts the downloaded files
                    ZipFile.ExtractToDirectory(filename, path);
                    System.IO.File.Delete(filename);
                }
                else
                {
                    Update.Content = "No Updates Found";
                    System.IO.File.Delete(filename);
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
                Update.Content = "Check for Updates";
            }
            catch (Exception error)
            {
                // Catch error
                MessageBox.Show($"Error Occured While Updating The Launcher: {error}");
            }
        }
    }
}