using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;


namespace LabStend_AFAR
{
    class Program
    {
        // afar.cpp : Defines the entry point for the console application.


        SerialPort port;

        int N = 6;  // количество бит в управляющем слове по умолчанию

        static void Main(string[] args)
        {
            bool Exit;           // флаг выхода из программы
            char Command[255];  // буфер промежуточного хранения команды
            int State;           // состояние анализатора команды
            int Cmd;             // код команды
            int N_Module;        // количество модулей
            int* Data;
            int DataCounter;
            int i;
            int Mask;

            i = 0;

            if (argc > 1)  // если есть аргумент командной строки
            {
                port = fopen(argv[1], "wb");
                if (port == NULL)
                {
                    Console.Write("port %s could not be open\n");
                    return;
                }
                else
                {
                    Console.Write("port %s is being used\n", argv[1]);
                }
                if (argc > 2)
                {
                    if (!sscanf(argv[2], "%i", &N))
                    {
                        Console.Write("invalid N bit\n");
                        return;
                    }
                }
                Console.Write("b_nit=%i\n", N);
            }
            else
            {
                Console.Write("usage: afar.exe <lpt_n> <n_bit>\nif n_bit ommited, n_bit=6 is assumed\nexample: afar.exe lpt1 8\n");

                /*
                Console.Write("need specify port name!\n");
                scanf("%s", Command);
                port = fopen(Command,"wb");
                if(port==NULL)
                {
                Console.Write("port %s could not be open\n", Command);
                return 0;
                }
                */
                return;
            }

            Console.Write("\n>");

            Exit = false;  // не выход
            State = 0;     // начало приема команды
            Cmd = 0;       // нет команды
            DataCounter = 0; // нет данных

            Mask = 1;
            for (i = 0; i < N; i++)
                Mask |= (1 << i);   // формируем маску для заданнго количества разрядов

            do
            {
                if (State != 3)  // если ждем данных, не отрабатываем
                {
                    scanf("%s", Command);  // читаем введенное слово до пробела
                }
                switch (State)
                {
                    //----------------------------------------------------------------
                    case 0: // начало обработки команды
                        if ((Command[0] == 'A') || (Command[0] == 'a'))
                        {
                            Cmd = 1;  // команда записи в аттеньюаторы
                            State = 1;
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
                            Console.Write("no such command: %s\n>", Command);
                            Cmd = 0;
                            State = 0;
                            // нераспознанная команда - ничего не меняем
                        }
                        break;
                    //----------------------------------------------------------------
                    case 1: // команда принята, читаем первый аргумент
                        if (sscanf(Command, "%i", &N_Module))
                        {
                            if ((Cmd == 1) || (Cmd == 2))  // команды раздельной записи
                                Data = (int*)calloc(N_Module, sizeof(int)); // выделяем память под хранение данных
                            else if (Cmd == 3)            // команда одновременной записи
                                Data = (int*)calloc(2 * N_Module, sizeof(int)); // выделяем память под хранение данных

                            DataCounter = 0; // обнуляем счетчик аргументов
                            State = 2;  // принято количество модулей в цепочке
                        }
                        else
                        {
                            Console.Write("error\n");
                            State = 0;
                        }

                        break;
                    //----------------------------------------------------------------
                    case 2: // первый аргумент принят, читаем все остальные
                        if (sscanf(Command, "%i", Data + DataCounter))  // читаем данное и записываем в нужную позицию
                        {
                            DataCounter++;
                            if ((DataCounter == N_Module) && ((Cmd == 1) || (Cmd == 2))) // получены все данные
                                State = 3;
                            else if ((DataCounter == 2 * N_Module) && ((Cmd == 3)))    // получены все данные
                                State = 3;
                        }

                        break;
                    //----------------------------------------------------------------
                    case 3:   // получено все, что нужно. Отрабатываем команду

                        if (Cmd == 1)
                        {
                            Console.Write("A write: ");
                            for (i = 0; i < N_Module; i++)
                            {
                                AttWrite((unsigned char)Data[i]);
                                Console.Write("%i ", Data[i] & Mask);
                            }
                            Console.Write("\n");
                        }
                        else if (Cmd == 2)
                        {
                            Console.Write("F write: ");
                            for (i = 0; i < N_Module; i++)
                            {
                                PhaseWrite((unsigned char)Data[i]);
                                Console.Write("%i ", Data[i] & Mask);
                            }
                            Console.Write("\n");
                        }
                        else if (Cmd == 3)
                        {
                            for (i = 0; i < N_Module; i++)
                            {
                                AttPhaseWrite((unsigned char)Data[i], (unsigned char)Data[i + N_Module]);
                            }
                            Console.Write("A write:");
                            for (i = 0; i < N_Module; i++)
                            {
                                Console.Write("%i ", Data[i] & Mask);
                            }
                            Console.Write("\n");
                            Console.Write("F write: ");
                            for (i = 0; i < N_Module; i++)
                            {
                                Console.Write("%i ", Data[i + N_Module] & Mask);
                            }
                            Console.Write("\n");
                        }

                        free(Data);  // освобождаем память

                        State = 0;   // все сбрасываем.
                        Cmd = 0;
                        N_Module = 0;
                        DataCounter = 0;
                        Console.Write("done\n>");
                        break;
                    //----------------------------------------------------------------
                    default:
                        break;
                }
            }
            while (!Exit);


            Console.Write("end working!\n");
            fclose(port);

        }

