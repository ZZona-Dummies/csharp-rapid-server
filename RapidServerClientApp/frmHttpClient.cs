﻿using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace RapidServerClientApp
{
    public partial class frmHttpClient : Form
    {
        public frmHttpClient()
        {
            InitializeComponent();
        }

        private RapidServer.Http.Type1.Client client = new RapidServer.Http.Type1.Client(false);

        public Hashtable Sites = new Hashtable();

        public Hashtable Tools = new Hashtable();

        public string stdout;

        public delegate void HandleResponseDelegate(string res);

        public delegate void ConnectFailedDelegate();

        public delegate void LogMessageDelegate(string message);

        private void client_ConnectFailed()
        {
            Invoke(new ConnectFailedDelegate(new System.EventHandler(this.ConnectFailed)));
        }

        private void client_HandleResponse(string res, object state)
        {
            Invoke(new HandleResponseDelegate(new System.EventHandler(this.HandleResponse)), res);
        }

        private void client_LogMessage(string message)
        {
            Invoke(new LogMessageDelegate(new System.EventHandler(this.LogMessage)), message);
        }

        //  form load
        private void frmHttpClient_Load(object sender, System.EventArgs e)
        {
            //  load the config
            this.LoadConfig();
            //  add the sites
            foreach (Site s in this.Sites.Values)
            {
                cboUrl.Items.Add(s.Url);
            }

            cboUrl.SelectedItem = cboUrl.Items(0);
            //  add the tools
            foreach (Tool t in this.Tools.Values)
            {
                cboBenchmarkTool.Items.Add(t.Name);
                cboBenchmarkTool2.Items.Add(t.Name);
                cboBenchmarkTool3.Items.Add(t.Name);
            }

            cboBenchmarkTool.SelectedItem = cboBenchmarkTool.Items(0);
            cboBenchmarkTool2.SelectedItem = cboBenchmarkTool2.Items(0);
            cboBenchmarkTool3.SelectedItem = cboBenchmarkTool3.Items(0);
        }

        // '' <summary>
        // '' Loads the server config file http.xml from disk and configures the server to operate as defined by the config.
        // '' </summary>
        // '' <remarks></remarks>
        void LoadConfig()
        {
            //  TODO: Xml functions are very picky after load, if we try to access a key that doesn't exist it will throw a 
            //    vague error that does not stop the debugger on the error line, and the innerexception states 'object reference 
            //    not set to an instance of an object'. a custom function GetValue() helps avoid nulls but not this. default values should
            //    be assumed by the server for cases when the value can't be loaded from the config, or the server should regenerate the config 
            //    per its known format and then load it.
            if ((IO.File.Exists("client.xml") == false))
            {
                this.CreateConfig();
            }

            Xml.XmlDocument cfg = new Xml.XmlDocument();
            try
            {
                cfg.Load("client.xml");
            }
            catch (Exception ex)
            {
                //  TODO: we need to notify the user that the config couldn't be loaded instead of just dying...
                Console.WriteLine(ex.Message);
                return;
            }

            Xml.XmlNode root = cfg["Settings"];
            //  parse the sites:
            foreach (Xml.XmlNode n in root["Sites"])
            {
                Site s = new Site();
                s.Name = n["Name"].GetValue;
                s.Description = n["Description"].GetValue;
                s.Url = n["Url"].GetValue;
                this.Sites.Add(s.Name, s);
            }

            //  parse the tools:
            foreach (Xml.XmlNode n in root["Tools"])
            {
                Tool t = new Tool();
                t.Name = n["Name"].GetValue;
                t.Path = n["Path"].GetValue;
                t.Speed = n["Speed"].GetValue;
                t.Time = n["Time"].GetValue;
                foreach (Xml.XmlNode nn in n["Data"])
                {
                    if ((nn.Name == "RPS"))
                    {
                        t.Data.RPS = nn.InnerText;
                    }
                    else if ((nn.Name == "CompletedRequests"))
                    {
                        t.Data.CompletedRequests = nn.InnerText;
                    }
                    else if ((nn.Name == "ResponseTime"))
                    {
                        t.Data.ResponseTime = nn.InnerText;
                    }

                }

                this.Tools.Add(t.Name, t);
            }

        }

        void CreateConfig()
        {
        }

        void DetectSystemInfo()
        {
            if ((Chart1.Titles.Count == 1))
            {
                //  get os
                Management.ManagementObjectSearcher wmios = new Management.ManagementObjectSearcher("SELECT * FROM  Win32_OperatingSystem");
                object os = wmios.Get.Cast(Of, Management.ManagementObject).First;
                string osName = os["Name"];
                if (osName.Contains("Windows 7"))
                {
                    osName = "Win7";
                }

                osName = (osName + (" " + os["OSArchitecture"].trim));
                //  get cpu
                Management.ManagementObjectSearcher wmicpu = new Management.ManagementObjectSearcher("SELECT * FROM  Win32_Processor");
                object cpu = wmicpu.Get.Cast(Of, Management.ManagementObject).First;
                string cpuName = cpu["Name"];
                cpuName = cpuName.Replace("(R)", "").Replace("(r)", "").Replace("(TM)", "").Replace("(tm)", "").Replace("CPU ", "").Trim;
                //  get ram
                Management.ManagementObjectSearcher wmiram = new Management.ManagementObjectSearcher("SELECT * FROM  Win32_ComputerSystem");
                object ram = wmiram.Get.Cast(Of, Management.ManagementObject).First;
                int totalRam = (ram["TotalPhysicalMemory"] / (1024 / (1024 / 1024)));
                //  print results to chart title
                Chart1.Titles.Add((osName + (" - "
                                + (cpuName + (" - "
                                + (totalRam + "GB"))))));
                Chart3.Titles.Add((osName + (" - "
                                + (cpuName + (" - "
                                + (totalRam + "GB"))))));
                Chart2.Titles.Add((osName + (" - "
                                + (cpuName + (" - "
                                + (totalRam + "GB"))))));
            }

        }

        //  runs the benchmark tool with selected parameters
        void RunBenchmark()
        {
            if ((TabControl3.SelectedTab.Text == "Speed"))
            {
                this.SpeedBenchmark();
            }
            else if ((TabControl3.SelectedTab.Text == "Time"))
            {
                this.TimeBenchmark();
            }
            else
            {

            }

        }

        //  TODO: this gets an unhandled exception when it tries to parse data that doesn't exist, we shouldn't assume 
        //    we'll always have the data and use a try...catch here with error reporting
        private string SubstringBetween(string s, string startTag, string endTag)
        {
            try
            {
                string ss = "";
                int i;
                if ((startTag.ToLower == "vbcrlf"))
                {
                    startTag = "\r\n";
                }

                if ((endTag.ToLower == "vbcrlf"))
                {
                    endTag = "\r\n";
                }

                i = s.IndexOf(startTag);
                ss = s.Substring((i + startTag.Length));
                i = ss.IndexOf(endTag);
                ss = ss.Substring(0, i);
                return ss.Trim;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }

        }

        private string[] ParseAny(string s)
        {
            if ((s == ""))
            {
                return new string[] {
                    "FAIL WHALE!"};
            }

            string[,] results;
            string[] spl = s.Split(",");
            if ((spl[0] == "stdout"))
            {
                //  data is in stdout
                if ((spl[1] == "between"))
                {
                    results[0] = this.SubstringBetween(this.stdout, spl[2], spl[3]);
                }

            }
            else
            {
                //  data is in a file
                IO.StreamReader f = new IO.StreamReader(spl[0]);
                string delim = "";
                if ((spl[1] == "tabs"))
                {
                    delim = '\t';
                }
                else
                {
                    delim = ",";
                }

                //  determine if the tool data contains a formula
                string[] formula = null;
                bool useFormula = false;
                if (spl[3].Contains("+"))
                {
                    //  the data we want requires a formula rather than a single value
                    formula = spl[3].Split("+");
                    useFormula = true;
                }

                //  TODO: check if we should read the file as rows or as summary...
                //  read the file as rows
                ArrayList lines = new ArrayList();
                while ((f.Peek != -1))
                {
                    lines.Add(f.ReadLine);
                }

                f.Close();
                f.Dispose();
                //  filter the rows
                if ((spl[2] == "first"))
                {
                    //  grab the first row only
                    for (int i = 0; (i
                                <= (lines.Count - 1)); i++)
                    {
                        lines.RemoveAt(1);
                    }

                    string line = lines[0];
                    string[] fields = line.Split(delim);
                    if ((useFormula == true))
                    {
                        int val = 0;
                        for (int i = 0; (i
                                    <= (formula.Length - 1)); i++)
                        {
                            val = (val + fields[formula[i]]);
                        }

                        results[0] = val;
                    }
                    else
                    {
                        results[0] = fields[spl[3]].Trim;
                    }

                }
                else if ((spl[2] == "last"))
                {
                    //  grab the last row only
                    for (int i = 0; (i
                                <= (lines.Count - 2)); i++)
                    {
                        lines.RemoveAt(0);
                    }

                    string line = lines[0];
                    string[] fields = line.Split(delim);
                    if ((useFormula == true))
                    {
                        int val = 0;
                        for (int i = 0; (i
                                    <= (formula.Length - 1)); i++)
                        {
                            val = (val + fields[formula[i]]);
                        }

                        results[0] = val;
                    }
                    else
                    {
                        results[0] = fields[spl[3]].Trim;
                    }

                }
                else
                {
                    //  grab all the rows after a specific row index
                    for (int i = 0; (i
                                <= (spl[2] - 1)); i++)
                    {
                        lines.RemoveAt(i);
                    }

                    for (int i = 0; (i
                                <= (lines.Count - 1)); i++)
                    {
                        string line = lines[i];
                        string[] fields = line.Split(delim);
                        if ((useFormula == true))
                        {
                            int val = 0;
                            for (int ii = 0; (ii
                                        <= (formula.Length - 1)); ii++)
                            {
                                val = (val + fields[formula[ii]]);
                            }

                            results[(results.Length - 1)] = val;
                        }
                        else
                        {
                            results[(results.Length - 1)] = fields[spl[3]].Trim;
                        }

                        object Preserve;
                        results[results.Length];
                    }

                }

            }

            return results;
        }

        void ParseResults(string results)
        {
            this.stdout = results;
            Tool currentTool = this.Tools(cboBenchmarkTool.Text);
            //  parse it
            string[] requestsPerSecond = this.ParseAny(currentTool.Data.RPS);
            string[] completedRequests = this.ParseAny(currentTool.Data.CompletedRequests);
            // Dim time() As String = ParseAny(currentTool.Data.ResponseTime)
            //  chart it
            bool failedParse = false;
            if (((requestsPerSecond[0] == "")
                        || (completedRequests[0] == "")))
            {
                failedParse = true;
            }

            if ((failedParse == false))
            {
                Site currentSite = null;
                foreach (Site s in this.Sites.Values)
                {
                    if ((s.Url == cboUrl.Text))
                    {
                        currentSite = s;
                    }

                }

                string seriesName = cboUrl.Text;
                if (currentSite)
                {
                    IsNot;
                    null;
                    seriesName = currentSite.Name;
                    //  update the rps log
                    //  TODO: remove sitename and match text color to legend color
                    TextBox1.AppendText((seriesName + (" - "
                                    + (requestsPerSecond[0] + "\r\n"))));
                    //  plot the requests completed value to the bar chart
                    if ((Chart1.Series.IndexOf(seriesName) == -1))
                    {
                        Chart1.Series.Add(seriesName);
                    }

                    Chart1.Series(seriesName).Points.AddXY(0, requestsPerSecond[0]);
                    //  plot the requests completed value to the bar chart
                    if ((Chart2.Series.IndexOf(seriesName) == -1))
                    {
                        Chart2.Series.Add(seriesName);
                    }

                    Chart2.Series(seriesName).Points.AddXY(0, completedRequests[0]);
                    //  plot the gnuplot data to the line graph
                    if ((Chart3.Series.IndexOf(seriesName) == -1))
                    {
                        //  create the series for this url
                        Chart3.Series.Add(seriesName);
                        Chart3.Series(seriesName).ChartType = DataVisualization.Charting.SeriesChartType.FastLine;
                    }
                    else
                    {
                        //  series was already plotted, clear the series and replot it
                        Chart3.Series(seriesName).Points.Clear();
                    }

                    // For Each s As String In time
                    //     Chart3.Series(seriesName).Points.AddXY(0, s)
                    // Next
                }
                else
                {
                    //  update the rps log
                    TextBox1.AppendText(("FAIL WHALE!" + "\r\n"));
                }

            }

        }

        void TimeBenchmark()
        {
            Tool t = this.Tools(cboBenchmarkTool.Text);
            string cmd = t.Time;
            cmd = cmd.Replace("%time", txtBenchmarkDuration.Text);
            cmd = cmd.Replace("%url", (cboUrl.Text.TrimEnd("/") + "/"));
            this.LogMessage((t.Path + cmd));
            ManagedProcess p = new ManagedProcess(t.Path, cmd);
            txtRaw.Text = p.Output.ToString;
            this.ParseResults(p.Output.ToString);
        }

        void SpeedBenchmark()
        {
            Tool t = this.Tools(cboBenchmarkTool.Text);
            string cmd = t.Speed;
            cmd = cmd.Replace("%num", txtBenchmarkNumber.Text);
            cmd = cmd.Replace("%conc", txtBenchmarkConcurrency.Text);
            cmd = cmd.Replace("%url", (cboUrl.Text.TrimEnd("/") + "/"));
            this.LogMessage((t.Path + cmd));
            ManagedProcess p = new ManagedProcess(t.Path, cmd);
            //  TODO: this throws an exception due to multithreading...
            //    https://stackoverflow.com/questions/24181910/stringbuilder-thread-safety?rq=1
            txtRaw.Text = p.Output.ToString;
            this.ParseResults(p.Output.ToString);
        }

        //  ramp by increasing concurrency each iteration: http://wiki.dreamhost.com/Web_Server_Performance_Comparison
        void RampBenchmark()
        {
        }

        //  append message to log
        void LogMessage(string message)
        {
            //  prepare the date
            string clrDate = "";
            Now.ToString("dd/MMM/yyyy:hh:mm:ss zzz");
            clrDate = clrDate.Remove(clrDate.LastIndexOf(":"), 1);
            //  log access events using CLF (combined log format):
            txtLog.AppendText("127.0.0.1");
            txtLog.AppendText(" -");
            //  remote log name - leave null for now
            txtLog.AppendText(" -");
            //  client username - leave null for now
            txtLog.AppendText((" ["
                            + (clrDate + "]")));
            txtLog.AppendText((" \"" + message.Replace("\r\n", " ").TrimEnd(" ")));
            txtLog.AppendText("\"");
            txtLog.AppendText("\r\n");
        }

        //  connect to server failed (invoked server event)
        void ConnectFailed()
        {
            txtRaw.Text = ("Could not connect." + "\r\n");
            ("Could not connect." + "\r\n");
        }

        //  response is being handled by the server (invoked server event)
        void HandleResponse(string res)
        {
            res;
        }

        private void btnGo_Click(object sender, System.EventArgs e)
        {
            btnGo.Enabled = false;
            txtRaw.Text = "";
            if ((TabControl1.SelectedTab.Text == "Benchmark"))
            {
                this.RunBenchmark();
            }
            else
            {
                client.Go(cboUrl.Text, null);
                if (cboUrl.Items.Contains(cboUrl.Text))
                {

                }
                else
                {
                    cboUrl.Items.Add(cboUrl.Text);
                }

            }

            btnGo.Enabled = true;
        }

        private void chkWrapLog_CheckedChanged(object sender, System.EventArgs e)
        {
            if ((chkWrapLog.Checked == true))
            {
                txtLog.WordWrap = true;
            }
            else
            {
                txtLog.WordWrap = false;
            }

        }

        private void btnDetectSystemInfo_Click(object sender, System.EventArgs e)
        {
            this.DetectSystemInfo();
        }

        private void btnClear_Click(object sender, System.EventArgs e)
        {
            while ((Chart1.Series.Count > 0))
            {
                Chart1.Series.RemoveAt(0);
            }

            while ((Chart3.Series.Count > 0))
            {
                Chart3.Series.RemoveAt(0);
            }

            while ((Chart2.Series.Count > 0))
            {
                Chart2.Series.RemoveAt(0);
            }

            TextBox1.Text = "";
        }

        private void cboBenchmarkTool_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            Tool t = this.Tools(cboBenchmarkTool.Text);
            if (t.Speed.Contains("%num"))
            {
                txtBenchmarkNumber.Enabled = true;
            }
            else
            {
                txtBenchmarkNumber.Enabled = false;
            }

            if (t.Speed.Contains("%conc"))
            {
                txtBenchmarkConcurrency.Enabled = true;
            }
            else
            {
                txtBenchmarkConcurrency.Enabled = false;
            }

            if (t.Time.Contains("%time"))
            {
                txtBenchmarkDuration.Enabled = true;
            }
            else
            {
                txtBenchmarkDuration.Enabled = false;
            }

            cboBenchmarkTool.SelectedIndex = ((ComboBox)(sender)).SelectedIndex;
            cboBenchmarkTool2.SelectedIndex = ((ComboBox)(sender)).SelectedIndex;
            cboBenchmarkTool3.SelectedIndex = ((ComboBox)(sender)).SelectedIndex;
        }
    }
    class DataPoint
    {

        public Hashtable Topics = new Hashtable();
    }
    class ManagedProcess
    {

        public Process Process = new Process();

        public Text.StringBuilder Output = new Text.StringBuilder();

        ManagedProcess(void filename, void commandline)
        {
            //  use a process to run the benchmark tool and read its results
            string results = "";
            Process p = this.Process;
            p.OutputDataReceived += new System.EventHandler(this.ReadOutputAsync);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = filename;
            p.StartInfo.Arguments = commandline;
            try
            {
                p.Start();
                p.BeginOutputReadLine();
                //  TODO: siege -c1000 causes a hang with WaitForExit() and no timeout...
                p.WaitForExit();
                // p.Close()
                // p.Dispose()
            }
            catch (Exception ex)
            {
                this.Output.Append("the tool process failed to run");
            }

        }

        void ReadOutputAsync(object sender, DataReceivedEventArgs e)
        {
            Output.AppendLine(e.Data);
        }
    }
    class Site
    {

        public string Name;

        public string Description;

        public string Url;
    }
    class Tool
    {

        public string Name;

        public string Path;

        public string Speed;

        public string Time;

        public ToolData Data = new ToolData();
    }
    class ToolData
    {

        public string RPS;

        public string CompletedRequests;

        public string ResponseTime;
    }
}