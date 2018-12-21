using FS_TI_MicroService.classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace FS_TI_MicroService
{
    static class Program
    {
        public static Settings cfg;
        public static HttpServer srv;
        public static String curDirectory;
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
            // init cur dir
            Program.curDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            Settings.cfgFile = Program.curDirectory + "\\config.json";
            // read config
            Program.cfg = Settings.Read();

            // try to run web server
            if (!runWebServer())
            {
               
            }
        }

        static bool runWebServer()
        {
            int port = Program.cfg.security.port;
            if (!(port > 0))
            {
          
                return false;
            }
            Program.srv = new MyHttpServer(17301);
            Thread thread = new Thread(new ThreadStart(Program.srv.listen));
            thread.Start();
            return true;
        }
    }
}
