using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace DispatcherService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected private IConfiguration returnConfiguration()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            return configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IConfiguration configuration = returnConfiguration();

            int timerTime = Convert.ToInt32(configuration["DispatcherSettings:timerTime"]);
            string scriptDir = configuration["DispatcherSettings:scriptDir"];
            string configName = configuration["DispatcherSettings:configName"];

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(scriptDir + "\\" + configName);
            }
            catch (Exception e)
            {
                _logger.LogInformation("7A: Cannot open XML Configuration at " + scriptDir + "\\" + configName + e);
            }

            try
            {
                timerTime = Convert.ToInt32(doc.DocumentElement.SelectSingleNode("/configuration/timerTime").InnerText);
            }
            catch (Exception e)
            {
                _logger.LogInformation("7C: Cannot reset timer time " + e);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Processing.");
                TimerElapsed();
                await Task.Delay(timerTime, stoppingToken);
            }
        }

        private void TimerElapsed()
        {
            int ErrorRun = 0;
            String serviceStartStopStatus = "started";

            IConfiguration configuration = returnConfiguration();

            string scriptDir = configuration["DispatcherSettings:scriptDir"];
            string configName = configuration["DispatcherSettings:configName"];

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(scriptDir + "\\" + configName);

                String pattern = "\\\\+";
                String replacement = "\\";

                Regex rgx = new Regex(pattern);

                String pattern2 = "\r";
                String replacement2 = "";
                Regex srp = new Regex(pattern2);

                String scriptName = doc.DocumentElement.SelectSingleNode("/configuration/scriptName").InnerText;
                String exeLocation = doc.DocumentElement.SelectSingleNode("/configuration/exeLocation").InnerText;
                String exeName = doc.DocumentElement.SelectSingleNode("/configuration/exeName").InnerText;
                String exeParameters = doc.DocumentElement.SelectSingleNode("/configuration/exeParameters").InnerText;

                scriptDir = rgx.Replace(scriptDir, replacement);
                scriptName = rgx.Replace(scriptName, replacement);
                exeLocation = rgx.Replace(exeLocation, replacement);
                exeName = rgx.Replace(exeName, replacement);
                exeParameters = rgx.Replace(exeParameters, replacement);

                scriptDir = srp.Replace(scriptDir, replacement2);
                scriptName = srp.Replace(scriptName, replacement2);
                exeLocation = srp.Replace(exeLocation, replacement);
                exeName = srp.Replace(exeName, replacement);
                exeParameters = srp.Replace(exeParameters, replacement);

                if (!System.IO.Directory.Exists(scriptDir))
                {
                    ErrorRun = 1;
                    _logger.LogInformation("1: The Script Directory " + scriptDir + " does not exist");
                }

                if (!File.Exists(scriptDir + "\\" + scriptName))
                {
                    ErrorRun = 1;
                    _logger.LogInformation("2: The Executable Script File " + scriptDir + "\\" + scriptName + " does not exist");
                }

                if (!File.Exists(scriptDir + "\\" + configName))
                {
                    ErrorRun = 1;
                    _logger.LogInformation("2: The Configuration File " + scriptDir + "\\" + configName + " does not exist");
                }

                if (!File.Exists(exeLocation + "\\" + exeName))
                {
                    ErrorRun = 1;
                    _logger.LogInformation("4: The Executable File " + exeLocation + "\\" + exeName + " does not exist");
                }

                if (ErrorRun == 0)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = false;
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = "\"" + exeLocation + "\\" + exeName + "\"";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    //Reconfig php input vars
                    if (exeParameters == "" || exeParameters == null)
                    {
                        _logger.LogInformation("DispatcherService: Starting without parameters.");
                        startInfo.Arguments = "\"" + scriptDir + "\\" + scriptName + "\" \"STATUS=" + serviceStartStopStatus + "\"  \"scriptDirectory=" + scriptDir + "\" \"configurationFile=" + configName + "\"";
                    }
                    else
                    {
                        _logger.LogInformation("DispatcherService: Starting with parameters.");
                        startInfo.Arguments = "\"" + exeParameters + " "+ scriptDir + "\\" + scriptName + "\" \"STATUS=" + serviceStartStopStatus + "\"  \"scriptDirectory=" + scriptDir + "\" \"configurationFile=" + configName + "\"";
                    }
                    try
                    {
                        // Start the process with the info we specified.
                        // Call WaitForExit and then the using statement will close.
                        using (Process exeProcess = Process.Start(startInfo))
                        {
                            exeProcess.WaitForExit();
                        }
                    }
                    catch
                    {
                        _logger.LogInformation("5: There was an error executing " + exeLocation + "\\" + exeName + " with the flags " + "\"" + scriptDir + "\\" + scriptName + "\"");
                    }

                }
            }
            catch (Exception f)
            {
                _logger.LogInformation("7B: Cannot open XML Configuration at " + scriptDir + "\\" + configName + f);
            }

        }

    }
}