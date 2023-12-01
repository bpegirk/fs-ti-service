using FsrmLib;
using System;
using System.IO;
using System.Security.AccessControl;

namespace FS_TI_MicroService.classes
{
    class FileSystem
    {
        static string disk;
        static string studentsFolder;
        static string teacherFolder;
        static string teacherXFolder;
        static string teacherTempFolder;
        static string domain;

        public FileSystem()
        {
            disk = Program.cfg.fs.rootFolder;
            if (disk.Substring(disk.Length - 1, 1) != "\\")
            {
                disk += "\\";
            }
            studentsFolder = disk + Program.cfg.fs.studentsFolder;
            teacherFolder = disk + Program.cfg.fs.teacherFolder;
            teacherXFolder = disk + Program.cfg.fs.teacherXFolder;
            teacherTempFolder = disk + Program.cfg.fs.teacherTempFolder;
            domain = Program.cfg.fs.domain;
        }

        public bool checkFIO(string fio)
        {
            string[] fioArr = fio.Split(' ');
            try
            {
                if (fioArr.Length < 3) return false;
                if (fioArr[0] == "") return false;
                if (fioArr[1] == "") return false;
                if (fioArr[2] == "") return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public string buildFamio(string fio)
        {
            string[] fioArr = fio.Split(' ');
            string famio = fioArr[0] + " " + fioArr[1][0] + "." + fioArr[2][0];
            return famio;
        }

        public bool studentFolderExist(string login)
        {
            string path = getPathStudentG(login);
            return Directory.Exists(path);
        }


        //Проверка на наличие папки сотрудника на диске G 
        public bool teacherFolderExist(string login)
        {
            string path = teacherFolder + login;
            return Directory.Exists(path);
        }

        //Проверка на наличие папки сотрудника на диске X 
        public bool teacherFolderXExist(string fio)
        {
            bool check = checkFIO(fio);
            if (check == false) return false;
            string famio = buildFamio(fio);
            string path = teacherXFolder + famio;
            return Directory.Exists(path);
        }

        //Проверка на наличие папки сотрудника на диске Temp 
        public bool teacherFolderTempExist(string fio)
        {
            bool check = checkFIO(fio);
            if (check == false) return false;
            string path = teacherTempFolder + fio;
            return Directory.Exists(path);
        }

        public string createTeacherFolderG(string login)
        {
            try
            {
                if (login == null || login.Length == 0) return "error login";

                string path = teacherFolder + login;
                Directory.CreateDirectory(path);
                setPermissionsFolder(login, path, true);
            }
            catch (Exception)
            {
                return "false";
            }
            return "true";
        }

        public string createTeacherFolderX(string fio)
        {
            try
            {
                bool check = checkFIO(fio);
                if (check == false) return "error fio";

                string famio = buildFamio(fio);
                string path = teacherXFolder + famio;
                Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                return "false";
            }
            return "true";
        }

        public string createTeacherFolderTemp(string login, string fio)
        {
            try
            {
                bool check = checkFIO(fio);
                if (check == false) return "error fio";
                string path = teacherTempFolder + fio;
                Directory.CreateDirectory(path);
                setPermissionsFolder(login, path, true);
            }
            catch (Exception)
            {
                return "error";
            }
            return "true";
        }

        public string createStudentFolder(string login)
        {
            try
            {
                string path = getPathStudentG(login);
                Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                return "error";
            }
            try
            {
                setPermissionsStudentFolder(login, true);
            }
            catch (Exception)
            {
                return "not login";
            }
            return "ok";
        }

        public AuthorizationRuleCollection getPermissionsStudentFolder(string login)
        {
            string path = getPathStudentG(login);
            DirectoryInfo dInfo = new DirectoryInfo(path);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            AuthorizationRuleCollection rules = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            foreach (FileSystemAccessRule rule in rules)
            {
                Console.WriteLine("Account:{0} Access type: {2} Right: {1} ", rule.IdentityReference.Value, rule.FileSystemRights,
                rule.AccessControlType);
            }
            return rules;
        }

        public AuthorizationRuleCollection setPermissionsStudentFolder(string login, bool access)
        {
            string path = getPathStudentG(login);
            DirectoryInfo dInfo = new DirectoryInfo(path);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            AuthorizationRuleCollection rules = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            //Задаём разрешения    
            FileSystemAccessRule newRule;
            //Удаляем все предущие разрешения
            dSecurity.RemoveAccessRule(getRuleAllow(login, AccessControlType.Allow | AccessControlType.Deny));

            if (access)
            {
                newRule = getRuleAllow(login, AccessControlType.Allow);

            }
            else
            {
                newRule = getRuleAllow(login, AccessControlType.Deny);
            }

            dSecurity.AddAccessRule(newRule);

            //Применяем разрешения
            Directory.SetAccessControl(path, dSecurity);
            return rules;
        }


        public AuthorizationRuleCollection setPermissionsFolder(string login, string path, bool access)
        {

            DirectoryInfo dInfo = new DirectoryInfo(path);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            AuthorizationRuleCollection rules = dSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            //Задаём разрешения    
            FileSystemAccessRule newRule;
            //Удаляем все предущие разрешения
            dSecurity.RemoveAccessRule(getRuleAllow(login, AccessControlType.Allow | AccessControlType.Deny));

            if (access)
            {
                newRule = getRuleAllow(login, AccessControlType.Allow);

            }
            else
            {
                newRule = getRuleAllow(login, AccessControlType.Deny);
            }

            dSecurity.AddAccessRule(newRule);

            //Применяем разрешения
            Directory.SetAccessControl(path, dSecurity);
            return rules;
        }

        public static string getPathStudentG(string login)
        {
            string year = "20" + login.Substring(0, 2);
            string path = studentsFolder + year + "\\" + login;
            return path;
        }


        private static FileSystemAccessRule getRuleAllow(string login, AccessControlType access)
        {
            FileSystemAccessRule newRule = new FileSystemAccessRule(domain + "\\" + login,
            FileSystemRights.Modify | FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory | FileSystemRights.Read | FileSystemRights.Write,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, access);
            return newRule;

        }


        public decimal getQuota(String path)
        {

            FsrmLib.FsrmQuotaManager qmg = new FsrmLib.FsrmQuotaManager();
            IFsrmQuota q = qmg.GetQuota(path);

            return q.QuotaLimit;
        }


        public decimal getUsedQuota(String path)
        {
            FsrmLib.FsrmQuotaManager qmg = new FsrmLib.FsrmQuotaManager();
            IFsrmQuota q = qmg.GetQuota(path);

            return q.QuotaLimit > 0 ? (int)Math.Round(q.QuotaUsed / q.QuotaLimit * 100, 0) : 0;
        }

        public void setQuota(String path, int limit)
        {
            FsrmLib.FsrmQuotaManager qmg = new FsrmLib.FsrmQuotaManager();
            IFsrmQuota q = qmg.GetQuota(path);
            q.QuotaLimit = limit;
            q.Commit();

        }

    }
}
