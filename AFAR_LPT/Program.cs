using System;
using System.IO.Ports;
using System.Threading;

namespace AFAR
{
    class Program
    {
        private static SerialPort port;
        private static int N = 6; // количество бит в управляющем слове по умолчанию
        private static int Mask;

        static void Main(string[] args)
        {
            bool Exit = false;
            int State = 0;
            int Cmd = 0;
            int N_Module = 0;
            int[] Data = null;
            int DataCounter = 0;

            // Параметры COM-порта 
            string comPortName = "COM9";
            int baudRate = 9600;          
            Parity parity = Parity.None;
            int dataBits = 8;
            StopBits stopBits = StopBits.One;

            string[] args1 = new string[]
            {
                comPortName,    // Имя порта 
                dataBits.ToString()        // Количество бит 
            };


            // Инициализация порта
            if (args1.Length > 0)
            {
                try
                {
                    port = new SerialPort();
                    port.PortName = args1[0];
                    port.BaudRate = baudRate;
                    port.Parity = parity;
                    port.DataBits = dataBits;
                    port.StopBits = stopBits;
                    port.WriteTimeout = 1000;

                    port.Open();

                    Console.WriteLine($"port {args1[0]} is being used");
                    Console.WriteLine($"Configuration: {baudRate} baud, {dataBits} data bits, {parity} parity, {stopBits} stop bits");
                }
                catch
                {
                    Console.WriteLine($"port {args1[0]} could not be open");
                    return;
                }

                if (args1.Length > 1)
                {
                    if (!int.TryParse(args1[1], out N))
                    {
                        Console.WriteLine("invalid N bit");
                        return;
                    }
                }
                Console.WriteLine($"b_nit={N}");
            }
            else
            {
                Console.WriteLine("usage: afar.exe <lpt_n> <n_bit>");
                Console.WriteLine("if n_bit ommited, n_bit=6 is assumed");
                //Console.WriteLine("example: afar.exe lpt1 8");
                return;
            }

            // Формирование маски для заданного количества разрядов
            Mask = 1;
            for (int i = 0; i < N; i++)
                Mask |= (1 << i);

            Console.Write("\n>");

            do
            {
                string Command = "";

                if (State != 3)
                {
                    Command = Console.ReadLine()?.Trim() ?? "";

                    // Разбиваем строку на части по пробелам
                    string[] parts = Command.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string part in parts)
                    {
                        ProcessCommand(part, ref State, ref Cmd, ref N_Module, ref Data, ref DataCounter, ref Exit);
                        if (Exit) break;
                    }
                }
                else
                {
                    // В состоянии 3 данные уже есть, просто выполняем команду
                    ExecuteCommand(Cmd, N_Module, Data, ref State, ref Cmd, ref N_Module, ref DataCounter);
                }
            }
            while (!Exit);

            Console.WriteLine("end working!");
            port?.Close();
        }

        static void ProcessCommand(string Command, ref int State, ref int Cmd, ref int N_Module,
                                   ref int[] Data, ref int DataCounter, ref bool Exit)
        {
            switch (State)
            {
                case 0: // начало обработки команды
                    string cmdUpper = Command.ToUpper();
                    if (cmdUpper == "A")
                    {
                        Cmd = 1; // команда записи в аттенюаторы
                        State = 1;
                    }
                    else if (cmdUpper == "F")
                    {
                        Cmd = 2; // команда записи в фазовращатели
                        State = 1;
                    }
                    else if (cmdUpper == "C")
                    {
                        Cmd = 3; // команда одновременной записи
                        State = 1;
                    }
                    else if (cmdUpper == "E" || cmdUpper == "Q")
                    {
                        Exit = true; // команда выхода
                    }
                    else
                    {
                        Console.WriteLine($"no such command: {Command}\n>");
                        Cmd = 0;
                        State = 0;
                    }
                    break;

                case 1: // команда принята, читаем первый аргумент
                    if (int.TryParse(Command, out N_Module))
                    {
                        if (Cmd == 1 || Cmd == 2)
                            Data = new int[N_Module];
                        else if (Cmd == 3)
                            Data = new int[2 * N_Module];

                        DataCounter = 0;
                        State = 2;
                    }
                    else
                    {
                        Console.WriteLine("error");
                        State = 0;
                    }
                    break;

                case 2: // первый аргумент принят, читаем все остальные
                    if (int.TryParse(Command, out int value))
                    {
                        Data[DataCounter] = value;
                        DataCounter++;

                        if ((DataCounter == N_Module) && (Cmd == 1 || Cmd == 2))
                            State = 3;
                        else if ((DataCounter == 2 * N_Module) && (Cmd == 3))
                            State = 3;
                    }
                    break;
            }
        }

