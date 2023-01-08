using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Timers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;

// https://learn.microsoft.com/en-us/dotnet/framework/network-programming/using-udp-services

namespace WindowsFormsApplication4
{
    public partial class Form1 : Form
    {
        private String bridge_ip = "192.168.0.11"; // fater cime
        //private String bridge_ip = "192.168.0.104";
        private int bridge_port = 32100;
        private UdpClient udp_client;
        private Thread udp_listen_thread;
        private bool udp_listen_thread_running;

        private String redony_mac_nappali_ajto_kicsi = "246f28257cc8000e";
        private String redony_mac_nappali_ajto_nagy = "246f28257cc80005";
        private String redony_mac_nappali_ablak_jobb = "246f28257cc80007";
        private String redony_mac_nappali_ablak_kozep = "246f28257cc80009";
        private String redony_mac_nappali_ablak_bal = "246f28257cc8000c";
        private String redony_mac_halo_ajto = "246f28257cc8000f";
        private String redony_mac_halo_ablak = "246f28257cc80001";
        private String redony_mac_emelet_feljaro = "246f28257cc80010";
        private String redony_mac_gyszoba_ablak = "246f28257cc8000d";
        private String redony_mac_gyszoba_ajto = "246f28257cc8000b";

        private String[] redony_mac_arr = { "246f28257cc8000e",
                                            "246f28257cc80005",
                                            "246f28257cc80007",
                                            "246f28257cc80009",
                                            "246f28257cc8000c",
                                            "246f28257cc8000f",
                                            "246f28257cc80001",
                                            "246f28257cc80010",
                                            "246f28257cc8000d",
                                            "246f28257cc8000b" };

        private String operation_down = "0";
        private String operation_up = "1";
        private String operation_stop = "2";
        private String operation_status = "5";

        private String bridge_access_token = "\"AccessToken\":\"AEB7ACBCD21AB55F787C9559F0E7F084\"";
        private String brigde_radio_type = "\"deviceType\":\"10000000\""; // 433mhz radio

        private System.Timers.Timer periodicTimer;
        private int timerPeriod = 200; // ms
        private int timer_i;

        public Form1()
        {
            InitializeComponent();

            udp_client = new UdpClient(bridge_port);

            udp_client.Client.ReceiveTimeout = 5000;

            udp_listen_thread_running = true;
            udp_listen_thread = new Thread(new ThreadStart(UdpReceiver));
            udp_listen_thread.IsBackground = true;
            udp_listen_thread.Start();

            StartPeriodicTimer();
        }

        private void StartPeriodicTimer()
        {
            // Create a timer with a two second interval.
            periodicTimer = new System.Timers.Timer(timerPeriod);
            // Hook up the Elapsed event for the timer. 
            periodicTimer.Elapsed += OnTimedEvent;
            periodicTimer.AutoReset = true;
            periodicTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            timer_i = (timer_i + 1) % redony_mac_arr.Length;

            read_device(redony_mac_arr[timer_i]);
        }

        public void textBox_setText(System.Windows.Forms.TextBox textbox, String txt)
        {
            if (InvokeRequired)
            {
                try
                {
                    this.Invoke((MethodInvoker)delegate () { textBox_setText(textbox, txt); });
                }
                catch (Exception ex)
                {

                }

                return;
            }
            textbox.Text = txt;
        }

        public void UpdatePosition(String mac, int position)
        {
            if (string.Equals(mac, redony_mac_halo_ablak))
            {
                textBox_setText(textBox_halo_ablak, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_halo_ajto))
            {
                textBox_setText(textBox_halo_ajto, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_nappali_ajto_kicsi))
            {
                textBox_setText(textBox_nappali_ajto_kicsi, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_nappali_ajto_nagy))
            {
                textBox_setText(textBox_nappali_ajto_nagy, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_nappali_ablak_jobb))
            {
                textBox_setText(textBox_nappali_ablak_jobb, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_nappali_ablak_kozep))
            {
                textBox_setText(textBox_nappali_ablak_kozep, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_nappali_ablak_bal))
            {
                textBox_setText(textBox_nappali_ablak_bal, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_gyszoba_ablak))
            {
                textBox_setText(textBox_gyszoba_ablak, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_gyszoba_ajto))
            {
                textBox_setText(textBox_gyszoba_ajto, position.ToString());
            }
            else if (string.Equals(mac, redony_mac_emelet_feljaro))
            {
                textBox_setText(textBox_emelet_feljaro, position.ToString());
            }
            else
            {
            }
        }

        void SendUdp(string command)
        {
            byte[] data = Encoding.ASCII.GetBytes(command);
            udp_client.Send(data, data.Length, bridge_ip, bridge_port);
        }

        private void UdpReceiver()
        {
            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Any, bridge_port);

