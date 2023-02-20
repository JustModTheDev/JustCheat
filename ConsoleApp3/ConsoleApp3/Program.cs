using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter process ID: ");
            int processId = int.Parse(Console.ReadLine());

            Process process = Process.GetProcessById(processId);

            Console.Write("Enter search term: ");
            string searchTerm = Console.ReadLine();

            Console.Write("Enter replacement term: ");
            string replacementTerm = Console.ReadLine();

            Console.WriteLine("Searching for {0} in process {1} ({2})", searchTerm, process.ProcessName, process.Id);

            while (true)
            {
                Console.WriteLine("Replacing {0} with {1}...", searchTerm, replacementTerm);

                IntPtr addressToWrite = IntPtr.Zero;
                bool found = false;
                byte[] replacementBytes = System.Text.Encoding.UTF8.GetBytes(replacementTerm);
                int searchLength = System.Text.Encoding.UTF8.GetByteCount(searchTerm);

                // If replacement string is shorter than search string, pad with spaces to ensure same length
                if (replacementBytes.Length < searchLength)
                {
                    Array.Resize(ref replacementBytes, searchLength);
                    for (int i = replacementBytes.Length; i < searchLength; i++)
                    {
                        replacementBytes[i] = 0x20; // Space character
                    }
                };

                foreach (ProcessModule module in process.Modules)
                {
                    IntPtr baseAddress = module.BaseAddress;
                    int moduleSize = module.ModuleMemorySize;

                    byte[] buffer = new byte[moduleSize];

                    // Read the module's memory
                    MemoryHelper.ReadProcessMemory(process.Handle, baseAddress, buffer, moduleSize);

                    // Search for the search term in the module's memory
                    int index = MemoryHelper.FindBytes(buffer, System.Text.Encoding.UTF8.GetBytes(searchTerm));

                    if (index != -1)
                    {
                        addressToWrite = baseAddress + index;
                        found = true;
                        Console.WriteLine("Found {0} at {1:X}", searchTerm, addressToWrite.ToInt64());
                        break;
                    }
                }

                if (!found)
                {
                    Console.WriteLine("Could not find {0} in process {1} ({2})", searchTerm, process.ProcessName, process.Id);
                    break;
                }

                // Write the replacement bytes to the process memory
                MemoryHelper.WriteProcessMemory(process.Handle, addressToWrite, replacementBytes, replacementBytes.Length);

                Console.WriteLine("Replacement completed. Enter new replacement term, or type 'exit' to quit:");

                string input = Console.ReadLine();
                if (input == "exit")
                {
                    Console.WriteLine("Exiting program.");
                    break;
                }
                else
                {
                    replacementTerm = input;
                }
            }

            Console.ReadLine();
        }
    }

    public static class MemoryHelper
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001
        }

        public static void ReadProcessMemory(IntPtr handle, IntPtr address, byte[] buffer, int size)
        {
            IntPtr bytesRead;
            ReadProcessMemory(handle, address, buffer, size, out bytesRead);
        }

        public static void WriteProcessMemory(IntPtr handle, IntPtr address, byte[] buffer, int size)
        {
            IntPtr bytesWritten;
            WriteProcessMemory(handle, address, buffer, size, out bytesWritten);
        }

        public static int FindBytes(byte[] haystack, byte[] needle)
        {
            int len = needle.Length;
            int limit = haystack.Length - len + 1;
            for (int i = 0; i < limit; i++)
            {
                bool success = true;
                for (int j = 0; j < len; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}