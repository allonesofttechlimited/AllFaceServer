using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BioWebServer.Classes
{
    public class CreateLogFiles
    {
        private string sLogFormat;
        private string sErrorDate;
        private string sErrorTime;

        public CreateLogFiles()
        {
            //sLogFormat used to create log files format :
            // dd/mm/yyyy hh:mm:ss AM/PM ==> Log Message
            sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";

            //this variable used to create log filename format "
            //for example filename : ErrorLogYYYYMMDD
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            sErrorDate = sYear + sMonth + sDay;

            sErrorTime = DateTime.Now.Hour.ToString() + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond;
        }
        public string CreateFolder(string sPathName)
        {
            string folderName = sPathName + sErrorDate;
            if (!Directory.Exists(folderName))
            {
                System.IO.Directory.CreateDirectory(folderName);
            }

            return folderName;
        }
        public void ErrorLog(string sPathName, string sErrMsg)
        {
            string folderName = CreateFolder(sPathName);
            StreamWriter sw = new StreamWriter(folderName + "\\" + sErrorTime + ".txt", true);
            sw.WriteLine(sLogFormat + sErrMsg);
            sw.Flush();
            sw.Close();
        }
    }
}
