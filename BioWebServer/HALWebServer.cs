using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Collections.Generic;

using CommandLine;

using VDT.FaceRecognition.SDK;
using System.Net.Sockets;
using System.Net;
using SuperWebSocket;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Logging;
using System.Drawing.Imaging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BioWebServer.Classes;
using System.Reflection;
using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;

namespace BioWebServer
{
    public partial class HALWebServer : Form
    {
        //Developed By Porosh @ 06-03(march)-2022
        private String faceSDKRootDir = "E:\\Face Porosh";
        private Capturer capturer=null;
        private int camera_id = 0;
        private VideoCapture camera;
        FacerecService service = null;
        FacerecService.Config capturerConfig = new FacerecService.Config("fda_tracker_capturer_uld.xml");

        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;
        //end Developed By Porosh @ 06-03(march)-2022
        CryptoLibrary cryptoLibrary = new CryptoLibrary();
        clsServiceHandler objServiceHandler=new clsServiceHandler();
        string strKey = Global.SecurityKey;
        public const int WM_DEVICECHANGE = 0x0219;
        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int WM_SESSIONNEW = 1224; //0x0400 + 200;
        public const int WM_SESSIONCLOSE = 1225; //0x0400 + 200;
        clsLogWritter objLogWriter = new clsLogWritter();
        int time = 0;
        int dvCatagory = -1;
        string result = "";
        private WebSocketServer mWebSocketServer;
        private FPMessage mFPMessage;
        private FPMMatchBuffer mFPMMatchBuffer;
        private int refsize;
        private int matsize;
        private byte[] refdata = null;
        private byte[] matdata = null;
        private Boolean isclose = false;
        private string mSessionID = "";
        string lstnMsg = "";
        private Boolean IsOpen = false;
        int steps = 2;

