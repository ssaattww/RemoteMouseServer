using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Management;
using System.Runtime.Versioning;

namespace RemoteController
{
    [SupportedOSPlatform("windows")]
    internal class MouseTask
    {
        private SerialPort serialPort;

        private IObservable<object> serialObserbable;
        private List<byte> buffer = new List<byte>();
        private Func<MousePos, Task<bool>> serialWritePosFunc;
        public MouseTask(SerialPort _serialPort)
        {

            serialPort = _serialPort;
            serialObserbable = Observable.FromEvent<SerialDataReceivedEventHandler, object>
            (
                handler => (sender, e) => handler(sender),
                handler =>
                {
                    serialPort.DataReceived += handler;
                },
                handler =>
                {
                    serialPort.DataReceived -= handler;
                }
            );

            serialWritePosFunc = async pos =>
            {
                var sendBytes = new List<byte> { pos.byte0, pos.byte1, pos.byte2, pos.byte3, pos.byte4, pos.byte5 };
                var recievedBytes = new List<byte>();
                
                var encodedBytes = COBS.Encode(sendBytes).ToArray();
                var decodedBytes = new List<byte>();

                var recievedBytesObserbable = serialObserbable.Select(sender =>
                {
                    var sp = sender as SerialPort;
                    var buf = new List<byte>();
                    if (sp != null)
                    {
                        var size = sp.BytesToRead;
                        for (int i = 0; i < size; i++) buf.Add((byte)sp.ReadByte());
                    }
                    return buf;
                }).Take(1);
                serialPort.Write(encodedBytes, 0, encodedBytes.Length);
                recievedBytes = await recievedBytesObserbable;
                decodedBytes = COBS.Decode(recievedBytes).ToList();

                return sendBytes.SequenceEqual(decodedBytes);
            };
        }

        public async Task<bool> Move(MousePos pos)
        {
            return await serialWritePosFunc(pos);
        }

        public static SerialPort getM5StackSerialPort()
        {
            var devNames = GetDeviceNames().ToList();

            var regComNo = new System.Text.RegularExpressions.Regex("(COM[1-9][0-9]?[0-9]?)");
            var regM5StackDevName = new System.Text.RegularExpressions.Regex("CP210x");

            var m5stackCOM = devNames
                .Where(devName => regM5StackDevName.IsMatch(devName))
                .Select(devName => regComNo.Match(devName).Value)
                .FirstOrDefault();
            if (m5stackCOM == null) return null;
            else return new SerialPort(m5stackCOM);
        }
        private static string[] GetDeviceNames()
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
