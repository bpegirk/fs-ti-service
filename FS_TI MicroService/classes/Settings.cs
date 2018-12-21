using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FS_TI_MicroService.classes
{
    class Settings
    {        
        public static String cfgFile;
        public Security security = new Security();
        public FS fs = new FS();
        public TI ti = new TI();
        public FIS fis = new FIS();

        public static Settings Read()
        {
            if (!File.Exists(cfgFile))
            {
                Write(new Settings());
            }
            return JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText(cfgFile));
        }

        public static void Write(Settings config)
        {
            System.IO.File.WriteAllText(cfgFile, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }

    class FS
    {

        public string rootFolder = "\\\\10.100.3.3\\";
        public string studentsFolder = "students\\";
        public string teacherFolder = "teachers\\";
        public string teacherXFolder = "teachers\\TEACHERS_ALL\\";
        public string teacherTempFolder = "fs-data\\temp\\";
        public string domain = "IAT";
    }

    class TI
    {
        public string host = "10.100.3.4";
        public string login = "ti";
        public string password = "qweqwe123";
    }

    class Security
    {
        public string key = "d88e6391-dfb5-4fa4-826e-324b5a04da2f";
        public int port = 17301;
    }

    class FIS
    {
        public string host = "http://10.0.3.1:8080/import/importservice.svc";
    }
}
