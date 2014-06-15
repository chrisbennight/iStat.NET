using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace istatServer
{
    /// <summary>
    /// Persists a list of authenticated clients
    /// </summary>
    public class Clients
    {
        private readonly string _clientsFileFullPath;
       
        private readonly List<string> _clients = new List<string>();

        public Clients(string dataBasePath, string clientsFileName)
        {
            _clientsFileFullPath = dataBasePath +  @"\" + clientsFileName;
            if (File.Exists(_clientsFileFullPath))
            {
                var ser = new XmlSerializer(typeof(List<string>));
                try
                {
                    using (TextReader tr = File.OpenText(_clientsFileFullPath))
                    {
                        _clients = (List<String>)ser.Deserialize(tr);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Error, string.Format("Corrupted clients file at: {0}.  Error was: {1}", _clientsFileFullPath, ex.Message), "Exception");
                    File.Delete(_clientsFileFullPath);
                }
            }
        }


        /// <summary>
        /// Adds a new client after they have been authenticated.  Persists to disk.
        /// </summary>
        /// <param name="duuid">duuid of client</param>
        public void AddClient(string duuid)
        {
            lock (_clients)
            {
                if (!_clients.Contains(duuid))
                {
                    _clients.Add(duuid);
                    var ser = new XmlSerializer(typeof (List<string>));
                    if (File.Exists(_clientsFileFullPath))
                        File.Delete(_clientsFileFullPath);
                    using (FileStream fs = File.OpenWrite(_clientsFileFullPath))
                    {
                        using (var sw = new StreamWriter(fs, Encoding.UTF8))
                        {
                            ser.Serialize(sw, _clients);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the clients has been previously authenticated
        /// </summary>
        /// <param name="duuid">duuid of client</param>
        /// <returns>true if authenticated, false if not</returns>
        public bool IsClientAuthenticated(string duuid)
        {
            lock (_clients)
            {
                return _clients.Contains(duuid);
            }
        }
        
        /// <summary>
        /// Removes all existing client associations
        /// </summary>
        public void ResetAuthorizations()
        {
            _clients.Clear();
        }
    }
}