        //----------------------------------------------------------------
        void AttWrite(unsigned char X)
        {
            int i;
            unsigned char D;

            for (i = 0; i < N; i++)
            {
                D = (0 << 2) | (0 << 1) | ((X >> (N - i - 1)) & 0x01);
                fputc(D, port);
                delay();
                D = (0 << 2) | (1 << 1) | ((X >> (N - i - 1)) & 0x01);
                fputc(D, port);
                delay();
            }
            D = (1 << 2) | (0 << 1) | ((X >> (N - i - 1)) & 0x01);
            fputc(D, port);
            delay();
            D = (0 << 2) | (0 << 1) | ((X >> (N - i - 1)) & 0x01);
            fputc(D, port);
            delay();
        }

        //------------------------------------------------------------
        void PhaseWrite(unsigned char Y)
        {
            unsigned char D;
            int i;

            for (i = 0; i < N; i++)
            {
                D = (0 << 6) | (0 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4);
                fputc(D, port);
                delay();
                D = (0 << 6) | (1 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4);
                fputc(D, port);
                delay();
            }
            D = (1 << 6) | (0 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4);
            fputc(D, port);
            delay();
            D = (0 << 6) | (0 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4);
            fputc(D, port);
            delay();
        }
        //------------------------------------------------------------
        void AttPhaseWrite(unsigned char X, unsigned char Y)
        {
            int i;
            unsigned char D;

            for (i = 0; i < N; i++)
            {
                D = (0 << 6) | (0 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4) | (0 << 2) | (0 << 1) | ((X >> (N - i - 1)) & 0x01);
                fputc(D, port);
                delay();
                D = (0 << 6) | (1 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4) | (0 << 2) | (1 << 1) | ((X >> (N - i - 1)) & 0x01);
                fputc(D, port);
                delay();
            }
            D = (1 << 6) | (0 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4) | (1 << 2) | (0 << 1) | ((X >> (N - i - 1)) & 0x01);
            fputc(D, port);
            delay();
            D = (0 << 6) | (0 << 5) | (((Y >> (N - i - 1)) & 0x01) << 4) | (0 << 2) | (0 << 1) | ((X >> (N - i - 1)) & 0x01);
            fputc(D, port);
            delay();
        }


        //------------------------------------------------------------
        inline void delay()
        {
            for (int i = 0; i < 1000; i++) ;
        }
    }
}
