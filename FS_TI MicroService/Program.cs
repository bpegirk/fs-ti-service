using FS_TI_MicroService.classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

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
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceMain()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
