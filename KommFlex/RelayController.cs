using System;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KommFlex
{
    class RelayController
    {
        private SerialPort serialPort1 = null;
        private Byte serialPortData = 0;
        public string ErrorMessage = "Ok.";

        public enum RelayControllerMessages
        {
            InitializePort = 80,
            InitializeTransport = 81,
            LightsOn = 0,
            LightsOff = 6,
        }

        public RelayController()
        {            

        }

        public bool Initialize()
        {
            bool bSuccess = false;
            try
            {
                serialPort1 = new SerialPort();
                serialPort1.PortName = "COM3";
                serialPort1.BaudRate = 9600;
                serialPort1.Parity = Parity.None;
                serialPort1.StopBits = StopBits.One;
                serialPort1.Handshake = Handshake.None;

                serialPort1.DataReceived += SerialPort1_DataReceived;

                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }

                serialPort1.Open(); //opens the port
                serialPort1.ReadTimeout = 500;

                if (serialPort1.IsOpen)
                {
                    SendSerialMsg(RelayControllerMessages.InitializePort);
                    SendSerialMsg(RelayControllerMessages.InitializeTransport);
                    SendSerialMsg(RelayControllerMessages.LightsOff);

                    bSuccess = true;
                    ErrorMessage = "Ok.";
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                
            }

            return bSuccess;
        }

        ~RelayController()
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    SendSerialMsg(RelayControllerMessages.LightsOff); // turn off the lights
                    serialPort1.DataReceived -= SerialPort1_DataReceived;
                    serialPort1.Close();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public bool SendSerialMsg(RelayControllerMessages b)
        {
            serialPortData = (Byte)b;           

            try
            {
                if (serialPort1.IsOpen)
                {
                    Byte[] Data = new byte[1] { serialPortData };
                    serialPort1.Write(Data, 0, 1); // send data to port
                    OpenCvSharp.Cv2.WaitKey(200);
                    ErrorMessage = "Ok.";
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return true;
        }

        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

        }
    }
}
