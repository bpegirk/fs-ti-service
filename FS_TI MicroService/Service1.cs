using System.Diagnostics;
using System.ServiceProcess;
using FS_TI_MicroService.classes;
using Newtonsoft.Json;
using System.Threading;

namespace FS_TI_MicroService
{
    public partial class ServiceMain : ServiceBase
    {
        public ServiceMain()
        {
            InitializeComponent();            
            InitLog();
        }

        protected override void OnStart(string[] args)
        {
            
            // init cur dir
            Program.curDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            Settings.cfgFile = Program.curDirectory + "\\config.json";
            // read config
            Program.cfg = Settings.Read();
            
            // try to run web server
            if (!runWebServer())
            {
                Stop();
            }

            AddLog("Service Started.");
        }

        protected override void OnStop()
        {
            AddLog("Service Stopped.");
        }

        private void InitLog()
        {
            try
            {
                if (!EventLog.SourceExists(srvName))
                {
                    EventLog.CreateEventSource(srvName, srvName);
                }
            }
            catch { }

        }

        public void AddLog(string log)
        {
            try
            {
                EventLog evtLog = new EventLog();
                evtLog.Source = srvName;
                evtLog.WriteEntry(log);
            }
            catch { }
        }


        public bool runWebServer()
        {
            int port = Program.cfg.security.port;
            if (!(port>0))
            {
                AddLog("Need port for web server in config");
                return false;
            }
            Program.srv = new MyHttpServer(17301);
            Thread thread = new Thread(new ThreadStart(Program.srv.listen));
            thread.Start();
            return true;
        }

    }
}