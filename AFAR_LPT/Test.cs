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


        // ------------------------------- Тест записи данных по линии ПК - Преобр
        public static void Start1()
        {

            Console.WriteLine("\n\t\tТест Преобразователя");

            bool Exit = false;
            // Параметры COM-порта 
            string comPortName1 = "COM3";
            

            int baudRate = 9600;

            Parity parity = Parity.None;
            int dataBits = 8;
            StopBits stopBits = StopBits.One;

            port1 = openPort(port1, comPortName1, baudRate, parity, dataBits, stopBits, 500, 500);
            Thread.Sleep(500);


            byte command = 0b0010010;

            byte[] commandBytes = { command };
            //byte[] accepted = new byte[1];

            int count = 1;

            while (!Exit)
            {
                Console.WriteLine("command: " + Convert.ToString(commandBytes[0], 2).PadLeft(8, '0'));
                Thread.Sleep(500);
                Console.WriteLine($"Тест №{count}:");

                WriteCommand(port1, commandBytes, out Exit);


                Console.WriteLine("Принято");

                Thread.Sleep(1000);

                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    Exit = true;
                }
                Console.WriteLine();

                commandBytes[0] ^= 1;
                count++;

            }
            port1.Close();

        }

        // -------------------------------Тест линии передачи ПК - Преобр (прм) - Преобр (прд) - ПК
        public static void Start2() {

            Console.WriteLine("\n\t\tТест двух преобразователей");

            bool Exit = false;
            // Параметры COM-порта 
            string comPortName1 = "COM3";
            string comPortName2 = "COM6";

            int baudRate = 9600;

            Parity parity = Parity.None;
            int dataBits = 8;
            StopBits stopBits = StopBits.One;

            port1 = openPort(port1, comPortName1, baudRate, parity, dataBits, stopBits, 500, 500);
            Thread.Sleep(500);

            port2 = openPort(port2, comPortName2, baudRate, parity, dataBits, stopBits, 500, 500);
            Thread.Sleep(500);


            byte command = 0b0001010;

            byte[] commandBytes = { command };
            byte[] accepted = new byte[1];

            int count = 1;

            while (!Exit) {
                Console.WriteLine("command: " + Convert.ToString(commandBytes[0], 2));
                Thread.Sleep(500);
                Console.WriteLine($"Тест №{count}:");

                WriteCommand(port1, commandBytes, out Exit);

                ReadCommand(port2, accepted, out Exit);

                Console.WriteLine("accepted: " +  accepted);

                Thread.Sleep(1000);

                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    Exit = true;
                }
                Console.WriteLine();

                commandBytes[0] ^= 1;
                count++;

            }

            port1.Close();
            port2.Close();

        }
        // ------------------------------- Тест записи данных "Бегущие огни" по линии ПК - Преобр
        public static void RunningLights()
        {

            Console.WriteLine("\n\t\tТест Преобразователя \"Бегущие огни\"");

            int delay = 200; // задержка в миллисекундах

            bool Exit = false;
            // Параметры COM-порта 
            string comPortName1 = "COM6";


            int baudRate = 9600;

            Parity parity = Parity.None;
            int dataBits = 8;
            StopBits stopBits = StopBits.One;

            port1 = openPort(port1, comPortName1, baudRate, parity, dataBits, stopBits, 500, 500);
            Thread.Sleep(500);


            byte command = 0b0010010;
            
            byte[] commandBytes = new byte[8];
            for (int i = 0; i < 8; i++) {
                commandBytes[i] = (byte)(1<<i);
            }


            while (!Exit)
            {
                for (int i = 0; i < 8; i++)
                {
                    Console.Write("command: " + Convert.ToString(commandBytes[i], 2).PadLeft(8, '0'));
                    Thread.Sleep(delay);
                    //Console.WriteLine($"Тест №{count}:");

                    WriteCommand(port1, commandBytes, out Exit);

                    Console.WriteLine("\tОК");

                    Thread.Sleep(delay * 2);
                    if (Exit == true) return;
                }

                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    Exit = true;
                }
                Console.WriteLine();

            }
            port1.Close();

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
                return port;
            }
        }

        static void WriteCommand(SerialPort port, byte[] commandBytes, out bool Exit) {
            Exit = false;
            try
            {
                if (!port.IsOpen)
                {
                    Console.WriteLine($" - порт {port.PortName} закрыт. переоткрытие...");
                    port.Open();
                }
                port.Write(commandBytes, 0, 1);
            }
            catch (TimeoutException)
            {
                Console.WriteLine(" - Запись: данные отсутствуют");
            }
            catch (FileNotFoundException)
            {
                ChangePortName(port, out Exit);
            }
        }

        static void ReadCommand(SerialPort port, byte[] commandBytes, out bool Exit) {
            Exit = false;
            try
            {
                if (!port.IsOpen)
                {
                    Console.WriteLine($" - порт {port.PortName} закрыт. переоткрытие...");
                    port.Open();
                }
                port.Read(commandBytes, 0, 1);
            }
            catch (TimeoutException)
            {
                Console.WriteLine(" - Чтение: данные отсутствуют");
            }
            catch (FileNotFoundException) {
                ChangePortName(port, out Exit);
            }
        }
        static void ChangePortName(SerialPort port, out bool Exit) {
            Exit = false;
            bool choose = true;
            ConsoleKey key;
            while (choose)
            {
                Console.Write($" - порт {port.PortName} недоступен. Сменить? [1] - да, [2] - нет, [Q] - выход: ");
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.D2)
                {
                    Console.WriteLine();
                    return;
                }
                if (key == ConsoleKey.D1)
                {
                    Console.WriteLine();
                    choose = !choose;
                }
                if (key == ConsoleKey.Q)
                {
                    Console.WriteLine();
                    Exit = true;
                    return;
                }
                Console.WriteLine();
            }
            Console.Write("Введите новый номер порта: ");
            int No;
            while (true){
                try
                {
                    No = int.Parse(Console.ReadLine());
                    Console.WriteLine();
                    break;
                }
                catch
                {
                    Console.WriteLine(" - Неверный формат числа");
                }
            }
            port.PortName = $"COM{No}";
            Console.WriteLine($"Новый номер порта: COM{No}");
        } 

    }
}
