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
}
