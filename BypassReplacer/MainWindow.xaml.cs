using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BypassReplacer
{
    public partial class MainWindow : Window
    {
        [Flags]
        public enum ThreadAccess
        {
            TERMINATE = 1,
            SUSPEND_RESUME = 2,
            GET_CONTEXT = 8,
            SET_CONTEXT = 0x10,
            SET_INFORMATION = 0x20,
            QUERY_INFORMATION = 0x40,
            SET_THREAD_TOKEN = 0x80,
            IMPERSONATE = 0x100,
            DIRECT_IMPERSONATION = 0x200,
            THREAD_ALL_ACCESS = 0x1F03FF
        }

        [DllImport("kernel32.dll")] private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")] private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")] private static extern int ResumeThread(IntPtr hThread);
        public MainWindow()
        {
            InitializeComponent();
            this.title.Text = "Обход by drazz & NANO & Serega007";

            Task.Run(() =>
            {
                Process currProcess = null;
                List<Process> multiProcess = new List<Process>();
                string filePath = "";
                string filePathMinecraft = "";

                Dispatcher.Invoke(() => this.inform.Text = "Запустите Cristalix...");

                while (true)
                {
                    try
                    {
                        double currTime = new TimeSpan(DateTime.Now.Ticks).TotalSeconds;
                        multiProcess.Clear();
                        Process[] process = Process.GetProcessesByName("java");
                        foreach (Process p in process)
                        {
                            string path = p.MainModule.FileName;

                            if (!path.Contains("\\.cristalix\\")) continue;

                            string pathSunEc = path.Replace("\\bin\\java.exe", "\\lib\\ext\\sunec.jar");
                            if (!File.Exists(pathSunEc)) continue;

                            if (currTime - new TimeSpan(p.StartTime.Ticks).TotalSeconds > 3)
                            {
                                multiProcess.Add(p);
                                continue;
                            }

                            Dispatcher.Invoke(() => this.inform.Text = "Cristalix найден! ждёмс чудо...");
                            filePath = pathSunEc;
                            filePathMinecraft = path.Substring(0, path.IndexOf("\\.cristalix\\")) + "\\.cristalix\\updates\\Minigames\\minecraft.jar";
                            currProcess = p;
                        }
                        if (filePath.Length > 0) break;
                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        Dispatcher.Invoke(() => this.inform.Text = $"Ошибка: {ex.Message}");
                        break;
                    }
                }
                if (filePath.Length == 0) return;
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sunec_temp");
                string tempFile = System.IO.Path.Combine(tempPath, "sunec.jar");
                try
                {
                    Directory.CreateDirectory(tempPath);
                    File.Copy(filePath, tempFile, overwrite: true);

                    Dispatcher.Invoke(() => this.inform.Text = "Подмена 1...");
                    JavaProcess(currProcess, false);
                    if (multiProcess.Count > 0)
                    {
                        Dispatcher.Invoke(() => this.inform.Text += "\n\nОбнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                    }
                    foreach (Process p in multiProcess)
                    {
                        p.Refresh();
                        if (!p.HasExited)
                        {
                            JavaProcess(p, false);
                        }
                    }
                    File.Copy("C:\\Xenoceal\\sunec.jar", filePath, overwrite: true);
                    JavaProcess(currProcess, true);

                    Dispatcher.Invoke(() => this.inform.Text = "Ждём запуска майнкрафта...");
                    if (multiProcess.Count > 0)
                    {
                        Dispatcher.Invoke(() => this.inform.Text += "\n\nОбнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                    }
                    while (true)
                    {
                        List<Process> list = FileUtil.WhoIsLocking(filePathMinecraft);
                        if (list.Count > 0)
                        {
                            bool found = false;
                            foreach (Process p in list)
                            {
                                if (p.Id == currProcess.Id)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                break;
                            }
                        }
                        Thread.Sleep(100);
                    }

                    Dispatcher.Invoke(() => this.inform.Text = "Подмена 2...");
                    JavaProcess(currProcess, false);
                    if (multiProcess.Count > 0)
                    {
                        Dispatcher.Invoke(() => this.inform.Text += "\n\nОбнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                    }
                    File.Copy(tempFile, filePath, overwrite: true);
                    JavaProcess(currProcess, true);
                    foreach (Process p in multiProcess)
                    {
                        p.Refresh();
                        if (!p.HasExited)
                        {
                            JavaProcess(p, true);
                        }
                    }
                    Dispatcher.Invoke(() => this.inform.Text = "Вроде всё прошло успешно, проверяйте");
                    Thread.Sleep(2000);

                    Dispatcher.Invoke(() => this.Close());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Dispatcher.Invoke(() => this.inform.Text = $"Ошибка: {ex.Message}");
                    try
                    {
                        foreach (Process p in multiProcess)
                        {
                            p.Refresh();
                            if (!p.HasExited)
                            {
                                JavaProcess(p, true);
                            }
                        }
                    }
                    catch { }
                }
                finally
                {
                    Directory.Delete(tempPath, recursive: true);
                }
            });
        }

        private void TextBoxPreview(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            Regex regex = new Regex("^[0-9-]+$");
            if (!regex.IsMatch(text))
            {
                e.Handled = true;
            }
        }

        private void JavaProcess(Process process, bool active)
        {
            try
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    IntPtr intPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, bInheritHandle: false, (uint)thread.Id);
                    if (intPtr != IntPtr.Zero)
                    {
                        if (active) ResumeThread(intPtr);
                        else SuspendThread(intPtr);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                this.inform.Text = $"Ошибка: {ex.Message}";
            }
        }
    }

    // https://stackoverflow.com/a/20623311/11235240
    static public class FileUtil
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        const int RmRebootReasonNone = 0;
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle,
                                              UInt32 nFiles,
                                              string[] rgsFilenames,
                                              UInt32 nApplications,
                                              [In] RM_UNIQUE_PROCESS[] rgApplications,
                                              UInt32 nServices,
                                              string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle,
                                    out uint pnProcInfoNeeded,
                                    ref uint pnProcInfo,
                                    [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
                                    ref uint lpdwRebootReasons);

        /// <summary>
        /// Find out what process(es) have a lock on the specified file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>Processes locking the file</returns>
        /// <remarks>See also:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
        /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
        /// 
        /// </remarks>
        static public List<Process> WhoIsLocking(string path)
        {
            uint handle;
            string key = Guid.NewGuid().ToString();
            List<Process> processes = new List<Process>();

            int res = RmStartSession(out handle, 0, key);

            if (res != 0)
                throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfoNeeded = 0,
                     pnProcInfo = 0,
                     lpdwRebootReasons = RmRebootReasonNone;

                string[] resources = new string[] { path }; // Just checking on one resource.

                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0)
                    throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);

                    if (res == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException) { }
                        }
                    }
                    else if (res == ERROR_MORE_DATA)
                    {
                        // TODO Serega007: какой-то кринж, иногда выбрасывает тут ошибку 234, просто тупо игнорируем её и похрен
                        // Ignore it
                    }
                    else if (res != 0)
                        throw new Exception("Could not list processes locking resource. Error code: " + res);
                }
                else if (res != 0)
                    throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                RmEndSession(handle);
            }

            return processes;
        }
    }
}
