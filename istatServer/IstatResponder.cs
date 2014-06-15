using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace istatServer
{
    /// <summary>
    /// Handles parsing the messages from the client and sending out the appropriate response.
    /// </summary>
    internal class IstatResponder
    {

        private const string HEADER = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
        private const string AUTHORIZE = "<isr ath=\"{0}\" ss=\"{1}\" c=\"{2}\" n=\"{3}\"></isr>";
        private const string KEEP_ALIVE = "<isr></isr>";
        private const string ACCEPT_CODE = "<isr ready=\"1\"></isr>";
        private const string REJECT_CODE = "<isr athrej=\"1\"></isr>";
        private const string SESSION = "<isr ds=\"{0}\" ts=\"{1}\" fs=\"{2}\" rid=\"{3}\">";
        private readonly Stat _stat;
        private byte[] _lastResponse;
        private readonly Clients _clients;
        private readonly string _authfileFullPath;
        private const string DEFAULT_PIN_CODE = "12345";
        private string _authcode;
        private string _pendingDuuid;

        internal IstatResponder(Stat s, string dataBasePath, string authFileName, Clients clients)
        {
            _stat = s;
            _clients = clients;
            if (!Directory.Exists(dataBasePath))
                Directory.CreateDirectory(dataBasePath);
            


            _authfileFullPath = dataBasePath + @"\" + authFileName;
            if (!File.Exists(_authfileFullPath))
                File.WriteAllText(_authfileFullPath, DEFAULT_PIN_CODE);
            Authcode = File.ReadAllText(_authfileFullPath);
        }

        public string Authcode
        {
            get { return _authcode; }
            set { _authcode = value; }
        }


        /// <summary>
        /// Determines the appropriate action to take for different message types
        /// </summary>
        /// <param name="message">Message from the client</param>
        /// <param name="stream">Active network stream to client</param>
        internal void HandleMessage(string message, NetworkStream stream)
        {
            if (message.Length == 5 && !message.Contains("<isr>")) //authorization PIN code
            {
                if (_pendingDuuid == null)
                {
                    Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Error, "Error, received PIN without prompt: ","Error");
                }

                if (message == Authcode) //code correct
                {
                    _clients.AddClient(_pendingDuuid);
                    Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose, "New client added: " + _pendingDuuid, "Message");
                    const string toClient = HEADER + ACCEPT_CODE;
                    byte[] data = Encoding.UTF8.GetBytes(toClient);
                    _lastResponse = data;
                    Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose,
                                      "Server (accept authorization): " + toClient, "Message");
                    stream.Write(data, 0, data.Length);
                    return;
                }
                else //code rejected
                {
                    const string toClient = HEADER + REJECT_CODE;
                    byte[] data = Encoding.UTF8.GetBytes(toClient);
                    _lastResponse = data;
                    Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose, "Server (reject authorization): " + toClient, "Message");
                    stream.Write(data, 0, data.Length);
                    return;
                }
            }

            var doc = new XmlDocument();
            using (var sr = new StringReader(message))
            {
                try
                {
                    doc.Load(sr);
                }
                catch (Exception ex) 
                {
                    //if we have a bad message payload just resend the last good one to keep client talking
                    Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Error, string.Format("Error parsing xml message, error was {0}, Message was {1}", ex.Message, message), "Exception");
                    if (_lastResponse != null)
                    {
                        Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose, "Server (bad packet, resend):", "Message");
                        stream.Write(_lastResponse, 0, _lastResponse.Length);
                        return;
                    }
                }
                
            }
            
            XmlNode o = doc.GetElementsByTagName("isr")[0];
            XmlNode n = o.FirstChild;
            
            switch (n.Name)
            {
                case "h" :  //client initial connection
                    Authorize(doc, stream);
                    break;
                case "conntest" :  //keepalive
                    KeepAlive(stream);
                    break;
                case "rid" : //data request
                    ReturnData(o, stream, n.InnerText);
                    break;
                case "dtf" : //more information about hard disks
                    ReturnDiskInfo(stream);
                    break;
            }
        }

        /// <summary>
        /// Returns information about current fixed disks on the system
        /// </summary>
        /// <param name="stream">Open network stream to client</param>
        private  void ReturnDiskInfo(NetworkStream stream)
        {
            string value = _stat.DISKS.Aggregate(HEADER, (current, d) => current + string.Format("<isr  t=\"{0}\" n=\"{1}\"></isr>", d.Total, d.Name));
            byte[] data = Encoding.UTF8.GetBytes(value);
            _lastResponse = data;
            Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose, "Server(Disk Info): " + value, "Message");
            stream.Write(data, 0, data.Length);
        }


        /// <summary>
        /// Handles standard data update/history requests
        /// </summary>
        /// <param name="isr">Parent node (name = isr) of the response parameters</param>
        /// <param name="stream">Open network stream to client</param>
        /// <param name="rid">Request ID (sourced from client request message)</param>
        private void ReturnData(XmlNode isr, NetworkStream stream, string rid)
        {
            var data = new StringBuilder();
            data.Append(HEADER);
            data.Append(String.Format(SESSION, 0, 0, 0, rid));
            foreach (XmlNode n in isr.ChildNodes)
            {
                switch (n.Name)
                {
                    case "c": //cpu request
                        data.Append("<CPU>");
                        string cval = n.InnerText;
                        if (cval == "-1") // initial status
                        {
                            lock (_stat.CPU)
                            {
                                var s = _stat.CPU.Last();
                                data.Append(string.Format("<c id=\"{0}\" u=\"{1}\" s=\"{2}\" n=\"{3}\"></c>", "-1",
                                                          s.User, s.System, s.Nice));
                            }
                        }
                        else // normal request
                        {
                            int cup = int.Parse(cval);
                            lock (_stat.CPU)
                            {
                                foreach (var s in _stat.CPU.Where(c => c.Uptime > cup))
                                {
                                    data.Append(string.Format("<c id=\"{0}\" u=\"{1}\" s=\"{2}\" n=\"{3}\"></c>",
                                                              s.Uptime, s.User, s.System, s.Nice));
                                }
                            }
                        }
                        data.Append("</CPU>");
                        break;
                    case "n": //net request
                        data.Append("<NET>");
                        string nval = n.InnerText;
                        if (nval == "-1") // initial status
                        {
                            lock (_stat.NET)
                            {
                                var s = _stat.NET.Last();
                                data.Append(string.Format("<n id=\"{0}\" d=\"{1}\" u=\"{2}\" t=\"{3}\"></n>", "-1",
                                                          s.Download, s.Upload, s.UnixTime));
                            }
                        }
                        else // normal request
                        {
                            int cup = int.Parse(nval);
                            lock (_stat.NET)
                            {
                                foreach (var s in _stat.NET.Where(c => c.Uptime > cup))
                                {
                                    data.Append(string.Format("<n id=\"{0}\" d=\"{1}\" u=\"{2}\" t=\"{3}\"></n>",
                                                              s.Uptime, s.Download, s.Upload, s.UnixTime));
                                }
                            }
                        }
                        data.Append("</NET>");
                        break;
                    case "m": //memory request 
                        data.Append(
                            string.Format(
                                "<MEM w=\"{0}\" a=\"{1}\" i=\"{2}\" f=\"{3}\" t=\"{4}\" su=\"{5}\" st=\"{6}\" pi=\"{7}\" po=\"{8}\"></MEM>",
                                _stat.MEM.Wired, _stat.MEM.Active, _stat.MEM.Inactive, _stat.MEM.Free, _stat.MEM.Total,
                                _stat.MEM.SwapUsed, _stat.MEM.SwapTotal, _stat.MEM.PageInCount, _stat.MEM.PageOutCount));
                        break;
                    case "lo": //load
                        data.Append(string.Format("<LOAD one=\"{0}\" fv=\"{1}\" ff=\"{2}\"></LOAD>",
                                                  _stat.LOAD.OneMinuteAverage, _stat.LOAD.FiveMinuteAverage, _stat.LOAD.TenMinuteAverage));
                        break;
                    case "t": //temps
                        data.Append(string.Format("<TEMPS>"));
                        data.Append(_stat.TEMPS.Aggregate("", (current, d) => current + string.Format("<t n=\"{0}\" i=\"{1}\" t=\"{2}\"></t>", d.Name, d.Index, d.TemperatureC))); 
                        data.Append(string.Format("</TEMPS>"));
                        break;
                    case "f": //fans
                        data.Append(string.Format("<FANS>"));
                        data.Append(_stat.FANS.Aggregate("", (current, d) => current + string.Format("<f n=\"{0}\" i=\"{1}\" s=\"{2}\"></f>", d.Name, d.Index, d.RPM))); 
                        data.Append(string.Format("</FANS>"));
                        break;
                    case "u": //uptime
                        data.Append(string.Format("<UPT u=\"{0}\"></UPT>", _stat.CurrentUptime));
                        break;
                    case "d": //disks
                        data.Append(string.Format("<DISKS>") +
                                    _stat.DISKS.Aggregate("",
                                                          (current, d) =>
                                                          current +
                                                          string.Format(
                                                              "<d n=\"{0}\" uuid=\"{1}\" f=\"{2}\" p=\"{3}\"></d>",
                                                              d.Name, d.Uuid, d.Free, d.PercentUsed)) +
                                    string.Format("</DISKS>"));
                        break;

                }
            }
            data.Append("</isr>");
            byte[] bdata = Encoding.UTF8.GetBytes(data.ToString());
            _lastResponse = bdata;
            Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose,"Server (data request): " + data.ToString(), "Message");
            stream.Write(bdata, 0, data.Length);
        }


      
        /// <summary>
        /// Handles authorizing clients.  On first connect a server set pin code should be requested, and then validated.  Otherwise the duuid is stored and no token is requested.
        /// </summary>
        /// <param name="doc">Client XML message</param>
        /// <param name="stream">Open network stream to client</param>
        /// <returns>Indicates if the client was recognized (if not next response will be plaintext (non-xml) auth token)</returns>
        private bool Authorize(XmlDocument doc, NetworkStream stream)
        {
            XmlNodeList duuidNodes = doc.GetElementsByTagName("duuid");
            string duuid = duuidNodes[0].InnerText;
            
            if (!_clients.IsClientAuthenticated(duuid)) //new client
            {
                string aS = GetAuthorizeString(false, _stat.CurrentUptime);
                byte[] data = Encoding.UTF8.GetBytes(aS);
                _lastResponse = data;
                Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose, "Server (New Client): " + aS, "Message");
                stream.Write(data, 0, data.Length);
                _pendingDuuid = duuid;
                return true;
            }
            else
            {
                string aS = GetAuthorizeString(true, _stat.CurrentUptime);
                byte[] data = Encoding.UTF8.GetBytes(aS);
                _lastResponse = data;
                Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose, "Server (Client Recognized): " + aS, "Message");
                stream.Write(data, 0, data.Length);
                return true;
            }
        }

        /// <summary>
        /// Generates the authorize response string
        /// </summary>
        /// <param name="newConnection">Has the client been previously authenticated</param>
        /// <param name="uptime">Current uptime</param>
        /// <returns></returns>
       private  string GetAuthorizeString(bool newConnection, long uptime)
        {
            return HEADER + String.Format(AUTHORIZE, newConnection ? 0 : 1, _stat.CPU.Count(), uptime + 1, uptime);
        }


        /// <summary>
        /// Responds to keepalive requests
        /// </summary>
        /// <param name="stream">Open network stream to client</param>
        private  void KeepAlive(NetworkStream stream)
        {
            const string keepalive = HEADER + KEEP_ALIVE;
            byte[] data = Encoding.UTF8.GetBytes(keepalive);
            _lastResponse = data;
            Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose, "Server (keep alive): " + keepalive, "Message");
            stream.Write(data, 0, data.Length);
        }
    }
}
