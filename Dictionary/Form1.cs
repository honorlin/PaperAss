using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Dictionary
{
    public partial class Form1 : Form
    {
        static string source = "";
        static string result = "";
        static int flag = 0;
        static String source_text = "";
        static string service_url = "";
        static string export_data = "";


        const int WH_KEYBOARD = 2;
        const int WH_KEYBOARD_LL = 13;
    
            public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    
            private static int m_HookHandle = 0;    // Hook handle
            private HookProc m_KbdHookProc;            // 鍵盤掛鉤函式指標
    

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);


            // 設置掛鉤.
            [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
            public static extern int SetWindowsHookEx(int idHook, HookProc lpfn,
            IntPtr hInstance, int threadId);
    
            // 將之前設置的掛鉤移除。記得在應用程式結束前呼叫此函式.
            [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
            public static extern bool UnhookWindowsHookEx(int idHook);
    
            // 呼叫下一個掛鉤處理常式（若不這麼做，會令其他掛鉤處理常式失效）.
            [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
            public static extern int CallNextHookEx(int idHook, int nCode,
            IntPtr wParam, IntPtr lParam);
    
            [DllImport("kernel32.dll")]
            static extern int GetCurrentThreadId();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
          
            start();//開始翻譯
        }

        void start()
        {
            export_data += source_text + "\r\n------------------------------------------------------------------------------------------------\r\n";


            source = service_url + HttpUtility.UrlEncode(source_text);

            webBrowser1.Navigate(source);



            //Thread oThreadB = new Thread(new ThreadStart(work));
            //oThreadB.Start();
        }


        void work()
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(source);
            req.Method = "GET";
            using (WebResponse wr = req.GetResponse())
            {
                //在這裡對接收到的頁面內容進行處理

                result = get_text(wr.GetResponseStream(), Encoding.GetEncoding("big5"));
            } 
        
        
        
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (flag == 1)
            {
                this.WindowState = FormWindowState.Normal;
                this.TopMost = true;
                flag = 0;
                start();
            }
            else
            {
                //this.TopMost = false;
            }


            //if (result != "")
             //   textBox2.Text = result;
            try
            {
                //textBox2.Text = webBrowser1.Document.ActiveElement.Name;
            }
            catch (Exception ex)
            {


            }
        }

        string get_text(Stream se, System.Text.Encoding en)
        {
            Stream mystream = se;
            string content = "";


            byte[] bte = new byte[1024];
            int ii = 0;

            do
            {
                ii = mystream.Read(bte, 0, 1024);
                content += en.GetString(bte, 0, 1024);

            } while (ii > 0);

            return content;
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            result = get_text(webBrowser1.DocumentStream, Encoding.UTF8);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            set_size();
            set_hook();
            service_url = "http://translate.google.com.tw/?hl=zh-TW&tab=wT#en|zh-TW|";
            webBrowser1.ScriptErrorsSuppressed = true;
        }


        void set_hook()
        { 
                if (m_HookHandle == 0)
                {
                    using (Process curProcess = Process.GetCurrentProcess())
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        m_KbdHookProc = new HookProc(Form1.KeyboardHookProc);
    
                        m_HookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, m_KbdHookProc,
                            GetModuleHandle(curModule.ModuleName), 0);
                    }
    
                    if (m_HookHandle == 0)
                    {
                        MessageBox.Show("呼叫 SetWindowsHookEx 失敗!");
                        return;
                    }
                }
                else
                {
                   
                }
        
        }


        void unset_hook()
        {

            if (m_HookHandle == 0)
            {
               
            }
            else
            {
                bool ret = UnhookWindowsHookEx(m_HookHandle);
                if (ret == false)
                {
                    MessageBox.Show("呼叫 UnhookWindowsHookEx 失敗!");
                    return;
                }
                m_HookHandle = 0;
            }
        
        }

         public static int KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
            {
                // 當按鍵按下及鬆開時都會觸發此函式，這裡只處理鍵盤按下的情形。
                bool isPressed = (lParam.ToInt32() & 0x80000000) == 0;   
   
                if (nCode < 0 || !isPressed)
               {
                    return CallNextHookEx(m_HookHandle, nCode, wParam, lParam);
                }
    
                // 取得欲攔截之按鍵狀態
                KeyStateInfo ctrlKey = KeyboardInfo.GetKeyState(Keys.ControlKey);   
                KeyStateInfo cKey = KeyboardInfo.GetKeyState(Keys.C);

                if (ctrlKey.IsPressed && cKey.IsPressed)
               {
                   //MessageBox
                   if (Clipboard.ContainsText(TextDataFormat.Text))
                   {
                       source_text = ClipboardGetText();
                       flag = 1;
                   }
               }
              
   
               return CallNextHookEx(m_HookHandle, nCode, wParam, lParam);
          }

         public static String ClipboardGetText()
         {
             String returnText = null;
             if (Clipboard.ContainsText(TextDataFormat.Text))
             {
                 returnText = Clipboard.GetText(TextDataFormat.Text);              
             }
             return returnText;
         }

         private void Form1_Resize(object sender, EventArgs e)
         {
             set_size();
         }

         void set_size()
         {
             groupBox3.Width = this.Width - 18;
             webBrowser1.Width = groupBox3.Width - 13;
             groupBox3.Height = this.Height - groupBox3.Top - 38;
             webBrowser1.Height = groupBox3.Height - 26;
         }

         private void radioButton1_CheckedChanged(object sender, EventArgs e)
         {
             if (radioButton1.Checked)
                 service_url = "http://translate.google.com.tw/?hl=zh-TW&tab=wT#en|zh-TW|";

         }

         private void radioButton2_CheckedChanged(object sender, EventArgs e)
         {
             if (radioButton2.Checked)
                 service_url = "http://tw.dictionary.yahoo.com/search?ei=UTF-8&p=";
         }

         private void textBox1_TextChanged(object sender, EventArgs e)
         {
             source_text = textBox1.Text;
         }

         private void Form1_FormClosed(object sender, FormClosedEventArgs e)
         {
             unset_hook();
         }

         private void 離開ToolStripMenuItem_Click(object sender, EventArgs e)
         {
             this.Dispose();
         }

         private void 使用方法ToolStripMenuItem_Click(object sender, EventArgs e)
         {
             Form_desp ff = new Form_desp();
             ff.Show();
         }

         private void 版本ToolStripMenuItem_Click(object sender, EventArgs e)
         {
             Form_version ff = new Form_version();
             ff.Show();
         }

         private void 匯出ToolStripMenuItem_Click(object sender, EventArgs e)
         {
             this.TopMost = false;

             try
             {

                 saveFileDialog1.ShowDialog();
                 saveFileDialog1.DefaultExt = "txt";

                 string file_path = saveFileDialog1.FileName;

                 if (file_path != "")
                 {

                     export_data = DateTime.Now.ToString() + " 翻譯記錄" + "\r\n------------------------------------------------------------------------------------------------\r\n" + export_data;

                     File.WriteAllText(file_path, export_data, Encoding.UTF8);
                     MessageBox.Show("匯出成功!");
                 }
             }
             catch (Exception ex)
             { 
             
             }
         }

         private void 功能ToolStripMenuItem_Click(object sender, EventArgs e)
         {

         }

         private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
         {

         }

         private void 模式ToolStripMenuItem_Click(object sender, EventArgs e)
         {

         }

         private void 全景ToolStripMenuItem_Click(object sender, EventArgs e)
         {

             if (groupBox3.Top == 79)
             {
                 groupBox3.Top = 28;
                 set_size();
             }
             else if (groupBox3.Top == 28)
             {
                 groupBox3.Top = 79;
                 set_size();
             }
         }

    }
     public class KeyboardInfo
       {
           private KeyboardInfo() { }
   
           [DllImport("user32")]
           private static extern short GetKeyState(int vKey);
   
           public static KeyStateInfo GetKeyState(Keys key)
           {
               int vkey = (int)key;
   
               if (key == Keys.Alt)
               {
                  vkey = 0x12;    // VK_ALT
               }
   
               short keyState = GetKeyState(vkey);
               int low = Low(keyState);
               int high = High(keyState);
               bool toggled = (low == 1);
               bool pressed = (high == 1);
   
               return new KeyStateInfo(key, pressed, toggled);
           }
   
           private static int High(int keyState)
           {
               if (keyState > 0)
               {
                   return keyState >> 0x10;
               }
               else
               {
                   return (keyState >> 0x10) & 0x1;
               }
   
           }
   
          private static int Low(int keyState)
          {
               return keyState & 0xffff;
           }
       }
   
   
       public struct KeyStateInfo
       {
           Keys m_Key;
           bool m_IsPressed;
           bool m_IsToggled;
   
           public KeyStateInfo(Keys key, bool ispressed, bool istoggled)
           {
               m_Key = key;
               m_IsPressed = ispressed;
               m_IsToggled = istoggled;
           }
   
           public static KeyStateInfo Default
           {
               get
               {
                   return new KeyStateInfo(Keys.None, false, false);
               }
           }
   
           public Keys Key
           {
               get { return m_Key; }
           }
   
           public bool IsPressed
           {
               get { return m_IsPressed; }
           }
   
           public bool IsToggled
           {
               get { return m_IsToggled; }
           }
       }
}