            while (udp_listen_thread_running)
            {
                try
                {
                    Byte[] data = udp_client.Receive(ref listenEndPoint);
                    String message = Encoding.ASCII.GetString(data);

                    string[] msg_split = message.Replace("\n","").Replace("\"","").Replace("{","").Replace("}","").Replace(" ", "").Split(',');

                    string msgType = msg_split[0].Split(':')[1];
                    string mac = msg_split[1].Split(':')[1];
                    string deviceType = msg_split[2].Split(':')[1];
                    string msgID = msg_split[3].Split(':')[1];

                    if (string.Equals(msgType, "WriteDeviceAck") || string.Equals(msgType, "Report") || string.Equals(msgType, "ReadDeviceAck"))
                    {
                        //int type = Int32.Parse(msg_split[4].Split(':')[1]);
                        //int operation = Int32.Parse(msg_split[5].Split(':')[1]);
                        int currentPosition = Int32.Parse(msg_split[6].Split(':')[1]);
                        //int currentAngle = Int32.Parse(msg_split[7].Split(':')[1]);
                        //int currentState = Int32.Parse(msg_split[8].Split(':')[1]);
                        //int voltageMode = Int32.Parse(msg_split[9].Split(':')[1]);
                        //int batteryLevel = Int32.Parse(msg_split[10].Split(':')[1]);
                        //int wirelessMode = Int32.Parse(msg_split[11].Split(':')[1]);
                        //int rssi = Int32.Parse(msg_split[12].Split(':')[1]);

                        UpdatePosition(mac, currentPosition);
                    }
                    else
                    {

                    }
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode != 10060)
                    {
                    }
                    else
                    {

                    }
                }

                Thread.Sleep(100); // tune for your situation, can usually be omitted
            }
        }

        private void write_device(String mac, String operation)
        {
            String dateplustime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            String command = "{\"msgType\":\"WriteDevice\",\"mac\":\"" + mac + "\"," + brigde_radio_type + "," + bridge_access_token + ",\"msgID\":" + dateplustime + ",\"data\":{\"operation\":" + operation + "}}";
            SendUdp(command);
        }

        private void read_device(String mac)
        {
            String dateplustime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            String command = "{\"msgType\":\"ReadDevice\",\"mac\":\"" + mac + "\"," + brigde_radio_type + "," + bridge_access_token + ",\"msgID\":" + dateplustime + "}";
            SendUdp(command);
        }

        // Nappali erkélyajtó, kicsi
        private void button1_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ajto_kicsi, operation_up);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ajto_kicsi, operation_stop);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ajto_kicsi, operation_down);
        }

// Nappali erkélyajtó, nagy, középen felnyíló
        private void button4_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ajto_nagy, operation_up);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ajto_nagy, operation_stop);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ajto_nagy, operation_down);
        }
// Nappali ablak, jobb oldali
        private void button7_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_jobb, operation_up);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_jobb, operation_stop);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_jobb, operation_down);
        }
// Nappali ablak, középső
        private void button10_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_kozep, operation_up);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_kozep, operation_stop);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_kozep, operation_down);
        }
// Nappali ablak, bal oldali
        private void button11_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_bal, operation_up);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_bal, operation_stop);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_nappali_ablak_bal, operation_down);
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button18_Click(object sender, EventArgs e)
        {
            // mind fel
            write_device(redony_mac_nappali_ajto_kicsi, operation_up);
            write_device(redony_mac_nappali_ajto_nagy, operation_up);
            write_device(redony_mac_nappali_ablak_jobb, operation_up);
            write_device(redony_mac_nappali_ablak_kozep, operation_up);
            write_device(redony_mac_nappali_ablak_bal, operation_up);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            // mind stop
            write_device(redony_mac_nappali_ajto_kicsi, operation_stop);
            write_device(redony_mac_nappali_ajto_nagy, operation_stop);
            write_device(redony_mac_nappali_ablak_jobb, operation_stop);
            write_device(redony_mac_nappali_ablak_kozep, operation_stop);
            write_device(redony_mac_nappali_ablak_bal, operation_stop);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            // mind le
            write_device(redony_mac_nappali_ajto_kicsi, operation_down);
            write_device(redony_mac_nappali_ajto_nagy, operation_down);
            write_device(redony_mac_nappali_ablak_jobb, operation_down);
            write_device(redony_mac_nappali_ablak_kozep, operation_down);
            write_device(redony_mac_nappali_ablak_bal, operation_down);
        }
// Hálószoba erkélyajtó
        private void button19_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ajto, operation_up);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ajto, operation_stop);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ajto, operation_down);
        }
// Hálószoba ablak
        private void button22_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ablak, operation_up);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ablak, operation_stop);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ablak, operation_down);
        }
// Hálószoba redőnyök fel
        private void button25_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ablak, operation_up);
            write_device(redony_mac_halo_ajto, operation_up);
        }
// Hálószoba redőnyök le
        private void button26_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ablak, operation_down);
            write_device(redony_mac_halo_ajto, operation_down);
        }
// Hálószoba mind Stop
        private void button38_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_halo_ablak, operation_stop);
            write_device(redony_mac_halo_ajto, operation_stop);
        }

        // Emeleti feljáró
        private void button27_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_emelet_feljaro, operation_up);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_emelet_feljaro, operation_stop);
        }

        private void button29_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_emelet_feljaro, operation_down);
        }
// Babaszoba ablak
        private void button34_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ablak, operation_up);
        }

        private void button33_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ablak, operation_stop);
        }

        private void button32_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ablak, operation_down);
        }
// Babaszoba erkélyajtó
        private void button37_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ajto, operation_up);
        }

        private void button36_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ajto, operation_up);
        }

        private void button35_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ajto, operation_up);
        }
// Babaszoba minden fel, és le
        private void button31_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ablak, operation_up);
            write_device(redony_mac_gyszoba_ajto, operation_up);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ablak, operation_stop);
            write_device(redony_mac_gyszoba_ajto, operation_stop);
        }
// Babaszoba mind Stop
        private void button39_Click(object sender, EventArgs e)
        {
            write_device(redony_mac_gyszoba_ablak, operation_down);
            write_device(redony_mac_gyszoba_ajto, operation_down);
        }
    }
}
