using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MRDVQS_YT3.Models
{
    public class RawPrinter
    {
        [StructLayout(LayoutKind.Sequential)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
        public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter")]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int Level, DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter")]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter")]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter")]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter")]
        public static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

        public static JObject fnSendStringToPrinter(string printerName, string data)
        {
            var result = new JObject();
            try
            {
                IntPtr hPrinter;
                DOCINFOA di = new DOCINFOA();
                di.pDocName = "Zebra Label";
                di.pDataType = "RAW";

                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    result["ErrCode"] = "0";
                    result["ErrMsg"] = "Failed to open printer.";
                }
                else
                {
                    StartDocPrinter(hPrinter, 1, di);
                    StartPagePrinter(hPrinter);

                    byte[] bytes = Encoding.UTF8.GetBytes(data);
                    WritePrinter(hPrinter, bytes, bytes.Length, out int written);

                    EndPagePrinter(hPrinter);
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);

                    result["ErrCode"] = "1";
                    result["ErrMsg"] = "Success";
                }
            }
            catch (Exception ex)
            {
                result["ErrCode"] = 0;
                result["ErrMsg"] = ex.Message;
            }

            return result;
        }
    }
}
