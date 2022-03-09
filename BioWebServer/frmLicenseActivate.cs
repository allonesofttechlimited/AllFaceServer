using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Management.Instrumentation;



namespace MIT_MGW.Forms
{
    public partial class frmLicenseActivate : Form
    {
        //#########################################
        public frmLicenseActivate(string strFormTitle)
        {
            InitializeComponent();
        }

        private void frmLicenseActivate_Load(object sender, EventArgs e)
        {
            getSystemInfo();
        }

        private void btnGetInfo_Click(object sender, EventArgs e)
        {
            getSystemInfo();
        }
        private void getSystemInfo()
        {
            ///Get Machien Name            
            ///string name = Environment.MachineName;
            ///string name = System.Net.Dns.GetHostName();
            ///string name = System.Windows.Forms.SystemInformation.ComputerName;
            ///string name = System.Environment.GetEnvironmentVariable(“COMPUTERNAME”);
            txtMachineName.Text = System.Net.Dns.GetHostName();

            ///#######################
            // First we create the ManagementObjectSearcher that
            // will hold the query used.
            // The class Win32_BaseBoard (you can say table)
            // contains the Motherboard information.
            // We are querying about the properties (columns)
            // Product and SerialNumber.
            // You can replace these properties by
            // an asterisk (*) to get all properties (columns).
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");

            // Executing the query...
            // Because the machine has a single Motherborad,
            // then a single object (row) returned.
            ManagementObjectCollection information = searcher.Get();
            foreach (ManagementObject obj in information)
            {
                // Retrieving the properties (columns)
                // Writing column name then its value
                foreach (PropertyData data in obj.Properties)
                {   // txtMachineSerial.Text= data.Name.ToString()+ data.Value.ToString();
                    txtMachineSerial.Text = txtMachineSerial.Text + data.Value.ToString() + Environment.NewLine;
                }
            }
            // For typical use of disposable objects
            // enclose it in a using statement instead.
            searcher.Dispose();

        }
        private void btnActivate_Click(object sender, EventArgs e)
        {
            MiT_License.clsDBConnectionReadWrite objLicense = new MiT_License.clsDBConnectionReadWrite();
            if (objLicense.SetLisence(rtbLisence.Text).Equals(""))
            {
                rtbLisence.Text = "License activates successfully";
            }
        }
    }
}
