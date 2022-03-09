using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioWebServer.Classes
{
    public class Class1
    {
        IntPtr phandler = IntPtr.Zero;
        UInt32 nAddr = 0xffffffff;
        public int initialization()
        {
            var ret = -1;
            int intialize = -1;
            UInt32 pwd = 0;

            ret = fgapi.PSOpenDeviceEx(ref phandler, 2, 1, 1, 2, 0);
            if (ret == 0)
            {
                ret = fgapi.PSVfyPwd(phandler, nAddr, ref pwd);
                if (ret == 0)
                {
                    intialize = 0;
                    return intialize;
                    // this.textBox1.Clear();
                    // this.textBox1.Text = "Device initialized successfully!";
                }
                else
                {
                    intialize = -1;
                    return intialize;
                    // this.textBox1.Clear();
                    //   this.textBox1.Text = "Device initialization failed!";
                }
            }
            else
            {
                intialize = -1;
                return intialize;
                // this.textBox1.Clear();
                //  this.textBox1.Text = "Device not found!";
            }

        }

        public int cleardevice()
        {
            int ret = -1;
            ret = fgapi.PSEmpty(phandler, nAddr);

            if (ret == 0)
            {
                return ret;
                //this.textBox1.Text = "Clear fingerprint ok!";
            }
            else
            {
                return ret;
                //this.textBox1.Clear();
                //this.textBox1.Text = "Device error!";
            }
        }

        public int enroll()
        {
            int ret = -1;
            ret = fgapi.PSGetImage(phandler, nAddr);
            if (ret == 0)
            {
                ret = fgapi.PSGenChar(phandler, nAddr, 1);
                if (ret == 0)
                {
                    ret = fgapi.PSGetImage(phandler, nAddr);
                    if (ret == 0)
                    {
                        ret = fgapi.PSGenChar(phandler, nAddr, 2);
                        if (ret == 0)
                        {
                            ret = fgapi.PSRegModule(phandler, nAddr);
                            if (ret == 0)
                            {
                                ret = fgapi.PSStoreChar(phandler, nAddr, 1, 1);
                                if (ret == 0)
                                {
                                    //this.textBox1.Text = "Enroll finger ok!";
                                    return ret;
                                }
                            }
                        }
                    }

                }
            }
            return ret;
        }
        public int enrollbuf(ref byte[] buf, ref byte[] ImageData)
        {
            int ret = -1;
            // byte[] buf = new byte[512];
            int len = 0;
            int saveFgnum = 1;
            ImageData = new byte[256 * 288];
            int ImageLength = 0;
            ret = initialization();
            if (ret == 0)
            {
                ret = enroll();
                if (ret == 0)
                {
                    ret = fgapi.PSLoadChar(phandler, nAddr, 0x01, saveFgnum);       //only upload one fingerprint ,you can upload more
                    if (ret == 0)
                    {
                        ret = fgapi.PSUpChar(phandler, nAddr, 0x01, buf, ref len);
                        if (ret == 0)
                        {
                            ret = fgapi.PSGetImage(phandler, nAddr);
                            if (ret == 0)
                            {
                                ret = fgapi.PSUpImage(phandler, nAddr, ImageData, ref ImageLength);
                                if (ret == 0)
                                {
                                    cleardevice();
                                    return ret;
                                }
                                else { cleardevice(); }
                            }
                            else { cleardevice(); }

                        }
                        else { cleardevice(); }
                    }
                    else { cleardevice(); }
                }
                else { cleardevice(); }
            }
            return ret;
        }
    }
}
