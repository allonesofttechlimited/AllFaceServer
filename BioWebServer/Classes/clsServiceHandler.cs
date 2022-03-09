using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioWebServer.Classes
{
    public class clsServiceHandler
    {
        string strKey = Global.SecurityKey;
        CryptoLibrary cryptoLibrary = new CryptoLibrary();

        public string Decrypt(string strValue)
        {
            return cryptoLibrary.Decrypt(strValue, strKey);
        }

        public string Encrypt(string strValue)
        {
            return cryptoLibrary.Encrypt(strValue, strKey);
        }

        public string Decrypt(string strValue, string strSessionKey)
        {
            return cryptoLibrary.Decrypt(strValue, strSessionKey);
        }

        public string Encrypt(string strValue, string strSessionKey)
        {
            return cryptoLibrary.Encrypt(strValue, strSessionKey);
        }

        public string EncryptionArray(string strText)
        {
            string strEncryptedValue = "";

            string[] strValue = strText.Split('*');

            for (int i = 0; i < strValue.Length; i++)
            {
                string strValueEncrypted = cryptoLibrary.Encrypt(strValue[i], strKey);
                strEncryptedValue = strEncryptedValue + strValueEncrypted + "*";
            }
            return strEncryptedValue.Substring(0, strEncryptedValue.Length - 1);
        }

        public string DecryptionArray(string strText)
        {
            string strEncryptedValue = "";

            string[] strValue = strText.Split('*');

            for (int i = 0; i < strValue.Length; i++)
            {
                string strValueEncrypted = cryptoLibrary.Encrypt(strValue[i], strKey);
                strEncryptedValue = strEncryptedValue + strValueEncrypted + "*";
            }
            return strEncryptedValue.Substring(0, strEncryptedValue.Length - 1);
        }

        public string EncryptionArray(string strText, string strSessionKey)
        {
            string strEncryptedValue = "";

            string[] strValue = strText.Split('*');

            for (int i = 0; i < strValue.Length; i++)
            {
                string strValueEncrypted = cryptoLibrary.Encrypt(strValue[i], strSessionKey);
                strEncryptedValue = strEncryptedValue + strValueEncrypted + "*";
            }
            return strEncryptedValue.Substring(0, strEncryptedValue.Length - 1);
        }

        public string DecryptionArray(string strText, string strSessionKey)
        {
            string strEncryptedValue = "";

            string[] strValue = strText.Split('*');

            for (int i = 0; i < strValue.Length; i++)
            {
                string strValueEncrypted = cryptoLibrary.Encrypt(strValue[i], strSessionKey);
                strEncryptedValue = strEncryptedValue + strValueEncrypted + "*";
            }
            return strEncryptedValue.Substring(0, strEncryptedValue.Length - 1);
        }
    }
}
