using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BioWebServer
{
    class fgapi
    {
        [System.Runtime.InteropServices.DllImport("SynoAPIEx.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int PSOpenDeviceEx(ref IntPtr pHandle, int nDeviceType, int iCom = 1, int iBaud = 1, int nPackageSize = 2, int iDevNum = 0);

        [System.Runtime.InteropServices.DllImport("SynoAPIEx.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int PSCloseDeviceEx(IntPtr Handle);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSVfyPwd(IntPtr Handle, UInt32 nAddr, ref UInt32 pPassword);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSGetImage(IntPtr Handle, UInt32 nAddr);


        [DllImport("SynoAPIEx.dll")]
        public static extern int PSUpImage(IntPtr Handle, UInt32 nAddr, byte[] pImageData, ref int iImageLength);


        [DllImport("SynoAPIEx.dll")]
        public static extern int PSImgData2BMP(byte[] pImgData, string pImageFile);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSGenChar(IntPtr Handle, UInt32 nAddr, int iBufferID);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSRegModule(IntPtr Handle, UInt32 nAddr);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSStoreChar(IntPtr Handle, UInt32 nAddr, int iBufferID, int iPageID);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSSearch(IntPtr Handle, UInt32 nAddr, int iBufferID, int iStartPage, int iPageNum, ref int iMbAddress, ref int iscore);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSDelChar(IntPtr Handle, UInt32 nAddr, int iStartPageID, int nDelPageNum);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSTemplateNum(IntPtr Handle, UInt32 nAddr, ref int iMbNum);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSLoadChar(IntPtr Handle, UInt32 nAddr, int iBufferID, int iPageID);

        [DllImport("SynoAPIEx.dll")]
        public static extern int PSUpChar(IntPtr Handle, UInt32 nAddr, int iBufferID, byte[] pTemplet, ref int iTempletLength);


        [DllImport("SynoAPIEx.dll")]
        public static extern int PSDownChar(IntPtr Handle, UInt32 nAddr, int iBufferID, byte[] pTemplet, int iTempletLength);


        [DllImport("SynoAPIEx.dll")]
        public static extern int PSEmpty(IntPtr Handle, UInt32 nAddr);
    }
}
