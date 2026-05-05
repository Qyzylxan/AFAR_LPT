using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace AFAR_LPT
{
    public class Test
    {
        private static SerialPort port1 = new SerialPort();
        private static SerialPort port2 = new SerialPort();
        private static int N = 6; // количество бит в управляющем слове по умолчанию

        // Тест линии передачи ПК - Преобр (прм) - Преобр (прд) - ПК
        public static void Start() {

            Console.WriteLine("\n\t\tТест Преобразователей");

            bool Exit = false;
            // Параметры COM-порта 
            string comPortName1 = "COM6";
            string comPortName2 = "COM7";

            int baudRate = 9600;

            Parity parity = Parity.None;
            int dataBits = 8;
            StopBits stopBits = StopBits.One;

            port1 = openPort(port1, comPortName1, baudRate, parity, dataBits, stopBits, 500, 500);
            Thread.Sleep(500);

            port2 = openPort(port2, comPortName2, baudRate, parity, dataBits, stopBits, 500, 500);
            Thread.Sleep(500);


            byte command = 0b0010010;

            byte[] commandBytes = { command };
            byte[] accepted = new byte[1];

            int count = 1;
            while (!Exit) {
                Console.WriteLine("command: " + command);
                Thread.Sleep(500);
                Console.WriteLine($"Тест №{count}:");

                WriteCommand(port1, commandBytes);

                ReadCommand(port2, accepted);

                Console.WriteLine("accepted: " +  accepted);

                Thread.Sleep(1000);

                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    Exit = true;
                }
                Console.WriteLine();

                count++;

            }

            port1.Close();
            port2.Close();


        }

        static SerialPort openPort(SerialPort port, string portName, int baudRate, Parity parity, 
                        int dataBits, StopBits stopBits, int writeTimeout, int readTimeout) {
            
            port = new SerialPort();
            port.PortName = portName;
            port.BaudRate = baudRate;
            port.Parity = parity;
            port.DataBits = dataBits;
            port.StopBits = stopBits;
            port.WriteTimeout = writeTimeout;
            port.ReadTimeout = readTimeout;
            try
            {
                port.Open();

                Console.WriteLine($"port {portName} is being used");
                Console.WriteLine($"Configuration: {baudRate} baud, {dataBits} data bits, {parity} parity, {stopBits} stop bits");
                return port;
            }
            catch
            {
                Console.WriteLine($"port {portName} could not be open");
                return null;
            }
        }

        static void WriteCommand(SerialPort port, byte[] commandBytes) {

            if (!port.IsOpen) {
                Console.WriteLine($"порт {port.PortName} закрыт. переоткрытие...");
                port.Open();
            }
            port.Write(commandBytes, 0, 1);
            
        }

        static void ReadCommand(SerialPort port, byte[] commandBytes) {
            if (!port.IsOpen)
            {
                Console.WriteLine($"порт {port.PortName} закрыт. переоткрытие...");
                port.Open();
            }
            try
            {
                port.Read(commandBytes, 0, 1);
            }
            catch (TimeoutException) {
                Console.WriteLine("Чтение: данные отсутствуют");
            }
        }

    }
}
