using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    const int idleTimeout = 30000;

    static void Main(string[] args)
    {
        // Get the process you want to monitor
        Process process = GetProcessToMonitor("notepad");

        if (process == null)
        {
            Console.WriteLine("Process not found.");
            return;
        }

        Console.WriteLine($"Monitoring idle time for process: {process.ProcessName} (ID: {process.Id})");

        DateTime lastInputTime = DateTime.Now;

        while (!process.HasExited)
        {
            if (IsProcessActive(process.Id))
            {
                if (IsUserActive())
                {
                    Console.WriteLine("User is active.");
                    lastInputTime = DateTime.Now; // Reset idle timer
                }
            }

            if (DateTime.Now.Subtract(lastInputTime).TotalMilliseconds > idleTimeout)
            {
                Console.WriteLine($"Process {process.ProcessName} (ID: {process.Id}) is idle for too long.");
                TakeIdleAction(process);
            }

            Thread.Sleep(100);
        }

        Console.WriteLine("Process has exited.");
    }

    private static Process GetProcessToMonitor(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        return processes.Length > 0 ? processes[0] : null;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private static bool IsProcessActive(int processId)
    {
        IntPtr activeWindowHandle = GetForegroundWindow();
        uint activeProcessId;
        GetWindowThreadProcessId(activeWindowHandle, out activeProcessId);
        return activeProcessId == processId;
    }

    private static bool IsUserActive()
    {
        LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (GetLastInputInfo(ref lastInputInfo))
        {
            uint idleTime = (uint)Environment.TickCount - lastInputInfo.dwTime;
            return idleTime < 100; 
        }

        return false;
    }

    private static void TakeIdleAction(Process process)
    {
        Console.WriteLine($"Taking action due to inactivity: {process.ProcessName} (ID: {process.Id})");
        process.Kill(); 
    }
}
