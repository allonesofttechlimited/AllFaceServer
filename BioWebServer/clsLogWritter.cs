using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BioWebServer
{
    public class clsLogWritter
    {
        string strSSL_Req_Log_Path;
        string strPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

        public clsLogWritter()
        {
            strPath = strPath.Substring(6);
        }
        public void CheckDirectory()
        {
            string strNow = DateTime.Now.ToString();
            strNow = strNow.Replace("/", "_");
            strNow = strNow.Replace(":", "");
            strNow = strNow.Replace(" ", "_");

            strNow = strNow.Substring(0, strNow.Length - 7) + strNow.Substring(strNow.Length - 2);

            if (!Directory.Exists(strPath + "\\Logs\\" + strNow))
            {
                DirectoryInfo di = Directory.CreateDirectory(strPath + "\\Logs\\" + strNow);
            }

            strSSL_Req_Log_Path = strPath + "\\Logs\\" + strNow + "\\SSL_REQ_LOG_" + strNow + ".txt";
        }

        public void WriteSSL_Req_LOG(string strLog)
        {
            CheckDirectory();
            StreamWriter sw = new StreamWriter(strSSL_Req_Log_Path, true);  // Create a new stream to append to the file              
            sw.Write(strLog);  // Write the log in to the file
            sw.Close(); // Close StreamWriter
        }
    }
}
