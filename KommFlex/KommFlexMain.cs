using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using System.Media;
using System.Timers;

/// Nuget Packges
using CefSharp;
using CefSharp.WinForms;
using CefSharp.Example.Handlers;
using SuperWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using Gma.System.MouseKeyHook;
using static KommFlex.RelayController;

namespace KommFlex
{
    public partial class KommFlexMain : Form
    {
        // Server Url
        private string str_server_url = "";

        // Reference files
        string strConfigFileName = Properties.Resources.CONFIG_FILE_NAME;
        string strBackgroundImageFileName = Properties.Resources.BACKGROUND_FILE_NAME;
        
        // Configuration
        private int my_endpoint_id = -1;
        private int my_endpoint_type = -1;
        // 0 : Central Dispatcher
        // 1 : Dispatcher
        // 2 : Client
        private string my_photo_viewer = Properties.Resources.DEFAULT_PHOTO_VIEWER;
        private int my_ringing_timeout = Int32.Parse(Properties.Resources.DEFAULT_RING_TIMEOUT);
        private string my_photo_folder = Properties.Resources.DEFAULT_PHOTO_FOLDER;
        private int my_primary_camera = Int32.Parse(Properties.Resources.DEFAULT_PRIMARY_CAMERA);
        private int my_secondary_camera = Int32.Parse(Properties.Resources.DEFAULT_SECONDARY_CAMERA);
        private int my_dispatcher_video_display_mode = Int32.Parse(Properties.Resources.DEFAULT_DISPATCHER_VIDEO_DISPLAY_MODE);
        private string my_station_id = Properties.Resources.DEFAULT_STATION_ID;
        private int my_dispatcher_id = 0;
        private int my_photoviewer_width = 600;

        private List<string> list_pictures = new List<string>();
        private int picture_index = -1;
        // CEF Browsers
        public ChromiumWebBrowser webBrowser;

        // WebSocket to communicate browsers
        private WebSocketServer wsServer;
        private List<WebSocketSession> wsClientList = new List<WebSocketSession>();

        // 2nd Camera members
        VideoCapture camera2;

        // Members for webrtc connection
        List<string> rtc_id_list = new List<string>();
        private string connected_rtc_id = "";
        private string my_rtc_id = "";
        string str_state = "INIT"; // "INIT", "CALLING", "INCOMMING", "RINGING"
        string str_call_start_time = "";
        string str_call_end_time = "";
        int is_passed_central_dispatcher = 0;
        string str_redirect_state = "";

        // Static Commands on WebSocket
        string str_cmd_prefix = "KOMMFLEXCOMMAND>>";
        string str_cmd_take_photo = "TAKEAPHOTO";
        string str_cmd_fail_take_photo = "Can not take a photo from client's 2nd camera.";
        string str_cmd_uploaded_photo = "UPLOADEDAPHOTO";
        string str_cmd_busy_state = "BUSYSTATE";
        string str_cmd_redirect_reason_busy = "REDIRECT_REASON_BUSY";
        string str_cmd_redirect_reason_timeout = "REDIRECT_REASON_TIMEOUT";
        string str_cmd_welcome_message = "Welcome to KommFlex!";

        // Keyboard hook for client application
        private IKeyboardMouseEvents m_GlobalHook;

        // Communication controller to communicate with internal web browser via websocket
        private CommController m_commController = new CommController();        

        // Ringing Sound Object
        private SoundPlayer Player = new SoundPlayer();

        // Timer for redirection
        private System.Timers.Timer aTimer = new System.Timers.Timer();
        private System.Timers.Timer pingTimer = new System.Timers.Timer();

        //System.Diagnostics.Process photo_process;

		// relay controller
        private RelayController relayController;
		
        public KommFlexMain()
        {           
            InitializeComponent();

            // Read config.xml and construct settings
            SetupApplicationSettings();

            // Check if serverURL is available and configuration is correct
            if (my_endpoint_type < 0 || my_endpoint_id < 0)
            {
                // Exit application
                MessageBox.Show(Properties.Resources.MSG_TEXT_CHECK_CONFIG, Properties.Resources.APPLICATION_NAME);
                System.Environment.Exit(0);
            }

            if (my_endpoint_type == 2)
            {
                my_dispatcher_id = m_commController.getDispatcherIdByClientId(my_endpoint_id.ToString());

                if (my_dispatcher_id <= 0)
                {
                    // Exit application
                    MessageBox.Show(Properties.Resources.MSG_TEXT_CANNOT_FIND_DISPATCHER, Properties.Resources.APPLICATION_NAME);
                    System.Environment.Exit(0);
                }
            }

            // Reconstruct UI layout in DISPATCHER or CLIENT mode
            SetupUI();

            // Show background image
            SetupBackgroundImage();

            // Setup the current state as INIT
            InitState();

            // Create Websocket to communicate with internal web browser properly
            bool ws_running = false;
            try
            {
                wsServer = new WebSocketServer();
                // static webrtc port
                int port = 8225;
                wsServer.Setup(port);
                wsServer.NewSessionConnected += WsServer_NewSessionConnected;
                wsServer.NewMessageReceived += WsServer_NewMessageReceived;
                wsServer.NewDataReceived += WsServer_NewDataReceived;
                wsServer.SessionClosed += WsServer_SessionClosed;
                ws_running = wsServer.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("Happy : Exception WebSocketServer");
            }

            // Checked websocket server is started successfully and Load video chat page in internal web browser properly
            if (ws_running)
            {
                InitBrowser();
            }
            else
            {
                // Exit application
                System.Environment.Exit(0);
            }

            if (my_endpoint_type == 2)
            {
                camera2 = new VideoCapture(my_secondary_camera);
                Cv2.WaitKey(1000);
                if (!camera2.IsOpened())
                {
                    MessageBox.Show("Cannot open 2nd camera", Properties.Resources.APPLICATION_NAME);
                    System.Environment.Exit(0);
                }
            }

        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            // Restore Size and Location
            if (Properties.Settings.Default.Maximized)
            {
                WindowState = FormWindowState.Maximized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else if (Properties.Settings.Default.Minimized)
            {
                WindowState = FormWindowState.Minimized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else
            {
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }

            // Relay Controller 
            relayController = new RelayController();
            if (!relayController.Initialize())
            {
                txtMsgBox.Text = relayController.ErrorMessage;
            }
        }
        
        private void onFormClosing(object sender, FormClosingEventArgs e)
        {
            // Save Size and Location
            if (WindowState == FormWindowState.Maximized)
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximized = true;
                Properties.Settings.Default.Minimized = false;
            }
            else if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Size = Size;
                Properties.Settings.Default.Maximized = false;
                Properties.Settings.Default.Minimized = false;
            }
            else
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximized = false;
                Properties.Settings.Default.Minimized = true;
            }
            Properties.Settings.Default.Save();

            m_commController.sendPing(my_endpoint_id.ToString());
        }

        private void onFormClosed(object sender, FormClosedEventArgs e)
        {
            // Remove momory when closed application
            try
            {
                // Stop websocket server
                wsServer.Stop();

                // Remove 2nd camera object
                if (camera2 != null)
                {
                    if (camera2.IsOpened())
                    {
                        camera2.Release();
                    }
                }

            }
            catch { }

            try
            {
                //photo_process.Kill();
            }
            catch
            {

            }
        }

        public void SetupBackgroundImage()
        {
            try
            {
                // Load background image
                var img = Image.FromFile(strBackgroundImageFileName);

                //// Cut the center of background image
                var cs = pictureBackground.Size;
                if (img.Size != cs)
                {
                    float ratio = Math.Max(cs.Height / (float)img.Height, cs.Width / (float)img.Width);
                    if (ratio > 1)
                    {
                        Func<float, int> calc = f => (int)Math.Ceiling(f * ratio);
                        img = new Bitmap(img, calc(img.Width), calc(img.Height));
                    }

                    var part = new Bitmap(cs.Width, cs.Height);
                    using (var g = Graphics.FromImage(part))
                    {
                        g.DrawImageUnscaled(img, (cs.Width - img.Width) / 2, (cs.Height - img.Height) / 2);
                    }
                    img = part;
                }

                // Load background image
                pictureBackground.BackgroundImageLayout = ImageLayout.Center;
                pictureBackground.BackgroundImage = img;
            }
            catch
            {

            }
        }

