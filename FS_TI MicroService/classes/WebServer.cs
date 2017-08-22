using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Threading;
using System.Web;

namespace FS_TI_MicroService.classes
{

    public class HttpProcessor
    {
        public TcpClient socket;
        public HttpServer srv;

        private Stream inputStream;
        public StreamWriter outputStream;
        public static StreamWriter log;

        public String http_method;
        public String http_url;
        public String http_protocol_versionstring;
        public Hashtable httpHeaders = new Hashtable();


        private static int MAX_POST_SIZE = 10 * 1024; // 1MB

        public HttpProcessor(TcpClient s, HttpServer srv)
        {
            this.socket = s;
            this.srv = srv;
            if (log == null)
            {
                log = new StreamWriter(@Program.curDirectory + "\\log.txt");
            }
        }


        private string streamReadLine(Stream inputStream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = inputStream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }

        public void process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            inputStream = new BufferedStream(socket.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));
            try
            {
                parseRequest();
                readHeaders();
                if (http_method.Equals("GET"))
                {
                    handleGETRequest();//Получение данных
                }
                else if (http_method.Equals("POST"))
                {
                    handlePOSTRequest();//Создание данных
                }
                else if (http_method.Equals("PUT"))
                {
                    handlePUTRequest(); //Обновление данных
                }
            }
            catch (Exception e)
            {
                log.WriteLine("Exception: " + e.ToString());
                writeFailure();
            }
            outputStream.Flush();
            // bs.Flush(); // flush any remaining output
            inputStream = null; outputStream = null; // bs = null;            
            socket.Close();
        }

        public void parseRequest()
        {
            String request = streamReadLine(inputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }
            http_method = tokens[0].ToUpper();
            http_url = tokens[1];
            http_protocol_versionstring = tokens[2];

            log.WriteLine("starting: " + request);
        }

        public void readHeaders()
        {
            log.WriteLine("readHeaders()");
            String line;
            while ((line = streamReadLine(inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    log.WriteLine("got headers");
                    return;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string value = line.Substring(pos, line.Length - pos);
                log.WriteLine("header: {0}:{1}", name, value);
                httpHeaders[name] = value;
            }
        }

        public void handleGETRequest()
        {
            srv.handleGETRequest(this);
        }

        private const int BUF_SIZE = 4096;
        public void handlePOSTRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            log.WriteLine("get post data start");
            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.httpHeaders.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(
                        String.Format("POST Content-Length({0}) too big for this simple server", content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    log.WriteLine("starting Read, to_read={0}", to_read);

                    int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    log.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            log.WriteLine("get post data end");
            srv.handlePOSTRequest(this, new StreamReader(ms));

        }

        public void handlePUTRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            log.WriteLine("get post data start");
            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.httpHeaders.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(
                        String.Format("PUT Content-Length({0}) too big for this simple server", content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    log.WriteLine("starting Read, to_read={0}", to_read);

                    int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    log.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            log.WriteLine("get post data end");
            srv.handlePUTRequest(this, new StreamReader(ms));

        }

        public void writeSuccess()
        {
            outputStream.Write("HTTP/1.0 200 OK\n");
            outputStream.Write("Content-Type: text/html\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");
        }

        public void writeSuccess(String str)
        {
            outputStream.Write("HTTP/1.0 200 OK\n");
            outputStream.Write("Content-Type: text/html\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");
            outputStream.Write(str);
        }


        public void writeFailure()
        {
            outputStream.Write("HTTP/1.0 404 File not found\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");
            outputStream.WriteLine("{\"success\":false, \"error\":{\"code\":500,\"message\":\"\"}}");
        }

        public void writeFailure(string param)
        {
            outputStream.Write("HTTP/1.0 404 File not found\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");
            outputStream.WriteLine("{\"success\": false , \"error\":{\"code\":500,\"message\":" + param + "}}");
        }
    }

    public abstract class HttpServer
    {

        protected int port;
        TcpListener listener;
        bool is_active = true;

        public HttpServer(int port)
        {
            this.port = port;
        }

        public void listen()
        {
            listener = new TcpListener(port);
            listener.Start();
            while (is_active)
            {
                TcpClient s = listener.AcceptTcpClient();
                HttpProcessor processor = new HttpProcessor(s, this);
                Thread thread = new Thread(new ThreadStart(processor.process));
                thread.Start();
                Thread.Sleep(1);
            }
        }

        public abstract void handleGETRequest(HttpProcessor p);
        public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
        public abstract void handlePUTRequest(HttpProcessor p, StreamReader inputData);
    }

    public class MyHttpServer : HttpServer
    {
        const string API_KEY = "d88e6391-dfb5-4fa4-826e-324b5a04da2f";
        FileSystem fs = new FileSystem();
        TrafficInspector ti = new TrafficInspector();

        public MyHttpServer(int port)
            : base(port)
        {
        }
        public override void handleGETRequest(HttpProcessor p)
        {
            string url = p.http_url;
            HttpProcessor.log.WriteLine("request: {0}", url);

            Uri myUri = new Uri("Http://localhost:8080" + url);

            string hostpart = myUri.Authority;
            string[] pathsegments = myUri.Segments;
            string querystring = myUri.Query;


            if (querystring == null || querystring.Length <= 0)
            {
                p.writeFailure("Not params");
                return;
            }
            string key = HttpUtility.ParseQueryString(querystring).Get("key");
            if (key != API_KEY)
            {
                p.writeFailure("Access denied");
                return;
            }

            p.writeSuccess();
            if (pathsegments[1].ToLower().Replace("/", "") == "student")
            {
                if (pathsegments[2].ToLower().Replace("/", "") == "folder_permissions")
                {
                    get_student_folder_permissions(querystring, p);
                }
                else if (pathsegments[2].ToLower().Replace("/", "") == "traffic")
                {
                    get_student_traffic(querystring, p);
                }
                else if (pathsegments[2].ToLower().Replace("/", "") == "folder")
                {
                    get_student_folder(querystring, p);
                }
            }
            else if (pathsegments[1].ToLower().Replace("/", "") == "teacher")
            {
                if (pathsegments[2].ToLower().Replace("/", "") == "traffic")
                {
                    get_teacher_traffic(querystring, p);
                }
                else if (pathsegments[2].ToLower().Replace("/", "") == "folder")
                {
                    get_employee_folder(querystring, p);
                }
            }
            else if (pathsegments[1].ToLower().Replace("/", "") == "help")
            {
                p.outputStream.WriteLine("<h2>Example usage:<h2>");

                //*******************GET********************//

                p.outputStream.WriteLine("<h4>Folder permissions student:</h4>");
                p.outputStream.WriteLine("GET: http://site/student/folder_permissions?logins=login,login,login&key=? ");

                p.outputStream.WriteLine("<h4>Folder exist student:</h4>");
                p.outputStream.WriteLine("GET: http://site/student/folder?logins=login,login,login&key=? ");

                p.outputStream.WriteLine("<h4>Folder exist teacher:</h4>");
                p.outputStream.WriteLine("GET: http://site/teacher/folder?logins=login,login&fio=FIO,FIO,FIO&key=? ");

                p.outputStream.WriteLine("<h4>Traffic info student:</h4>");
                p.outputStream.WriteLine("GET: http://site/student/traffic?fio=FIO,FIO,FIO&key=? ");

                p.outputStream.WriteLine("<h4>Traffic info teacher:</h4>");
                p.outputStream.WriteLine("GET: http://site/teacher/traffic?fio=FIO,FIO,FIO&key=? ");

                //*******************POST********************//

                p.outputStream.WriteLine("<h4>Create folder student:</h4>");
                p.outputStream.WriteLine("POST: http://site/student/folder?logins=login,login,login&key=?");

                p.outputStream.WriteLine("<h4>Create folder student:</h4>");
                p.outputStream.WriteLine("POST: http://site/student/folder?logins=login,login,login&key=?");

                //********************PUT*******************//

                p.outputStream.WriteLine("<h4>Update traffic student:</h4>");
                p.outputStream.WriteLine("PUT: http://site/student/taffic?cash=100&fio=FIO,FIO,FIO&cash=?&key=?");

                p.outputStream.WriteLine("<h4>Update traffic teacher:</h4>");
                p.outputStream.WriteLine("PUT: http://site/teacher/taffic?cash=100&fio=FIO,FIO,FIO&cash=?&key=?");
            }
        }
        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            HttpProcessor.log.WriteLine("POST request: {0}", p.http_url);
            string param = inputData.ReadToEnd();
            if (param == null || param.Length == 0)
            {
                p.writeFailure("Error query");
                return;
            }
            string key = HttpUtility.ParseQueryString(param).Get("key");
            if (key != API_KEY)
            {

                p.writeFailure("Access denied");
                return;
            }

            Uri myUri = new Uri("Http://localhost:80" + p.http_url);

            string hostpart = myUri.Authority;
            string[] pathsegments = myUri.Segments;

            p.writeSuccess();
            if (pathsegments[1].ToLower().Replace("/", "") == "student")
            {
                if (pathsegments[2].ToLower().Replace("/", "") == "folder")
                {
                    post_student_folder(param, p);
                }
            }
            else if (pathsegments[1].ToLower().Replace("/", "") == "teacher")
            {
                if (pathsegments[2].ToLower().Replace("/", "") == "folder")
                {
                    post_teacher_folder(param, p);
                }
                else if (pathsegments[2].ToLower().Replace("/", "") == "folder_g")
                {
                    post_teacher_folder_g(param, p);
                }
                else if (pathsegments[2].ToLower().Replace("/", "") == "folder_x")
                {
                    post_teacher_folder_x(param, p);
                }
                else if (pathsegments[2].ToLower().Replace("/", "") == "folder_t")
                {
                    post_teacher_folder_t(param, p);
                }
            }
        }

        public override void handlePUTRequest(HttpProcessor p, StreamReader inputData)
        {
            HttpProcessor.log.WriteLine("PUT request: {0}", p.http_url);
            string param = inputData.ReadToEnd();
            if (param == null || param.Length == 0)
            {
                p.writeFailure("Error query");
                return;
            }
            string key = HttpUtility.ParseQueryString(param).Get("key");
            if (key != API_KEY)
            {
                p.writeFailure("Access denied");
                return;
            }
            Uri myUri = new Uri("Http://localhost:80" + p.http_url);

            string hostpart = myUri.Authority;
            string[] pathsegments = myUri.Segments;

            p.writeSuccess();
            if (pathsegments[1].ToLower().Replace("/", "") == "student")
            {
                if (pathsegments[2].ToLower().Replace("/", "") == "traffic")
                {
                    put_student_traffic(param, p);
                }
            }
            else if (pathsegments[1].ToLower().Replace("/", "") == "teacher")
            {
                if (pathsegments[2].ToLower().Replace("/", "") == "traffic")
                {
                    put_teacher_traffic(param, p);
                }
            }
        }

        private void get_student_folder_permissions(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");

            if (logins == null)
            {
                p.writeFailure("Error params");
                return;
            }

            string[] usersLogins = logins.Split(',');

            Dictionary<string, object> usersRole = new Dictionary<string, object>();
            foreach (string login in usersLogins)
            {
                Dictionary<int, object> ruleList = new Dictionary<int, object>();
                try
                {
                    AuthorizationRuleCollection rules = fs.getPermissionsStudentFolder(login);

                    int i = 0;
                    foreach (FileSystemAccessRule rule in rules)
                    {
                        Dictionary<string, object> paramRule = new Dictionary<string, object>();
                        paramRule.Add("Account", rule.IdentityReference.Value);
                        paramRule.Add("Access", rule.AccessControlType.ToString());
                        paramRule.Add("Rights", rule.FileSystemRights.ToString());
                        ruleList.Add(i++, paramRule);
                    }
                }
                catch (Exception ex)
                {
                    HttpProcessor.log.WriteLine(ex.Message);
                    ruleList = null;
                }

                usersRole.Add(login, ruleList);
            }
            var serializer = JsonConvert.SerializeObject(usersRole);
            p.outputStream.Write(serializer);
        }

        private void get_student_folder(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");

            if (logins == null) { p.outputStream.WriteLine("Error params"); return; }

            string[] usersLogins = logins.Split(',');

            Dictionary<string, string> folders = new Dictionary<string, string>();
            foreach (string login in usersLogins)
            {
                bool exist = fs.studentFolderExist(login);
                folders.Add(login, exist.ToString());
            }
            var serializer = JsonConvert.SerializeObject(folders);
            p.outputStream.WriteLine(serializer);
        }

        private void get_employee_folder(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");
            string fio = HttpUtility.ParseQueryString(paramValue).Get("fio");

            if (logins == null) { p.outputStream.WriteLine("Error params"); return; }

            string[] loginsArr = logins.Split(',');
            string[] fioArr = fio.Split(',');

            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < loginsArr.Count(); i++)
            {
                Dictionary<string, string> status = new Dictionary<string, string>();
                bool folder_g = fs.teacherFolderExist(loginsArr[i]);
                status.Add("exist_folder_G", folder_g.ToString());
                string folder_x = "";
                string folder_Temp = "";
                if (fs.checkFIO(fioArr[i]) == true)
                {
                    folder_x = fs.teacherFolderXExist(fioArr[i]).ToString();
                    folder_Temp = fs.teacherFolderTempExist(fioArr[i]).ToString();
                }
                else
                {
                    folder_x = "error fio";
                    folder_Temp = "error fio";
                }

                status.Add("exist_folder_X", folder_x.ToString());
                status.Add("exist_folder_Temp", folder_Temp.ToString());
                result.Add(loginsArr[i], status);
            }
            var serializer = JsonConvert.SerializeObject(result);
            p.outputStream.WriteLine(serializer);
        }

        private void get_student_traffic(string paramValue, HttpProcessor p)
        {
            string fio_param = HttpUtility.ParseQueryString(paramValue).Get("fio");
            if (fio_param == null) { p.outputStream.WriteLine("Error params"); return; }

            string[] fios = fio_param.Split(',');
            Dictionary<string, object> usersTraffic = new Dictionary<string, object>();
            foreach (string fio in fios)
            {
                string GUID = ti.getGuidByFIO(fio);
                var data = ti.getUserInfo(GUID);
                usersTraffic.Add(fio, data);
            }
            var serializer = JsonConvert.SerializeObject(usersTraffic);
            p.outputStream.WriteLine(serializer);
        }


        private void get_teacher_traffic(string paramValue, HttpProcessor p)
        {
            string fio_param = HttpUtility.ParseQueryString(paramValue).Get("fio");
            if (fio_param == null) { p.outputStream.WriteLine("Error params"); return; }

            string[] fios = fio_param.Split(',');
            Dictionary<string, object> usersTraffic = new Dictionary<string, object>();
            foreach (string fio in fios)
            {
                string GUID = ti.getGuidByFIO(fio);
                var data = ti.getUserInfo(GUID);
                usersTraffic.Add(fio, data);
            }
            var serializer = JsonConvert.SerializeObject(usersTraffic);
            p.outputStream.WriteLine(serializer);
        }

        private void put_student_traffic(string paramValue, HttpProcessor p)
        {
            string fio_param = HttpUtility.ParseQueryString(paramValue).Get("fio");

            int cash = 0;
            bool succes = int.TryParse(HttpUtility.ParseQueryString(paramValue).Get("cash"), out cash);
            if (!succes)
            {
                p.outputStream.WriteLine("Error param CASH");
                return;
            }

            string[] fios = fio_param.Split(',');
            List<string> GUIDs = new List<string>();

            foreach (string fio in fios)
            {
                var guid = ti.getGuidByFIO(fio);
                if (guid != null)
                {
                    GUIDs.Add(guid);
                }
            }
            List<object> result = new List<object>();
            if (GUIDs.Count > 0)
            {
                bool success = ti.addCash(GUIDs.ToArray(), cash);

                foreach (var guid in GUIDs)
                {
                    object user = ti.getUserInfo(guid);
                    result.Add(user);
                }
                var serializer = JsonConvert.SerializeObject(result);
                p.outputStream.WriteLine(serializer);
            }
            else
            {
                p.outputStream.Write("{\"status\":\"false\"");
            }

        }


        private void put_teacher_traffic(string paramValue, HttpProcessor p)
        {
            string fio_param = HttpUtility.ParseQueryString(paramValue).Get("fio");

            int cash = 0;
            bool succes = int.TryParse(HttpUtility.ParseQueryString(paramValue).Get("cash"), out cash);
            if (!succes)
            {
                p.outputStream.WriteLine("Error param CASH");
                return;
            }

            string[] fios = fio_param.Split(',');
            List<string> GUIDs = new List<string>();

            foreach (string fio in fios)
            {
                GUIDs.Add(ti.getGuidByFIO(fio));
            }
            List<object> result = new List<object>();
            if (GUIDs.Count > 0)
            {
                bool success = ti.addCash(GUIDs.ToArray(), cash);

                foreach (var guid in GUIDs)
                {
                    object user = ti.getUserInfo(guid);
                    result.Add(user);
                }
                var serializer = JsonConvert.SerializeObject(result);
                p.outputStream.WriteLine(serializer);
            }
            else
            {
                p.outputStream.Write("{\"status\":false}");
            }

        }

        private void post_student_folder(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");

            if (logins == null)
            {
                p.outputStream.WriteLine("Error params");
                return;
            }

            string[] loginsArr = logins.Split(',');


            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < loginsArr.Count(); i++)
            {
                Dictionary<string, string> folder = new Dictionary<string, string>();
                folder.Add("folder", loginsArr[i]);
                if (!fs.studentFolderExist(loginsArr[i]))
                {
                    string stat = fs.createStudentFolder(loginsArr[i]);
                    folder.Add("status", stat.ToString());
                }
                else
                {
                    folder.Add("status", "exist");
                }
                result.Add(loginsArr[i], folder);
            }
            var serializer = JsonConvert.SerializeObject(result);
            p.outputStream.WriteLine(serializer);
        }


        private void post_teacher_folder(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");
            string fio = HttpUtility.ParseQueryString(paramValue).Get("fio");

            if (logins == null || fio == null)
            {
                p.outputStream.WriteLine("Error params");
                return;
            }

            string[] fioArr = fio.Split(',');
            string[] loginsArr = logins.Split(',');
            //p.writeSuccess();

            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < loginsArr.Count(); i++)
            {
                Dictionary<string, string> status = new Dictionary<string, string>();
                if (!fs.teacherFolderExist(loginsArr[i]))
                {
                    string statFolderG = fs.createTeacherFolderG(loginsArr[i]);
                    status.Add("folder_G", statFolderG.ToString());
                }
                else
                {
                    status.Add("folder_G", "exist");
                }

                if (!fs.teacherFolderXExist(fioArr[i]))
                {
                    string statFolderX = fs.createTeacherFolderX(fioArr[i]);
                    status.Add("folder_X", statFolderX);
                }
                else
                {
                    status.Add("folder_X", "exist");
                }

                if (!fs.teacherFolderTempExist(fioArr[i]))
                {
                    string statFolderTemp = fs.createTeacherFolderTemp(loginsArr[i], fioArr[i]);
                    status.Add("folder_Temp", statFolderTemp);
                }
                else
                {
                    status.Add("folder_Temp", "exist");
                }
                result.Add(loginsArr[i], status);
            }

            var serializer = JsonConvert.SerializeObject(result);
            p.outputStream.WriteLine(serializer);
        }


        private void post_teacher_folder_g(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");
            string fio = HttpUtility.ParseQueryString(paramValue).Get("fio");

            if (logins == null || fio == null)
            {
                p.outputStream.WriteLine("Error params");
                return;
            }

            string[] fioArr = fio.Split(',');
            string[] loginsArr = logins.Split(',');
            //p.writeSuccess();

            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < loginsArr.Count(); i++)
            {
                Dictionary<string, string> status = new Dictionary<string, string>();
                if (!fs.teacherFolderExist(loginsArr[i]))
                {
                    string statFolderG = fs.createTeacherFolderG(loginsArr[i]);
                    status.Add("folder_G", statFolderG.ToString());
                }
                else
                {
                    status.Add("folder_G", "exist");
                }
                result.Add(loginsArr[i], status);
            }

            var serializer = JsonConvert.SerializeObject(result);
            p.outputStream.WriteLine(serializer);
        }


        private void post_teacher_folder_x(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");
            string fio = HttpUtility.ParseQueryString(paramValue).Get("fio");

            if (logins == null || fio == null)
            {
                p.outputStream.WriteLine("Error params");
                return;
            }

            string[] fioArr = fio.Split(',');
            string[] loginsArr = logins.Split(',');
            //p.writeSuccess();

            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < loginsArr.Count(); i++)
            {
                Dictionary<string, string> status = new Dictionary<string, string>();

                if (!fs.teacherFolderXExist(fioArr[i]))
                {
                    string statFolderX = fs.createTeacherFolderX(fioArr[i]);
                    status.Add("folder_X", statFolderX);
                }
                else
                {
                    status.Add("folder_X", "exist");
                }
                result.Add(loginsArr[i], status);
            }

            var serializer = JsonConvert.SerializeObject(result);
            p.outputStream.WriteLine(serializer);
        }

        private void post_teacher_folder_t(string paramValue, HttpProcessor p)
        {
            string logins = HttpUtility.ParseQueryString(paramValue).Get("logins");
            string fio = HttpUtility.ParseQueryString(paramValue).Get("fio");

            if (logins == null || fio == null)
            {
                p.outputStream.WriteLine("Error params");
                return;
            }

            string[] fioArr = fio.Split(',');
            string[] loginsArr = logins.Split(',');
            //p.writeSuccess();

            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < loginsArr.Count(); i++)
            {
                Dictionary<string, string> status = new Dictionary<string, string>();

                if (!fs.teacherFolderTempExist(fioArr[i]))
                {
                    string statFolderTemp = fs.createTeacherFolderTemp(loginsArr[i], fioArr[i]);
                    status.Add("folder_Temp", statFolderTemp);
                }
                else
                {
                    status.Add("folder_Temp", "exist");
                }
                result.Add(loginsArr[i], status);
            }

            var serializer = JsonConvert.SerializeObject(result);
            p.outputStream.WriteLine(serializer);
        }
    }
}
