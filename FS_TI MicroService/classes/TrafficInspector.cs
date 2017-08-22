using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Xml;
using TrafInsp;

namespace FS_TI_MicroService.classes
{
    class TrafficInspector
    {

        //Возвращает GUID пользователя траффик инспектора
        public string getGuidByFIO(string FIO)
        {

            ITrafInspAdmin ti = connect();
            if (ti == null) return null;
            string GUID = null;
            try
            {
                GUID = ti.ItemGUIDByName(APIListType.itUser, FIO);
            }
            catch (Exception) { }
            ti = null;
            return GUID;
        }

        //Получаем данных о трафике пользователя
        public Dictionary<string, object> getUserInfo(string GUID)
        {
            ITrafInspAdmin ti = connect();
            if (ti == null) return null;

            Dictionary<string, object> retValue = new Dictionary<string, object>();
            try
            {
                string value = ti.GetList(APIListType.itUser, GUID, null, ConfigAttrLevelType.conf_AttrLevelState);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(value);
                XmlAttributeCollection node = doc.ChildNodes.Item(1).FirstChild.Attributes;
                for (int i = 0; i < node.Count; i++)
                {
                    retValue.Add(node.Item(i).Name, node.Item(i).Value);
                }
            }
            catch (Exception) { }
            ti = null;
            return retValue;
        }

        //Выдача трафика пользователю
        public bool addCash(string[] GUID, int cash)
        {
            ITrafInspAdmin ti = connect();
            Dictionary<string, object> retValue = new Dictionary<string, object>();
            try
            {
                string s = String.Join(",", GUID);
                ti.UserAddCash(APIListType.itUser, s, cash, "Scripts");
            }
            catch (Exception)
            {
                ti = null;
                Console.WriteLine("Error add Cash");
                return false;
            }
            ti = null;
            return true;
        }

        private ITrafInspAdmin connect()
        {
            try
            {
                Type t = Type.GetTypeFromProgID("TrafInsp.TrafInspAdmin", "10.100.3.4");
                if (t == null)
                {
                    Console.WriteLine("Unable to find the COM type TrafInsp.TrafInspAdmin");
                    return null;
                };
                ITrafInspAdmin ti = (ITrafInspAdmin)Activator.CreateInstance(t);

                if (ti != null)
                {
                    Console.WriteLine("Connect TI succeed.");
                }
                else
                {
                    Console.WriteLine("Connect TI failed");
                }

                Permissions perm = ti.QueryPermissions();

                APIPermLogonErr v = perm.DoSharedLogon("ti", "qweqwe123", "C# robot");
                switch (v)
                {
                    case APIPermLogonErr.alsNone:
                        Console.WriteLine("success access");
                        break;
                    case APIPermLogonErr.alsSharedDis:
                        Console.WriteLine("Bad shared permission");
                        break;
                    case APIPermLogonErr.alsSharedBadPass:
                        Console.WriteLine("Bad shared password");
                        break;
                    case APIPermLogonErr.alsNTLMErr:
                        Console.WriteLine("NTLM error permission");
                        break;
                    case APIPermLogonErr.alsCheckWinErr:
                        Console.WriteLine("Win error permission");
                        break;
                    default:
                        break;
                }
                return ti;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Traffic Inspector: " + e.ToString());
            }
            return null;
        }
    }
}