        public void ShowClientPicture(int index)
        {
            try
            {
                if (index >= list_pictures.Count)
                    return;

                string fn = list_pictures.ElementAt<string>(index);

                if (my_endpoint_type == 2)
                    return;

                picture_index = index;

                // Show Image with "UniformToFill" mode
                if (picClientCamera.InvokeRequired)
                {
                    picClientCamera.Invoke(new MethodInvoker(delegate
                    {
                        picClientCamera.Size = new System.Drawing.Size(my_photoviewer_width, panelMainBrowser.Size.Height);
                        panelMainBrowser.Location = new System.Drawing.Point(picClientCamera.Size.Width);
                        panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width - picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                        lblImagePage.Text = (index + 1).ToString() + " / " + list_pictures.Count;
                        lblImagePage.Location = new System.Drawing.Point((picClientCamera.Size.Width - lblImagePage.Size.Width)/2, lblImagePage.Location.Y);
                        btnPrevImage.Location = new System.Drawing.Point(lblImagePage.Location.X - 40, btnPrevImage.Location.Y);
                        btnNextImage.Location = new System.Drawing.Point(lblImagePage.Location.X + lblImagePage.Size.Width + 10, btnNextImage.Location.Y);
                        btnPrevImage.Visible = true;
                        btnNextImage.Visible = true;

                    }));
                }
                else
                {
                    picClientCamera.Size = new System.Drawing.Size(my_photoviewer_width, panelMainBrowser.Size.Height);
                    panelMainBrowser.Location = new System.Drawing.Point(picClientCamera.Size.Width);
                    panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width - picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                    lblImagePage.Text = (index + 1).ToString() + " / " + list_pictures.Count;
                    lblImagePage.Location = new System.Drawing.Point((picClientCamera.Size.Width - lblImagePage.Size.Width) / 2, lblImagePage.Location.Y);
                    btnPrevImage.Location = new System.Drawing.Point(lblImagePage.Location.X - 40, btnPrevImage.Location.Y);
                    btnNextImage.Location = new System.Drawing.Point(lblImagePage.Location.X + lblImagePage.Size.Width + 10, btnNextImage.Location.Y);
                    btnPrevImage.Visible = true;
                    btnNextImage.Visible = true;
                }

                var cs = picClientCamera.Size;
                //// Load background image
                var img = Image.FromFile(fn);

                float cs_ratio = (float)cs.Width / (float)cs.Height;
                float img_ratio = (float)img.Width / (float)img.Height;

                if (cs_ratio > img_ratio)
                {
                    var part = new Bitmap(img.Width, (int)((float)img.Width / cs_ratio));

                    using (var g = Graphics.FromImage(part))
                    {
                        g.DrawImageUnscaled(img, 0, -(img.Height - (int)((float)img.Width / cs_ratio)) / 2);
                    }
                    img = part;
                }
                else
                {
                    var part = new Bitmap((int)((float)img.Height * cs_ratio), img.Height);

                    using (var g = Graphics.FromImage(part))
                    {
                        g.DrawImageUnscaled(img, -(img.Width - (int)((float)img.Height * cs_ratio)) / 2, 0);
                    }
                    img = part;
                }

                // Load background image
                picClientCamera.BackgroundImageLayout = ImageLayout.Stretch;
                picClientCamera.BackgroundImage = img;
            }
            catch
            {

            }

        }

