using DiscordRPC.Logging;
using DiscordRPC;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
namespace OSFRLauncher
{
    enum LauncherStatus
    {
        ready,
        installing,
        installingfailed,
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
                        Button.Content = "Play";
                        break;
                    case LauncherStatus.installing:
                        Button.Content = "Installing";
                        break;
                    case LauncherStatus.installingfailed:
                        Button.Content = "Installing Failed";
                        break;
                    case LauncherStatus.extractingfailed:
                        Button.Content = "Extracting Failed";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            Initialize();
            path = Directory.GetCurrentDirectory();
            clientzip = Path.Combine(path, "Client.zip");
            serverzip = Path.Combine(path, "Server.zip");
        }

        private async void Download(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            progressPercent.Visibility = Visibility.Visible;
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
                MessageBox.Show($"Error installing game files: {error}");
            }
        }

        private void ExtractFiles(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (File.Exists(serverzip))
                {
                    // Extracts server files
                    ZipFile.ExtractToDirectory(serverzip, path);
                    File.Delete(serverzip);
                }
                if (File.Exists(clientzip))
                {
                    // Extracts client files
                    ZipFile.ExtractToDirectory(clientzip, path);
                    File.Delete(clientzip);
                    Status = LauncherStatus.ready;
                    progressBar.Visibility = Visibility.Hidden;
                    progressPercent.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception error)
            {
                // Catch error if extracting has failed
                Status = LauncherStatus.extractingfailed;
                MessageBox.Show($"Error extracting game files: {error}");
            }
        }

        private void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            progressPercent.Text = "%" + (e.ProgressPercentage).ToString();
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
    }
}





