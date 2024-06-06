
using Microsoft.Win32;
using Newtonsoft.Json;
using SimpleNet.DataStructures;
using SimpleNet.Lobbys;
using System.Diagnostics;
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
            try
            {
                var ipApiClient = new HttpClient();
                var ipApiUrl = "http://ip-api.com/json/";
                var response = await ipApiClient.GetStringAsync(ipApiUrl);
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(response);
                LocalIP = jsonResponse.query;
            }
            catch (Exception ex)
            {
                Console.WriteLine("获取公网IP地址时发生异常：" + ex.Message);
            }
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
                string currentPath = Assembly.GetExecutingAssembly().Location;
                Directory.CreateDirectory(BaseDir);
                using FileStream stream = File.OpenWrite(Path.Combine(BaseDir, "StartPath.txt"));
                using StreamWriter writer = new(stream);
                writer.Write(currentPath);
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
