using System;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace RemoteController
{
    class PortDataReceived
    {
        public static void Main()
        {
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
