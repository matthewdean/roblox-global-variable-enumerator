using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Web.Helpers;

namespace PluginSettingsWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            // find the current version of studio
            var robloxDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox");
            var currentStudioVersion = (string) Registry.GetValue(@"HKEY_CURRENT_USER\Software\ROBLOX Corporation\Roblox", "curQTStudioVer", null);
            var studioDirectory = Path.Combine(robloxDirectory, "Versions", currentStudioVersion);
            var studioExecutable = Path.Combine(studioDirectory, "RobloxStudioBeta.exe");

            // search RobloxStudioBeta.exe for ASCII strings of two or more characters
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "strings.exe";
            info.Arguments = "-q -n 2 -a " + studioExecutable;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            // collect only strings which are valid Lua identifiers
            var luaIdentifier = new Regex(@"^[_a-zA-Z][\w]*$");
            var possibleIdentifiers = new HashSet<string>();

            using (var process = new Process())
            {
                process.StartInfo = info;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null && luaIdentifier.IsMatch(e.Data))
                    {
                        possibleIdentifiers.Add(e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }

            // write possible identifiers to the global plugin's settings file
            var pluginDirectory = Path.Combine(robloxDirectory, @"InstalledPlugins\0");
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }
            var pluginSettingsFile = Path.Combine(pluginDirectory, "settings.json");
            var serializer = new JavaScriptSerializer();
            var output = serializer.Serialize(new { Candidates = possibleIdentifiers });
            File.WriteAllText(pluginSettingsFile, output);

            // open studio to perform a brute-force search of possible identifiers against the global environment
            var script = @"
                wait(0)
                local plugin = PluginManager():CreatePlugin()
                local candidates = plugin:GetSetting('Candidates')
                plugin:SetSetting('Candidates', {})
                local globalEnvironment = getfenv()
                local globalVariables = {}
                for _, identifier in pairs(candidates) do
                    if globalEnvironment[identifier] ~= nil then
                        table.insert(globalVariables, identifier)
                    end
                end
                plugin:SetSetting('GlobalVariables', globalVariables)
                game:GetService('TestService'):DoCommand('ShutdownClient')
            ";
            var arguments = String.Format("-script \"{0}\"", script);
            using (var studio = Process.Start(studioExecutable, arguments))
            {
                studio.WaitForExit();
            }

            // open the plugin settings file and read the results
            var settings = Json.Decode(File.ReadAllText(pluginSettingsFile));
            foreach (string globalVariable in settings.GlobalVariables)
            {
                Console.WriteLine(globalVariable);
            }
            Console.ReadLine();
        }
    }
}
