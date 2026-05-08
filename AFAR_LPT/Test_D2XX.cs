using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using static FTD2XX_NET.FTDI;

namespace AFAR_LPT
{
    public class Test_D2XX
    {
        static FTDI port1 = new FTDI();
        static FTDI port2 = new FTDI();

        private static int N = 6; // количество бит в управляющем слове по умолчанию


        // ------------------------------- Тест записи данных по линии ПК - Преобр
        public static void Start1()
        {
            string COMPortName = "COM2";

            Console.WriteLine("\n\t\tТест Преобразователя D2XX");

            FTDI.FT_STATUS status;
            bool Exit = false;
            uint baudRate = 9600;
            
            // Открытие порта Преобразователя (по индексу 0)
            
            OpenPort(port1, 0, baudRate, 'w', 500, 500);
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

                WriteCommand(port1, commandBytes);


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

            Console.WriteLine("\n\t\tТест двух преобразователей D2XX");

            bool Exit = false;


            uint baudRate = 9600;

            string COMPortName1 = "COM2";
            string COMPortName2 = "COM3";

            port1.GetCOMPort(out COMPortName1);
            if (OpenPort(port1, 0, baudRate, 'w', 500, 500) == FT_STATUS.FT_OK) {
                Console.WriteLine($"Преобразователь 1 открыт на запись по {COMPortName1}");
                Thread.Sleep(500);
            }

            port2.GetCOMPort(out COMPortName2);
            if (OpenPort(port2, 1, baudRate, 'r', 500, 500) == FT_STATUS.FT_OK)
            {
                Console.WriteLine($"Преобразователь 2 открыт на чтение по {COMPortName2}");
                Thread.Sleep(500);
            }

            byte command = 0b0001010;

            byte[] commandBytes = { command };
            byte[] accepted = new byte[1];

            int count = 1;

            while (!Exit) {
                Console.WriteLine("command: " + Convert.ToString(commandBytes[0], 2));
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

                commandBytes[0] ^= 1;
                count++;

            }

            port1.Close();
            port2.Close();

        }
        // ------------------------------- Тест записи данных "Бегущие огни" по линии ПК - Преобр
        public static void RunningLights()
        {
            int delay = 200; // задержка в миллисекундах

            Console.WriteLine("\n\t\tТест Преобразователя \"Бегущие огни\" D2XX");

            bool Exit = false;
            // Параметры COM-порта 
            string comPortName1 = "COM6";


            uint baudRate = 9600;

            OpenPort(port1, 0, baudRate, ' ', 500, 500);
            Thread.Sleep(500);


            // Формирование диагонального массива данных 
            byte[] commandBytes = new byte[8];
            for (int i = 0; i < 8; i++) {
                commandBytes[i] = (byte)(1<<i);
            }

            // массив буфера данных для работы с FT_Write();
            byte[] dataBuffer = {0x00};
            // Сброс в "0" всех битов перед началом отправки данных
            dataBuffer[0] = 0x00;
            WriteCommand(port1, dataBuffer);

            while (!Exit)
            {
                for (int i = 0; i < 8; i++)
                {
                    dataBuffer[0] = commandBytes[i];
                    Console.Write("command: " + Convert.ToString(dataBuffer[0], 2).PadLeft(8, '0'));
                    Thread.Sleep(delay);

                    if (WriteCommand(port1, dataBuffer) == FT_STATUS.FT_OK) Console.WriteLine("\tОК");

                    Thread.Sleep(delay*2);
                }
                // Сброс в "0" всех битов перед завершением отправки данных
                dataBuffer[0] = 0x00;
                WriteCommand(port1, dataBuffer);

                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    Exit = true;
                }
                Console.WriteLine();

            }
            port1.Close();

        }



        // --------------------------------- Служебные функции-обёртки
        static FT_STATUS OpenPort(FTDI port, uint portIndex, uint baudRate, char pinConfig,
                        int writeTimeout, int readTimeout) {
            FT_STATUS status;

            // Установка скорости
            port.SetBaudRate(baudRate);
            
            // Открытие порта Преобразователя (по индексу 0)
            status = port.OpenByIndex(0);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("Не удалось открыть устройство");
                return status;
            }

            // ucMask требуемое значение для битовой маски режима.
            // Это устанавливает, какие биты работают как входы, какие как выходы.
            // Значение бита 0 устанавливает соответствующий вывод как вход,
            // а 1 устанавливает соответствующий вывод как выход. 
            byte mask = 0xFF;   // Все выводы как выходы
            switch (pinConfig) {
                case 'w': mask = 0xFF; break;
                case 'r': mask = 0x00; break;
                default: break;
            }

            // Включение асинхронного режима Bit Bang
            status = port.SetBitMode(mask, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);

            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("Не удалось включить Bit Bang режим");
                port.Close();
                return status;
                
            }
            return status;
        }

        static FT_STATUS WriteCommand(FTDI port, byte[] commandBytes) {
            FT_STATUS status = FT_STATUS.FT_OK;

            uint bytesWritten = 0;
            try
            {
                if (!port.IsOpen)
                {
                    Console.WriteLine($" - порт устройства {port.GetCOMPort} закрыт. переоткрытие...");
                    port.OpenByIndex(0);
                }

                status = port.Write(commandBytes, 1, ref bytesWritten);
                if (status != FT_STATUS.FT_OK) {
                    Console.WriteLine("Запись не удалась");
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine(" - Запись: данные отсутствуют");
                status = FT_STATUS.FT_FAILED_TO_WRITE_DEVICE;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                status = FT_STATUS.FT_DEVICE_NOT_FOUND;
                //ChangePortName(port);
            }
            return status;
        }

        static void ReadCommand(FTDI port, byte[] commandBytes) {
            uint bytesWritten = 0;
            try
            {
                if (!port.IsOpen)
                {
                    Console.WriteLine($" - порт {port.GetCOMPort} закрыт. переоткрытие...");
                    port.OpenByIndex(1);
                }
                port.Read(commandBytes, 1, ref bytesWritten);
            }
            catch (TimeoutException)
            {
                Console.WriteLine(" - Чтение: данные отсутствуют");
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                //ChangePortName(port);
            }
        }
        static void ChangePortName(SerialPort port) {
            bool choose = true;
            while (choose)
            {
                Console.Write($" - порт {port.PortName} недоступен. Сменить? [1] - да, [2] - нет: ");
                if (Console.ReadKey().Key == ConsoleKey.D2)
                {
                    Console.WriteLine();
                    return;
                }
                if (Console.ReadKey().Key == ConsoleKey.D1)
                {
                    Console.WriteLine();
                    choose = !choose;
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