        public void SetupApplicationSettings()
        {           
            // Check if there is config.xml
            if (!File.Exists(strConfigFileName))
            {
                // If not exist config file, create new config file with INPUT of ServerURL and EndpointID
                string sEndpointId = "";
                string sServerURL = "";

                // Show input dialog for server URL
                while (true)
                {
                    sServerURL = InputBox(Properties.Resources.MSG_TEXT_INPUT_SERVER_URL_LABEL, Properties.Resources.MSG_TEXT_INPUT_SERVER_URL);

                    if (sServerURL.Length > 0)
                    {
                        str_server_url = sServerURL;
                        break;
                    }
                }

                Console.WriteLine("KommFlex >> ServerURL : " + sEndpointId.ToString());
                // Setup the ServerURL in websocket server.
                m_commController.setServerBaseUrl(str_server_url); 

                // Show input dialog for Endpoint ID
                while (true)
                {
                    sEndpointId = InputBox(Properties.Resources.MSG_TEXT_INPUT_ENDPOINT_ID_LABLE, Properties.Resources.MSG_TEXT_INPUT_ENDPOINT_ID);

                    if (sEndpointId.Length > 0)
                    {
                        int dEndpointType = m_commController.getEndpointTypeById(sEndpointId);

                        if (dEndpointType >= 0 && dEndpointType <= 2)
                        {
                            my_endpoint_id = Int32.Parse(sEndpointId);
                            my_endpoint_type = dEndpointType;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine("KommFlex >> My Endpoint ID : " + sEndpointId.ToString());

                // Create config.xml file
                DataSet ds = new DataSet();
                DataTable dt = new DataTable();
                ds.Clear();
                try
                {
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_SERVER_URL);
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_ENDPOINT_ID);
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_STATION_ID);
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_PICTURE_FOLDER);
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_PICTURE_WIDTH);
                    //dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_PICTURE_DISPLAY_APP);
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_PRIMARY_CAMERA);
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_SECONDARY_CAMERA);
                    dt.Columns.Add(Properties.Resources.CONFIG_ITEM_NAME_RING_TIMEOUT);
                    //dt.Columns.Add(Properties.Resources.DEFAULT_DISPATCHER_VIDEO_DISPLAY_MODE);
                    
                    dt.Rows.Add(dt.NewRow());
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_SERVER_URL] = str_server_url;
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_ENDPOINT_ID] = sEndpointId;
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_STATION_ID] = my_station_id;
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_PICTURE_FOLDER] = my_photo_folder;
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_PICTURE_WIDTH] = my_photoviewer_width;
                    //dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_PICTURE_DISPLAY_APP] = my_photo_viewer;
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_PRIMARY_CAMERA] = my_primary_camera;
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_SECONDARY_CAMERA] = my_secondary_camera;
                    dt.Rows[0][Properties.Resources.CONFIG_ITEM_NAME_RING_TIMEOUT] = my_ringing_timeout;
                    //dt.Rows[0][Properties.Resources.DEFAULT_DISPATCHER_VIDEO_DISPLAY_MODE] = my_dispatcher_video_display_mode;

                    ds.Tables.Add(dt);

                    ds.WriteXml(strConfigFileName);

                    m_commController.setStationId(my_station_id);
                }
                catch { }
            }
            else
            {
                // Read config.xml if there is XML file
                DataSet ds = new DataSet();
                DataTable dt = new DataTable();
                ds.Clear();
                try
                {
                    ds.ReadXml(strConfigFileName);

                    string sServerURL = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_SERVER_URL].ToString();
                    string sEndpointId = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_ENDPOINT_ID].ToString();
                    string sStationId = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_STATION_ID].ToString();
                    string sPictureFolder = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_PICTURE_FOLDER].ToString();
                    //string sPictureDisplayApp = ds.Tables[0].Rows[0][4].ToString();
                    string sPictureWidth = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_PICTURE_WIDTH].ToString();
                    string sPrimaryCamera = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_PRIMARY_CAMERA].ToString();
                    string sSecondaryCamera = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_SECONDARY_CAMERA].ToString();
                    string sRingingTimeout = ds.Tables[0].Rows[0][Properties.Resources.CONFIG_ITEM_NAME_RING_TIMEOUT].ToString();
                    //string sDispatcherVideoDisplayMode = ds.Tables[0].Rows[0][8].ToString();

                    if (sServerURL.Trim().Length == 0)
                    {
                        // Exit application if there is not server url
                        MessageBox.Show(Properties.Resources.MSG_TEXT_CANNOT_FIND_SERVER, Properties.Resources.APPLICATION_NAME);
                        System.Environment.Exit(0);
                    }

                    // Reinit members from config.xml
                    str_server_url = sServerURL;
                    my_endpoint_id = Int32.Parse(sEndpointId);
                    m_commController.setServerBaseUrl(str_server_url); 
                    my_endpoint_type = m_commController.getEndpointTypeById(sEndpointId);
                    //my_photo_viewer = sPictureDisplayApp;
                    my_photoviewer_width = Int32.Parse(sPictureWidth);
                    my_photo_folder = sPictureFolder;
                    my_station_id = sStationId;
                    //my_dispatcher_video_display_mode = Int32.Parse(sDispatcherVideoDisplayMode);

                    m_commController.setStationId(my_station_id);
                    try
                    {
                        int ring_time_out = Int32.Parse(sRingingTimeout);
                        if (ring_time_out > 0)
                        {
                            my_ringing_timeout = ring_time_out;
                        }

                    }
                    catch { }

                    try
                    {
                        my_primary_camera = Int32.Parse(sPrimaryCamera);
                        my_secondary_camera = Int32.Parse(sSecondaryCamera);
                    }
                    catch { }

                    Console.WriteLine("KommFlex >> EndpointID : " + my_endpoint_id.ToString());
                    Console.WriteLine("KommFlex >> EndpointType : " + my_endpoint_type);
                }
                catch { }
            }
        }

        public void SetupUI(){
            try
            {
                // Timer Event
                aTimer.Interval = 1000 * my_ringing_timeout;
                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEvent;

                pingTimer.Interval = 1000 * 60 * 10; // 10 mins
                pingTimer.Elapsed += OnTimedPingEvent;
                pingTimer.Enabled = true;
                m_commController.sendPing(my_endpoint_id.ToString());

                // Setup Audio
                System.IO.Stream str = Properties.Resources.ring;
                Player.Stream = str;

                // Set style of buttons
                btnStartClient.Location = new System.Drawing.Point(-500, btnStartClient.Location.Y);

                btnStartCall.FlatStyle = FlatStyle.Flat;
                btnStartCall.FlatAppearance.BorderSize = 0;

                btnExitCall.FlatStyle = FlatStyle.Flat;
                btnExitCall.FlatAppearance.BorderSize = 0;

                btn2ndCamera.FlatStyle = FlatStyle.Flat;
                btn2ndCamera.FlatAppearance.BorderSize = 0;

                btnSendMessage.FlatStyle = FlatStyle.Flat;
                btnSendMessage.FlatAppearance.BorderSize = 0;

                if (my_endpoint_type == 2)
                {
                    // Reconstruct UI layout if this is CLIENT mode.
                    this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;

                    btnStartCall.Visible = false;
                    btn2ndCamera.Visible = false;
                    btnExitCall.Visible = false;
                    btnSendMessage.Visible = false;

                    txtMsgBox.Location = new System.Drawing.Point(0, txtMsgBox.Location.Y);
                    txtMsgBox.Size = new System.Drawing.Size(this.Size.Width, txtMsgBox.Size.Height);
                    txtMsgBox.ReadOnly = true;

                    // Create HOOK object to capture KEY-INPUT in CLIENT mode => PrintScreen KEY
                    m_GlobalHook = Hook.GlobalEvents();
                    m_GlobalHook.KeyUp += GlobalHookKeyPress;

                    picClientCamera.Visible = false;
                    lblImagePage.Visible = false;
                    btnPrevImage.Visible = false;
                    btnNextImage.Visible = false;
                }
                else
                {
                    //if (panelMainBrowser.InvokeRequired)
                    //{
                    //    panelMainBrowser.Invoke(new MethodInvoker(delegate
                    //    {
                    //        panelMainBrowser.Location = new System.Drawing.Point(picClientCamera.Size.Width);
                    //        panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width - picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                    //        picClientCamera.Size = new System.Drawing.Size(picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                    //    }));
                    //}
                    //else
                    //{
                    //    panelMainBrowser.Location = new System.Drawing.Point(picClientCamera.Size.Width);
                    //    panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width - picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                    //    picClientCamera.Size = new System.Drawing.Size(picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                    //}
                }

                if (panelMainBrowser.InvokeRequired)
                {
                    panelMainBrowser.Invoke(new MethodInvoker(delegate
                    {
                        panelMainBrowser.Location = new System.Drawing.Point(0, 0);
                        panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width, panelMainBrowser.Size.Height);
                    }));
                }
                else
                {
                    panelMainBrowser.Location = new System.Drawing.Point(0, 0);
                    panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width, panelMainBrowser.Size.Height);
                }

                if (pictureBackground.InvokeRequired)
                {
                    pictureBackground.Invoke(new MethodInvoker(delegate
                    {
                        pictureBackground.Location = panelMainBrowser.Location;
                        pictureBackground.Size = panelMainBrowser.Size;
                        pictureBackground.BringToFront();
                        lblDescription.BringToFront();
                        // Show the current state in text box
                        txtMsgBox.Text = Properties.Resources.MSG_TEXT_CONNECTING_TO_SERVER;
                    }));
                }
                else
                {
                    pictureBackground.Location = panelMainBrowser.Location;
                    pictureBackground.Size = panelMainBrowser.Size;
                    pictureBackground.BringToFront();
                    lblDescription.BringToFront();
                    // Show the current state in text box
                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_CONNECTING_TO_SERVER;
               }
            }
            catch {
                Console.WriteLine("KommFlex >> Exception SetupUI");
            }

        }

        public void InitBrowser()
        {
            try
            {
                // Load web page in Cef browser
                if (!Cef.IsInitialized)
                {
                    CefSettings settings = new CefSettings();

                    // Add the --enable-media-stream and --enable-usermedia-screen-capturing flag for WebRTC
                    settings.CefCommandLineArgs.Add("enable-media-stream", "1");
                    settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing", "1");
                    settings.CefCommandLineArgs.Remove("mute-audio");
                    Cef.Initialize(settings);
                }

                // MainBrowser for video-chatting
                webBrowser = new ChromiumWebBrowser(str_server_url + "/public/KommFlex.html");
                this.panelMainBrowser.Controls.Add(webBrowser);
                webBrowser.Dock = DockStyle.Fill;
                webBrowser.FrameLoadEnd += OnBrowserFrameLoadEnd;
                webBrowser.BringToFront();

                lblDescription.BringToFront();
            }
            catch
            {
                Console.WriteLine("KommFlex >> Exception InitBrowser");
            }

        }

        public void InitState()
        {
            try
            {                
                if (my_endpoint_type != 2)
                {
                    list_pictures.Clear();
                    picture_index = -1;

                    if (picClientCamera.InvokeRequired)
                    {
                        picClientCamera.Invoke(new MethodInvoker(delegate
                        {
                            picClientCamera.Size = new System.Drawing.Size(0, panelMainBrowser.Size.Height);
                            panelMainBrowser.Location = new System.Drawing.Point(picClientCamera.Size.Width);
                            panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width - picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                            lblImagePage.Text = "";
                            btnPrevImage.Visible = false;
                            btnNextImage.Visible = false;
                        }));
                    }
                    else
                    {
                        picClientCamera.Size = new System.Drawing.Size(0, panelMainBrowser.Size.Height);
                        panelMainBrowser.Location = new System.Drawing.Point(picClientCamera.Size.Width);
                        panelMainBrowser.Size = new System.Drawing.Size(this.Size.Width - picClientCamera.Size.Width, panelMainBrowser.Size.Height);
                        lblImagePage.Text = "";
                        btnPrevImage.Visible = false;
                        btnNextImage.Visible = false;
                    }
                }

                str_redirect_state = "";
                // Set state variable
                str_state = "INIT";

                if (my_endpoint_type == 2)
                {
                    if (str_call_start_time != "" && connected_rtc_id != "")
                    {
                        str_call_end_time = m_commController.getServerTime();
                        string dispatcher_id = m_commController.getEndpointIdByRTCId(connected_rtc_id).ToString();

                        int logid = m_commController.AddCallLog(my_endpoint_id.ToString(), dispatcher_id, str_call_start_time, str_call_end_time, is_passed_central_dispatcher);
                        if (logid > 0)
                        {
                            m_commController.AddPictureLog(logid.ToString(), list_pictures);
                        }
                    }

                    list_pictures.Clear();
                }

                // Remove the old connection data
                connected_rtc_id = "";
                str_call_start_time = "";
                str_call_end_time = "";
                is_passed_central_dispatcher = 0;


                string my_name = m_commController.getEndpointNameById(my_endpoint_id);

                if (lblDescription.InvokeRequired)
                {
                    lblDescription.Invoke(new MethodInvoker(delegate
                    {
                        lblDescription.Text = my_name;// "";
                    }));
                }
                else
                {
                    lblDescription.Text = my_name;// "";
                }
                

                // Stop Timer Conter
                aTimer.Enabled = false;


                // Stop Ringing Audio
                try{
                    Player.Stop();
                }catch{}

                // Show background image
                if (pictureBackground.InvokeRequired)
                {
                    pictureBackground.Invoke(new MethodInvoker(delegate
                    {
                        pictureBackground.Visible = true;
                    }));
                }
                else
                {
                    pictureBackground.Visible = true;
                }

                // Disable StartCall Button
                if (btnStartCall.InvokeRequired)
                {
                    btnStartCall.Invoke(new MethodInvoker(delegate
                    {
                        btnStartCall.Enabled = false;
                        btnStartCall.BackgroundImage = Properties.Resources.icon_camera_disabled;

                    }));
                }
                else
                {
                    btnStartCall.Enabled = false;
                    btnStartCall.BackgroundImage = Properties.Resources.icon_camera_disabled;
                }

                // Disable ExitCall Button
                if (btnExitCall.InvokeRequired)
                {
                    btnExitCall.Invoke(new MethodInvoker(delegate
                    {
                        btnExitCall.Enabled = false;
                        btnExitCall.BackgroundImage = Properties.Resources.icon_exit_disabled;

                    }));
                }
                else
                {
                    btnExitCall.Enabled = false;
                    btnExitCall.BackgroundImage = Properties.Resources.icon_exit_disabled;
                }

                // Disable 2nd-Camera-Picture Button
                if (btn2ndCamera.InvokeRequired)
                {
                    btn2ndCamera.Invoke(new MethodInvoker(delegate
                    {
                        btn2ndCamera.Enabled = false;
                        btn2ndCamera.BackgroundImage = Properties.Resources.icon_2ndcam_disabled;

                    }));
                }
                else
                {
                    btn2ndCamera.Enabled = false;
                    btn2ndCamera.BackgroundImage = Properties.Resources.icon_2ndcam_disabled;
                }

                // Disable SendMessage Button
                if (btnSendMessage.InvokeRequired)
                {
                    btnSendMessage.Invoke(new MethodInvoker(delegate
                    {
                        btnSendMessage.Enabled = false;
                        btnSendMessage.BackgroundImage = Properties.Resources.icon_sendmsg_disabled;

                    }));
                }
                else
                {
                    btnSendMessage.Enabled = false;
                    btnSendMessage.BackgroundImage = Properties.Resources.icon_sendmsg_disabled;
                }

                // Disable MessageTextBox
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        txtMsgBox.Enabled = false;

                    }));
                }
                else
                {
                    txtMsgBox.Enabled = false;
                }

                // Close secondary camera if it is opened already
                //if (camera2 != null)
                //{
                //    if (camera2.IsOpened())
                //    {
                //        camera2.Release();
                //    }
                //}

                try
                {
                    //photo_process.Kill();
                }
                catch
                {

                }

            }
            catch { }
        }

        public void SetCallingState()
        {
            try
            {
                // Set CALLING state
                str_state = "CALLING";
                str_call_start_time = m_commController.getServerTime();

                // Stop Timer Counter
                try
                {
                    aTimer.Enabled = false;
                }
                catch { }

                // Stop Rining Audio
                try{
                    Player.Stop();
                }catch{}

                // Hide background image
                if (pictureBackground.InvokeRequired)
                {
                    pictureBackground.Invoke(new MethodInvoker(delegate
                    {
                        pictureBackground.Location = panelMainBrowser.Location;
                        pictureBackground.Size = panelMainBrowser.Size;
                        pictureBackground.Visible = false;
                    }));
                }
                else
                {
                    pictureBackground.Location = panelMainBrowser.Location;
                    pictureBackground.Size = panelMainBrowser.Size;
                    pictureBackground.Visible = false;
                }


                // Disable StartCall Button
                if (btnStartCall.InvokeRequired)
                {
                    btnStartCall.Invoke(new MethodInvoker(delegate
                    {
                        btnStartCall.Enabled = false;
                        btnStartCall.BackgroundImage = Properties.Resources.icon_camera_disabled;

                    }));
                }
                else
                {
                    btnStartCall.Enabled = false;
                    btnStartCall.BackgroundImage = Properties.Resources.icon_camera_disabled;
                }
                
                // Enable ExitCall Button
                if (btnExitCall.InvokeRequired)
                {
                    btnExitCall.Invoke(new MethodInvoker(delegate
                    {
                        btnExitCall.Enabled = true;
                        btnExitCall.BackgroundImage = Properties.Resources.icon_exit;

                    }));
                }
                else
                {
                    btnExitCall.Enabled = true;
                    btnExitCall.BackgroundImage = Properties.Resources.icon_exit;
                }

                // Enable 2ndCameraPicture Button
                if (btn2ndCamera.InvokeRequired)
                {
                    btn2ndCamera.Invoke(new MethodInvoker(delegate
                    {
                        btn2ndCamera.Enabled = true;
                        btn2ndCamera.BackgroundImage = Properties.Resources.icon_2ndcam;

                    }));
                }
                else
                {
                    btn2ndCamera.Enabled = true;
                    btn2ndCamera.BackgroundImage = Properties.Resources.icon_2ndcam;
                }

                // Enable SendMessage Button
                if (btnSendMessage.InvokeRequired)
                {
                    btnSendMessage.Invoke(new MethodInvoker(delegate
                    {
                        btnSendMessage.Enabled = true;
                        btnSendMessage.BackgroundImage = Properties.Resources.icon_sendmsg;

                    }));
                }
                else
                {
                    btnSendMessage.Enabled = true;
                    btnSendMessage.BackgroundImage = Properties.Resources.icon_sendmsg;
                }

                // Enable MessageTextBox
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        txtMsgBox.Enabled = true;
                        txtMsgBox.Text = "";
                    }));
                }
                else
                {
                    txtMsgBox.Enabled = true;
                    txtMsgBox.Text = "";
                }
            }
            catch
            {

            }

        }

        public void SetInCommingState()
        {
            try
            {
                // Set INCOMMING state
                str_state = "INCOMMING";

                // Stop Timer Counter
                try
                {
                    aTimer.Enabled = false;
                }
                catch { }

                // Play ringing sound
                try{
                    Player.PlayLooping();
                }catch{}

                // Hide background image
                if (pictureBackground.InvokeRequired)
                {
                    pictureBackground.Invoke(new MethodInvoker(delegate
                    {
                        pictureBackground.Location = panelMainBrowser.Location;
                        pictureBackground.Size = panelMainBrowser.Size;
                        pictureBackground.Visible = false;
                    }));
                }
                else
                {
                    pictureBackground.Location = panelMainBrowser.Location;
                    pictureBackground.Size = panelMainBrowser.Size;
                    pictureBackground.Visible = false;
                }
                
                // Enable CallAccept button
                if (btnStartCall.InvokeRequired)
                {
                    btnStartCall.Invoke(new MethodInvoker(delegate
                    {
                        btnStartCall.Enabled = true;
                        btnStartCall.BackgroundImage = Properties.Resources.icon_camera;

                    }));
                }
                else
                {
                    btnStartCall.Enabled = true;
                    btnStartCall.BackgroundImage = Properties.Resources.icon_camera;
                }

                // Enable RejectCall Button
                if (btnExitCall.InvokeRequired)
                {
                    btnExitCall.Invoke(new MethodInvoker(delegate
                    {
                        btnExitCall.Enabled = true;
                        btnExitCall.BackgroundImage = Properties.Resources.icon_exit;

                    }));
                }
                else
                {
                    btnExitCall.Enabled = true;
                    btnExitCall.BackgroundImage = Properties.Resources.icon_exit;
                }

                // Disabele 2ndCamera Picture Button
                if (btn2ndCamera.InvokeRequired)
                {
                    btn2ndCamera.Invoke(new MethodInvoker(delegate
                    {
                        btn2ndCamera.Enabled = false;
                        btn2ndCamera.BackgroundImage = Properties.Resources.icon_2ndcam_disabled;

                    }));
                }
                else
                {
                    btn2ndCamera.Enabled = false;
                    btn2ndCamera.BackgroundImage = Properties.Resources.icon_2ndcam_disabled;
                }

                // Disable SendMessage Button
                if (btnSendMessage.InvokeRequired)
                {
                    btnSendMessage.Invoke(new MethodInvoker(delegate
                    {
                        btnSendMessage.Enabled = false;
                        btnSendMessage.BackgroundImage = Properties.Resources.icon_sendmsg_disabled;

                    }));
                }
                else
                {
                    btnSendMessage.Enabled = false;
                    btnSendMessage.BackgroundImage = Properties.Resources.icon_sendmsg_disabled;
                }

                // Disable MessageTextBox
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        txtMsgBox.Enabled = false;
                        txtMsgBox.Text = Properties.Resources.MSG_TEXT_INCOMMING_CALL;

                    }));
                }
                else
                {
                    txtMsgBox.Enabled = false;
                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_INCOMMING_CALL;
                }
            }
            catch { }
        }

        private void OnBrowserFrameLoadEnd(object sender, FrameLoadEndEventArgs args)
        {
            // Add some progress when loaded web page in cef browser
        }             

        private void WsServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            // Remove websocket client object in the object list
            try
            {
                Console.WriteLine("KommFlex >> WebsocketSession Closed.");
                foreach (WebSocketSession s in wsClientList)
                {
                    if (s == session)
                    {
                        wsClientList.Remove(s);
                        Console.WriteLine("KommFlex >> Removed an Websocket Session!");
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void WsServer_NewDataReceived(WebSocketSession session, byte[] value)
        {
            Console.WriteLine("KommFlex >> NewDataReceived from websocket");
        }

        private void WsServer_NewMessageReceived(WebSocketSession session, string value)
        {
            Console.WriteLine("KommFlex >> New Message Received From Client : " + value);

            JObject json_value = JObject.Parse(value);
            string str_msg = json_value["msg"].ToString();
            string str_data = json_value["data"].ToString();

            if (str_msg.CompareTo("OpenedWebSocketVideoChannel") == 0)
            {
                // Loaded browser contents

            }else if (str_msg.CompareTo("LoggedInRoom") == 0)
            {
                // Logged in KommFlex Video Chat room
                Console.WriteLine("KommFlex >> Logged in KommFlex chat room!");                
                m_commController.updateChannelInfo(str_data, my_endpoint_id.ToString(), my_endpoint_type.ToString(), my_station_id);
                my_rtc_id = str_data;    
                
            }else if (str_msg.CompareTo("LoginFailure") == 0)
            {
                // Failed to login in chat room
                InitState();

                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        txtMsgBox.Text = Properties.Resources.MSG_TEXT_FAIL_CONNECTION;
                    }));
                }

                // Exit application
                MessageBox.Show(Properties.Resources.MSG_TEXT_CLOSED_APP_CONNECTION_ERR, Properties.Resources.APPLICATION_NAME);
                System.Environment.Exit(0);
            }
            else if (str_msg.CompareTo("ConnectedRTCIds") == 0)
            {                        
                // Got the connected client list in chat room
                str_data = str_data.Replace("[", "");
                str_data = str_data.Replace("]", "");
                str_data = str_data.Replace("\"", "");

                // Construct the client list
                string[] str_contact_list = new string[] { "" };
                str_contact_list = str_data.Split(',');
                rtc_id_list.Clear();
                foreach (string rtc_id in str_contact_list)
                {
                    rtc_id_list.Add(rtc_id.Trim());
                }

                // Show current state in TextMessageBox
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        if (rtc_id_list.Count == 0)
                        {
                            // Not found any client to communicate
                            if (my_endpoint_type == 2)
                            {
                                txtMsgBox.Text = Properties.Resources.MSG_TEXT_NO_DISPATCHER;
                            }
                            else
                            {
                                txtMsgBox.Text = Properties.Resources.MSG_TEXT_NO_CLIENT;
                            }
                        }
                        else
                        {
                            // Ready to communicate
                            if (str_state == "INIT")
                            {
                                if(my_endpoint_type == 2)
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_PRESS_BUTTON;
                                else
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_READY_CALL;
                            }
                        }
                    }));
                }                   
            }
            else if (str_msg.CompareTo("InCommingCall") == 0)
            {
                if (str_state == "INIT")
                {
                    if (my_endpoint_type == 2)
                    {
                        // CLIENT can't be called from any CLIENT or DISPATCHER
                        // CLIENT will reject all InCommingCall
                        JObject objMessage = new JObject(
                            new JProperty("msg", "RejectCall"),
                            new JProperty("data", "")
                        );

                        sendMessageToWebsocket(objMessage);
                        InitState();
                        return;
                    }

                    Console.WriteLine("KommFlexx >> Incomming a call from");
                    Console.WriteLine("RTCId : " + str_data);

                    string endpointName = m_commController.getEndpointNameByRTCId(str_data);
                    Console.WriteLine("EndpointName : " + endpointName);

                    ////////Redirect Reason
                    if (my_endpoint_type == 0)
                    {
                        if (str_redirect_state != "")
                            endpointName = endpointName + " (" + str_redirect_state + ")";
                    }
                    ////////////////////////

                    // Display Caller's Name
                    if (lblDescription.InvokeRequired)
                    {
                        lblDescription.Invoke(new MethodInvoker(delegate
                        {
                            lblDescription.Text = endpointName;
                        }));
                    }
                    else
                    {
                        lblDescription.Text = endpointName;
                    }

                    // Change state on dispatcher
                    SetInCommingState();
                }
                else
                {
                    if(my_endpoint_type != 2)
                    {
                        // BUSY NOW
                        JObject objData = new JObject(
                            new JProperty("rtcid", str_data),
                            new JProperty("text", str_cmd_prefix + str_cmd_busy_state)
                        );

                        JObject objMessage = new JObject(
                            new JProperty("msg", "SendTextMessage"),
                            new JProperty("data", objData.ToString())
                        );

                        sendMessageToWebsocket(objMessage);
                    }
                }
            }
            else if (str_msg.CompareTo("CallCancelled") == 0 || str_msg.CompareTo("CallRejected") == 0 || str_msg.CompareTo("CallFailure") == 0 || str_msg.CompareTo("ERROR") == 0 || str_msg.CompareTo("StreamClosed") == 0)
            {
                // If connection is closed, state will be INIT
                InitState();

                // Show state text
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        if (my_endpoint_type == 2)
                        {
                            txtMsgBox.Text = Properties.Resources.MSG_TEXT_PRESS_BUTTON;

                        }else
                        {
                            txtMsgBox.Text = Properties.Resources.MSG_TEXT_READY_CALL;
                        }
                    }));
                } 
            }
            else if (str_msg.CompareTo("CallSuccess") == 0)
            {
                // If CALLING is accepted, video chat is available
                // CALLINGSTATE
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        txtMsgBox.Text = "";
                    }));
                }
                
                SetCallingState();
            }
            else if (str_msg.CompareTo("StreamAccepted") == 0)
            {
                // Remote Video Stream is shown successfully
                // CALLINGSTATE
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        txtMsgBox.Text = "";
                    }));
                }

                
                // Regisger connected RTCID
                connected_rtc_id = str_data;

                SetCallingState();
            }
            else if (str_msg.CompareTo("ERROR") == 0)
            {
                // If any error is gotten while try to communicate,
                // Communcation will be disconnected.
                InitState();
                if (txtMsgBox.InvokeRequired)
                {
                    txtMsgBox.Invoke(new MethodInvoker(delegate
                    {
                        txtMsgBox.Text = "";
                    }));
                } 
            }
            else if (str_msg.CompareTo("ReceivedTextMessage") == 0)
            {
                // Received text-message from websocket
                JObject json_screen_item = JObject.Parse(str_data);
                string str_rtcid = json_screen_item["rtcid"].ToString();
                string str_msg_text = json_screen_item["text"].ToString();

                if (str_msg_text.StartsWith(str_cmd_welcome_message))
                {
                    // got message "Welcome to KommFlex!"
                    // This means CEF browser page loaded successfully

                    // Change Text message box
                    if (txtMsgBox.InvokeRequired)
                    {
                        txtMsgBox.Invoke(new MethodInvoker(delegate
                        {
                            if (str_state == "INIT")
                            {
                                if (my_endpoint_type == 2)
                                {
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_PRESS_BUTTON;
                                }
                                else
                                {
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_READY_CALL;
                                }

                            }
                        }));
                    }    
                }else if (str_msg_text.StartsWith(str_cmd_prefix))
                {
                    // If commands(such as Take2ndCameraPhoto or UploadedPhotoImage) is received, run the proper actions
                    if (str_msg_text.StartsWith(str_cmd_prefix + str_cmd_take_photo))
                    {
                        // Change TextMessageBox
                        if (txtMsgBox.InvokeRequired)
                        {
                            txtMsgBox.Invoke(new MethodInvoker(delegate
                            {
                                txtMsgBox.Text = Properties.Resources.MSG_TEXT_TAKING_PHOTO;
                            }));
                        }

                        // Take a photo from client's 2nd camera
                        string pic_name = takeAPhotoFrom2ndCamera();
                        if (pic_name == "")
                        { 
                            // If failed to take a photo, send the state via websocket
                            JObject objData = new JObject(
                                new JProperty("rtcid", connected_rtc_id),
                                new JProperty("text", str_cmd_prefix + str_cmd_fail_take_photo)
                            );

                            JObject objMessage = new JObject(
                                new JProperty("msg", "SendTextMessage"),
                                new JProperty("data", objData.ToString())
                            );

                            sendMessageToWebsocket(objMessage);

                            // Change TextMessageBox
                            if (txtMsgBox.InvokeRequired)
                            {
                                txtMsgBox.Invoke(new MethodInvoker(delegate
                                {
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_FAIL_TAKING_PHOTO;
                                }));
                            }
                        }
                        else
                        {
                            // If successed to take a photo, try to upload image file
                            string uploaded_name = UploadFile(pic_name);

                            if (uploaded_name == "")
                            {
                                // If failed to uplaod image file, send the state via websocket
                                JObject objData = new JObject(
                                    new JProperty("rtcid", connected_rtc_id),
                                    new JProperty("text", str_cmd_prefix + str_cmd_fail_take_photo)
                                );

                                JObject objMessage = new JObject(
                                    new JProperty("msg", "SendTextMessage"),
                                    new JProperty("data", objData.ToString())
                                );

                                sendMessageToWebsocket(objMessage);

                                // Change TextMessageBox
                                if (txtMsgBox.InvokeRequired)
                                {
                                    txtMsgBox.Invoke(new MethodInvoker(delegate
                                    {
                                        txtMsgBox.Text = Properties.Resources.MSG_TEXT_FAIL_UPLOADING_PHOTO;
                                    }));
                                }
                            }
                            else
                            {
                                // If uploaded a photo successfully, send filename via websocket
                                JObject objData = new JObject(
                                    new JProperty("rtcid", connected_rtc_id),
                                    new JProperty("text", str_cmd_prefix + str_cmd_uploaded_photo + uploaded_name)
                                );

                                JObject objMessage = new JObject(
                                    new JProperty("msg", "SendTextMessage"),
                                    new JProperty("data", objData.ToString())
                                );

                                sendMessageToWebsocket(objMessage);

                                // Change TextMessageBox
                                if (txtMsgBox.InvokeRequired)
                                {
                                    txtMsgBox.Invoke(new MethodInvoker(delegate
                                    {
                                        txtMsgBox.Text = Properties.Resources.MSG_TEXT_SUCCESS_TAKING_PHOTO;
                                    }));
                                }

                                list_pictures.Add(uploaded_name);
                            }
                        }
                    }
                    else if (str_msg_text.StartsWith(str_cmd_prefix + str_cmd_uploaded_photo))
                    {
                        // If received uploaded image file name, try to download image
                        string fn = str_msg_text.Substring(str_cmd_prefix.Length + str_cmd_uploaded_photo.Length);

                        // Setup download directory
                        string ret_filePath = "";
                        if(my_photo_folder.CompareTo("Downloads") == 0)
                            ret_filePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\" + fn + ".jpg";
                        else
                            ret_filePath = my_photo_folder + "\\" + fn + ".jpg";

                        // Try to download
                        WebClient client1 = new WebClient();
                        client1.DownloadFile(str_server_url + "/" + fn, ret_filePath);

                        try
                        {
                            // show picture
                            // photo_process = System.Diagnostics.Process.Start(my_photo_viewer, "\"" + ret_filePath + "\"");
                            list_pictures.Add(ret_filePath);
                            ShowClientPicture(list_pictures.Count - 1);
                        }
                        catch
                        {
                            // Show picture using OpenCV, if there is any error to display image
                            using (var src = new Mat(ret_filePath, ImreadModes.Unchanged))
                            {
                                using (var window = new Window("Client 2nd cam pic", image: src, flags: WindowMode.AutoSize))
                                {
                                    Cv2.WaitKey();
                                }
                            }

                        }

                        // Change TextMessageBox
                        if (txtMsgBox.InvokeRequired)
                        {
                            txtMsgBox.Invoke(new MethodInvoker(delegate
                            {
                                txtMsgBox.Text = "";
                            }));
                        }  
                    }
                    else if (str_msg_text.StartsWith(str_cmd_prefix + str_cmd_fail_take_photo))
                    {
                        // If received FAILED-TAKE-PHOTO, show the state in TextMessageBox
                        if (txtMsgBox.InvokeRequired)
                        {
                            txtMsgBox.Invoke(new MethodInvoker(delegate
                            {
                                txtMsgBox.Text = str_cmd_fail_take_photo;
                            }));
                        }  
                    }
                    else if (str_msg_text.StartsWith(str_cmd_prefix + str_cmd_busy_state))
                    {
                        // BUSY STATE
                        if (str_state == "RINGING")
                        {
                            RedirectCallToCentralDispatcher(str_cmd_redirect_reason_busy);
                        }
                    }
                    else if (str_msg_text.StartsWith(str_cmd_prefix + str_cmd_redirect_reason_busy))
                    {
                        str_redirect_state = "BUSY";
                    }
                    else if (str_msg_text.StartsWith(str_cmd_prefix + str_cmd_redirect_reason_timeout))
                    {
                        str_redirect_state = "TIMEOUT";
                    }
                }
                else
                {
                    // If received general text message, show this message.
                    // Only CLIENT is available to receive message
                    if (my_endpoint_type != 2)
                        return;

                    // Display the Text message received from dispatcher
                    if (txtMsgBox.InvokeRequired)
                    {
                        txtMsgBox.Invoke(new MethodInvoker(delegate
                        {
                            txtMsgBox.Text = str_msg_text;
                        }));
                    }
                }
            }
        }

        private void WsServer_NewSessionConnected(WebSocketSession session)
        {
            try
            {
                // An Web page is loaded in CEF browser
                Console.WriteLine("KommFlex >> NewSessionConnected");

                // Add new client session in client object list
                bool flg = true;
                foreach (WebSocketSession s in wsClientList)
                {
                    if (s == session)
                    {
                        flg = false;
                    }
                }
                if (flg)
                {
                    wsClientList.Add(session);
                    Console.WriteLine("Happy : Added New Session!");
                }

            }
            catch { }
        }

        private string takeAPhotoFrom2ndCamera()
        {
            try
            {
                //for (int cam_num = 0; cam_num < 2; cam_num++)
                {
                    if (!relayController.SendSerialMsg(RelayControllerMessages.LightsOn))
                    {
                        txtMsgBox.Text = relayController.ErrorMessage;
                    }

                    // Open Camera
                    //camera2 = new VideoCapture(my_secondary_camera);

                    //Cv2.WaitKey(1000);

                    // Check if camera is opened successfully.
                    if (camera2.IsOpened())
                    {
                        // Setup resolution of camera
                        camera2.Set(CaptureProperty.FrameWidth, 1920);
                        camera2.Set(CaptureProperty.FrameHeight, 1080);
                        int cam_w = (int)(camera2.Get(CaptureProperty.FrameWidth));
                        int cam_h = (int)(camera2.Get(CaptureProperty.FrameHeight));
                        camera2.Set(CaptureProperty.FrameWidth, cam_w);
                        camera2.Set(CaptureProperty.FrameHeight, cam_h);

                        // Create frame image buffer
                        Mat image = new Mat();
                        Cv2.WaitKey(1000);

                        int cnt = 0;
                        //Console.WriteLine("KommFlex >> Opened Camera : " + my_primary_camera.ToString());
                        while (cnt < 30)
                        {
                            // Try to take a photo from camera
                            camera2.Read(image);
                            if (!image.Empty())
                            {
                                // Setup picture folder
                                string picture_name = "";
                                if (my_photo_folder.CompareTo("Downloads") == 0)
                                    picture_name = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg";
                                else
                                    picture_name = my_photo_folder + "\\" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg";

                                // Save Image
                                image.SaveImage(picture_name);

                                Cv2.WaitKey(500);

                                // Close camera
                                //camera2.Release();
                                return picture_name;
                            }
                            else
                                cnt++;

                            Cv2.WaitKey(500);
                        }

                        //camera2.Release();
                    }
                    else
                    {

                    }
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                if (!relayController.SendSerialMsg(RelayControllerMessages.LightsOff))
                {
                    txtMsgBox.Text = relayController.ErrorMessage;
                }
            }

            return "";
        }

        private void sendMessageToWebsocket(JObject msgObj)
        {
            // Send message via websocket
            try
            {
                foreach (WebSocketSession session in wsClientList)
                {
                    session.Send(msgObj.ToString());

                    Console.WriteLine("KommFlex >> Sent new message to Websocket Client : " + msgObj.ToString());
                }

            }
            catch { }
        }

        private bool IsControlAtFront(Control control)
        {
            // Check if UI element stands at front of window
            try
            {
                while (control.Parent != null)
                {
                    if (control.Parent.Controls.GetChildIndex(control) == 0)
                    {
                        control = control.Parent;
                        if (control.Parent == null)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;

            }
            catch { return false; }
        }

        // Take 2nd camera photo 
        private void btn2ndCamera_Click(object sender, EventArgs e)
        {
            // When try to take a photo from DISPATCHER
            try
            {
                if (connected_rtc_id.Length == 0)
                    return;

                // Send COMMAND message to client to take a photo 
                JObject objData = new JObject(
                    new JProperty("rtcid", connected_rtc_id),
                    new JProperty("text", str_cmd_prefix + str_cmd_take_photo)
                );

                JObject objMessage = new JObject(
                    new JProperty("msg", "SendTextMessage"),
                    new JProperty("data", objData.ToString())
                );

                sendMessageToWebsocket(objMessage);

                // Change state message box
                txtMsgBox.Text = Properties.Resources.MSG_TEXT_TAKING_CLIENT_PHOTO;

            }
            catch { }
        }

        // Accept Call in Dispatcher side
        private void btnCall_Click(object sender, EventArgs e)
        {
            // On DISPATCHER, when click ACCEPT button
            // Send ACCEPT state via websocket
            try
            {
                if (str_state == "INCOMMING" && my_endpoint_type != 2)
                {
                    JObject objMessage = new JObject(
                        new JProperty("msg", "AcceptCall"),
                        new JProperty("data", "")
                    );
                    sendMessageToWebsocket(objMessage);
                }

            }
            catch { }
        }
        
        private string UploadFile(string filePath)
        {
            // Try to upload file (from filePath) 
            Console.WriteLine("KommFlex >> Uploading a file");

            // Create Socket Object
            var client = new WebClient();
            var uri = new Uri(str_server_url + "/UploadFile/");
            try
            {
                client.Headers.Add("fileName", System.IO.Path.GetFileName(filePath));
                byte[] res = client.UploadFile(uri, filePath);
                string result = System.Text.Encoding.UTF8.GetString(res);
                Console.WriteLine("KommFlex >> Upload File : " + result);

                return result;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "KommFlex");
                return "";
            }
        }
        
        private void GlobalHookKeyPress(object sender, KeyEventArgs e)
        {
            // On client side, ring to dispatcher once click a button
            CallDispatcherFromClient(e);

        }

        private void OnCallButtonKeyUp(object sender, KeyEventArgs e)
        {
            CallDispatcherFromClient(e);
        }

        private void CallDispatcherFromClient(KeyEventArgs e)
        {
            // On client side, ring to dispatcher once click a button
            try
            {
                ActiveControl = btnStartClient;
                if (my_endpoint_type == 2)
                {
                    if (str_state == "INIT")
                    {
                        // On CLIENT side, check all key-press
                        Console.WriteLine("KommFlex >> KeyPress: \t{0}", e.KeyCode);

                        // Check if pressed PrtScreen key
                        if (e.KeyCode == Keys.PrintScreen || e.KeyCode == Keys.Print)
                        {
                            // Get a available dispatcher rtc id
                            connected_rtc_id = m_commController.getRTCIdByEndpointId(my_dispatcher_id);//m_commController.findAvailableDispatcherRTCId(rtc_id_list);

                            Console.WriteLine("KommFlex >> Found a dispatcher : " + connected_rtc_id);

                            // Set State : RINGING
                            str_state = "RINGING";

                            // Check if WebRTC id is available
                            if (connected_rtc_id.Length == 0 || !rtc_id_list.Contains(connected_rtc_id))
                            {
                                RedirectCallToCentralDispatcher(str_cmd_redirect_reason_timeout);
                                return;
                            }

                            // Send Call request
                            putRing();

                            // Change TextMessageBox
                            if (txtMsgBox.InvokeRequired)
                            {
                                txtMsgBox.Invoke(new MethodInvoker(delegate
                                {
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_CALLING_DISPATCHER;
                                }));
                            }
                            else
                            {
                                txtMsgBox.Text = Properties.Resources.MSG_TEXT_CALLING_DISPATCHER;
                            }
                        }
                    }
                }
                ActiveControl = btnStartClient;

            }
            catch (Exception)
            {
                try
                {
                    InitState();
                    ActiveControl = btnStartClient;
                }
                catch
                {

                }
            }
        }
        
        private void putRing()
        {
            try
            {
                if (connected_rtc_id.Length == 0)
                    return;

                // Start Timer Counter for redirection               
                try
                {
                    aTimer.Enabled = true;
                }
                catch { }

                // Play rining sound
                try
                {
                    Player.PlayLooping();
                }
                catch { }

                // Send Ring Request via websocket
                JObject objData = new JObject(
                    new JProperty("rtcid", connected_rtc_id),
                    new JProperty("camnum", my_secondary_camera)
                );

                JObject objMessage = new JObject(
                    new JProperty("msg", "Ring"),
                    new JProperty("data", objData.ToString())
                );

                sendMessageToWebsocket(objMessage);

                // Hide Background Image
                if (pictureBackground.InvokeRequired)
                {
                    pictureBackground.Invoke(new MethodInvoker(delegate
                    {
                        pictureBackground.Visible = false;
                    }));
                }
                else
                {
                    pictureBackground.Visible = false;
                }

                // Display dispatcher's name
                string endpointName = m_commController.getEndpointNameByRTCId(connected_rtc_id);
                if (lblDescription.InvokeRequired)
                {
                    lblDescription.Invoke(new MethodInvoker(delegate
                    {
                        lblDescription.Text = endpointName;
                    }));
                }
                else
                {
                    lblDescription.Text = endpointName;
                }
            }
            catch (Exception)
            {

            }
        }

        private void RedirectCallToCentralDispatcher(string reason)
        {
            // When emmited timer event for redirection
            try
            {
                // Check if CLIENT side and RINGING state
                if (my_endpoint_type == 2)
                {
                    if (str_state == "RINGING")
                    {
                        // Cancel old call request
                        JObject objMessage = new JObject(
                            new JProperty("msg", "HangUp"),
                            new JProperty("data", "")
                        );

                        sendMessageToWebsocket(objMessage);

                        // Stop timer event
                        aTimer.Enabled = false;

                        if (reason == str_cmd_redirect_reason_timeout)
                            is_passed_central_dispatcher = 1;
                        else if (reason == str_cmd_redirect_reason_busy)
                            is_passed_central_dispatcher = 2;

                        // Change Text Message Box
                        if (txtMsgBox.InvokeRequired)
                        {
                            txtMsgBox.Invoke(new MethodInvoker(delegate
                            {
                                txtMsgBox.Text = Properties.Resources.MSG_TEXT_REDIRECTING_CALL;

                            }));
                        }
                        else
                        {
                            txtMsgBox.Text = Properties.Resources.MSG_TEXT_REDIRECTING_CALL;
                        }


                        // Redirecting
                        try
                        {
                            // Get Central Dispatcher
                            Console.WriteLine("KommFlex >> Get Central dispatcher");
                            connected_rtc_id = m_commController.findCentralDispatcherRTCId(rtc_id_list);

                            Console.WriteLine("KommFlexx >> Central Dispatcher ID" + connected_rtc_id);

                            // Check central dispatcher id and change state
                            if (connected_rtc_id.Length == 0)
                            {
                                if (txtMsgBox.InvokeRequired)
                                {
                                    txtMsgBox.Invoke(new MethodInvoker(delegate
                                    {
                                        InitState();
                                        txtMsgBox.Text = Properties.Resources.MSG_TEXT_FAIL_REDIRECTING_CALL;

                                    }));
                                }
                                else
                                {
                                    InitState();
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_FAIL_REDIRECTING_CALL;
                                }
                                return;
                            }

                            ///////////////////////////////////
                            JObject objData = new JObject(
                                new JProperty("rtcid", connected_rtc_id),
                                new JProperty("text", str_cmd_prefix + reason)
                            );

                            JObject objMessage1 = new JObject(
                                new JProperty("msg", "SendTextMessage"),
                                new JProperty("data", objData.ToString())
                            );

                            sendMessageToWebsocket(objMessage1);
                            ////////////////////////////////////

                            // Send call request to dispatcher
                            putRing();

                            // Chage TextMessageBox
                            if (txtMsgBox.InvokeRequired)
                            {
                                txtMsgBox.Invoke(new MethodInvoker(delegate
                                {
                                    txtMsgBox.Text = Properties.Resources.MSG_TEXT_CALLING_CENTRAL_DISPATCHER;
                                }));
                            }
                            else
                            {
                                txtMsgBox.Text = Properties.Resources.MSG_TEXT_CALLING_CENTRAL_DISPATCHER;
                            }

                            // Change state : "RINGING"
                            str_state = "RINGING";

                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch { }
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            RedirectCallToCentralDispatcher(str_cmd_redirect_reason_timeout);
        }

        private void OnTimedPingEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("ping : " + my_endpoint_id);
            m_commController.sendPing(my_endpoint_id.ToString());
        }

        // SendMessage - Handler
        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            // When clicked SendMessageBox button
            SendMessageToClient();
        }
        
        // TextMessage - Key event handler
        private void OnMessageText_KeyUp(object sender, KeyEventArgs e)
        {
            // Send message when click enter on DISPATCHER mode
            if (e.KeyCode == Keys.Enter && my_endpoint_type != 2)
            {
                SendMessageToClient();
            }
        }

        // SendMessage - body
        private void SendMessageToClient()
        {
            try
            {
                string msg = txtMsgBox.Text;

                if (connected_rtc_id.Length == 0)
                    return;

                // Ignore EMPTY text
                if (msg.Trim().Length == 0)
                    return;

                // SendTextMessage
                JObject objData = new JObject(
                    new JProperty("rtcid", connected_rtc_id),
                    new JProperty("text", msg)
                );

                JObject objMessage = new JObject(
                    new JProperty("msg", "SendTextMessage"),
                    new JProperty("data", objData.ToString())
                );

                sendMessageToWebsocket(objMessage);

                txtMsgBox.Text = "";

            }
            catch { }
        }
        // Exit-Call
        private void btnExitCall_Click(object sender, EventArgs e)
        {
            // When clicked ExitCall button
            try
            {
                // Send Exit via Websocket
                if (str_state == "CALLING")
                {
                    JObject objMessage = new JObject(
                        new JProperty("msg", "HangUp"),
                        new JProperty("data", "")
                    );

                    sendMessageToWebsocket(objMessage);
                }
                else if (str_state == "INCOMMING")
                {
                    // Send Ring
                    JObject objMessage = new JObject(
                        new JProperty("msg", "RejectCall"),
                        new JProperty("data", "")
                    );

                    sendMessageToWebsocket(objMessage);
                }
            }
            catch
            { }
        }        

        // Window-activated handler
        private void OnActivated(object sender, EventArgs e)
        {
            // Manage UI elements
            if (my_endpoint_type == 2)
            {                
                ActiveControl = btnStartClient;

            }
        }

        // Video-activated handler
        private void OnVideoPanelFocus(object sender, EventArgs e)
        {
            try
            {
                // Manage UI elements
                if (my_endpoint_type == 2)
                {
                    ActiveControl = btnStartClient;
                    btnStartClient.Focus();
                }

            }
            catch { }

        }

        // Window resize - handler
        private void OnWindowResize(object sender, EventArgs e)
        {
            // Remake background image whenever resized window
            SetupBackgroundImage();
            ShowClientPicture(picture_index);
        }

        // Input Dialog
        public static string InputBox(string text, string caption)
        {
            // Create InputBox dialog for input some data such as ServerURL or EndpointID
            Form prompt = new Form()
            {
                Width = 450,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Text = text };
            TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 320, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.ControlBox = false;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        private void OnPrevImage(object sender, EventArgs e)
        {
            if (list_pictures.Count > 0)
            {
                ShowClientPicture((picture_index - 1 + list_pictures.Count) % list_pictures.Count);
            }
        }

        private void OnNextImage(object sender, EventArgs e)
        {
            if (list_pictures.Count > 0)
            {
                ShowClientPicture((picture_index + 1) % list_pictures.Count);
            }
        }
    }
}
