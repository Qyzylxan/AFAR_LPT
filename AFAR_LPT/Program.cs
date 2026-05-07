using System;
using System.IO.Ports;
using System.Threading;

namespace AFAR_LPT
{
    class Program
    {
        private static SerialPort port;
        private static int N = 6; // количество бит в управляющем слове по умолчанию
        private static int Mask;

        static void Main(string[] args)
        {
            bool work = true;
            ConsoleKey key;
            while(work) {
                Console.Write("\nВыбор режима работы: \n " +
                    "1 - Тест одного преобразователя \n " +
                    "2 - Тест двух преобразователей \n " +
                    "3 - Запись в МУАФ \n " +
                    "4 - Бегущие огни на преобразователе \n " +
                    "Q - выход");

                Console.Write("\n> ");
                key = Console.ReadKey().Key;
                Console.WriteLine();
                switch (key) {
                    case ConsoleKey.D1: Test.Start1(); break;
                    case ConsoleKey.D2: Test.Start2(); break;
                    case ConsoleKey.D3: MUAF.Program(port, N, Mask); break;
                    case ConsoleKey.D4: Test.RunningLights(); break;
                    case ConsoleKey.Q: work = false; break;
                    default: break;
                }
            }
            return;


            bool Exit;  // флаг выхода из программы
            char[] Command = new char[255];     // буфер промежуточного хранения команды
            int State;      // состояние анализатора команды
            int Cmd;        // код команды
            int N_Module = 0;   // количество модулей
            int[] Data = null;
            int DataCounter;

            // Параметры COM-порта 
            string comPortName = "COM6";
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

            Console.Write("\n>");

            Exit = false;  // не выход
            State = 0;     // начало приема команды
            Cmd = 0;       // нет команды
            DataCounter = 0; // нет данных

            // Формирование маски для заданного количества разрядов
            Mask = 1;
            for (int i = 0; i < N; i++)
                Mask |= (1 << i);   // формируем маску для заданнго количества разрядов

            do
            {
                //Command = "";
                Console.WriteLine("Доступные команды: \n" +
                    "A - команда записи в аттенюаторы\n" +
                    "F - команда записи в фазовращатели\n" +
                    "C - команда одновременной записи\n" +
                    "E/Q - команды выхода\n" +
                    " Введите команду: ");
                if (State != 3)     // если ждем данных, не отрабатываем
                {
                    Command = Console.ReadLine().ToCharArray();     // читаем введенное слово до пробела
                }
                switch (State)
                {
                    case 0: // начало обработки команды
                        Console.WriteLine("начало обработки команды");
                        Console.WriteLine($"Command[0] == {Command[0]}");
                        if ((Command[0] == 'A') || (Command[0] == 'a'))
                        {
                            Cmd = 1;  // команда записи в аттенюаторы
                            State = 1;
                            Console.WriteLine($"Cmd = {Cmd}; State = {State}");
                        }
                        else if ((Command[0] == 'F') || (Command[0] == 'f'))
                        {
                            Cmd = 2;  // команда записи в фазовращатели
                            State = 1;
                        }
                        else if ((Command[0] == 'C') || (Command[0] == 'c'))
                        {
                            Cmd = 3;  // команда одновременной записи 
                            State = 1;
                        }
                        else if ((Command[0] == 'E') || (Command[0] == 'e'))
                        {
                            Exit = true;  // команда выхода
                        }
                        else if ((Command[0] == 'Q') || (Command[0] == 'q'))
                        {
                            Exit = true;  // команда выхода
                        }
                        else
                        {
                            Console.WriteLine($"no such command: {Command}\n>");
                            Cmd = 0;
                            State = 0;
                            // нераспознанная команда - ничего не меняем
                        }
                        break;

                    case 1: // команда принята, читаем первый аргумент
                        Console.WriteLine("команда принята, читаем первый аргумент");
                        Console.Write($"{Command} = ");
                        printCharArray(Command);
                        
                        for (int i = 1; i < Command.Length; i++){
                            if (Char.IsDigit(Command[i]))
                            {
                                N_Module = Command[i];
                                if (Cmd == 1 || Cmd == 2)       // команды раздельной записи
                                {
                                    Console.WriteLine("команды раздельной записи");
                                    Data = new int[N_Module];
                                }
                                else if (Cmd == 3)              // команда одновременной записи
                                {
                                    Console.WriteLine("команды одновременной записи");
                                    Data = new int[2 * N_Module];
                                }

                                DataCounter = 0;
                                State = 2;
                            }
                            else
                            {
                                Console.WriteLine("error");
                                Console.WriteLine($"ошибка чтения команды: {Command} -> {N_Module} ");
                                State = 0;
                            }
                        }
                        break;

                    case 2: // первый аргумент принят, читаем все остальные
                        Console.WriteLine("первый аргумент принят, читаем все остальные");
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

                    case 3:
                        Console.WriteLine("получено все, что нужно. Отрабатываем команду");
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
                        State = 0;     // все сбрасываем.
                        Cmd = 0;
                        N_Module = 0;
                        DataCounter = 0;
                        Console.WriteLine("done\n>");
                        break;
                    default:
                        break;
                }
            }
            while (!Exit);

            Console.WriteLine("end working!");
            port?.Close();
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
            Thread.Sleep(1); // примерно 1 мс, в оригинале был пустой цикл
        }

        static void printCharArray(char[] arr) {
            foreach (char a in arr) {
                Console.Write(a + ", ");
            }
            Console.WriteLine();
        }
    }
}