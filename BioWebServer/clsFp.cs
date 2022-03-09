using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BioWebServer
{
    class clsFp
    {
        IntPtr phandler = IntPtr.Zero;
        UInt32 nAddr = 0xffffffff;
        string result = "";

        public string deviceInitialization()
        {
            //IntPtr[] phandler=new IntPtr[1];
            var ret = -1;
            //IntPtr phandler = IntPtr.Zero;

            //UInt32 nAddr = 0xffffffff;
            //int nAddr = 4294967295;

            UInt32 pwd = 0;

            ret = fgapi.PSOpenDeviceEx(ref phandler, 2, 1, 1, 2, 0);
            if (ret == 0)
            {
                //IntPtr handler = IntPtr.Zero;
                ret = fgapi.PSVfyPwd(phandler, nAddr, ref pwd);
                if (ret == 0)
                {
                    //  this.textBox1.Clear();
                    result = "Device initialized successfully!";
                }
                else
                {

                    // this.textBox1.Clear();
                    result = "Device initialization failed!";
                }
            }
            else
            {

                // this.textBox1.Clear();
                result = "Device not found!";
            }
            return result;
        }

        //get image
        public string getImage(byte[] ImageData, string ImagePath)
        {
            //UInt32 nAddr = 0xffffffff;
            try
            {
                int ret = -1;
                int i = 0;
                // byte[] ImageData = new byte[256 * 288];
                //int ImageLength = 0;
                // string ImagePath = "\\Sample1.bmp";
                //  ret = fgapi.PSGetImage(phandler, nAddr);
                //if (ret == 0)
                // {
                // this.textBox1.Clear();
                //     result = "Get image ok!";
                //    ret = fgapi.PSUpImage(phandler, nAddr, ImageData, ref ImageLength);
                //   if (ret == 0)
                //   {
                //this.textBox1.Clear();
                // result = "upload image ok!";
                string strBase64Image = Convert.ToBase64String(ImageData);
                ret = fgapi.PSImgData2BMP(ImageData, ImagePath);
                string path = ImagePath;
                Bitmap bmp = new Bitmap(path);
                MemoryStream ms = new MemoryStream();

                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                byte[] arr = new byte[ms.Length];

                ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length);
                ms.Close();

                string strBase64 = Convert.ToBase64String(arr);
                
                if (ret == 0)
                {
                    result = "000" + strBase64;

                    // pictureBox1.Image = Image.FromFile("D:\\Finger.bmp");
                }
                else
                {
                    result = "002";
                }
                // }
                // }
                return result;
            }
            catch (Exception ex)
            {
                return result = "003";
            }
            finally
            {
                Array.Clear(ImageData, 0, ImageData.Length);
                //Array.Clear(pbuf1, 0, pbuf1.Length);
            }
        }

        //enroll fingerprint
        public string enrollFingerprint()
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
                                    result = "Enroll finger ok!";
                                }
                            }
                        }
                    }

                }
            }
            return result;
        }

        //Search fingerprint
        public string matchFingerprint()
        {
            int ret = -1;
            int iMbAddress = 0;
            int iscore = 0;
            int fgnum = 0;
            ret = fgapi.PSGetImage(phandler, nAddr);
            if (ret == 0)
            {
                ret = fgapi.PSGenChar(phandler, nAddr, 1);
                if (ret == 0)
                {

                    fgapi.PSTemplateNum(phandler, nAddr, ref fgnum);
                    fgapi.PSSearch(phandler, nAddr, 0x01, 0, fgnum + 1, ref iMbAddress, ref iscore);
                    if (iscore >= 10)
                    {

                        result = "Fingerprint match ok!";// Score is " + Convert.ToString(iscore);

                    }
                    else
                    {
                        result = "Fingerprint does not match! Score is " + Convert.ToString(iscore);
                    }
                }
            }
            return result;
        }

        //Del single fingerprint
        public string DelSingleFingerprint()
        {
            int ret = -1;
            int delstartid = 0;
            int fgnum = 0;
            fgapi.PSTemplateNum(phandler, nAddr, ref fgnum);

            ret = fgapi.PSDelChar(phandler, nAddr, delstartid, 1);
            if (ret == 0)
            {
                result = "There are" + Convert.ToString(fgnum) + "Fingerprint , delete the first" + Convert.ToString(delstartid) + "Fingerprints success!";
            }
            else
            {

                // this.textBox1.Clear();
                result = "Device error!";
            }
            return result;
        }

        //download fingerprint
        public string downloadFingerprint(byte[] pbuf, string fileName)
        {
            int ret = -1;
            // byte[] pbuf = new byte[1024 + 1];
            int len = 0;
            int saveFgnum = 1;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "Fingerprint Files\\" + fileName.ToString();
                //path = "\\Sample1.mb";
                FileStream fs = new FileStream(path, FileMode.CreateNew);
                //ret = fgapi.PSLoadChar(phandler, nAddr, 0x01, saveFgnum);       //only upload one fingerprint ,you can upload more
                //if (ret == 0)
                //{
                //    ret = fgapi.PSUpChar(phandler, nAddr, 0x01, pbuf, ref len);
                //    if (ret == 0)
                //    {
                fs.Write(pbuf, 0, pbuf.Length);
                fs.Close();
                result = "000";
                //  result = "Download fingerprint sucessful!";
                // }
                //else
                //{

                //   // this.textBox1.Clear();
                //    result = "Device error!";
                //}
                //}
                return result;
            }
            catch (Exception ex)
            {
                result = ex.Message.ToString();
                return result;
            }
        }

        //Upload fingerprint
        public string uploadFingerprint(string path)
        {
            int ret = -1;
            int fgid = 1;  // 
            byte[] pbuf = new byte[1024 + 1];  //
            int len = 0;


            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader r = new BinaryReader(fs);
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(pbuf, 0, 1024);
            fs.Close();

            ret = fgapi.PSDownChar(phandler, nAddr, 0x01, pbuf, pbuf.Length);
            if (ret == 0)
            {
                ret = fgapi.PSStoreChar(phandler, nAddr, 1, fgid);
                if (ret == 0)
                {

                    result = "Upload fingerprint successful!";
                }
                else
                {

                    // this.textBox1.Clear();
                    result = "Device error!";
                }
            }

            return result;

        }

        //Clear fingerprint
        public string clearDevice()
        {
            int ret = -1;
            ret = fgapi.PSEmpty(phandler, nAddr);
            if (ret == 0)
            {
                result = "Clear fingerprint ok!";
            }
            else
            {

                // this.textBox1.Clear();
                result = "Device error!";
            }
            return result;

        }

        public string countFingerprint(object sender, EventArgs e)
        {
            int ret = -1;
            int delstartid = 0;
            int fgnum = 0;
            fgapi.PSTemplateNum(phandler, nAddr, ref fgnum);

            result = "There are" + Convert.ToString(fgnum) + "Fingerprint!";

            return result;
        }

        //private void Form1_Load(object sender, EventArgs e)
        //{
        //    string strPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
        //    txtPath.Text = strPath.Substring(6);
        //}



        public string getFPScore(string Src, string Dst)
        {
            // Data buffer for incoming data.  
            byte[] bytes = new byte[5];
            int bytesRec = 0;
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[1];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 8221);

                // Create a TCP/IP  socket.  
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    /// Console.WriteLine("Socket connected to {0}",
                    //   sender.RemoteEndPoint.ToString());



                    // Encode the data string into a byte array.  
                    byte[] msg = Encoding.ASCII.GetBytes(Src + "," + Dst + "," + "end");

                    // Send the data through the socket.  
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.  
                    bytesRec = sender.Receive(bytes);
                    // Console.WriteLine("Echoed test = {0}",
                    //   Encoding.ASCII.GetString(bytes, 0, bytesRec));
                    string message = ConvertBytesToString(bytes, bytes.Length);

                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    return message.ToString();

                }
                catch (ArgumentNullException ane)
                {
                    return bytesRec.ToString();
                    // Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    return bytesRec.ToString();
                    // Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    return bytesRec.ToString();
                    //  Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                //  Console.WriteLine(e.ToString());
                return bytesRec.ToString();
            }

        }


        private string ConvertBytesToString(byte[] bytes, int iRx)
        {
            try
            {
                char[] chars = new char[iRx];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                d.GetChars(bytes, 0, iRx, chars, 0);
                string szData = new string(chars);
                return szData;
            }
            catch (Exception ex)
            {
                //if (chkbxDebServerLog.Checked)
                //{
                // waitHandle.WaitOne();
                // objISO_Log_Writter.WriteIN_ISO_LOG_GP(DateTime.Now.ToString() + ":" + "ISO SRVR Error Message  --->>>>" + ex.Message + Environment.NewLine, ex.Message);
                // waitHandle.Set();

                //}
                return ex.Message.ToString();
            }
        }




        public string verfiyFP(byte[] pbuf, string[] files)
        {
            int[] score = new int[1];
            int ret = -1;
            int fgid = 1;  // 
            byte[] pbuf1 = new byte[512];  //
            int len = 0;
            int count = files.Length;
            string message = "111";
            try
            {
                for (int i = 0; i < count; i++)
                {
                    string file = files[i];
                    string[] file1 = file.Split(';');
                    file = file1[0];
                    string index = file1[1];
                    pbuf1 = Convert.FromBase64String(file);

                    // clsMatchAPI obj = new clsMatchAPI();
                    int a = 0;// obj.match(pbuf, pbuf1, (byte)3, score);

                    a = MatchApi.MatchTemplate(pbuf, pbuf1);

                    // int a = score[0];
                    if (a > 25)
                    {
                        message = "000" + "*" + index;
                        break;
                    }
                    //message = message;
                }
                return message;
            }
            catch (Exception ex)
            {
                message = "004";
                return message;
            }
            finally
            {
                //Array.Clear(pbuf, 0, pbuf.Length);
                //Array.Clear(pbuf1, 0, pbuf1.Length);
                pbuf = null;
                pbuf1 = null;
            }
        }

        public string verfiyFPForAgent(byte[] pbuf, string[] files)
        {
            int[] score = new int[1];
            int ret = -1;
            int fgid = 1;  // 
            byte[] pbuf1 = new byte[512];  //
            int len = 0;
            int count = files.Length;
            string message = "111";
            try
            {
                for (int i = 0; i < count; i++)
                {
                    string file = files[i];
                    string[] file1 = file.Split(';');
                    file = file1[0];
                    string index = file1[1];
                    pbuf1 = Convert.FromBase64String(file);

                    // clsMatchAPI obj = new clsMatchAPI();
                    int a = 0;// obj.match(pbuf, pbuf1, (byte)3, score);

                    a = MatchApi.MatchTemplate(pbuf, pbuf1);

                    // int a = score[0];
                    if (a > 25)
                    {
                        message = "000" + "*" + index;
                        break;
                    }
                    //message = message;
                }
                return message;
            }
            catch (Exception ex)
            {
                message = "004";
                return message;
            }
            finally
            {
                //Array.Clear(pbuf, 0, pbuf.Length);
                //Array.Clear(pbuf1, 0, pbuf1.Length);
                pbuf = null;
                pbuf1 = null;
            }
        }
    }
}
