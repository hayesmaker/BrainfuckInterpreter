using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Problems:
// Skipping loop doesn't work when []], it ends early
// Can't nest loops

namespace BrainfuckInterpreter
{
    class Program
    {
        private static int _skip;
        private static int _memIndex;
        private static int _maxIndex;
        private static int _commandIndex;
        private static int _loopIndex = -1;
        private static List<char> _mem = new List<char> { default };

        /// <summary>
        /// This is an interpreter of the Brainfuck langauge https://en.wikipedia.org/wiki/Brainfuck
        /// </summary>
        /// <param name="file">File to interpret</param>
        /// <param name="verbose">Show all values of memory changes</param>
        /// <param name="line">Show current command index</param>
        private static void Main(FileInfo file, bool verbose, bool line)
        {
            if (!file.Exists)
            {
                Console.WriteLine("Please provide a valid file path");
                return;
            }

            var commands = File.ReadAllText(file.FullName).ToCharArray();
            while (_commandIndex < commands.Length)
            {
                if (line)
                {
                    Console.WriteLine(commands[_commandIndex]);
                }
                Next(commands[_commandIndex++]);
            }
            Console.WriteLine();

            if (verbose)
            {
                Console.WriteLine("---");
                Console.WriteLine($"Max Index: {_maxIndex}");
                Console.WriteLine($"Cur Index: {_memIndex}");
                for (int i = 0; i <= _maxIndex; i++)
                {
                    Console.WriteLine($"{i}[{(int)_mem[i]}]:{_mem[i]}");
                }
            }
        }

        private static void Next(char command)
        {
            if (_skip != 0)
            {
                if (command == ']')
                {
                    _skip--;
                }
            }
            else
            {
                switch (command)
                {
                    case '>':
                        _memIndex++;
                        if (_maxIndex < _memIndex)
                        {
                            _mem.Add(default);
                            _maxIndex = _memIndex;
                        }
                        break;
                    case '<':
                        _memIndex--;
                        break;
                    case '+':
                        _mem[_memIndex]++;
                        break;
                    case '-':
                        _mem[_memIndex]--;
                        break;
                    case '.':
                        Console.Write(_mem[_memIndex]);
                        break;
                    case ',':
                        _mem[_memIndex] = Console.ReadKey(true).KeyChar;
                        break;
                    case '[':
                        if (_mem[_memIndex] == default)
                        {
                            _skip++;
                        }
                        else
                        {
                            _loopIndex = _commandIndex;
                        }
                        break;
                    case ']':
                        if (_mem[_memIndex] == default)
                        {
                            _skip--;
                        }
                        else
                        {
                            _commandIndex = _loopIndex;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
