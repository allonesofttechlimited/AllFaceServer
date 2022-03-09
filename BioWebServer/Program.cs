using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BioWebServer
{
    static class Program
    {
        /// <summary>
       
        /// </summary>
        [STAThread]
        static void Main()
        {

            string strError;
            MiT_License.clsDBConnectionReadWrite objLicense = new MiT_License.clsDBConnectionReadWrite();
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //if (objLicense.CheckLicense().Equals(""))
                //{
                    bool createNew;
                    using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, Application.ProductName, out createNew))
                    {
                        if (createNew)
                        {
                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);
                            Application.Run(new HALWebServer());
                        }
                        else
                        {
                            Application.Exit();
                        }
                    }
                //}
                //else
                //{
                //    Application.Run(new MIT_MGW.Forms.frmLicenseActivate(""));
                //}
            }
            catch (Exception ex)
            {
                strError = ex.Message.ToString();
            }


            //bool createNew;            
            //using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, Application.ProductName, out createNew))
            //{
            //    if (createNew)
            //    {
            //        Application.EnableVisualStyles();
            //        Application.SetCompatibleTextRenderingDefault(false);
            //        Application.Run(new Form1());
            //    }
            //    else
            //    {
            //        Application.Exit();                        
            //    }
            //}

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
        }
    }
}
