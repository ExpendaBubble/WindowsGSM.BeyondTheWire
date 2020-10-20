using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;

namespace WindowsGSM.Plugins
{
    public class BTW : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.BeyondTheWire", // WindowsGSM.XXXX
            author = "ExpendaBubble",
            description = " WindowsGSM plugin to add support for Beyond The Wire Dedicated Server",
            version = "1.0",
            url = "https://github.com/ExpendaBubble/WindowsGSM.BeyondTheWire", // Github repository link (Best practice)
            color = "#ffc40b" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "1064780"; // Game server appId, the BTW dedicated server is 1064780

        // - Standard Constructor and properties
        public BTW(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => "WireGameServer.exe"; // Game server start path
        public string FullName = "Beyond The Wire Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new UT3(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "7887"; // Default port
        public string QueryPort = "27165"; // Default query port
        public string Defaultmap = ""; // Default map name
        public string Maxplayers = "100"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            // Creating config path
            string configDir = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), @"WireGame\ServerConfig");
            Directory.CreateDirectory(configDir);

            // cfg files
            string serverCFG = Path.Combine(configDir, "Server.cfg");

            var sb = new StringBuilder();
            sb.Append($"-log");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerIP) ? string.Empty : $" MULTIHOME={_serverData.ServerIP}");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" Port={_serverData.ServerPort}");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerQueryPort) ? string.Empty : $" QueryPort={_serverData.ServerQueryPort}");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? string.Empty : $" FIXEDMAXPLAYERS={_serverData.ServerMaxPlayer}");
            sb.Append(string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}");
        }


        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string runPath = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), "WireGame\\Binaries\\Win64\\WireGameServer.exe");

            string param = "-log";
            param += $" {_serverData.ServerParam}";

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = runPath,
                    Arguments = param.ToString()
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }


        // - Stop server function
        public async Task Stop(Process p) => await Task.Run(() => { p.Kill(); });
    }
}
