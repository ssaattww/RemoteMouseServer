using System;
using System.IO.Ports;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RemoteController
{
    [SupportedOSPlatform("windows")]
    class PortDataReceived
    {
        public static void Main()
        {
            var devNames = GetDeviceNames().ToList();
            var regComNo = new System.Text.RegularExpressions.Regex("(COM[1-9][0-9]?[0-9]?)");
            var regM5StackDevName = new System.Text.RegularExpressions.Regex("CP210x");
            var m5stackCOM = devNames
                .Where(devName => regM5StackDevName.IsMatch(devName))
                .Select(devName => regComNo.Match(devName).Value).FirstOrDefault();
            Console.WriteLine(m5stackCOM);
            SerialPort mySerialPort = new SerialPort("COM11");

            mySerialPort.BaudRate = 115200;

            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            mySerialPort.Open();
            MousePos pos = new MousePos();
            pos.buttons = (sbyte)Buttons.LEFT;
            pos.pressing = (sbyte)(1);
            pos.x = 0;
            pos.y = 0;
            pos.wheel = 0;
            pos.hwheel = 0;
            var bytes = new List<byte> { pos.byte0, pos.byte1, pos.byte2, pos.byte3, pos.byte4, pos.byte5 };
            var encoded = COBS.Encode(bytes).ToArray();
            Console.WriteLine("start");
            mySerialPort.Write(encoded, 0, encoded.Count());
            for(int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                mySerialPort.Write(encoded, 0, encoded.Count());
            }
            
            pos.pressing = 0;
            
            Thread.Sleep(100);
            bytes = new List<byte> { pos.byte0, pos.byte1, pos.byte2, pos.byte3, pos.byte4, pos.byte5 };
            encoded = COBS.Encode(bytes).ToArray();
            mySerialPort.Write(encoded, 0, encoded.Count());

            Console.WriteLine("Press any key to continue...");
            Console.WriteLine();
            Console.ReadKey();
            mySerialPort.Close();
        }

        private static void DataReceivedHandler(
                            object sender,
                            SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received:");
            Console.Write(indata);
        }
        public static string[] GetDeviceNames()
        {
            var deviceNameList = new System.Collections.ArrayList();
            var check = new System.Text.RegularExpressions.Regex("(COM[1-9][0-9]?[0-9]?)");

            ManagementClass mcPnPEntity = new ManagementClass("Win32_PnPEntity");
            ManagementObjectCollection manageObjCol = mcPnPEntity.GetInstances();

            //全てのPnPデバイスを探索しシリアル通信が行われるデバイスを随時追加する
            foreach (ManagementObject manageObj in manageObjCol)
            {
                //Nameプロパティを取得
                var namePropertyValue = manageObj.GetPropertyValue("Name");
                if (namePropertyValue == null)
                {
                    continue;
                }
                else
                {
                    //Nameプロパティ文字列の一部が"(COM1)～(COM999)"と一致するときリストに追加"
                    string name = namePropertyValue.ToString();
                    if (check.IsMatch(name))
                    {
                        deviceNameList.Add(name);
                    }
                }
            }

            //戻り値作成
            if (deviceNameList.Count > 0)
            {
                string[] deviceNames = new string[deviceNameList.Count];
                int index = 0;
                foreach (var name in deviceNameList)
                {
                    deviceNames[index++] = name.ToString();
                }
                return deviceNames;
            }
            else
            {
                return null;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MousePos
    {
        [FieldOffset(0)]
        public sbyte buttons;

        [FieldOffset(1)]
        public sbyte pressing;

        [FieldOffset(2)]
        public sbyte x;

        [FieldOffset(3)]
        public sbyte y;

        [FieldOffset(4)]
        public sbyte wheel;

        [FieldOffset(5)]
        public sbyte hwheel;

        [FieldOffset(0)]
        public byte byte0;
        [FieldOffset(1)]
        public byte byte1;
        [FieldOffset(2)]
        public byte byte2;
        [FieldOffset(3)]
        public byte byte3;
        [FieldOffset(4)]
        public byte byte4;
        [FieldOffset(5)]
        public byte byte5;
    }

    public enum Buttons
    {
        LEFT = 1,
        RIGHT = 2,
        MIDDLE = 4,
        BACK = 8,
        FORWARD = 16,
    }
}
