using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IL2CPP_Injector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void injectDllButton_Click(object sender, RoutedEventArgs e)
        {
            if (processListView.SelectedItem == null)
            {
                new ToastContentBuilder().SetToastDuration(ToastDuration.Short).AddText("No Process Selected!").Show();
                return;
            }

            var selectedProcess = (ProcessData)processListView.SelectedItem;
            Task.Factory.StartNew(() =>
            {
                string dllPath = string.Empty;

                OpenFileDialog openFileDialog = new OpenFileDialog();
                if ((bool)openFileDialog.ShowDialog()!)
                {
                    dllPath = openFileDialog.FileName;
                }

                nint hProc = Imports.OpenProcess(0xFFFF, false, selectedProcess.PID);
                nint loadLibraryProc = Imports.GetProcAddress(Imports.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                nint allocated =
                    Imports.VirtualAllocEx(hProc, IntPtr.Zero, (uint)dllPath.Length + 1, 0x00001000 | 0x00002000, 0x40);
                Imports.WriteProcessMemory(hProc, allocated, Encoding.UTF8.GetBytes(dllPath), (uint)dllPath.Length + 1, out _);
                Imports.CreateRemoteThread(hProc, IntPtr.Zero, 0, loadLibraryProc, allocated, 0, IntPtr.Zero);
            });
        }

        private void refreshlistButton_Click(object sender, RoutedEventArgs e)
        {
            processListView.Items.Clear();
            Task.Factory.StartNew(() =>
            {
                Process[] processList = Process.GetProcesses();
                foreach (Process process in processList)
                {
                    try{
                        ProcessModuleCollection modules = process.Modules;
                        foreach (ProcessModule module in modules)
                        {
                            if (module.ModuleName!.Contains("GameAssembly.dll"))
                            {
                                ProcessData procData = new ProcessData()
                                {
                                    Name = process.ProcessName,
                                    PID = process.Id,
                                    ModuleCount = process.Modules.Count
                                };
                                Dispatcher.Invoke(() =>
                                {
                                    processListView.Items.Add(procData);
                                });
                            }
                        }
                    }
                    catch{}
                }
            });
        }

        public struct ProcessData 
        {
            public string Name { get; set; }
            public int PID { get; set; }
            public int ModuleCount { get; set; }
        }
    }
}
