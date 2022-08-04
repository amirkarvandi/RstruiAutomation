using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticRstrui
{
    public static class Interop
    {
        internal const uint PROCESS_ALL_ACCESS = (uint)(0x000F0000L | 0x00100000L | 0xFFF);
        internal const uint MEM_COMMIT = 0x1000;
        internal const uint MEM_RELEASE = 0x8000;
        internal const uint PAGE_READWRITE = 0x04;

        internal const int WM_SETFOCUS = 0x0007;
        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_KEYUP = 0x0101;
        internal const int TVM_GETNEXTITEM = 0x1100 + 10;
        internal const int TVM_SELECTITEM = 0x1100 + 11;
        internal const int TVM_GETITEMW = 0x1100 + 62;
        internal const int TVGN_ROOT = 0x0000;
        internal const int TVGN_NEXT = 0x0001;
        internal const int TVGN_CHILD = 0x0004;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        [DllImport("kernel32")]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32")]
        internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize,
            uint flAllocationType, uint flProtect);

        [DllImport("kernel32")]
        internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);

        [DllImport("kernel32")]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref LVITEM buffer,
            int dwSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref TVITEM buffer,
            int dwSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer,
            int dwSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32")]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern int PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        internal static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        #region structs

        /// <summary>
        ///     from 'http://dotnetjunkies.com/WebLog/chris.taylor/'
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LVITEM
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public readonly int iImage;
        }

        /// <summary>
        ///     from '.\PlatformSDK\Include\commctrl.h'
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TVITEM
        {
            public uint mask;
            public IntPtr hItem;
            public readonly uint state;
            public readonly uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public readonly uint iImage;
            public readonly uint iSelectedImage;
            public readonly uint cChildren;
            public readonly IntPtr lParam;
        }

        #endregion structs
    }


    internal class Program
    {
        static IntPtr SysListViewHwnd = IntPtr.Zero;

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }


        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        private static void SetLVItemState(int item, IntPtr HwndListView, IntPtr hProcess, IntPtr lpRemoteBuffer)
        {
            const int LVM_FIRST = 0x1000;
            const int LVM_SETITEMSTATE = LVM_FIRST + 43;
            const int LVIF_STATE = 0x0008;

            const int LVIS_FOCUSED = 0x0001;
            const int LVIS_SELECTED = 0x0002;

            var lvItem = new Interop.LVITEM
            {
                mask = LVIF_STATE,
                iItem = item,
                iSubItem = 0,
                state = LVIS_FOCUSED | LVIS_SELECTED,
                stateMask = LVIS_FOCUSED | LVIS_SELECTED
            };

            // copy local lvItem to remote buffer
            var success = Interop.WriteProcessMemory(hProcess, lpRemoteBuffer, ref lvItem,
                Marshal.SizeOf(typeof(Interop.LVITEM)), IntPtr.Zero);
            if (!success)
            {
                Console.WriteLine("Failed to write to process memory");
            }

            // Send the message to the remote window with the address of the remote buffer
            if (Interop.SendMessage(HwndListView, LVM_SETITEMSTATE, (IntPtr)item, lpRemoteBuffer) == IntPtr.Zero)
            {
                Console.WriteLine("LVM_GETITEM Failed");
            }
        }

        private static bool IsSysListView32(IntPtr hWnd)
        {
            int nRet;
            // Pre-allocate 256 characters, since this is the maximum class name length.
            StringBuilder ClassName = new StringBuilder(256);
            //Get the window class name
            nRet = GetClassName(hWnd, ClassName, ClassName.Capacity);
            if (nRet != 0)
            {
                return (string.Compare(ClassName.ToString(), "SysListView32", true, CultureInfo.InvariantCulture) == 0);
            }
            else
            {
                return false;
            }
        }

        public static bool GetComponentName(IntPtr hWnd)
        {
            int nRet;
            // Pre-allocate 256 characters, since this is the maximum class name length.
            StringBuilder ClassName = new StringBuilder(256);
            //Get the window class name
            nRet = GetClassName(hWnd, ClassName, ClassName.Capacity);
            if (nRet != 0)
            {
                Console.WriteLine(ClassName.ToString());
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void GetComponents(IntPtr mainHwnd)
        {
            IntPtr Result = mainHwnd;
            IntPtr TopLvlHandle = IntPtr.Zero;

            if (mainHwnd != IntPtr.Zero)
            {
                //
                // Select element in SysListView32
                //
                do
                {
                    TopLvlHandle = FindWindowEx(mainHwnd, TopLvlHandle, null, null);
                    Result = IntPtr.Zero;

                    if (TopLvlHandle != IntPtr.Zero)
                    {
                        Console.WriteLine("=> 0x" + TopLvlHandle.ToString("X") + " - ");
                        GetComponentName(Result);
                    }

                    do
                    {
                        Result = FindWindowEx(TopLvlHandle, Result, null, null);

                        if (Result != IntPtr.Zero)
                        {
                            if (IsSysListView32(Result) && SysListViewHwnd == IntPtr.Zero)
                            {
                                SysListViewHwnd = Result;
                                Console.Write("(Found)");
                            }

                            Console.Write("   \t0x" + Result.ToString("X") + " - ");
                            GetComponentName(Result);
                        }

                    } while (Result != IntPtr.Zero);

                } while (TopLvlHandle != IntPtr.Zero);

            }
            else
            {
                Console.WriteLine("System Restore window not found!");
            }

        }

        static void Main(string[] args)
        {
            IntPtr RstruiHandle = IntPtr.Zero;

            //
            // It's also possible to use https://www.autoitscript.com/site/autoit/
            //
            const int BM_CLICK = 0x00F5;

            const string lpClassName = "System Restore";
            IntPtr mainHwnd = FindWindow(null, lpClassName);

            if (mainHwnd != IntPtr.Zero)
            {
                Console.WriteLine("[*] Getting components");
                GetComponents(mainHwnd);

                IntPtr buttonHwnd = IntPtr.Zero;

                Console.WriteLine("\n");
                Console.WriteLine("[*] Finding the 'Next' & 'Finish' Button that are in the same location");
                buttonHwnd = FindWindowEx(mainHwnd, buttonHwnd, "Button", null);
                buttonHwnd = FindWindowEx(mainHwnd, buttonHwnd, "Button", null);

                Console.WriteLine("[*] Button handle found : 0x" + buttonHwnd.ToString("X"));

                System.Threading.Thread.Sleep(2000);

                Console.WriteLine("[*] Setting Active Window (Button)");
                SetActiveWindow(buttonHwnd);

                Console.WriteLine("[*] Sending Click Message (Button)");
                SendMessage(buttonHwnd, BM_CLICK, 0, 0);


                //
                // Check for handle of SysListView32
                //
                if (SysListViewHwnd == IntPtr.Zero)
                {
                    Console.WriteLine("[-] Err, handle of SysListView32 not found!");
                    goto EndLabel;
                }

                Console.WriteLine("[*] Handle of SysListView32 found : 0x" + SysListViewHwnd.ToString("X"));

                Console.WriteLine("[*] Getting process handle");

                Process[] processes = Process.GetProcessesByName("rstrui");

                foreach (Process p in processes)
                {
                    RstruiHandle = p.Handle;
                }

                if (RstruiHandle == IntPtr.Zero)
                {
                    Console.WriteLine("[-] Err, process handle not found!");
                    goto EndLabel;
                }

                Console.WriteLine("[*] Process handle found - 0x" + RstruiHandle.ToString("X"));

                Console.WriteLine("[*] Allocating memory on the remote process");

                IntPtr RemoteProcessMemoryAddr = VirtualAllocEx(RstruiHandle,
                    IntPtr.Zero,
                    0x100,
                    (uint)AllocationType.Commit,
                    (uint)MemoryProtection.ExecuteReadWrite);

                if (RemoteProcessMemoryAddr == IntPtr.Zero)
                {
                    Console.WriteLine("[-] Err, cannot allocate memory on the remote process!");
                    goto EndLabel;
                }

                Console.WriteLine("[*] Memory successfully allocated on the remote process (Address : 0x" + RemoteProcessMemoryAddr.ToString("X") + ")");

                System.Threading.Thread.Sleep(2000);
                Console.WriteLine("[*] Select item in SysListView32");

                SetLVItemState(0, SysListViewHwnd, RstruiHandle, RemoteProcessMemoryAddr);

                System.Threading.Thread.Sleep(2000);

                Console.WriteLine("[*] Pressing the 'Next' button again (second page)");
                SendMessage(buttonHwnd, BM_CLICK, 0, 0);

                System.Threading.Thread.Sleep(2000);

                Console.WriteLine("[*] Pressing the 'Finish' button (third page)");

                //
                // Commented to avoid restoring, uncomment on final build
                //
                // SendMessage(buttonHwnd, BM_CLICK, 0, 0);


            }
            else
            {
                Console.WriteLine("System Restore window not found!");
            }
        EndLabel:

            Console.ReadKey();

        }
    }
}