        private byte[] rawimagedata = null;
        private int rawimagesize = 0;
        private byte[] wsqimagedata = null;
        private int wsqimagesize = 0;
        Class1 obj = new Class1();

       

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, int msg, uint wParam, uint lParam);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, int msg, uint wParam, ref String lParam);

        [DllImport("User32.dll", EntryPoint = "PostMessage")]
         public static extern int PostMessage(IntPtr hWnd,int Msg,int wParam,int lParam);
        


        public HALWebServer()
        {
            InitializeComponent();

            refsize = 0;
            refdata = new byte[512];
            matsize = 0;
            matdata = new byte[512];

            rawimagedata = new byte[256 * 360];
            wsqimagedata = new byte[256 * 360];            
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void AppendStatus(String txt)
        {
            this.textBox1.AppendText(txt+"\n");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
            this.Left = Screen.GetBounds(this).Width - this.Width - 5;
            this.Top = Screen.GetBounds(this).Height - this.Height - 50;

            mFPMessage = new FPMessage();
            mFPMMatchBuffer = new FPMMatchBuffer();

            this.Setup();
            this.mWebSocketServer.Start();
            this.notifyIcon1.ShowBalloonTip(1000, "Fingerprint Web Server", "Runing", ToolTipIcon.Info);

            //Developed By Porosh For Face Recognition it is depended on .net 4.7.2 sdk So  I Commented the open Device Part
            //StartServer();
            service = FacerecService.createService(faceSDKRootDir + "\\conf\\facerec", @"E:\\Face Porosh\\license");
           // FacerecService.Config capturerConfig = new FacerecService.Config("fda_tracker_capturer_uld.xml");

            //if (fpengine.OpenDevice(0, 0, 0) == 1)
            //{
            //    if (fpengine.LinkDevice() == 1)
            //    {
            //        dvCatagory = 1; // new device
            //        AppendStatus("Fingerprint Device Ready!");
            //        IsOpen = true;
            //    }
            //    else if (obj.initialization() == 0)
            //    {
            //        dvCatagory = 0; // old device
            //        AppendStatus("Fingerprint Device Ready!");
            //        IsOpen = true;
            //    }
            //    else
            //    {
            //        AppendStatus("Link Device Fail!");
            //    }
            //}
            //else
            //{
            //    AppendStatus("Open Device Fail!");
            //}

            AppendStatus("Fingerprint Server Ready!");            
            timer2.Enabled = true;

            //AppendStatus(Application.ExecutablePath);
            FirstAutoRun(true);
            CheckAutoRun(Application.ExecutablePath, true);

            //label2.Visible = true;
            //UInt32 serial = fpengine.GetDeviceSnNum();
            //string strSer = serial.ToString();
            //if (strSer == "0")
            //{
            //    textBox2.Text = "0";
            //}
            //else
            //{
            //    textBox2.Text = strSer;
            //}

            //label2.Text = "Device Serial: " + strSer;

            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in filterInfoCollection)
                cboCamera.Items.Add(Device.Name);
            cboCamera.SelectedIndex = 0;
            videoCaptureDevice = new VideoCaptureDevice();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //fpengine.CloseDevice();
            this.mWebSocketServer.Stop();
        }

        protected void Setup(WebSocketServer websocketServer, Action<ServerConfig> configurator)
        {
            var rootConfig = new RootConfig { DisablePerformanceDataCollector = true };
            websocketServer.NewSessionConnected += new SessionHandler<WebSocketSession>(mWebSocketServer_NewSessionConnected);
            websocketServer.SessionClosed += new SessionHandler<WebSocketSession, SuperSocket.SocketBase.CloseReason>(mWebSocketServer_SessionClosed);
            websocketServer.NewDataReceived += new SessionHandler<WebSocketSession, byte[]>(mWebSocketServer_NewDataReceived);

            var config = new ServerConfig();
            configurator(config);

            var ret = websocketServer.Setup(rootConfig, config, null, null, new ConsoleLogFactory(), null, null);
            
            mWebSocketServer = websocketServer;
        }

        public virtual void Setup()
        {
            Setup(new WebSocketServer(), c =>
            {
                c.Name = "Fingerprint Server";
                c.Port = 21187;
                c.Ip = "127.0.0.1";
                c.MaxConnectionNumber = 100;
            });

            mWebSocketServer.NewMessageReceived += new SessionHandler<WebSocketSession, string>(mWebSocketServer_NewMessageReceived);
        }

        
        void mWebSocketServer_NewMessageReceived(WebSocketSession session, string e)
        {
            Console.WriteLine("Server Message:" + e+"  "+ session.SessionID);
            int workmsg = 0;
            int retmsg = 0;

            mSessionID = session.SessionID;
            try
            {
                if (e.Equals("enrol"))
                {
                    fpengine.EnrolFpChar();
                }
                else if (e.Equals("capture"))
                {
                    fpengine.GenFpChar();
                }
                else if (e.Equals("match"))
                {

                }
                else
                {
                    FPMCommand fm = jsonhelper.parse<FPMCommand>(e);
                    if (fm.cmd.Equals("match"))
                    {
                        string strPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                        strPath = strPath.Substring(6) + "\\GWPSK.mit";
                        if (!File.Exists(strPath))
                        {
                            SendNetMessage(10, 3, null, null, null);
                            return;
                        }
                        else
                        {
                            string text = File.ReadAllText(strPath, Encoding.UTF8);
                            text = text.Replace("\r\n", "");
                            string decValue = cryptoLibrary.Decrypt(text, strKey);

                            string deviceSerial = fm.data1.ToString();
                            string passKey = fm.data2.ToString();

                            if (deviceSerial.Trim().ToString() != textBox2.Text.Trim().ToString())
                            {
                              //  sc = 4;
                                SendNetMessage(10, 4, null, null, null);
                                return;
                            }

                            if (passKey.Trim().ToString() != decValue.Trim().ToString())
                            {
                               // sc = 5;
                                SendNetMessage(10, 5, null, null, null);
                                return;
                            }

                        }

                        try
                        {
                            if (fm.data1.Length > 100)
                            {
                                byte[] tp1 = Convert.FromBase64String(fm.data1);
                                tp1.CopyTo(mFPMMatchBuffer.data1buf, 0);
                                mFPMMatchBuffer.data1size = tp1.Length;
                            }
                            if (fm.data2.Length > 100)
                            {
                                byte[] tp2 = Convert.FromBase64String(fm.data2);
                                tp2.CopyTo(mFPMMatchBuffer.data2buf, 0);
                                mFPMMatchBuffer.data2size = tp2.Length;
                            }
                            int sc = fpengine.MatchTemplateOne(mFPMMatchBuffer.data2buf, mFPMMatchBuffer.data1buf, mFPMMatchBuffer.data1size);

                            SendNetMessage(fpengine.FPM_MATCH, sc, null, null, null);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    else if (fm.cmd.Equals("setdata"))
                    {
                        try
                        {
                            if (fm.data1.Length > 100)
                            {
                                byte[] tp1 = Convert.FromBase64String(fm.data1);
                                tp1.CopyTo(mFPMMatchBuffer.data1buf, 0);
                                mFPMMatchBuffer.data1size = tp1.Length;
                            }
                            if (fm.data2.Length > 100)
                            {
                                byte[] tp2 = Convert.FromBase64String(fm.data2);
                                tp2.CopyTo(mFPMMatchBuffer.data2buf, 0);
                                mFPMMatchBuffer.data2size = tp2.Length;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    else if (fm.cmd.Equals("enrol"))
                    {

                        if (fm.data1.ToString() == "GetSerial")
                        {
                            SendNetMessageSerial(10, 6, textBox2.Text.Trim().ToString(), null, null);
                        }

                        else
                        {
                            int sc = 0;
                            string strPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                            strPath = strPath.Substring(6) + "\\GWPSK.mit";
                            if (!File.Exists(strPath))
                            {
                                sc = 3;
                                SendNetMessage(10, sc, null, null, null);
                                return;
                            }
                            else
                            {
                                string text = File.ReadAllText(strPath, Encoding.UTF8);
                                text = text.Replace("\r\n", "");
                                string decValue = cryptoLibrary.Decrypt(text, strKey);

                                string deviceSerial = fm.data1.ToString();
                                string passKey = fm.data2.ToString();

                                if (deviceSerial.Trim().ToString() != textBox2.Text.Trim().ToString())
                                {
                                    sc = 4;
                                    SendNetMessage(10, sc, null, null, null);
                                    return;
                                }

                                if (passKey.Trim().ToString() != decValue.Trim().ToString())
                                {
                                    sc = 5;
                                    SendNetMessage(10, sc, null, null, null);
                                    return;
                                }

                            }
                            lstnMsg = "enrol";

                            fpengine.EnrolFpChar();
                        }
                    }
                    else if (fm.cmd.Equals("capture"))
                    {
                        fpengine.GenFpChar();
                    }
                    else if (fm.cmd.Equals("opendevice"))
                    {
                        ReOpenDevice();
                    }
                    else if (fm.cmd.Equals("wsqimage"))
                    {
                        fpengine.ImageToWsq(rawimagedata, 256, 288, 8, 500, 2.833755f, wsqimagedata, ref wsqimagesize);


                        FileStream fswsq = new FileStream(".\\test.wsq", FileMode.Create);
                        fswsq.Write(wsqimagedata, 0, wsqimagesize);
                        fswsq.Close();

                        byte[] wsq = new byte[wsqimagesize];
                        Array.Copy(wsqimagedata, 0, wsq, 0, wsqimagesize);

                        var data = File.ReadAllBytes(".\\test.wsq");
                        //var s2 = Convert.ToBase64String(data, 0, data.Length);

                        var equalCheck = wsq.SequenceEqual(data);

                        SendNetMessage(fpengine.FPM_WSQIMAGE, 0, null, null, data);
                    }
                    else if (fm.cmd.Equals("LogInData"))
                    {
                        workmsg = 11;
                        retmsg = 0;
                        string strReturn = textBox2.Text.Trim().ToString();
                        SendNetMessage(workmsg, retmsg, strReturn);
                    }
                    else if (fm.cmd.Equals("E"))
                    {
                        workmsg = 14;
                        retmsg = 0;
                        string strReturn;
                        string value = fm.value.ToString();
                        string functionName = fm.functionName.ToString();
                        if (fm.key.ToString() != "")
                        {
                            string key = fm.key.ToString();
                            if (value.Contains("*"))
                            {
                                strReturn = objServiceHandler.EncryptionArray(value, key);
                            }
                            else
                            {
                                strReturn = objServiceHandler.Encrypt(value, key);
                            }
                        }
                        else
                        {
                            if (value.Contains("*"))
                            {
                                strReturn = objServiceHandler.EncryptionArray(value);    
                            }
                            else
                            {
                                strReturn = objServiceHandler.Encrypt(value);
                            }
                        }
                        strReturn = functionName + "|" + strReturn;
                        SendNetMessage(workmsg, retmsg, strReturn);
                    }
                    else if (fm.cmd.Equals("D"))
                    {
                        workmsg = 14;
                        retmsg = 1;
                        string strReturn;
                        string value = fm.value.ToString();
                        string functionName = fm.functionName.ToString();
                        if (fm.key.ToString() != "")
                        {
                            string key = fm.key.ToString();
                            if (value.Contains("*"))
                            {
                                strReturn = objServiceHandler.DecryptionArray(value, key);
                            }
                            else
                            {
                                strReturn = objServiceHandler.Decrypt(value, key);
                            }
                        }
                        else
                        {
                            if (value.Contains("*"))
                            {
                                strReturn = objServiceHandler.DecryptionArray(value);
                            }
                            else
                            {
                                strReturn = objServiceHandler.Decrypt(value);
                            }
                        }
                        strReturn = functionName + "|" + strReturn;
                        SendNetMessage(workmsg, retmsg, strReturn);
                    }
                }
            }
            catch(Exception ex)
            {
                CreateLogFiles Err = new CreateLogFiles();
                string m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Err.ErrorLog(m_exePath + "\\LOGS\\", ex.Message);
                SendNetMessage(workmsg, retmsg, "error");
            }       
        }

        protected void mWebSocketServer_NewDataReceived(WebSocketSession session, byte[] e)
        {            
            Console.WriteLine("Server Data:" + e + "  " + session.SessionID);
            //session.Send(e, 0, e.Length);
        }
        void mWebSocketServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason reason)
        {
            Console.WriteLine("{0:HH:MM:ss}  与客户端:{1}的会话被关闭 原因：{2}", DateTime.Now, session.Path, reason);
                        
            //String msg = String.Format("{0:HH:MM:ss}  与客户端:{1}的会话被关闭 原因：{2}", DateTime.Now, session.Path, reason);
            //SendMessage(this.Handle, WM_SESSIONCLOSE, 0,1);
        }

        void mWebSocketServer_NewSessionConnected(WebSocketSession session)
        {
            Console.WriteLine("{0:HH:MM:ss}  与客户端:{1}创建新会话", DateTime.Now, session.Path);
            
            //String msg = String.Format("{0:HH:MM:ss}  与客户端:{1}创建新会话", DateTime.Now, session.Path);
            //SendMessage(this.Handle, WM_SESSIONNEW, 1, 1);
        }

        public string Encrypt(string textToEncrypt, string key)
        {
            // string key = "BHMACLDBApp";
            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;

            rijndaelCipher.KeySize = 0x80;
            rijndaelCipher.BlockSize = 0x80;
            byte[] pwdBytes = Encoding.UTF8.GetBytes(key);
            byte[] keyBytes = new byte[0x10];
            int len = pwdBytes.Length;
            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }
            Array.Copy(pwdBytes, keyBytes, len);
            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;
            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
            byte[] plainText = Encoding.UTF8.GetBytes(textToEncrypt);
            return Convert.ToBase64String(transform.TransformFinalBlock(plainText, 0, plainText.Length));
        }

        private void SendNetMessage(int workmsg, int retmsg, byte[] data1, byte[] data2, byte[] image)
        {
            mFPMessage.workmsg = workmsg;
            mFPMessage.retmsg = retmsg;
            //MemoryStream ms = new MemoryStream(image, 0, image.Length);
            //ms.Write(image, 0, image.Length);
            //System.Drawing.Image imagetr = System.Drawing.Image.FromStream(ms, true);

            string strReturn = "";

            //mFPMessage.data1 = data1;
            //mFPMessage.data2 = data2;
            //mFPMessage.image = image;
            if (data1 != null && dvCatagory == 0)
            {
                if (retmsg == 1)
                {
                    Random rnd = new Random();
                    int ran = rnd.Next(100000, 999999);
                    string encr = Encrypt(ran.ToString(), "Fingerprint Data For Both Device"); //asdfghjkl
                    string data = Convert.ToBase64String(data1, 0, data1.Length);
                    if (data2 != null)
                    {
                        //string strReturn = ws.storeFingerPrint1(data, "162862", "1578222393742", "1", data2);
                        strReturn = storeFingerPrint(data1, "162813", "1578222393742", "1", data2);
                    }
                    else
                    {
                        //string strReturn = ws.storeFingerPrint1(data, "162862", "1578222393742", "1", image);
                        strReturn = storeFingerPrint(data1, "162814", "1578222393742", "1", image);
                    }


                    mFPMessage.data1 = data;


                }
                else
                {
                    mFPMessage.data1 = "null";
                }
            }
            else { mFPMessage.data1 = "null"; }
            if (data1 == null)
            {
                if (data2 != null)
                    mFPMessage.data2 = Convert.ToBase64String(data2, 0, data2.Length);
                else
                    mFPMessage.data2 = "null";
                if (image != null)
                    mFPMessage.image = Convert.ToBase64String(image, 0, image.Length);
                else
                    mFPMessage.image = "null";
            }
            else if(dvCatagory == 0)
            {
                mFPMessage.workmsg = 15;
                if (data2 != null)
                    mFPMessage.image = strReturn;
                else
                    mFPMessage.image = "null";
                if (data2 != null)
                    mFPMessage.data2 = Convert.ToBase64String(data2, 0, data2.Length);
                else
                    mFPMessage.data2 = "null";
            }
            else
            {
                if (data1 != null)
                    mFPMessage.data1 = Convert.ToBase64String(data1, 0, data1.Length);
                else
                    mFPMessage.data1 = "null";
            }



            String cmd = "";
            try
            {
                cmd = jsonhelper.stringify(mFPMessage);
            }
            catch
            {
                cmd = "error";
            }


            foreach (var sendSession in mWebSocketServer.GetAllSessions())
            {
                if (sendSession.SessionID.Equals(mSessionID))
                {
                    //sendSession.Send(jsonhelper.stringify(mFPMessage));
                    sendSession.Send(cmd);
                    break;
                }
            }
            //*/
        }

        public string storeFingerPrint(byte[] pbuf, string accID, string requestID, string index, byte[] pbuf1)
        {
            string msg = "";
            try
            {
                clsServiceHandler objServiceHandelar = new clsServiceHandler();
                //int isExists = objServiceHandelar.checkExististingFpIds(accID, index);
                //if (isExists > 0)
                //{
                //    return "800";
                //}


                clsFp objFP = new clsFp();
                string reqTime = DateTime.Now.ToString();
                //string reqType = "STFP";
                // clsServiceHandler objServiceHandelar = new clsServiceHandler();
                // int intNumOfFile = objServiceHandelar.getNumberOfFile(accID);
                //intNumOfFile = intNumOfFile + 1;
                string fileSnapNo = "";
                //if (intNumOfFile == 1010)
                //{
                fileSnapNo = DateTime.Now.ToString("ddMMyyyyhhmmss");
                //}
                //else
                //{
                //    fileSnapNo = intNumOfFile.ToString();
                //}
                string fileName = accID + "SnapNo" + fileSnapNo;
                //string result = System.Text.Encoding.UTF8.GetString(pbuf);
                //string hex = Convert.ToBase64String(pbuf);
                //string result = objServiceHandelar.storeFpIds(accID, fileName, "", requestID, index, hex);
                string strMonDate = DateTime.Now.ToString("ddMMyyyy");
                //if (result == "000")
                //{

                string path1 = AppDomain.CurrentDomain.BaseDirectory + "Fingerprint Files";
                if (!Directory.Exists(path1))
                {
                    Directory.CreateDirectory(path1);
                }

                string path = AppDomain.CurrentDomain.BaseDirectory + "Fingerprint Files\\" + fileName.ToString() + ".bmp";


                string result = objFP.getImage(pbuf1, path);

                //result = "000";
                //}
                string resTime = DateTime.Now.ToString();
                //string Resresult = "";
                string SplitedResult = result.Substring(3);
                //Resresult = objServiceHandelar.InsertResponse(accID, requestID, index, reqTime, resTime, reqType, result);
                //if (Resresult == "000")
                //{
                return SplitedResult;
                //}
                //else
                //{
                //    return Resresult;
                //}
            }
            catch (Exception ex)
            {
                msg = "333";

                objLogWriter.WriteSSL_Req_LOG(DateTime.Now.ToString("ddMMyyyy HH:mm:ss") + ":" + "Error Code:" + msg + "Error Message : storeFingerPrint - " + ex.Message.ToString());

                return msg;
            }

            finally
            {
                Array.Clear(pbuf, 0, pbuf.Length);
                Array.Clear(pbuf1, 0, pbuf1.Length);
            }

        }
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

        private void SendNetMessageSerial(int workmsg, int retmsg, string data1, byte[] data2, byte[] image)
        {
            mFPMessage.workmsg = workmsg;
            mFPMessage.retmsg = retmsg;
            //mFPMessage.data1 = data1;
            //mFPMessage.data2 = data2;
            //mFPMessage.image = image;
            if (data1 != null)
                mFPMessage.data1 = data1;//Convert.ToBase64String(data1, 0, data1.Length);
            else
                mFPMessage.data1 = "null";
            if (data2 != null)
                mFPMessage.data2 = Convert.ToBase64String(data2, 0, data2.Length);
            else
                mFPMessage.data2 = "null";
            if (image != null)
                mFPMessage.image = Convert.ToBase64String(image, 0, image.Length);
            else
                mFPMessage.image = "null";

            String cmd = "";
            try
            {
                cmd = jsonhelper.stringify(mFPMessage);
            }
            catch
            {
                cmd = "error";
            }

            //广播
            /*
            foreach (var sendSession in mWebSocketServer.GetAllSessions())
            {
                sendSession.Send(cmd);
            }
            */
            //指定
            ///*
            foreach (var sendSession in mWebSocketServer.GetAllSessions())
            {
                if (sendSession.SessionID.Equals(mSessionID))
                {
                    //sendSession.Send(jsonhelper.stringify(mFPMessage));
                    sendSession.Send(cmd);
                    break;
                }
            }
            //*/
        }
        private void SendNetMessage(int workmsg, int retmsg, string strReturn = "")
        {
            mFPMessage.workmsg = workmsg;
            mFPMessage.retmsg = retmsg;
           
            //if (data1 != null)
            //    mFPMessage.data1 = Convert.ToBase64String(data1, 0, data1.Length);
            //else
            //    mFPMessage.data1 = "null";
            //if (data2 != null)
            //    mFPMessage.data2 = Convert.ToBase64String(data2, 0, data2.Length);
            //else
            //    mFPMessage.data2 = "null";

            String cmd = "";
            try
            {
                mFPMessage.data1 = strReturn;
                cmd = jsonhelper.stringify(mFPMessage);
            }
            catch
            {
                cmd = "error";
            }
            //System.Threading.Thread.Sleep(100);
            try
            {
                foreach (var sendSession in mWebSocketServer.GetAllSessions())
                {
                    //System.Threading.Thread.Sleep(1000);

                    if (sendSession.SessionID.Equals(mSessionID))
                    {
                        sendSession.Send(cmd);
                        break;
                    }
                    else
                    {
                        mFPMessage.data1 = strReturn;
                        cmd = jsonhelper.stringify(mFPMessage);
                        sendSession.Send(cmd);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

                CreateLogFiles Err = new CreateLogFiles();
                string m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Err.ErrorLog(m_exePath + "\\LOGS\\", ex.Message);
            }
        }                

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            if (this.Visible)
                this.Hide();
            else
                this.Show();
        }

        private void SendNetMessage1(int workmsg, int retmsg, byte[] data1, byte[] data2, byte[] image)
        {
            mFPMessage.workmsg = workmsg;
            mFPMessage.retmsg = retmsg;
            //mFPMessage.data1 = data1;
            //mFPMessage.data2 = data2;
            //mFPMessage.image = image;
            if (data1 != null)
                mFPMessage.data1 = Convert.ToBase64String(data1, 0, data1.Length);
            else
                mFPMessage.data1 = "null";
            if (data2 != null)
                mFPMessage.data2 = Convert.ToBase64String(data2, 0, data2.Length);
            else
                mFPMessage.data2 = "null";
            if (image != null)
                mFPMessage.image = Convert.ToBase64String(image);//, 0, image.Length);
            else
                mFPMessage.image = "null";

            String cmd = "";
            try
            {
                cmd = jsonhelper.stringify(mFPMessage);
            }
            catch
            {
                cmd = "error";
            }


            /*
            foreach (var sendSession in mWebSocketServer.GetAllSessions())
            {
                sendSession.Send(cmd);
            }
            */

            ///*
            foreach (var sendSession in mWebSocketServer.GetAllSessions())
            {
                if (sendSession.SessionID.Equals(mSessionID))
                {
                    //sendSession.Send(jsonhelper.stringify(mFPMessage));
                    sendSession.Send(cmd);
                    break;
                }
            }
            //*/
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isclose)
            {
                e.Cancel = true;
                this.Hide();
            }            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (dvCatagory == 1)
            {
                newdevice();
            }
            else if (dvCatagory == 0)
            {
                olddevice();
            }

           
        }

        public void newdevice()
        {
            int wm = fpengine.GetWorkMsg();
            int rm = fpengine.GetRetMsg();
            switch (wm)
            {
                case fpengine.FPM_DEVICE:
                    //AppendStatus("Not Open Reader");
                    SendNetMessage(fpengine.FPM_DEVICE, 0, null, null, null);
                    break;
                case fpengine.FPM_PLACE:
                    //AppendStatus("Please Plase Finger");
                    SendNetMessage(fpengine.FPM_PLACE, 0, null, null, null);
                    break;
                case fpengine.FPM_LIFT:
                    //AppendStatus("Please Lift Finger");
                    SendNetMessage(fpengine.FPM_LIFT, 0, null, null, null);
                    break;
                case fpengine.FPM_ENROLL:
                    {
                        if (rm == 1)
                        {
                            //AppendStatus("Enrol Fingerprint Template Succeed");
                            fpengine.GetFpCharByEnl(refdata, ref refsize);
                            SendNetMessage(fpengine.FPM_ENROLL, 1, refdata, null, null);
                        }
                        else
                        {
                            //AppendStatus("Enrol Fingerprint Template Fail");
                            SendNetMessage(fpengine.FPM_ENROLL, 0, null, null, null);
                        }
                        //timer1.Enabled = false;
                    }
                    break;
                case fpengine.FPM_GENCHAR:
                    {
                        if (rm == 1)
                        {
                            //AppendStatus("Capure Fingerprint Template Succeed");
                            fpengine.GetFpCharByGen(matdata, ref matsize);
                            //SendNetMessage(fpengine.FPM_GENCHAR, 1, matdata, null, null);
                            SendNetMessage(fpengine.FPM_GENCHAR, 1, null, matdata, null);
                        }
                        else
                        {
                            //AppendStatus("Capure Fingerprint Template Fail");
                            SendNetMessage(fpengine.FPM_GENCHAR, 0, null, null, null);
                        }
                        //timer1.Enabled = false;
                    }
                    break;
                case fpengine.FPM_NEWIMAGE:
                    {
                        //System.Drawing.Bitmap fingerbmp = new Bitmap(255, 288);
                        /*
                        Graphics g = Graphics.FromImage(fingerbmp);
                        fpengine.DrawImage(g.GetHdc(), 0, 0);
                        g.Dispose();
                        pictureBoxFinger.Image = fingerbmp;
                        */
                        System.Drawing.Bitmap fingerbmp = new Bitmap(255, 288);
                        Graphics g = Graphics.FromImage(fingerbmp);
                        fpengine.DrawImage(g.GetHdc(), 0, 0);
                        g.Dispose();

                        MemoryStream ms = new MemoryStream();
                        fingerbmp.Save(ms, ImageFormat.Png);
                        ms.Position = 0;
                        byte[] image = new byte[ms.Length];
                        ms.Read(image, 0, Convert.ToInt32(ms.Length));
                        ms.Flush();

                        SendNetMessage(fpengine.FPM_NEWIMAGE, 0, null, null, image);

                        fpengine.GetImage(rawimagedata, ref rawimagesize);
                    }
                    break;
                case fpengine.FPM_TIMEOUT:
                    {
                        SendNetMessage(fpengine.FPM_TIMEOUT, 0, null, null, null);
                        //AppendStatus("Time Out");
                    }
                    break;
            }
        }

        public void olddevice()
        {
            if (lstnMsg == "enrol")
            {
                time++;
                switch (steps)
                {
                    case fpengine.FPM_DEVICE:
                        //AppendStatus("Not Open Reader");
                        SendNetMessage(fpengine.FPM_DEVICE, 0, null, null, null);
                        break;
                    case fpengine.FPM_PLACE:
                        //AppendStatus("Please Plase Finger");
                        SendNetMessage(fpengine.FPM_PLACE, 0, null, null, null);
                        steps = 6;
                        break;

                    case fpengine.FPM_ENROLL:
                        {
                            int ret = -1;
                            ret = obj.enrollbuf(ref refdata, ref rawimagedata);
                            if (ret == 0)
                            {
                                //AppendStatus("Enrol Fingerprint Template Succeed");
                                lstnMsg = "";
                                steps = 2;
                                time = 0;
                                // fpengine.GetFpCharByEnl(refdata, ref refsize);

                                System.Drawing.Bitmap fingerbmp = new Bitmap(255, 288);
                                Graphics g = Graphics.FromImage(fingerbmp);
                                fpengine.DrawImage(g.GetHdc(), 0, 0);
                                g.Dispose();

                                MemoryStream ms = new MemoryStream();
                                fingerbmp.Save(ms, ImageFormat.Png);
                                ms.Position = 0;
                                byte[] image = new byte[ms.Length];

                                ms.Read(image, 0, Convert.ToInt32(ms.Length));
                                ms.Flush();

                                //SendNetMessage(fpengine.FPM_NEWIMAGE, 0, null, refdata, image);

                                fpengine.GetImage(rawimagedata, ref rawimagesize);

                                SendNetMessage(fpengine.FPM_ENROLL, 1, refdata, rawimagedata, image);
                            }
                            else
                            {
                                if (time > 30)
                                {
                                    lstnMsg = "";
                                    steps = 2;
                                    time = 0;
                                    //AppendStatus("Enrol Fingerprint Template Fail");
                                    SendNetMessage(fpengine.FPM_ENROLL, 0, null, null, null);
                                }
                            }
                            //timer1.Enabled = false;
                        }
                        break;

                    case fpengine.FPM_NEWIMAGE:
                        {
                            //System.Drawing.Bitmap fingerbmp = new Bitmap(255, 288);
                            /*
                            Graphics g = Graphics.FromImage(fingerbmp);
                            fpengine.DrawImage(g.GetHdc(), 0, 0);
                            g.Dispose();
                            pictureBoxFinger.Image = fingerbmp;
                            */
                            System.Drawing.Bitmap fingerbmp = new Bitmap(255, 288);
                            Graphics g = Graphics.FromImage(fingerbmp);
                            fpengine.DrawImage(g.GetHdc(), 0, 0);
                            g.Dispose();

                            MemoryStream ms = new MemoryStream();
                            fingerbmp.Save(ms, ImageFormat.Png);
                            ms.Position = 0;
                            byte[] image = new byte[256 * 288];
                            ms.Read(image, 0, Convert.ToInt32(256 * 288));
                            ms.Flush();
                            byte[] buf1 = new byte[256 * 288];
                            int ret = -1;
                            ret = obj.enrollbuf(ref refdata, ref buf1);
                            if (ret == 0)
                            {
                                SendNetMessage1(fpengine.FPM_NEWIMAGE, 0, null, null, buf1);
                            }
                            else
                            {
                                if (time > 30)
                                {
                                    lstnMsg = "";
                                    steps = 2;
                                    time = 0;
                                    SendNetMessage1(fpengine.FPM_NEWIMAGE, 0, null, null, null);
                                }
                            }

                            fpengine.GetImage(buf1, ref rawimagesize);
                            steps = 6;
                        }
                        break;
                    case fpengine.FPM_TIMEOUT:
                        {
                            SendNetMessage(fpengine.FPM_TIMEOUT, 0, null, null, null);
                            //AppendStatus("Time Out");
                        }
                        break;
                }
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isclose = true;
            this.Close();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Enabled= false;
            this.Hide();
        }

        public void FirstAutoRun(bool isCurrentUser)
        {
            RegistryKey reg = null;
            try
            {
                String name = "Run";
                if (isCurrentUser)
                {
                    reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\FGTIT\BioWebServer\", true);
                    if (reg == null)
                        reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\FGTIT\BioWebServer\");
                    if (reg.GetValue(name) == null)
                    {
                        SetAutoRun(Application.ExecutablePath, true, true);
                        reg.SetValue(name, true);
                    }
                }
                else
                {
                    reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\FGTIT\BioWebServer\", true);
                    if (reg == null)
                        reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\FGTIT\BioWebServer\");
                    if (reg.GetValue(name) == null)
                    {
                        SetAutoRun(Application.ExecutablePath, true, false);
                        reg.SetValue(name, true);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                if (reg != null)
                    reg.Close();
            }
        }

        public void CheckAutoRun(string fileName, bool isCurrentUser)
        {
            RegistryKey reg = null;
            try
            {
                if (!System.IO.File.Exists(fileName))
                    throw new Exception("Not File!");
                String name = fileName.Substring(fileName.LastIndexOf(@"\") + 1);
                if (isCurrentUser)
                {
                    reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if (reg == null)
                        reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    if (reg.GetValue(name) == null)
                    {
                        autoRunToolStripMenuItem.Checked = false;
                    }
                    else
                    {
                        autoRunToolStripMenuItem.Checked = true;
                    }
                }
                else
                {
                    reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if (reg == null)
                        reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    if (reg.GetValue(name) == null)
                    {
                        autoRunToolStripMenuItem.Checked = false;
                    }
                    else
                    {
                        autoRunToolStripMenuItem.Checked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                if (reg != null)
                    reg.Close();
            }
        }

        public void SetAutoRun(string fileName, bool isAutoRun,bool isCurrentUser)
        {
            RegistryKey reg = null;
            try
            {
                if (!System.IO.File.Exists(fileName))
                    throw new Exception("Not File!");
                String name = fileName.Substring(fileName.LastIndexOf(@"\") + 1);
                if (isCurrentUser)
                {
                    reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if (reg == null)
                        reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    if (isAutoRun)
                    {
                        if(reg.GetValue(name)==null)
                        {
                            reg.SetValue(name, fileName);
                        }
                        else
                        {
                            if (!(reg.GetValue(name).Equals(fileName)))
                                reg.SetValue(name, fileName);
                        }                        
                    }
                    else
                    {
                        //reg.SetValue(name, false);
                        reg.DeleteValue(name);
                    }
                }
                else
                {
                    reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if (reg == null)
                        reg = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    if (isAutoRun)
                    {
                        if (reg.GetValue(name) == null)
                        {
                            reg.SetValue(name, fileName);
                        }
                        else
                        {
                            if (!(reg.GetValue(name).Equals(fileName)))
                                reg.SetValue(name, fileName);
                        }
                    }
                    else
                    {
                        //reg.SetValue(name, false);
                        reg.DeleteValue(name);
                    }
                }                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                if (reg != null)
                    reg.Close();
            }

        }

        private void reopenDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReOpenDevice();
        }

        private void ReOpenDevice()
        {
            this.textBox1.Clear();
            fpengine.CloseDevice();
            if (fpengine.OpenDevice(0, 0, 0) == 1)
            {
                if (fpengine.LinkDevice() == 1)
                {
                    dvCatagory = 1;
                    AppendStatus("Fingerprint Device Ready!");
                    //fpengine.EnrolFpChar();
                    //timer1.Enabled = true;
                    IsOpen = true;
                }
                else if (obj.initialization() == 0)
                {
                    dvCatagory = 0;
                    AppendStatus("Fingerprint Device Ready!");
                    IsOpen = true;
                }
                else
                {
                    AppendStatus("Link Device Fail!");
                }
            }
            else
            {
                AppendStatus("Open Device Fail!");
            }

            AppendStatus("Fingerprint Server Ready!");
        }

        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
                    {
                        timer3.Enabled= true;
                    }
                    break;
                case WM_SESSIONNEW:
                    {
                        String msg = String.Format("{0:HH:MM:ss}  {1}", DateTime.Now, "New Session");
                        AppendStatus(msg);
                    }                    
                    break;
                case WM_SESSIONCLOSE:
                    {
                        String msg = String.Format("{0:HH:MM:ss}  {1}", DateTime.Now, "Close Session");
                        AppendStatus(msg);
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        private void closeDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fpengine.CloseDevice();
            IsOpen = false;
            AppendStatus("Fingerprint Device Close!");
        }

        private void autoRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (autoRunToolStripMenuItem.Checked)
            {
                SetAutoRun(Application.ExecutablePath, false, true);
                autoRunToolStripMenuItem.Checked = false;
            }
            else
            {
                SetAutoRun(Application.ExecutablePath, true, true);
                autoRunToolStripMenuItem.Checked = true;
            }
        }

        private void timer3_Tick(object sender, EventArgs e)
       {
            timer3.Enabled = false;
            string strSer = "";

            Boolean ishave = false;
            if (USB.IsUsbDevice(0x2009, 0x7638))
            {
                ishave = true;
            }
            else if (USB.IsUsbDevice(0x2109, 0x7638))
            {
                ishave = true;
            }
            else if (USB.IsUsbDevice(0x2109, 0x7368))
            {
                ishave = true;
            }
            else if (USB.IsUsbDevice(0x0453, 0x9005))
            {
                ishave = true;
            }
            if (ishave)
            {
                if (!IsOpen)
                {
                   
                    ReOpenDevice();
                    label2.Visible = true;
                    UInt32 serial = fpengine.GetDeviceSnNum();

                    if (serial == 0)
                    {
                        string binaryPath =
                            System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        string text = System.IO.File.ReadAllText(binaryPath + "\\MiT_license.txt");
                        strSer = text.ToString();
                    }
                    else
                    {
                        strSer = serial.ToString();
                    }
                    //TextReader tr = new StreamReader(@"myfile.txt");
                    //string myText = tr.ReadLine();
                    
                    textBox2.Text = strSer;
                }
            }
            else
            {
                if (IsOpen)
                {
                   // label2.Visible = false;
                    textBox2.Text = "";
                    fpengine.CloseDevice();
                    IsOpen = false;
                    AppendStatus("Fingerprint Device Close!");
                }
            }
        }

        private void deviceImageConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fpengine.CloseDevice();
            IsOpen = false;
            AppendStatus("Fingerprint Device Close!");

            ReOpenDevice();

            int imgfmt = 1;
            int tpfmt = 0;

            fpengine.GetUpConfig(ref imgfmt, ref tpfmt);

            Form2 form2 = new Form2();
            form2.setImageFormat(imgfmt);
            if (form2.ShowDialog(this) == DialogResult.OK)
            {
                imgfmt = form2.getImageFormat();

                tpfmt = 0;
                fpengine.SetUpConfig(imgfmt, tpfmt);
            }            
        }

        private void PassKeyStripMenuItem3_Click(object sender, EventArgs e)
        {
            frmPassKey frmPassKey = new frmPassKey();
            frmPassKey.ShowDialog();
        }

        private void tagDeviceMenu_Click(object sender, EventArgs e)
        {
            //websocketServer.TagDevice();
        }
        #region 
        //Developed by Porosh @ 06-03-2022
        private void btnWebCam_Click(object sender, EventArgs e)
        {
                camera = new VideoCapture(camera_id);
            if (camera.IsOpened)
            {
                if(capturer == null)
                {
                    capturer = service.createCapturer(capturerConfig);
                    Application.Idle += GetFrame;
                    pictureBox1.Visible = true;
                    btnWebCam.Enabled = false;
                    button1.Visible = true;
                    string[] args = new string[5];
                    args[0] = "--config_dir";
                    args[1] = @"E:\Face Porosh\example\csharp\video_recognition_demo\BioWebServer v3.0\BioWebServer\conf\facerec";
                    args[2] = "--database_dir";
                    args[3] = "../../bin/base";
                    //// args[3] = @"E:\Face Porosh\example\csharp\video_recognition_demo\BioWebServer v3.0\BioWebServer\bin\base";
                    args[4] = "0";
                    VideoRecognitionDemo.Main(args, service, camera,capturer);

                    //// StartServer();


                    //string m = faceDec(args);

                    //string[] b = MyConstants.MyFirstConstant;
                    //int x = MyConstants.cnt;
                    //string k = "";
                   
                    //TestRecognition(service,capturer, args, capturerConfig);
                }
                //string[] args = new string[5];
                //args[0] = "--config_dir";
                //args[1] = @"E:\Face Porosh\example\csharp\video_recognition_demo\BioWebServer v3.0\BioWebServer\conf\facerec";
                //args[2] = "--database_dir";
                //args[3] = "../../bin/base";
                //// args[3] = @"E:\Face Porosh\example\csharp\video_recognition_demo\BioWebServer v3.0\BioWebServer\bin\base";
                //args[4] = "0";
                
                //TestRecognition(service, capturer, args, capturerConfig,camera);

            }
            //string[] args = new string[5];
            //args[0] = "--config_dir";
            //args[1] = @"E:\Face Porosh\example\csharp\video_recognition_demo\BioWebServer v3.0\BioWebServer\conf\facerec";
            //args[2] = "--database_dir";
            //args[3] = "../../bin/base";
            //// args[3] = @"E:\Face Porosh\example\csharp\video_recognition_demo\BioWebServer v3.0\BioWebServer\bin\base";
            //args[4] = "0";
            //TestRecognition(service, capturer, args, capturerConfig);
            //pic.Visible = true;

            //videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cboCamera.SelectedIndex].MonikerString);
            //videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
            //videoCaptureDevice.Start();
        }
        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            pic.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //videoCaptureDevice.Stop();
            //pic.Visible = false;
            capturer.Dispose();
            camera.Dispose();
            camera.Stop();
            service.Dispose();
            pictureBox1.Visible = false;
            button1.Visible = false;
            btnWebCam.Enabled = true;
            camera = null;
            capturer = null;
            service = null;
            //camera.Dispose();
             service = FacerecService.createService(faceSDKRootDir + "\\conf\\facerec", @"E:\\Face Porosh\\license");
             capturerConfig = new FacerecService.Config("fda_tracker_capturer_uld.xml");
            // capturer = service.createCapturer(capturerConfig);
            // service.Dispose();
            // Application.Idle += GetFrame;

        }


        
        private void GetFrame(object sender, EventArgs e)
        {
            if (camera!=null)
            {
                Image<Bgr, byte> image = camera.QueryFrame().ToImage<Bgr, byte>();
                drawDetections(image);
                pictureBox1.Image = image.ToBitmap();
            }
           
        }
        private void GetFrameStop(object sender, EventArgs e)
        {
            camera.Dispose();

            pictureBox1.Visible = false;
        }

        private void drawDetections(Image<Bgr, byte> image)
        {
            Mat frame_m = image.Mat.Clone();
            byte[] data = new byte[frame_m.Total.ToInt32() * frame_m.NumberOfChannels];
            Marshal.Copy(frame_m.DataPointer, data, 0, (int)data.Length);
            RawImage ri_frame = new RawImage(frame_m.Width, frame_m.Height, RawImage.Format.FORMAT_BGR, data);
            List<RawSample> detected = capturer.capture(ri_frame);
            foreach (RawSample sample in detected)
            {
                RawSample.Rectangle rect = sample.getRectangle();
                image.Draw(new Rectangle((int)rect.x,
                                         (int)rect.y,
                                         (int)rect.width,
                                         (int)rect.height),
                           new Bgr(0, 255, 0),
                           2);
            }
        }
        #endregion

        #region
        //for  face reccognition
        //Developed By Porosh 06-march-2022
        public static class MyConstants
        {
            public static string[] MyFirstConstant = new string[15];
            public static int cnt = 0;
            public static double[] livescore = new double[15];
            public static string[] matchedwith = new string[15];
        }


       public class Options
        {
            [Option("config_dir", Default = "../../../conf/facerec", HelpText = "Path to config directory.")]
            public string config_dir { get; set; }




            [Option("license_dir", Default = null, HelpText = "Path to license directory [optional].")]
            public string license_dir { get; set; }

            [Option("database_dir", Default = "../../base", HelpText = "Path to database directory.")]
            public string database_dir { get; set; }

            [Option("method_config", Default = "method6v7_recognizer.xml", HelpText = "Recognizer config file.")]
            public string method_config { get; set; }

            [Option("recognition_distance_threshold", Default = 7000.0f, HelpText = "Recognition distance threshold.")]
            public float recognition_distance_threshold { get; set; }

            [Option("frame_fps_limit", Default = 25f, HelpText = "Frame fps limit.")]
            public float frame_fps_limit { get; set; }

            [Value(0, MetaName = "video_sources", HelpText = "List of video sources (id of web-camera, url of rtsp stream or path to video file)")]
            public IEnumerable<string> video_sources { get; set; }
        };

       public class VideoRecognitionDemo
        {
            public static FacerecService svc;
            public static FacerecService service;
            public static Recognizer recognizer;
            public static Capturer capturer;
            public static Database database;
            public static VideoWorker video_worker;
            public static Liveness2DEstimator _liveness_2d_estimator;

            public static int Main(string[] args, FacerecService service,VideoCapture capture,Capturer capturer )
            {
                svc = service;
                 //StartServer(args);

                string m = faceDec(args,capture/*,capturer*/,service);

                string[] b = MyConstants.MyFirstConstant;
                int x = MyConstants.cnt;
                string k = "";
                return 0;
            }

            public static string faceDec(string[] args,VideoCapture capture/*, Capturer capturer*/, FacerecService service)
            {
                try
                {

                    // print usage
                    Console.WriteLine("Usage: dotnet csharp_video_recognition_demo.dll [OPTIONS] <video_source>...");
                    Console.WriteLine("Examples:");
                    Console.WriteLine("    Webcam:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec 0");
                    Console.WriteLine("    RTSP stream:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec rtsp://localhost:8554/");
                    Console.WriteLine("");

                    // parse arguments
                    bool error = false;
                    Options options = new Options();
                    CommandLine.Parser.Default.ParseArguments<Options>(args)
                        .WithParsed<Options>(opts => options = opts)
                        .WithNotParsed<Options>(errs => error = true);

                    // exit if argument parsign error
                    if (error) return "";

                    // print values of arguments
                    Console.WriteLine("Arguments:");
                    foreach (var opt in options.GetType().GetProperties())
                    {
                        if (opt.Name == "video_sources")
                        {
                            Console.Write("video sources = ");
                            foreach (string vs in options.video_sources)
                            {
                                Console.Write(vs + " ");
                            }
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine("--{0} = {1}", opt.Name, opt.GetValue(options, null));
                        }
                    }
                    Console.WriteLine("\n");

                    //parameters parse
                    string config_dir = options.config_dir;
                    string license_dir = @"E:\Face3 - Copy - Copy\3_10_0\license";
                    //string database_dir = options.database_dir;
                    string database_dir = @"E:\Face3 - Copy - Copy\3_10_0\bin\base";
                    string method_config = options.method_config;
                    float recognition_distance_threshold = options.recognition_distance_threshold;
                    float frame_fps_limit = options.frame_fps_limit;
                    List<string> video_sources = new List<string>(options.video_sources);

                    // check params
                    MAssert.Check(config_dir != string.Empty, "Error! config_dir is empty.");
                    MAssert.Check(database_dir != string.Empty, "Error! database_dir is empty.");
                    MAssert.Check(method_config != string.Empty, "Error! method_config is empty.");
                    MAssert.Check(recognition_distance_threshold > 0, "Error! Failed recognition distance threshold.");

                    List<ImageAndDepthSource> sources = new List<ImageAndDepthSource>();
                    List<string> sources_names = new List<string>();


                    MAssert.Check(video_sources.Count > 0, "Error! video_sources is empty.");

                    for (int i = 0; i < video_sources.Count; i++)
                    {
                        sources_names.Add(string.Format("OpenCvS source {0}", i));
                        sources.Add(new OpencvSource(video_sources[i]/*,capture*/));
                    }


                    MAssert.Check(sources_names.Count == sources.Count);

                    // print sources
                    Console.WriteLine("\n{0} sources: ", sources.Count);

                    for (int i = 0; i < sources_names.Count; ++i)
                        Console.WriteLine("  {0}", sources_names[i]);
                    Console.WriteLine("");

                    if (svc == null) {
                        // create facerec servcie
                        FacerecService service =
                            FacerecService.createService(
                                config_dir,
                                license_dir);
                    }



                    Console.WriteLine("Library version: {0}\n", svc.getVersion());

                    // create database
                    if (recognizer != null) { recognizer = null; }
                   /* Recognizer*/ recognizer = svc.createRecognizer(method_config, true, false, false);
                    if (capturer != null) { capturer = null; }
                    /*Capturer */
                    capturer = svc.createCapturer("common_capturer4_lbf_singleface.xml");
                    Database database = new Database(
                        database_dir,
                        recognizer,
                        capturer,
                        recognition_distance_threshold);
                    recognizer.Dispose();
                    capturer.Dispose();

                    FacerecService.Config vw_config = new FacerecService.Config("video_worker_fdatracker_blf_fda.xml");
                    // vw_config.overrideParameter("single_match_mode", 1);
                    vw_config.overrideParameter("search_k", 10);
                    vw_config.overrideParameter("not_found_match_found_callback", 1);
                    vw_config.overrideParameter("downscale_rawsamples_to_preferred_size", 0);

                    //ActiveLiveness.CheckType[] checks = new ActiveLiveness.CheckType[3]
                    //{
                    //	ActiveLiveness.CheckType.BLINK,
                    //			ActiveLiveness.CheckType.TURN_RIGHT,
                    //			ActiveLiveness.CheckType.SMILE
                    //};


                    // create one VideoWorker
                    if (video_worker != null) { video_worker.Dispose(); }
                    /*VideoWorker*/
                    video_worker =
                        svc.createVideoWorker(
                            new VideoWorker.Params()
                                .video_worker_config(vw_config)
                                .recognizer_ini_file(method_config)
                                .streams_count(sources.Count)
                                //.age_gender_estimation_threads_count(sources.Count)
                                //.emotions_estimation_threads_count(sources.Count)
                                //.active_liveness_checks_order(checks)
                                .processing_threads_count(sources.Count)
                                .matching_threads_count(sources.Count));

                    // set database
                    video_worker.setDatabase(database.vwElements, Recognizer.SearchAccelerationType.SEARCH_ACCELERATION_1);

                    //for (int i = 0; i < sources_names.Count; ++i)
                    //{
                    //    OpenCvSharp.Window window = new OpenCvSharp.Window(sources_names[i]);

                    //    OpenCvSharp.Cv2.ImShow(sources_names[i], new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
                    //}

                    // prepare buffers for store drawed results
                    Mutex draw_images_mutex = new Mutex();
                    List<OpenCvSharp.Mat> draw_images = new List<OpenCvSharp.Mat>(sources.Count);

                    // create one worker per one source
                    List<Worker> workers = new List<Worker>();
                    // FacerecService facerecService = FacerecService.createService(options.config_dir, options.license_dir);
                    if (_liveness_2d_estimator != null) { /*_liveness_2d_estimator = null;*/
                        svc = null;
                        //if (svc == null)
                        //{
                        //    // create facerec servcie
                        //    svc =
                        //        FacerecService.createService(
                        //            config_dir,
                        //            license_dir);
                        //}
                        svc.forceOnlineLicenseUpdate();
                       // _liveness_2d_estimator = svc.createLiveness2DEstimator("liveness_2d_estimator_v2.xml");
                    }
                    else
                    {
                        _liveness_2d_estimator = svc.createLiveness2DEstimator("liveness_2d_estimator_v2.xml");
                    }
                  //  /*Liveness2DEstimator*/ _liveness_2d_estimator = svc.createLiveness2DEstimator("liveness_2d_estimator_v2.xml");





                    for (int i = 0; i < sources.Count; ++i)
                    {
                        string val = "";
                        draw_images.Add(new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
                        workers.Add(new Worker(
                            database,
                            video_worker,
                            sources[i],
                            i,  // stream_id
                            draw_images_mutex,
                            draw_images[i],
                            frame_fps_limit, _liveness_2d_estimator, val/*, svc, capturer, capture*/
                            ));


                    }

                    // draw results until escape presssed

                    for (; ; )
                    {

                        {
                            draw_images_mutex.WaitOne();
                            for (int i = 0; i < draw_images.Count; ++i)
                            {
                                OpenCvSharp.Mat drawed_im = workers[i]._draw_image;
                                if (!drawed_im.Empty())
                                {
                                    OpenCvSharp.Cv2.ImShow(sources_names[i], drawed_im);

                                    draw_images[i] = new OpenCvSharp.Mat();
                                    if (MyConstants.cnt >= 15)
                                    {
                                        foreach (Worker w in workers)
                                        {
                                           // service.Dispose();
                                            //video_worker.Dispose(); 
                                            //w.Dispose();
                                            //video_worker.Dispose();
                                            //Main(args);
                                            int s = w._match_found_callback_id;
                                            capturer.Dispose();
                                           // w.Dispose();
                                            //string test = w.liveness_2d_res_with_score.ToString();
                                        }
                                        //break;
                                    }
                                    else
                                    {
                                        MyConstants.cnt += 1;
                                    }

                                }
                               // i += 1;

                            }
                            draw_images_mutex.ReleaseMutex();


                        }

                        int key = OpenCvSharp.Cv2.WaitKey(20);
                        if (27 == key)
                        {
                            foreach (Worker w in workers)
                            {
                                w.Dispose();
                            }
                            break;
                        }

                        if (' ' == key)
                        {
                            Console.WriteLine("enable processing 0");
                            video_worker.enableProcessingOnStream(0);
                        }

                        if (13 == key)
                        {
                            Console.WriteLine("disable processing 0");
                            video_worker.disableProcessingOnStream(0);
                        }


                        if ('r' == key)
                        {
                            Console.WriteLine("reset trackerOnStream");
                            video_worker.resetTrackerOnStream(0);
                        }


                        // check exceptions in callbacks
                        video_worker.checkExceptions();
                    }

                    //StartServer(args);


                    // force free resources
                    // otherwise licence error may occur
                    // when create sdk object in next time 
                    svc.Dispose();
                    video_worker.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine("video_recognition_show exception catched: '{0}'", e.ToString());
                    return "Exception";
                }


                return "";


            }

            public static string matchedWith(string matched)
            {

                //for (int i = 0; i < 15; i++) {
                int occurrences = MyConstants.matchedwith.Count(x => x == matched);
                reload(occurrences, matched);
                //}
                Console.WriteLine("************************");
                Console.WriteLine(matched);
                return matched;
            }

            public static void reload(int occurances, string matched)
            {
                double avgliveness = 0.0;
                string livenessstring = "";
                string matchedwith = "";
                for (int i = 0; i < MyConstants.matchedwith.Length; i++)
                {
                    avgliveness += MyConstants.livescore[i];

                    if (i == (MyConstants.livescore.Length - 1))
                    {

                        if ((avgliveness / 15) > 0.91)
                        {
                            if (occurances >= 13)
                            {
                                livenessstring = "Real" + " and matched with \t" + matched;
                                Console.WriteLine("****************##############");
                                Console.WriteLine("REAL");
                                Console.WriteLine("****************##############");

                                Console.WriteLine(livenessstring);
                            }
                            else
                            {
                                livenessstring = "Real" + " but not matched  ";
                                Console.WriteLine(livenessstring);
                            }
                        }
                        else
                        {
                            if (occurances >= 13)
                            {
                                livenessstring = "Fake" + " matched with \t" + matched;
                                Console.WriteLine(livenessstring);
                            }
                            else
                            {
                                livenessstring = "Fake" + " and not matched";
                                Console.WriteLine(livenessstring);
                            }

                        }
                    }

                }
            }


            public static async void StartServer(string[] args)
            {
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

                try
                {

                    // Create a Socket that will use Tcp protocol
                    Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    // A Socket must be associated with an endpoint using the Bind method
                    listener.Bind(localEndPoint);
                    // Specify how many requests a Socket can listen before it gives Server busy response.
                    // We will listen 15 requests at a time
                    listener.Listen(15);
                    //StartClient();
                    Console.WriteLine("Waiting for a connection...");

                    //string m = faceDec(args,capture);

                    string[] b = MyConstants.MyFirstConstant;
                    int x = MyConstants.cnt;
                    string k = "";

                    Socket handler = listener.Accept();

                    // Incoming data from the client.
                    string data = null;
                    byte[] bytes = null;

                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOF>") > -1)
                        {
                            break;
                        }
                    }

                    Console.WriteLine("Text received : {0}", data);

                    byte[] msg = Encoding.ASCII.GetBytes(data);
                    handler.Send(msg);
                    //handler.Shutdown(SocketShutdown.Both);
                    //handler.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine("\n Press any key to continue...");
                Console.ReadKey();
            }

            public static void StartClient()
            {
                byte[] bytes = new byte[1024];

                try
                {
                    // Connect to a Remote server
                    // Get Host IP Address that is used to establish a connection
                    // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                    // If a host has multiple addresses, you will get a list of addresses
                    IPHostEntry host = Dns.GetHostEntry("localhost");
                    IPAddress ipAddress = host.AddressList[0];
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                    // Create a TCP/IP  socket.
                    Socket sender = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Connect the socket to the remote endpoint. Catch any errors.
                    try
                    {
                        // Connect to Remote EndPoint
                        sender.Connect(remoteEP);

                        Console.WriteLine("Socket connected to {0}",
                            sender.RemoteEndPoint.ToString());

                        // Encode the data string into a byte array.
                        byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");

                        // Send the data through the socket.
                        int bytesSent = sender.Send(msg);

                        // Receive the response from the remote device.
                        int bytesRec = sender.Receive(bytes);
                        Console.WriteLine("Echoed test = {0}",
                            Encoding.ASCII.GetString(bytes, 0, bytesRec));

                        // Release the socket.
                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();

                    }
                    catch (ArgumentNullException ane)
                    {
                        Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("SocketException : {0}", se.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public async void TestRecognition(FacerecService facerecService,Capturer capturer, string[] args, FacerecService.Config config, VideoCapture camera)
        {
            try
            {

                // print usage
                Console.WriteLine("Usage: dotnet csharp_video_recognition_demo.dll [OPTIONS] <video_source>...");
                Console.WriteLine("Examples:");
                Console.WriteLine("    Webcam:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec 0");
                Console.WriteLine("    RTSP stream:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec rtsp://localhost:8554/");
                Console.WriteLine("");

                // parse arguments
                bool error = false;
                Options options = new Options();
                CommandLine.Parser.Default.ParseArguments<Options>(args)
                    .WithParsed<Options>(opts => options = opts)
                    .WithNotParsed<Options>(errs => error = true);

                // exit if argument parsign error
                if (error)
                {

                }
                else {
                    // print values of arguments
                    Console.WriteLine("Arguments:");
                    foreach (var opt in options.GetType().GetProperties())
                    {
                        if (opt.Name == "video_sources")
                        {
                            Console.Write("video sources = ");
                            foreach (string vs in options.video_sources)
                            {
                                Console.Write(vs + " ");
                            }
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine("--{0} = {1}", opt.Name, opt.GetValue(options, null));
                        }
                    }
                    Console.WriteLine("\n");

                    //parameters parse
                   // string config_dir = options.config_dir;
                    string config_dir = @"E:\Face3 - Copy - Copy\3_10_0\conf\facerec";
                    string license_dir = @"E:\Face3 - Copy - Copy\3_10_0\license";
                    //string database_dir = options.database_dir;
                    string database_dir = @"E:\Face3 - Copy - Copy\3_10_0\bin\base";
                    string method_config = options.method_config;
                    float recognition_distance_threshold = options.recognition_distance_threshold;
                    float frame_fps_limit = options.frame_fps_limit;
                    List<string> video_sources = new List<string>(options.video_sources);

                    // check params
                    MAssert.Check(config_dir != string.Empty, "Error! config_dir is empty.");
                    MAssert.Check(database_dir != string.Empty, "Error! database_dir is empty.");
                    MAssert.Check(method_config != string.Empty, "Error! method_config is empty.");
                    MAssert.Check(recognition_distance_threshold > 0, "Error! Failed recognition distance threshold.");

                    List<ImageAndDepthSource> sources = new List<ImageAndDepthSource>();
                    List<string> sources_names = new List<string>();


                    MAssert.Check(video_sources.Count > 0, "Error! video_sources is empty.");

                    for (int i = 0; i < video_sources.Count; i++)
                    {
                        sources_names.Add(string.Format("OpenCvS source {0}", i));
                        sources.Add(new OpencvSource(video_sources[i]/*,camera*/));
                    }


                    MAssert.Check(sources_names.Count == sources.Count);

                    // print sources
                    Console.WriteLine("\n{0} sources: ", sources.Count);

                    for (int i = 0; i < sources_names.Count; ++i)
                        Console.WriteLine("  {0}", sources_names[i]);
                    Console.WriteLine("");

                   // create facerec servcie
                   FacerecService service =
                       FacerecService.createService(
                           config_dir,
                           license_dir);

                    Console.WriteLine("Library version: {0}\n", service.getVersion());

                    // create database

                    Recognizer recognizer = service.createRecognizer(method_config, true, false, false);
                    // Capturer capturer = service.createCapturer("common_capturer4_lbf_singleface.xml");
                    Database database = new Database(
                        database_dir,
                        recognizer,
                        capturer,
                        recognition_distance_threshold);
                    recognizer.Dispose();
                    capturer.Dispose();

                    //FacerecService.Config vw_config = config;
                    FacerecService.Config vw_config = new FacerecService.Config("video_worker_fdatracker_blf_fda.xml");
                    // vw_config.overrideParameter("single_match_mode", 1);
                    vw_config.overrideParameter("search_k", 10);
                    vw_config.overrideParameter("not_found_match_found_callback", 1);
                    vw_config.overrideParameter("downscale_rawsamples_to_preferred_size", 0);

                    //ActiveLiveness.CheckType[] checks = new ActiveLiveness.CheckType[3]
                    //{
                    //	ActiveLiveness.CheckType.BLINK,
                    //			ActiveLiveness.CheckType.TURN_RIGHT,
                    //			ActiveLiveness.CheckType.SMILE
                    //};


                    // create one VideoWorker
                    VideoWorker video_worker =
                        service.createVideoWorker(
                            new VideoWorker.Params()
                                .video_worker_config(vw_config)
                                .recognizer_ini_file(method_config)
                                .streams_count(sources.Count)
                                //.age_gender_estimation_threads_count(sources.Count)
                                //.emotions_estimation_threads_count(sources.Count)
                                //.active_liveness_checks_order(checks)
                                .processing_threads_count(sources.Count)
                                .matching_threads_count(sources.Count));

                    // set database
                    video_worker.setDatabase(database.vwElements, Recognizer.SearchAccelerationType.SEARCH_ACCELERATION_1);

                    for (int i = 0; i < sources_names.Count; ++i)
                    {
                        OpenCvSharp.Window window = new OpenCvSharp.Window(sources_names[i]);

                        OpenCvSharp.Cv2.ImShow(sources_names[i], new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
                    }

                    // prepare buffers for store drawed results
                    Mutex draw_images_mutex = new Mutex();
                    List<OpenCvSharp.Mat> draw_images = new List<OpenCvSharp.Mat>(sources.Count);

                    // create one worker per one source
                    List<Worker> workers = new List<Worker>();
                      facerecService = FacerecService.createService(options.config_dir, options.license_dir);

                    Liveness2DEstimator _liveness_2d_estimator = facerecService.createLiveness2DEstimator("liveness_2d_estimator_v2.xml");





                    for (int i = 0; i < sources.Count; ++i)
                    {
                        string val = "";
                       // draw_images.Add(new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
                        workers.Add(new Worker(
                            database,
                            video_worker,
                            sources[i],
                            i,  // stream_id
                            draw_images_mutex,
                            draw_images[i],
                            frame_fps_limit, _liveness_2d_estimator, val/*, service,capturer,camera*/
                            ));


                    }

                    // draw results until escape presssed

                    for (; ; )
                    {

                        {
                            draw_images_mutex.WaitOne();
                            for (int i = 0; i < draw_images.Count; ++i)
                            {
                                OpenCvSharp.Mat drawed_im = workers[i]._draw_image;
                                if (!drawed_im.Empty())
                                {
                                   // OpenCvSharp.Cv2.ImShow(sources_names[i], drawed_im);
                                   

                                    draw_images[i] = new OpenCvSharp.Mat();
                                    if (MyConstants.cnt >= 15)
                                    {
                                        foreach (Worker w in workers)
                                        {
                                            //service.Dispose();
                                            //video_worker.Dispose(); 
                                            //w.Dispose();
                                            //video_worker.Dispose();
                                            //Main(args);
                                            int s = w._match_found_callback_id;

                                            //string test = w.liveness_2d_res_with_score.ToString();
                                        }
                                        //break;
                                    }
                                    //else
                                    //{
                                    //    MyConstants.cnt += 1;
                                    //}

                                }
                                //i += 1;

                            }
                            draw_images_mutex.ReleaseMutex();


                        }

                        int key = OpenCvSharp.Cv2.WaitKey(20);
                        if (27 == key)
                        {
                            foreach (Worker w in workers)
                            {
                                w.Dispose();
                            }
                            break;
                        }

                        if (' ' == key)
                        {
                            Console.WriteLine("enable processing 0");
                            video_worker.enableProcessingOnStream(0);
                        }

                        if (13 == key)
                        {
                            Console.WriteLine("disable processing 0");
                            video_worker.disableProcessingOnStream(0);
                        }


                        if ('r' == key)
                        {
                            Console.WriteLine("reset trackerOnStream");
                            video_worker.resetTrackerOnStream(0);
                        }


                        // check exceptions in callbacks
                        video_worker.checkExceptions();
                    }

                    //StartServer(args);


                    // force free resources
                    // otherwise licence error may occur
                    // when create sdk object in next time 
                    facerecService.Dispose();
                    video_worker.Dispose();
                }
              
            }
            catch (Exception e)
            {
               
            }

        }
        public static string faceDec(string[] args,VideoCapture camera)
        {
            try
            {

                // print usage
                Console.WriteLine("Usage: dotnet csharp_video_recognition_demo.dll [OPTIONS] <video_source>...");
                Console.WriteLine("Examples:");
                Console.WriteLine("    Webcam:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec 0");
                Console.WriteLine("    RTSP stream:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec rtsp://localhost:8554/");
                Console.WriteLine("");

                // parse arguments
                bool error = false;
                Options options = new Options();
                CommandLine.Parser.Default.ParseArguments<Options>(args)
                    .WithParsed<Options>(opts => options = opts)
                    .WithNotParsed<Options>(errs => error = true);

                // exit if argument parsign error
                if (error) return "";

                // print values of arguments
                Console.WriteLine("Arguments:");
                foreach (var opt in options.GetType().GetProperties())
                {
                    if (opt.Name == "video_sources")
                    {
                        Console.Write("video sources = ");
                        foreach (string vs in options.video_sources)
                        {
                            Console.Write(vs + " ");
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("--{0} = {1}", opt.Name, opt.GetValue(options, null));
                    }
                }
                Console.WriteLine("\n");

                //parameters parse
                string config_dir = options.config_dir;
                string license_dir = @"E:\Face3 - Copy - Copy\3_10_0\license";
                //string database_dir = options.database_dir;
                string database_dir = @"E:\Face3 - Copy - Copy\3_10_0\bin\base";
                string method_config = options.method_config;
                float recognition_distance_threshold = options.recognition_distance_threshold;
                float frame_fps_limit = options.frame_fps_limit;
                List<string> video_sources = new List<string>(options.video_sources);

                // check params
                MAssert.Check(config_dir != string.Empty, "Error! config_dir is empty.");
                MAssert.Check(database_dir != string.Empty, "Error! database_dir is empty.");
                MAssert.Check(method_config != string.Empty, "Error! method_config is empty.");
                MAssert.Check(recognition_distance_threshold > 0, "Error! Failed recognition distance threshold.");

                List<ImageAndDepthSource> sources = new List<ImageAndDepthSource>();
                List<string> sources_names = new List<string>();


                MAssert.Check(video_sources.Count > 0, "Error! video_sources is empty.");

                for (int i = 0; i < video_sources.Count; i++)
                {
                    sources_names.Add(string.Format("OpenCvS source {0}", i));
                   // sources.Add(new OpencvSource(video_sources[i]));
                }


                MAssert.Check(sources_names.Count == sources.Count);

                // print sources
                Console.WriteLine("\n{0} sources: ", sources.Count);

                for (int i = 0; i < sources_names.Count; ++i)
                    Console.WriteLine("  {0}", sources_names[i]);
                Console.WriteLine("");

               // create facerec servcie
               FacerecService service =
                   FacerecService.createService(
                       config_dir,
                       license_dir);

                Console.WriteLine("Library version: {0}\n", service.getVersion());

                // create database

                Recognizer recognizer = service.createRecognizer(method_config, true, false, false);
                Capturer capturer = service.createCapturer("common_capturer4_lbf_singleface.xml");
                Database database = new Database(
                    database_dir,
                    recognizer,
                    capturer,
                    recognition_distance_threshold);
                recognizer.Dispose();
                capturer.Dispose();
                
                FacerecService.Config vw_config = new FacerecService.Config("video_worker_fdatracker_blf_fda.xml");
                // vw_config.overrideParameter("single_match_mode", 1);
                vw_config.overrideParameter("search_k", 10);
                vw_config.overrideParameter("not_found_match_found_callback", 1);
                vw_config.overrideParameter("downscale_rawsamples_to_preferred_size", 0);

                //ActiveLiveness.CheckType[] checks = new ActiveLiveness.CheckType[3]
                //{
                //	ActiveLiveness.CheckType.BLINK,
                //			ActiveLiveness.CheckType.TURN_RIGHT,
                //			ActiveLiveness.CheckType.SMILE
                //};


                // create one VideoWorker
                VideoWorker video_worker =
                    service.createVideoWorker(
                        new VideoWorker.Params()
                            .video_worker_config(vw_config)
                            .recognizer_ini_file(method_config)
                            .streams_count(sources.Count)
                            //.age_gender_estimation_threads_count(sources.Count)
                            //.emotions_estimation_threads_count(sources.Count)
                            //.active_liveness_checks_order(checks)
                            .processing_threads_count(sources.Count)
                            .matching_threads_count(sources.Count));

                // set database
                video_worker.setDatabase(database.vwElements, Recognizer.SearchAccelerationType.SEARCH_ACCELERATION_1);

                for (int i = 0; i < sources_names.Count; ++i)
                {
                    OpenCvSharp.Window window = new OpenCvSharp.Window(sources_names[i]);

                    OpenCvSharp.Cv2.ImShow(sources_names[i], new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
                }

                // prepare buffers for store drawed results
                Mutex draw_images_mutex = new Mutex();
                List<OpenCvSharp.Mat> draw_images = new List<OpenCvSharp.Mat>(sources.Count);

                // create one worker per one source
                List<Worker> workers = new List<Worker>();
                 FacerecService facerecService = FacerecService.createService(options.config_dir, options.license_dir);
               
                Liveness2DEstimator _liveness_2d_estimator = facerecService.createLiveness2DEstimator("liveness_2d_estimator_v2.xml");





                for (int i = 0; i < sources.Count; ++i)
                {
                    string val = "";
                   // draw_images.Add(new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
                    workers.Add(new Worker(
                        database,
                        video_worker,
                        sources[i],
                        i,  // stream_id
                        draw_images_mutex,
                        draw_images[i],
                        frame_fps_limit, _liveness_2d_estimator, val/*, facerecService,capturer,camera*/
                        ));


                }

                // draw results until escape presssed

                for (; ; )
                {

                    {
                        draw_images_mutex.WaitOne();
                        for (int i = 0; i < draw_images.Count; ++i)
                        {
                            OpenCvSharp.Mat drawed_im = workers[i]._draw_image;
                            if (!drawed_im.Empty())
                            {
                                OpenCvSharp.Cv2.ImShow(sources_names[i], drawed_im);

                                draw_images[i] = new OpenCvSharp.Mat();
                                if (MyConstants.cnt >= 15)
                                {
                                    foreach (Worker w in workers)
                                    {
                                        //service.Dispose();
                                        //video_worker.Dispose(); 
                                        //w.Dispose();
                                        //video_worker.Dispose();
                                        //Main(args);
                                        int s = w._match_found_callback_id;

                                        //string test = w.liveness_2d_res_with_score.ToString();
                                    }
                                    //break;
                                }
                                //else
                                //{
                                //    MyConstants.cnt += 1;
                                //}

                            }
                            //i += 1;

                        }
                        draw_images_mutex.ReleaseMutex();


                    }

                    int key = OpenCvSharp.Cv2.WaitKey(20);
                    if (27 == key)
                    {
                        foreach (Worker w in workers)
                        {
                            w.Dispose();
                        }
                        break;
                    }

                    if (' ' == key)
                    {
                        Console.WriteLine("enable processing 0");
                        video_worker.enableProcessingOnStream(0);
                    }

                    if (13 == key)
                    {
                        Console.WriteLine("disable processing 0");
                        video_worker.disableProcessingOnStream(0);
                    }


                    if ('r' == key)
                    {
                        Console.WriteLine("reset trackerOnStream");
                        video_worker.resetTrackerOnStream(0);
                    }


                    // check exceptions in callbacks
                    video_worker.checkExceptions();
                }

                //StartServer(args);


                // force free resources
                // otherwise licence error may occur
                // when create sdk object in next time 
                facerecService.Dispose();
                video_worker.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("video_recognition_show exception catched: '{0}'", e.ToString());
                return "Exception";
            }


            return "";


        }

        public static string matchedWith(string matched)
        {

            //for (int i = 0; i < 15; i++) {
            int occurrences = MyConstants.matchedwith.Count(x => x == matched);
            reload(occurrences, matched);
            //}
            Console.WriteLine("########################");
            Console.WriteLine(matched);
            return matched;
        }

        public static void reload(int occurances, string matched)
        {
            double avgliveness = 0.0;
            string livenessstring = "";
            string matchedwith = "";
            for (int i = 0; i < MyConstants.matchedwith.Length; i++)
            {
                avgliveness += MyConstants.livescore[i];

                if (i == (MyConstants.livescore.Length - 1))
                {

                    if ((avgliveness / 15) > 0.91)
                    {
                        if (occurances >= 13)
                        {
                            livenessstring = "Real" + " and matched with " + matched;
                            Console.WriteLine("##############");
                            Console.WriteLine("REAL");
                            Console.WriteLine("##############");

                            Console.WriteLine(livenessstring);
                        }
                        else
                        {
                            livenessstring = "Real" + "but not matched";
                            Console.WriteLine(livenessstring);
                        }
                    }
                    else
                    {
                        if (occurances >= 13)
                        {
                            livenessstring = "Fake" + " matched with" + matched;
                            Console.WriteLine(livenessstring);
                        }
                        else
                        {
                            livenessstring = "Fake" + " and not matched";
                            Console.WriteLine(livenessstring);
                        }

                    }
                }

            }
        }


        public static async void StartServer()
        {
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            try
            {

                // Create a Socket that will use Tcp protocol
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 15 requests at a time
                listener.Listen(15);
                //StartClient();
                Console.WriteLine("Waiting for a connection...");

                //string m = faceDec(args);

                //string[] b = MyConstants.MyFirstConstant;
                //int x = MyConstants.cnt;
                //string k = "";

                Socket handler = listener.Accept();

                // Incoming data from the client.
                string data = null;
                byte[] bytes = null;

                while (true)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                }

                Console.WriteLine("Text received : {0}", data);

                byte[] msg = Encoding.ASCII.GetBytes(data);
                handler.Send(msg);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }
        #endregion
    }



}
