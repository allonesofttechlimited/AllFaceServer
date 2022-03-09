using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BioWebServer
{
    public class MatchApi
    {
        [System.Runtime.InteropServices.DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int MatchTemplate(byte[] pSrcData, byte[] pDstData);
    }
}