        static void ExecuteCommand(int Cmd, int N_Module, int[] Data,
                                   ref int State, ref int CmdReset, ref int N_ModuleReset, ref int DataCounterReset)
        {
            if (Cmd == 1)
            {
                Console.Write("A write: ");
                for (int i = 0; i < N_Module; i++)
                {
                    AttWrite((byte)Data[i]);
                    Console.Write($"{Data[i] & Mask} ");
                }
                Console.WriteLine();
            }
            else if (Cmd == 2)
            {
                Console.Write("F write: ");
                for (int i = 0; i < N_Module; i++)
                {
                    PhaseWrite((byte)Data[i]);
                    Console.Write($"{Data[i] & Mask} ");
                }
                Console.WriteLine();
            }
            else if (Cmd == 3)
            {
                for (int i = 0; i < N_Module; i++)
                {
                    AttPhaseWrite((byte)Data[i], (byte)Data[i + N_Module]);
                }
                Console.Write("A write: ");
                for (int i = 0; i < N_Module; i++)
                {
                    Console.Write($"{Data[i] & Mask} ");
                }
                Console.WriteLine();
                Console.Write("F write: ");
                for (int i = 0; i < N_Module; i++)
                {
                    Console.Write($"{Data[i + N_Module] & Mask} ");
                }
                Console.WriteLine();
            }

            // Сброс состояния
            State = 0;
            CmdReset = 0;
            N_ModuleReset = 0;
            DataCounterReset = 0;
            Console.WriteLine("done\n>");
        }

        static void AttWrite(byte X)
        {
            for (int i = 0; i < N; i++)
            {
                byte bit = (byte)((X >> (N - i - 1)) & 0x01);

                byte D = (byte)((0 << 2) | (0 << 1) | bit);
                WriteToPort(D);
                Delay();

                D = (byte)((0 << 2) | (1 << 1) | bit);
                WriteToPort(D);
                Delay();
            }

            // Фиксация данных
            byte lastBit = (byte)((X >> (N - N)) & 0x01);
            byte D_le = (byte)((1 << 2) | (0 << 1) | lastBit);
            WriteToPort(D_le);
            Delay();

            D_le = (byte)((0 << 2) | (0 << 1) | lastBit);
            WriteToPort(D_le);
            Delay();
        }

        static void PhaseWrite(byte Y)
        {
            for (int i = 0; i < N; i++)
            {
                byte bit = (byte)(((Y >> (N - i - 1)) & 0x01) << 4);

                byte D = (byte)((0 << 6) | (0 << 5) | bit);
                WriteToPort(D);
                Delay();

                D = (byte)((0 << 6) | (1 << 5) | bit);
                WriteToPort(D);
                Delay();
            }

            // Фиксация данных
            byte lastBit = (byte)(((Y >> (N - N)) & 0x01) << 4);
            byte D_le = (byte)((1 << 6) | (0 << 5) | lastBit);
            WriteToPort(D_le);
            Delay();

            D_le = (byte)((0 << 6) | (0 << 5) | lastBit);
            WriteToPort(D_le);
            Delay();
        }

        static void AttPhaseWrite(byte X, byte Y)
        {
            for (int i = 0; i < N; i++)
            {
                byte phaseBit = (byte)(((Y >> (N - i - 1)) & 0x01) << 4);
                byte attBit = (byte)((X >> (N - i - 1)) & 0x01);

                byte D = (byte)((0 << 6) | (0 << 5) | phaseBit | (0 << 2) | (0 << 1) | attBit);
                WriteToPort(D);
                Delay();

                D = (byte)((0 << 6) | (1 << 5) | phaseBit | (0 << 2) | (1 << 1) | attBit);
                WriteToPort(D);
                Delay();
            }

            // Фиксация данных
            byte lastPhaseBit = (byte)(((Y >> (N - N)) & 0x01) << 4);
            byte lastAttBit = (byte)((X >> (N - N)) & 0x01);

            byte D_le = (byte)((1 << 6) | (0 << 5) | lastPhaseBit | (1 << 2) | (0 << 1) | lastAttBit);
            WriteToPort(D_le);
            Delay();

            D_le = (byte)((0 << 6) | (0 << 5) | lastPhaseBit | (0 << 2) | (0 << 1) | lastAttBit);
            WriteToPort(D_le);
            Delay();
        }

        static void WriteToPort(byte data)
        {
            if (port != null && port.IsOpen)
            {
                byte[] buffer = new byte[] { data };
                port.Write(buffer, 0, 1);
                // Для отладки можно раскомментировать:
                // Console.Write($"[{data:X2}]");
            }
        }

        static void Delay()
        {
            Thread.Sleep(1); // ~1 мс, в оригинале был пустой цикл
        }
    }
}