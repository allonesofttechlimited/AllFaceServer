using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioWebServer
{
    public class FPMessage
    {
        public int workmsg = 0;
        public int retmsg = 0;
        //public byte[] data1 = null;
        //public byte[] data2 = null;
        //public byte[] image = null;
        public String data1 = "";
        public String data2 = "";
        public String image = "";
    }

    public class FPMCommand
    {
        public String cmd = "";
        public String data1 = "";
        public String data2 = "";
        public String nid = "";
        public String dob = "";
        public String userName = "";
        public String password = "";
        public String customerAccount = "";
        public String amount = "";
        public String eMerchantAccountNo = "";
        public String ePin = "";
        public String deviceId = "";
        public String strKey = "";
        public String securityFinger = "";
        public String securityOTP = "";
        public String securityPIN = "";
        public String toAccount = "";
        public String AgentAccount = "";
        public String AgentPIN = "";
        public String fingerDeviceSerial = "";
        public String value = "";
        public String key = "";
        public String functionName = "";
    }

    public class FPMMatchBuffer
    {
        public String sessionid="";
        public int data1size = 0;
        public int data2size = 0;
        public byte[] data1buf=new byte[512];
        public byte[] data2buf=new byte[512];
    }
}
