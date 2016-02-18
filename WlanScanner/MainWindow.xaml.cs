using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;

namespace WlanScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<String> macs;
        Dictionary<string,string> manufacturerMacs;

        Boolean buttonClicked;
        NotifyIcon nIcon = new NotifyIcon();
        String macTxt;
        Ping pingSender;
        private System.Windows.Forms.ContextMenu m_menu;
        string hostname;
        public MainWindow()
        {
            manufacturerMacs = new Dictionary<string, string>();

            nIcon.Click += new System.EventHandler(NotifyIcon_Click);
            m_menu = new System.Windows.Forms.ContextMenu();
            m_menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Exit", new System.EventHandler(Exit_Click)));
            nIcon.ContextMenu = m_menu;
            this.nIcon.Visible = true;
            this.nIcon.Icon = SystemIcons.Information;

            System.Net.IPHostEntry ipd = System.Net.Dns.GetHostEntry("127.0.0.1");
            hostname = ipd.HostName;
            InitializeComponent();
            buttonClicked = false;
            pingSender = new Ping();
            parseFile();
            
        }

        

        private void parseFile()
        {
            
            Task.Run(() =>
            {
                

                // Read the file and display it line by line.
                try
                {
                    int lineCounter = 0;
                    String line;
                    String[] lineStr = {"rty"};
                    System.IO.StreamReader file =
                       new System.IO.StreamReader("oui_dataset.dat");
                
                
                    while ((line = file.ReadLine()) != null)
                    {
                        
                        if (!line.StartsWith("#"))
                        {
                            macTxt = line;
                            lineStr = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                            
                            if (lineStr.Length > 3)
                            {
                                string linestr1 = lineStr[1];
                                int lineStrCounter = 4;
                                linestr1 = lineStr[3];
                                while (lineStrCounter < lineStr.Length)
                                {
                                    linestr1 += " " + lineStr[lineStrCounter];
                                    lineStrCounter++;
                                }

                                manufacturerMacs.Add(lineStr[0], linestr1);
                            }
                            
                        }
                    
                        lineCounter++;
                    }
                    
                    file.Close();
                }
                catch (Exception ex)
                {
                    errorLabel.Dispatcher.BeginInvoke(new Action(() => setErrorLabel("Parsing error: " + ex.Message)));
                }
            });
        }
        private void btnExecArp_Click(object sender, RoutedEventArgs e)
        {
            this.btnPrevious.IsEnabled = true;
           

            
            buttonClicked = !buttonClicked;
            if (buttonClicked)
            {
                btnExecArp.Content = "Stop";
                macs = new List<String>();
                txtFirstName.Text = "";
                this.btnNext.IsEnabled = false;
                this.refresh_btn.IsEnabled = true;
            }
            else
            {
                btnExecArp.Content = "Start Scan";
                this.btnNext.IsEnabled = true;
                Task.WaitAll();
            }
            int counter = 0;
            Boolean changed = true;
            
            Task.Run(() => {
                while ( buttonClicked)
                {
                    try
                    {

                        string output = " ";
                        string dev = "Unknown";
                        
                        int lanID = 1;
                        for (int i = 0; i < 255; i++)
                        {
                            if (counter > 0 && macs.Count < 1)
                            {
                                lanID++;
                                counter = 0;
                            }
                            string ip = "192.168." + lanID.ToString() + "." + i.ToString();
                            if(counter > 0){
                                byte[] buffer = new byte[32];
                                PingOptions options = new PingOptions ();
                                options.DontFragment = true;
                                PingReply reply = pingSender.Send(ip, 120, buffer, options);
                                if (!(reply.Status == IPStatus.Success))
                                    continue;
                                

                            }
                            System.Diagnostics.Process process = new System.Diagnostics.Process();
                            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

                            startInfo.RedirectStandardError = true;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.UseShellExecute = false;
                            startInfo.CreateNoWindow = true;
                            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            /*startInfo.FileName = "ping";
                            startInfo.Arguments = " -a 192.168.1." + i.ToString() + " -n 1";
                            process.StartInfo = startInfo;
                            process.Start();
                            process.StandardOutput.ReadLine();
                            process.StandardOutput.ReadLine();
                            String prc = process.StandardOutput.ReadLine();
                            hostname = prc.Split(' ')[2];
                            process.WaitForExit();*/
                            startInfo.FileName = "arp";
                            startInfo.Arguments = "-a 192.168." + lanID.ToString() + "." + i.ToString();
                            process.StartInfo = startInfo;
                            process.Start();
                            process.StandardOutput.ReadLine();
                            process.StandardOutput.ReadLine();
                            output = process.StandardOutput.ReadToEnd();
                            output = output.Trim();
                            //process.WaitForExit();
                            if (!macs.Contains(output) && output != null)
                            {
                                String[] newentry = output.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                                if (newentry.Length > 6)
                                {
                                    newentry[6] = newentry[6].Replace("-", ":");
                                    newentry[6] = newentry[6].Remove(8);
                                    newentry[6] = newentry[6].ToUpper();

                                    if (manufacturerMacs.ContainsKey(newentry[6]))
                                    {
                                        dev = "Device: " + manufacturerMacs[newentry[6]] + "\n";
                                        dev += output;
                                        output = dev + "\n";
                                        if (!macs.Contains(output) && output != null)
                                        {
                                            changed = true;
                                            macs.Add(output);
                                        }
                                    }                                 
                                }
                            }

                            if (changed)
                            {
                                
                                String labelTxt = "Connected Devices : " + macs.Count;
                                deviceCounterLabel.Dispatcher.BeginInvoke(new Action(() => setDeviceCounterLabel(labelTxt)));
                                macTxt = "List of Devices Connected with " + hostname + "\n";
                                foreach (String item in macs)
                                {
                                    if(item.Equals(""))
                                        continue;

                                    macTxt = macTxt + "\n" + item;
                                    txtFirstName.Dispatcher.BeginInvoke(new Action(() => setTextArea(item, counter)));
                                }
                                changed = false;
                            }

                            process.WaitForExit();

                        }

                        System.Threading.Thread.Sleep(30 * 1000);

                    }
                    catch (Exception ex)
                    {
                        //throw new Exception("OS error while executing " + ": " + ex.Message, ex);

                        errorLabel.Dispatcher.BeginInvoke(new Action(() => setErrorLabel("Scanning error: " + ex.Message)));
                    }
                    counter++;
                }
            });
        }

        private void NotifyIcon_Click(object sender, System.EventArgs e)
        {
            System.Windows.Forms.MouseEventArgs me = (System.Windows.Forms.MouseEventArgs)e;
            if (me.Button == System.Windows.Forms.MouseButtons.Left)
            {
                String txt = "0";
                if (macs != null)
                {
                    txt = macs.Count.ToString();
                }

                this.nIcon.ShowBalloonTip(1000, "LAN Scanner", "Connected Devices: " + txt, ToolTipIcon.Warning);
            }
            
        }

        protected void Exit_Click(Object sender, System.EventArgs e)
        {
            Close();
        }

        private void setTextArea(String text, int counter)
        {
            
            
            txtFirstName.Text = macTxt;
            if(counter != 0)
                this.nIcon.ShowBalloonTip(1000, "New connection " + DateTime.Now.ToString("h:mm:ss tt"), text, ToolTipIcon.Info);
            
        }

        private void setTextAreaWlan(String text)
        {

            //txtFirstName.AppendText(text);
            txtFirstName.Text = text;

        }

        private void setDeviceCounterLabel(String text)
        {
            deviceCounterLabel.Content = text;
        }

        private void setErrorLabel(String text)
        {
            errorLabel.Visibility = Visibility.Visible; 
            errorLabel.Content = text;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            deviceCounterLabel.Content = "Wireless Networks Available";
            Task.Run(() =>
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "netsh";
                startInfo.Arguments = " wlan show networks mode=bssid";
                process.StartInfo = startInfo;
                process.Start();
                //txtFirstName.Dispatcher.BeginInvoke(new Action(() => setTextAreaWlan("Began Scanning...")));
                String txt = "";
                String result = process.StandardOutput.ReadToEnd();
                
                
                Char[] delims = {' ', '\n', '\t'};
                String[] res = result.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                int countme = 0;
                int countme2 = 0;
                int countme3 = 0;
                foreach (String st in res)
                {
                    if (st.Contains("BSSID"))  
                    {
                        countme++;
                        txt = txt + "\n" + "Possible Password: ";
                    }
                    else if (st.Contains("Signal"))
                    {
                        countme3++;
                        txt = txt + "\n" + "Signal: ";
                    }
                    else if (countme3 > 0 && countme3 < 2)
                    {
                        countme3++;

                    }
                    else if (countme3 == 2)
                    {
                        
                        txt += st + "\n";
                        countme3 = 0;
                    }
                    else if (countme > 0 && countme < 3)
                    {
                        countme++;

                    }
                    else if (countme == 3)
                    {
                        String nst = st.Replace(":", "");
                        txt += nst;
                        countme = 0;
                    }
                    else if (st.Contains("SSID") && !st.Contains("BSS"))
                    {
                        countme2++;
                        txt = txt + "\n" + "NetWork: ";
                    }
                    else if (countme2 > 0 && countme2 < 3)
                    {
                        countme2++;
                    }
                    else if (countme2 == 3)
                    {
                        txt = txt + st;
                        countme2 = 0;
                    }

                    
                }
                process.WaitForExit();
                txtFirstName.Dispatcher.BeginInvoke(new Action(() => setTextAreaWlan(txt.Trim())));
                
            });
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            deviceCounterLabel.Content = "Connected Devices";
            String txt = "";
            if (macs.Count > 0)
            {
                
                foreach (String item in macs)
                {
                    txt += item + "\n";
                }
                txtFirstName.Dispatcher.BeginInvoke(new Action(() => setTextAreaWlan(txt)));
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (macs.Count > 0)
            {
                string txt = "List of Devices Connected with " + hostname + "\n\n";
                foreach (String item in macs)
                {
                    txt += item + "\n";
                }
                
                txtFirstName.Dispatcher.BeginInvoke(new Action(() => setTextAreaWlan(txt)));
            }
        }

    }


    
}
