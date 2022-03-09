using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace BioWebServer
{
    public partial class frmPassKey : Form
    {
        private static byte[] bytes;
        public frmPassKey()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
           
            if (txtPassKey.Text.Trim().ToString()=="")
            {
                MessageBox.Show("Please insert Passkey");
                return;
            }

            string strPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            strPath = strPath.Substring(6) + "\\GWPSK.mit";
            
            string encrPassKey = Encrypt(txtPassKey.Text.Trim().ToString(),"BangladeshMicroTechLtd");
            string savePass = SetLisence(encrPassKey, strPath);

            DialogResult dr = MessageBox.Show(savePass, "Pass Key", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                this.Close();
            }
            else
            {
 
            }


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
        public string Decrypt(string textToDecrypt, string key)
        {
            string Reslt_Decrypted = "";
            try
            {
                int keySize = 0x80;
                int blockSize = 0x80;
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

                RijndaelManaged rijndaelCipher = new RijndaelManaged();
                rijndaelCipher.Mode = CipherMode.CBC;
                rijndaelCipher.Padding = PaddingMode.PKCS7;

                //rijndaelCipher.KeySize = 0x80;
                // rijndaelCipher.BlockSize = 0x80;
                rijndaelCipher.KeySize = keySize;
                rijndaelCipher.BlockSize = blockSize;

                string s = textToDecrypt.Trim().Replace(" ", "+");
                if (s.Length % 4 > 0)
                    s = s.PadRight(s.Length + 4 - s.Length % 4, '=');

                byte[] encryptedData = Convert.FromBase64String(s);
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
                // byte[] plainText = rijndaelCipher.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                // return Encoding.UTF8.GetString(plainText); 

                byte[] plainText = rijndaelCipher.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                Reslt_Decrypted = encoding.GetString(plainText).ToString();
            }
            catch (Exception ex)
            {
                Reslt_Decrypted = "Decryption wrong";
            }
            return Reslt_Decrypted;

        }


        public string SetLisence(string strLisence, string strPath)
        {
        //  string  strLicensePath = "GWPSK.mit";
            //strTelConString = Encrypt(strTelConString);

            if (File.Exists(strPath))
            {
                File.Delete(strPath);
            }

            try
            {
                StreamWriter sw = new StreamWriter(strPath, true);  // Create a new stream to append to the file              
                sw.WriteLine(strLisence);
                sw.Close(); // Close StreamWriter          
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
            return "Pass Key Saved Successful";
        }

    }
}
