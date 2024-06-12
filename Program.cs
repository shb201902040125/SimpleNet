
using Microsoft.Win32;
using Newtonsoft.Json;
using SimpleNet.DataStructures;
using SimpleNet.Lobbys;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace SimpleNet
{
    internal class Program
    {
        static string BaseDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleNet");
        static int? _ipv4Port;
        static int? _ipv6Port;
        static string? _localSeverName;
        internal static string? LocalIP;
        static void Main(string[] args)
        {
            var processes = Process.GetProcessesByName("SimpleNet");
            if (processes.Any(process => process.Id != Environment.ProcessId))
            {
                Console.WriteLine("This program is not repeatable");
                return;
            }
            string fail = string.Empty;
            if (!UpdateStartPath(ref fail))
            {
                Console.WriteLine(fail);
                return;
            }
            if (!ReadSettings(ref fail))
            {
                Console.WriteLine(fail);
                return;
            }
            GetIP();
            MainLobby mainLobby = new MainLobby();
            mainLobby.StartListen(_ipv4Port, _ipv6Port, _localSeverName);
            while(true)
            {
                Thread.Sleep(30000);
            }
        }
        private static async void GetIP()
        {
            Dns.BeginGetHostAddresses(Dns.GetHostName(), ar =>
            {
                IPAddress[] array = Dns.EndGetHostAddresses(ar);
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        LocalIP = array[i].ToString();
                        Console.WriteLine(LocalIP);
                        return;
                    }
                }
                Console.WriteLine("Fail Find Local IP");
            }, null);
        }
        private static bool ReadSettings(ref string failReason)
        {
            string settings = Path.Combine(BaseDir, "Settings.txt");
            if (File.Exists(settings))
            {
                string[] lines = File.ReadAllLines(settings);
                foreach (string line in lines)
                {
                    try
                    {
                        string[] contents = line.Split(" ");
                        switch (contents[0].ToLower())
                        {
                            case "p4":
                            case "ipv4port":
                                {
                                    _ipv4Port = int.Parse(contents[1]);
                                    break;
                                }
                            case "p6":
                            case "ipv6port":
                                {
                                    _ipv6Port = int.Parse(contents[1]);
                                    break;
                                }
                            case "lsn":
                            case "localservername":
                                {
                                    _localSeverName = contents[1];
                                    break;
                                }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Argument row that could not be resolved:" + line);
                        Console.WriteLine("Override the current configuration with the default configuration?\nInput Yes to apply or other to skip");
                        if(Console.ReadLine()?.ToLower()=="yes")
                        {
                            File.Delete(settings);
                            return ReadSettings(ref settings);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Auto create default settings...");
                using FileStream stream = File.OpenWrite(settings);
                using StreamWriter writer = new(stream);
                writer.WriteLine("ipv4port 7777");
                writer.WriteLine("ipv6port 7777");
                writer.WriteLine("localservername SimpleNet");
                writer.Close();
                stream.Close();
                Console.WriteLine("The default settings is deployed");
                return ReadSettings(ref failReason);
            }
            if(!_ipv4Port.HasValue)
            {
                failReason = "At least one Ipv4 port must be configured";
                return false;
            }
            return true;
        }
        private static bool UpdateStartPath(ref string failReason)
        {
            try
            {
                string currentPath = Path.Combine(Environment.CurrentDirectory, "SimpleNet.exe");
                Directory.CreateDirectory(BaseDir);
                File.WriteAllText(Path.Combine(BaseDir, "StartPath.txt"), currentPath);
                return true;
            }
            catch(Exception e)
            {
                failReason = e.ToString();
                return false;
            }
        }
    }
}
