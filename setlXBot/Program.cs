using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using setlXBot.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Discord.Commands;
using System.Net;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Timers;

namespace setlXBot
{
    class Program
    {
        private DiscordClient client;
        private Logger mainLogger;
        private Dictionary<string, Process> processList;

        static void Main(string[] args) => new Program().Start();

        public void Start()
        {
            client = new DiscordClient();
            mainLogger = new Logger(Helper.ParseEnum<LogLevel>(ConfigurationManager.AppSettings["LogLevel"]), ConfigurationManager.AppSettings["LogFile"]);
            processList = new Dictionary<string, Process>();

            client.UsingCommands(x =>
                {
                    x.PrefixChar = Convert.ToChar(ConfigurationManager.AppSettings["Prefix"]);
                    x.HelpMode = HelpMode.Public;
                });

            client.GetService<CommandService>().CreateCommand("version")
                .Alias(new string[] { "v" })
                .Description("Shows setlX version.")
                .Do(e =>
                {
                    try
                    {
                        mainLogger.Debug(e.User.Name, "Showed setlX version");
                        ExecuteSetlX("--version", e, "setlX", processList);
                    }
                    catch (Exception ex)
                    {
                        Error(e, "command version |", ex.Message);
                    }
                });

            client.GetService<CommandService>().CreateCommand("setlx")
                .Alias(new string[] { "s", "exec" })
                .Description("Starts interactive mode or executes the first attached setlx file. 'arg' can be: 'code'... ")
                .Parameter("arg", ParameterType.Optional)
                .Do(e =>
                {
                    try

                        if (processList.ContainsKey(e.Channel.Name) && !processList[e.Channel.Name].HasExited)
                        {
                            e.Channel.SendMessage("There is already a setlX process");
                        }
                        else
                        {
                            if (e.Message.Attachments.Count() > 0) // File Mode
                            {
                                Message.Attachment setlxFile = e.Message.Attachments.First();
                                if (setlxFile.Filename.ToLower().EndsWith("stlx"))
                                {
                                    string setlxFilePath = Path.Combine(@"X:\temp", setlxFile.Filename);
                                    FileInfo fi = new FileInfo(setlxFilePath);
                                    if (fi.Exists)
                                    {
                                        mainLogger.Debug(e.User.Name, setlxFilePath, "deleted");
                                        fi.Delete();
                                    }
                                    using (var client = new WebClient())
                                    {
                                        client.DownloadFile(setlxFile.Url, setlxFilePath);
                                        mainLogger.Debug(e.User.Name, setlxFilePath, "downloaded");
                                    }

                                    if (!String.IsNullOrEmpty(e.GetArg("arg")))
                                    {
                                        string arg = e.GetArg("arg");
                                        switch (arg)
                                        {
                                            case "code":
                                                Print(e, File.ReadAllText(setlxFilePath), setlxFile.Filename, "Code");
                                                mainLogger.Debug(e.User.Name, "Code of ", setlxFilePath, "was printed");
                                                break;
                                        }
                                    }

                                    ExecuteSetlX(setlxFilePath, e, setlxFile.Filename, processList);

                                }
                                else
                                {
                                    e.Channel.SendMessage("There is no .stlx file in the attachments...");
                                    mainLogger.Debug(e.User.Name, "There is no .stlx file in the attachments...");
                                }
                            }
                            else //Interactive Mode
                            {
                                ExecuteSetlX(String.Empty, e, "Interactive Mode", processList);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Error(e, "command setlx |", ex.Message);
                    }
                });


            client.GetService<CommandService>().CreateCommand("input")
                .Alias(new string[] { "i" })
                .Description("Writes string in StandartInput of setlX process in your current channel.")
                .Parameter("input", ParameterType.Required)
                .Do(e =>
                {
                    try
                    {
                        if (processList.ContainsKey(e.Channel.Name) && !processList[e.Channel.Name].HasExited)
                        {
                            string input = e.GetArg("input");
                            mainLogger.Debug(e.Message.User.Name, input, "was written in", e.Message.Channel.Name);

                            StreamWriter sw = processList[e.Channel.Name].StandardInput;
                            sw.WriteLine(input);
                        }
                        else
                        {
                            List<string> processDeleteList = new List<string>();
                            foreach (KeyValuePair<string, Process> proc in processList)
                            {
                                if (proc.Value.HasExited)
                                {
                                    processDeleteList.Add(proc.Key);
                                }
                            }
                            processDeleteList.ForEach(x => processList.Remove(x));

                            e.Channel.SendMessage("There is no setlx process in this channel!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Error(e, "command input |", ex.Message);
                    }
                });

            client.GetService<CommandService>().CreateCommand("kill")
                .Alias(new string[] { "k", "exit", "e" })
                .Description("Kills the setlX-process your current channel.")
                .Do(e =>
                {
                    try
                    {
                        processList[e.Channel.Name].Kill();
                        processList.Remove(e.Channel.Name);
                    }
                    catch (Exception ex)
                    {
                        Error(e, "command input |", ex.Message);
                    }
                });

            client.GetService<CommandService>().CreateCommand("list")
                .Alias(new string[] { "l" })
                .Description("Shows you the process list")
                .Hide()
                .Do(e =>
                {
                    try
                    {
                        if (processList != null && processList.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder();

                            foreach (KeyValuePair<string, Process> proc in processList)
                            {
                                sb.Append(proc.Key);
                                sb.Append(" - ");
                                sb.Append(proc.Value.Id);
                                sb.AppendLine();
                            }

                            e.Channel.SendMessage(sb.ToString());
                        }
                        else
                        {
                            e.Channel.SendMessage("There are no setlx processes.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Error(e, "command input |", ex.Message);
                    }
                });

            client.ExecuteAndWait(async () =>
            {
                await client.Connect(ConfigurationManager.AppSettings["BotToken"], TokenType.Bot);
                client.SetGame("C#");

                mainLogger.Info("System", "setlXBot is up!");
            });
        }

        public async void ExecuteSetlX(string arguments, CommandEventArgs cea, string name, Dictionary<string, Process> processList)
        {
            bool err = false;
            string stdout = String.Empty;
            string stderr = String.Empty;
            int stdoutCount = 0;
            bool timerSet = false;
            bool exited = false;

            Message msg = await cea.Channel.SendMessage(String.Format("**Executing {0}**", name));

            Process proc = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = ConfigurationManager.AppSettings["SetlXPath"],
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };
            proc.ErrorDataReceived += (sender, e) =>
            {
                if (e != null && !String.IsNullOrEmpty(e.Data))
                {
                    stderr = String.Format("{0}{1}", stderr, e.Data);

                    err = true;
                    if (e.Data.Length > Convert.ToInt32(ConfigurationManager.AppSettings["MessageMaxLength"]))
                    {
                        Regex regex = new Regex(String.Format(@"[\s\S]{{1,{0}}}", ConfigurationManager.AppSettings["MessageMaxLength"]));
                        MatchCollection outputPartsCode = regex.Matches(e.Data);

                        for (int i = 0; i < outputPartsCode.Count; i++)
                        {
                            cea.Channel.SendMessage(String.Format("```{0}```", outputPartsCode[i].Groups[0].Value));
                        }
                    }
                    else
                    {
                        cea.Channel.SendMessage(String.Format("```{0}```", e.Data));
                    }
                }
            };
            proc.OutputDataReceived += (sender, e) =>
            {
                if (e != null && !String.IsNullOrEmpty(e.Data) && !e.Data.Equals("=> "))
                {
                    int allowedCharAmount = Convert.ToInt32(ConfigurationManager.AppSettings["MessageMaxLength"]);
                    string temp = String.Format("{0}\n{1}", stdout, e.Data);
                    if (temp.Length <= allowedCharAmount)
                    {
                        stdout = temp;
                        stdoutCount++;
                    }
                    else if (!String.IsNullOrEmpty(stdout) && stdout.Length <= allowedCharAmount)
                    {
                        SendMessage(stdout, allowedCharAmount, cea);
                        stdout = e.Data;
                        stdoutCount = 0;
                    }
                    else
                    {
                        SplitStringInParts(temp, allowedCharAmount).ForEach(x =>
                        {
                            cea.Channel.SendMessage(String.Format("```{0}```", x));
                        });
                        stdout = String.Empty;
                        stdoutCount = 0;

                    }

                    if (stdoutCount == Convert.ToInt32(ConfigurationManager.AppSettings["StdOutErrCounter"]))
                    {
                        SendMessage(stdout, allowedCharAmount, cea);
                        stdout = String.Empty;
                        stdoutCount = 0;
                    }

                    if (!timerSet)
                    {
                        Timer t = new Timer();
                        t.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["StdOutErrInterval"]);
                        t.Elapsed += (senderII, eII) =>
                        {
                            SendMessage(stdout, allowedCharAmount, cea);
                            stdout = String.Empty;

                            t.Stop();
                            timerSet = false;
                        };
                        t.Start();
                        timerSet = true;
                    }

                    if (proc.HasExited)
                    {
                        exited = true;
                    }
                }
            };
            proc.Exited += (sender, e) =>
            {
                Timer t = new Timer();
                t.Interval = 1000;
                t.Elapsed += (senderII, eII) =>
                {
                    if (exited)
                    {
                        if (err)
                        {
                            cea.Channel.SendMessage(String.Format("**{0} exited __without__ succes**", name));
                            mainLogger.Debug("System", "Process", proc.Id.ToString(), "-", name, "exited without success");
                        }
                        else
                        {
                            cea.Channel.SendMessage(String.Format("**{0} exited with succes**", name));
                            mainLogger.Debug("System", "Process", proc.Id.ToString(), "-", name, "exited with success");
                        }

                        processList.Remove(cea.Channel.Name);
                        t.Stop();
                    }
                };

                t.Start();
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            if (processList.Any(x => x.Key.Equals(cea.Channel.Name)))
            {
                processList[cea.Channel.Name] = proc;
            }
            else
            {
                processList.Add(cea.Channel.Name, proc);
            }


            mainLogger.Debug("System", "Process", proc.Id.ToString(), "-", name, "started");
        }

        private async void SendMessage(string output, int allowedCharAmount, CommandEventArgs cea)
        {
            if (!String.IsNullOrEmpty(output) && output.Length <= allowedCharAmount)
            {
                await cea.Channel.SendMessage(String.Format("```{0}```", output));
                output = String.Empty;
            }
            else
            {
                SplitStringInParts(output, ConfigurationManager.AppSettings["MessageMaxLength"]).ForEach(x =>
                {
                    cea.Channel.SendMessage(String.Format("```{0}```", x));
                });
            }
        }

        public async void Print(CommandEventArgs cea, string output, string name, string type)
        {
            if (output == null && String.IsNullOrEmpty(output))
            {
                return;
            }
            Message lastMessage;

            Regex regex = new Regex(@"[\s\S]{1,1900}");
            MatchCollection outputPartsCode = regex.Matches(output);
            int parts = Convert.ToInt32(Math.Ceiling((double)output.Length / (double)1900));

            if (outputPartsCode.Count == 1)
            {
                lastMessage = await cea.Channel.SendMessage(String.Format("```setlX\n{0} {1}:\n\n{2}```", name, type, outputPartsCode[0].Groups[0].Value));
            }
            else
            {
                for (int i = 0; i < outputPartsCode.Count; i++)
                {
                    lastMessage = await cea.Channel.SendMessage(String.Format("```setlX\n{0} {1} Part {2}/{3}:\n\n{4}```", name, type, (i + 1), parts, outputPartsCode[i].Groups[0].Value));
                }
            }
        }

        public void Error(CommandEventArgs cea, params string[] args)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                sb.Append(args[i]);
                sb.Append(" ");
            }

            cea.Channel.SendMessage(String.Format("An error occured: {0}", sb.ToString()));
            mainLogger.Error("System", "An error occured:", sb.ToString());
        }


        public List<string> SplitStringInParts(string output, string maxStringLength)
        {
            return SplitStringInParts(output, Convert.ToInt32(maxStringLength));
        }

        public List<string> SplitStringInParts(string output, int maxStringLength)
        {
            List<string> res = new List<string>();

            Regex regex = new Regex(String.Format(@"[\s\S]{{1,{0}}}", maxStringLength));
            MatchCollection outputPartsCode = regex.Matches(output);

            for (int i = 0; i < outputPartsCode.Count; i++)
            {
                res.Add(outputPartsCode[i].Groups[0].Value);
            }

            return res;
        }
    }
}
