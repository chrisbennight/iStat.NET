using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace istatServer
{
    /// <summary>
    /// Main server class - catched TCP requests
    /// </summary>
    public class IstatServer : IDisposable
    {

        private readonly TcpListener _istat;
        private const int ISTAT_PORT = 5109;
        private readonly Stat _stat;
        private readonly IstatResponder _responder;
        private bool _listen = true;
        private readonly BackgroundWorker _bw = new BackgroundWorker();
        private readonly NotifyIcon _trayNotify;
        private readonly ContextMenu _trayContextMenu = new ContextMenu();
        private readonly Settings _settingsForm;
        private const string CLIENTS_SUBDIRECTORY = @"\iStatNet";
        private const string AUTH_FILE_NAME = "pincode.txt";
        private const string CLIENTS_FILE_NAME = "clients.xml";
        private readonly Clients _clients;
     




        public IstatServer()
        {
            string dataBasePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + CLIENTS_SUBDIRECTORY;
            _stat = new Stat();
            _settingsForm = new Settings(dataBasePath + @"\" + AUTH_FILE_NAME, dataBasePath + @"\" + CLIENTS_FILE_NAME);
            _clients = new Clients(dataBasePath, CLIENTS_FILE_NAME);
            _responder = new IstatResponder(_stat, dataBasePath, AUTH_FILE_NAME, _clients);
            _trayNotify = new NotifyIcon
            {
                Text = "iStatServer.NET",
                Icon = new Icon(SystemIcons.Application, 40, 40),
                ContextMenu = _trayContextMenu,
                Visible = true
            };

            _trayContextMenu.MenuItems.Add("Settings", (sender, e) =>
                                                           {
                                                               _settingsForm.Location =
                                                                   new Point(Cursor.Position.X - _settingsForm.Width,
                                                                             Cursor.Position.Y - _settingsForm.Height);
                                                               _settingsForm.StartPosition = FormStartPosition.Manual;
                                                               _settingsForm.TopMost = true;
                                                               _settingsForm.Show();
                                                           });
            _trayContextMenu.MenuItems.Add("Exit", (sender, e) =>
                                                       {
                                                           _trayNotify.Visible = false;
                                                           Stop();
                                                           Application.Exit();
                                                       });
            
            
            _settingsForm.AuthCodeChanged += (sender, e) =>
                                                 {
                                                     _responder.Authcode = e.AuthCode;
                                                 };
            _settingsForm.AuthReset += (sender, e) => _clients.ResetAuthorizations();


            _istat = new TcpListener(IPAddress.Any, ISTAT_PORT);
            Start();

        }

     

        private void ListenerDoWork(object sender, DoWorkEventArgs e)
        {
            _istat.Start();
            while (_listen)
            {
                if (!_istat.Pending())
                {
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
                using (TcpClient client = _istat.AcceptTcpClient())
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        while (true)
                        {
                            var buffer = new byte[4096];
                            int bytesRead;
                            if ((bytesRead = stream.Read(buffer, 0, 4096)) == 0)
                                break;
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Verbose,"Client: " + message, "Message");
                            try
                            {
                                _responder.HandleMessage(message, stream); 
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLineIf(Program.LogLevel.Level >= TraceLevel.Error,"Exception in message handler: " + ex.Message, "Exception");
                            }

                        }
                    }

                }
            }
        }

        public void Start()
        {
            _listen = true;
            _bw.DoWork += ListenerDoWork;
            _bw.RunWorkerAsync();
        }

        public void Stop()
        {
            _listen = false;
        }

        public void Dispose()
        {
            Stop();
            _trayNotify.Dispose();
            _istat.Stop();
            _stat.Dispose();
        }

    }
}
