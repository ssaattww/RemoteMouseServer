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
        private static bool handled = false;
        private static List<byte> buffer = new List<byte>();
        public static void Main()
        {
            using (var m5stackSp = MouseTask.getM5StackSerialPort())
            {
                m5stackSp.BaudRate = 115200;
                m5stackSp.Open();

                var mouse = new MouseTask(m5stackSp);
                if (mouse != null)
                {
                    var pos = new MousePos { buttons = 1, pressing = 1 };
                    Console.WriteLine(mouse.Move(pos).Result ? "true" : "false");
                }
                m5stackSp.Close();
            }

            //mainAction();
        }
        private static void mainAction()
        {
            var devNames = GetDeviceNames().ToList();

            var regComNo = new System.Text.RegularExpressions.Regex("(COM[1-9][0-9]?[0-9]?)");
            var regM5StackDevName = new System.Text.RegularExpressions.Regex("CP210x");

            var m5stackCOM = devNames
                .Where(devName => regM5StackDevName.IsMatch(devName))
                .Select(devName => regComNo.Match(devName).Value)
                .FirstOrDefault();

            if (m5stackCOM is null) return;

            SerialPort mySerialPort = new SerialPort(m5stackCOM);

            mySerialPort.BaudRate = 115200;

            mySerialPort.Open();
            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            
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
            var decoded = new List<byte>();

            for (int i = 0; i < 2; i++)
            {
                mySerialPort.Write(encoded, 0, encoded.Count());
                while (!handled) Thread.Sleep(10);
                handled = false;
                decoded = COBS.Decode(buffer).ToList();
                if (decoded.SequenceEqual(bytes)) Console.WriteLine("matched");
            }

            pos.pressing = 0;
            bytes = new List<byte> { pos.byte5, pos.byte4, pos.byte3, pos.byte2, pos.byte1, pos.byte0 };
            encoded = COBS.Encode(bytes).ToArray();
            mySerialPort.Write(encoded, 0, encoded.Count());

            while (!handled) Thread.Sleep(10);
            handled = false;
            decoded = COBS.Decode(buffer).ToList();
            if (decoded.SequenceEqual(bytes)) Console.WriteLine("matched");

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
            // string indata = sp.ReadExisting();
            var size = sp.BytesToRead;
            buffer.Clear();

            Console.WriteLine($"recieved data size:{size}");
            for(int i = 0; i < size; i++)
            {
                buffer.Add((byte)sp.ReadByte());
            }
            buffer.ForEach(buf => Console.Write($"{buf},"));
            Console.WriteLine();

            handled = true;
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
}
