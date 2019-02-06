using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

// ` is a special character in this interpreter used for timing stuff
// It will be stored as timestamps, and then the diff of the pairs will be shown

namespace BrainfuckInterpreter
{
    internal class Program
    {
        // Used in underlining in debug
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private const string UNDERLINE = "\x1B[4m";
        private const string RESET = "\x1B[24m";

        // Verbose information
        private static int _totalInstructionRan;
        private static List<Stopwatch> _stopwatches = new List<Stopwatch>();
        private static List<int> _totalTicks = new List<int>();
        private static bool _addTotalTicks;

        // Used in interpreting
        private static bool _outputNumber;
        private static bool _skip;
        private static int _skipLoop;
        private static int _memIndex;
        private static int _maxMemIndex;
        private static int _commandIndex;
        private static int _loopIndex;
        private static string _output = string.Empty;
        private static List<char> _mem = new List<char> { default };
        private static List<int> _loop = new List<int> { default };

        /// <summary>
        /// This is an interpreter of the Brainfuck langauge https://en.wikipedia.org/wiki/Brainfuck
        /// </summary>
        /// <param name="file">File to interpret</param>
        /// <param name="verbose">Show all values of memory changes</param>
        /// <param name="debug">Debug line by showing dump step by step</param>
        /// <param name="step">Wait on user input before debugging to next character</param>
        /// <param name="timer">If debuggin, wait x milliseconds to go to next step</param>
        /// <param name="clear">Run Console.Clear() on every step for cleaner viewring</param>
        /// <param name="num">Output numbers instead of characters</param>
        private static void Main(FileInfo file,
                                bool verbose,
                                bool debug,
                                bool clear,
                                int timer,
                                bool step = true,
                                bool num = false)
        {
            if (!file.Exists)
            {
                Console.WriteLine("Please provide a valid file path");
                return;
            }

            _outputNumber = num;

            if (debug)
            {
                IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
                GetConsoleMode(handle, out var mode);
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                SetConsoleMode(handle, mode);

                // If timer is set, then we don't want to wait to loop
                if (timer != 0)
                {
                    step = false;
                }
            }

            var characters = new char[] { '>', '<', '+', '-', '.', ',', '[', ']', '`' };

            var commands = File.ReadAllText(file.FullName)
                .Where(x => characters.Contains(x))
                .ToArray();

            while (_commandIndex < commands.Length)
            {
                if (debug && clear)
                {
                    Console.Clear();
                }

                Next(commands[_commandIndex]);
                _totalInstructionRan++;
                if (_addTotalTicks)
                {
                    _totalTicks[_totalTicks.Count - 1]++;
                }

                if (debug)
                {
                    Console.Write($"\nSkip: {_skip} - {_skipLoop}");
                    Console.Write($"\nCommands: {Underline("", commands.Cast<object>(), _commandIndex)}");
                    Console.Write($"\n   Loops: {Underline(",", _loop.Cast<object>(), _loopIndex)}");
                    Console.Write($"\n  Memory: {Underline(",", _mem.Cast<object>(), _memIndex, (o) => (int)(char)o)}\n");

                    if (step)
                    {
                        Console.ReadLine();
                    }
                    else
                    {
                        Task.Delay(timer).Wait();
                    }
                }
                _commandIndex++;
            }
            Console.WriteLine();

            if (verbose)
            {
                Console.WriteLine("---");
                Console.WriteLine($"Max Mem Index: {_maxMemIndex}");
                Console.WriteLine($"Current Index: {_memIndex}");
                Console.WriteLine($"  Total Ticks: {_totalInstructionRan}");

                Console.WriteLine("~~~Memory~~~");
                for (var i = 0; i <= _maxMemIndex; i++)
                {
                    Console.WriteLine($"{i}[{(int)_mem[i]}]:{_mem[i]}");
                }

                Console.WriteLine("~~~Timings~~~");
                for (int i = 0; i < _stopwatches.Count; i++)
                {
                    Console.WriteLine($"{i}[{_totalTicks[i]}]:{_stopwatches[i].Elapsed}");
                }
            }

            // When debugging, output is not shown
            if (debug)
            {
                Console.WriteLine(_output);
            }
        }

        private static void Next(char command)
        {
            // My time command get priority since it isn't in the standard, so it also gets to return early
            // lucky it
            if (command == '`')
            {
                var index = _stopwatches.Count - 1;
                if (index == -1 || !_stopwatches[index].IsRunning)
                {
                    _stopwatches.Add(Stopwatch.StartNew());
                    _totalTicks.Add(0);
                    _addTotalTicks = true;
                }
                else
                {
                    _stopwatches[index].Stop();
                    _addTotalTicks = false;
                }
                return;
            }

            if (!_skip)
            {
                switch (command)
                {
                    case '>':
                        _memIndex++;
                        if (_maxMemIndex < _memIndex)
                        {
                            _mem.Add(default);
                            _maxMemIndex = _memIndex;
                        }
                        break;
                    case '<':
                        if (_memIndex == 0)
                        {
                            Console.Write("\nError trying to move left at cell 0\n");
                            Environment.Exit(0);
                        }
                        _memIndex--;
                        break;
                    case '+':
                        _mem[_memIndex]++;
                        break;
                    case '-':
                        _mem[_memIndex]--;
                        break;
                    case '.':
                        if (_outputNumber)
                        {
                            Console.Write((int)_mem[_memIndex]);
                        }
                        else
                        {
                            Console.Write(_mem[_memIndex]);
                        }
                        _output += _mem[_memIndex];
                        break;
                    case ',':
                        _mem[_memIndex] = Console.ReadKey(true).KeyChar;
                        break;
                }
            }

            if (command == '[')
            {
                if (!_skip && _mem[_memIndex] == default)
                {
                    _skipLoop = _loopIndex;
                    _skip = true;
                }
                _loopIndex++;
                _loop.Add(_commandIndex);
            }
            else if (command == ']')
            {
                if (_skip || _mem[_memIndex] == default)
                {
                    _loop.RemoveAt(_loopIndex);
                    _loopIndex--;
                    if (_skipLoop == _loopIndex)
                    {
                        _skip = false;
                    }
                }
                else
                {
                    _commandIndex = _loop[_loopIndex];
                }
            }
        }

        private static string Underline(string separator, IEnumerable<object> objects, int index, Func<object, object> f = null)
        {
            if (f is null)
            {
                f = (o) => o;
            }

            var s = string.Empty;
            var i = 0;
            foreach (var o in objects)
            {
                if (i == index)
                {
                    s += UNDERLINE + f(o) + RESET;
                }
                else
                {
                    s += f(o);
                }

                if (i != objects.Count() - 1)
                {
                    s += separator;
                }
                i++;
            }
            return s;
        }
    }
}
