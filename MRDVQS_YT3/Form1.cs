using bpac;
using CefSharp;
using CefSharp.WinForms;
using MRDVQS_YT3.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Management;
using System.Net.Http;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace MRDVQS_YT3
{
    public partial class Form1 : Form
    {
        private ChromiumWebBrowser chromiumWebBrowser;
        public Form1()
        {
            InitializeComponent();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PrinterDriver WHERE Name = 'Brother PT-P950NW,3,Windows x64'");
            var drivers = searcher.Get().Cast<ManagementObject>();
            if (drivers.Count() == 0)
            {
                var process = Process.Start(@"Redist\bsp15bw1104aus.exe");
                process.WaitForExit();
            }
            if (!ConnectionModel.checkInstalled("b-PAC3 Client Component (64bit)"))
            {
                var process = Process.Start(@"Redist\bPAC3CCISetup_64.msi");
                process.WaitForExit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            chromiumWebBrowser = new ChromiumWebBrowser(ConnectionModel.webAddress)
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(chromiumWebBrowser);
            chromiumWebBrowser.JavascriptMessageReceived += fnWebMessageReceive;
        }

        private void fnWebMessageReceive(object sender, CefSharp.JavascriptMessageReceivedEventArgs e)
        {
            JObject returnMsg = new JObject();
            try
            {
                JObject msg = JObject.FromObject(e.Message);
                string type = msg["action"].ToString();
                if (type == "LOAD_PARAMETER")
                {
                    var filePath = Path.Combine(Application.StartupPath, "parameter.txt");
                    returnMsg = fnPostDefaultParameter(filePath);
                    fnWebMessageReponse(returnMsg);
                }

                else if (type == "SAVE_PARAMETER")
                {
                    JObject newData = msg["value"] as JObject;
                    if (newData != null)
                    {
                        var filePath = Path.Combine(Application.StartupPath, "parameter.txt");
                        returnMsg = fnGetDefaultParameter(newData, filePath);
                        fnWebMessageReponse(returnMsg);
                    }
                    else
                    {
                        JObject result = new JObject
                        {
                            ["action"] = "SAVE_PARAMETER",
                            ["ErrCode"] = 0,
                            ["ErrMsg"] = "No parameter to save!"
                        };
                        chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({result.ToString(Newtonsoft.Json.Formatting.None)});");
                    }
                }
                else if (type == "PRINT_QRCODE")
                {
                    string qrCode = msg["value"].ToString();
                    if (!string.IsNullOrEmpty(qrCode))
                    {
                        returnMsg = fnPrintQR(qrCode);
                        fnWebMessageReponse(returnMsg);
                    }
                    else
                    {
                        JObject result = new JObject
                        {
                            ["action"] = "PRINT_QRCODE",
                            ["ErrCode"] = 0,
                            ["ErrMsg"] = "Invalid QR Code data.",
                            ["ErrBack"] = qrCode
                        };
                        chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({result.ToString(Newtonsoft.Json.Formatting.None)});");
                    }
                }
                else if (type == "PRINT_TEST")
                {
                    string qrCode = msg["value"].ToString();
                    bool isOnline = fnCheckPrinter();
                    if (string.IsNullOrEmpty(qrCode))
                    {
                        JObject result = new JObject
                        {
                            ["action"] = "PRINT_TEST",
                            ["ErrCode"] = 0,
                            ["ErrMsg"] = "Invalid QR Code data.",
                            ["ErrBack"] = qrCode
                        };
                        chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({result.ToString(Newtonsoft.Json.Formatting.None)});");
                    }
                    else if (!isOnline)
                    {
                        JObject result = new JObject
                        {
                            ["action"] = "PRINT_TEST",
                            ["ErrCode"] = 0,
                            ["ErrMsg"] = "Printer is offline.",
                            ["ErrBack"] = qrCode
                        };
                        chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({result.ToString(Newtonsoft.Json.Formatting.None)});");
                    }
                    else
                    {
                        returnMsg = fnPrintQR(qrCode);
                        fnWebMessageReponse(returnMsg);
                    }
                }
                else if (type== "CHECK_PRINTER")
                {
                    bool isOnline = fnCheckPrinter();
                    JObject result = new JObject
                    {
                        ["action"] = "CHECK_PRINTER",
                        ["ErrCode"] = isOnline ? 1 : 0,
                        ["ErrMsg"] = isOnline ? "Printer is online." : "Printer is offline."
                    };
                    chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({result.ToString(Newtonsoft.Json.Formatting.None)});");
                }
                else
                {
                    JObject result = new JObject
                    {
                        ["action"] = "UNKNOWN_ACTION",
                        ["ErrCode"] = 0,
                        ["ErrMsg"] = "Unknown action type: " + type
                    };
                    chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({result.ToString(Newtonsoft.Json.Formatting.None)});");
                }
            }
            catch (Exception ex)
            {
                JObject result = new JObject
                {
                    ["action"] = "FAIL_TRANSFER",
                    ["ErrCode"] = 0,
                    ["ErrMsg"] = "Error in fnWebMessageReceive: " + ex.Message
                };
                chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({result.ToString(Newtonsoft.Json.Formatting.None)});");
            }
        }

        public JObject fnPostDefaultParameter(string path)
        {
            JObject parameter = new JObject(); ;
            JObject msg = new JObject(); ;
            try
            {
                if (File.Exists(path))
                    parameter = JObject.Parse(File.ReadAllText(path));
                else
                {
                    parameter = new JObject
                    {
                        ["lang"] = "vn",
                        ["device"] = "",
                        ["product_yt3"] = "",
                        ["product_swift"] = "",
                        ["package_product"] = "",
                    };

                    File.WriteAllText(
                        path,
                        parameter.ToString(Newtonsoft.Json.Formatting.None)
                    );
                }
                msg = new JObject
                {
                    ["action"] = "LOAD_PARAMETER",
                    ["ErrCode"] = 1,
                    ["ErrMsg"] = "success",
                    ["data"] = parameter
                };
            }
            catch (Exception ex)
            {
                msg = new JObject
                {
                    ["action"] = "LOAD_PARAMETER",
                    ["ErrCode"] = 0,
                    ["ErrMsg"] = "Error in fnPostDefaultParameter: " + ex.Message
                };
            }
            return msg;
            //chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({msg.ToString(Newtonsoft.Json.Formatting.None)});");
        }

        public JObject fnGetDefaultParameter(JObject newData, string path)
        {
            JObject currentData = new JObject();
            JObject msg = new JObject();
            try
            {
                if (File.Exists(path))
                    currentData = JObject.Parse(File.ReadAllText(path));
                else
                    currentData = new JObject();

                foreach (var prop in newData.Properties())
                {
                    currentData[prop.Name] = prop.Value;
                }
                File.WriteAllText(path, currentData.ToString(Newtonsoft.Json.Formatting.None));

                msg = new JObject
                {
                    ["action"] = "SAVE_PARAMETER",
                    ["ErrCode"] = 1,
                    ["ErrMsg"] = "Success"
                };
            }
            catch (Exception ex)
            {
                msg = new JObject
                {
                    ["action"] = "SAVE_PARAMETER",
                    ["ErrCode"] = 0,
                    ["ErrMsg"] = "Error in fnGetDefaultParameter: " + ex.Message
                };
            }
            return msg;
            //chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({msg.ToString(Newtonsoft.Json.Formatting.None)});");
        }

        public JObject fnPrintQR(string qrCode)
        {
            JObject msg = new JObject();
            try
            {
                bpac.DocumentClass doc = new bpac.DocumentClass();
                if (doc.Open(ConnectionModel.templateAddress) != false)
                {
                    doc.GetObject("objQRCode").Text = qrCode;
                    doc.StartPrint("", PrintOptionConstants.bpoDefault);
                    doc.PrintOut(1, PrintOptionConstants.bpoDefault);
                    doc.EndPrint();
                    doc.Close();

                    msg = new JObject
                    {
                        ["action"] = "PRINT_QRCODE",
                        ["ErrCode"] = 1,
                        ["ErrMsg"] = "Print success",
                        ["ErrBack"] = qrCode
                    };
                }
                else
                {
                    msg = new JObject
                    {
                        ["action"] = "PRINT_QRCODE",
                        ["ErrCode"] = 0,
                        ["ErrMsg"] = "Failed to open print template.",
                        ["ErrBack"] = qrCode
                    };
                }
            }
            catch (Exception ex)
            {
                msg = new JObject
                {
                    ["action"] = "PRINT_QRCODE",
                    ["ErrCode"] = 0,
                    ["ErrMsg"] = "Error in fnPrintQR: " + ex.Message,
                    ["ErrBack"] = qrCode
                };
            }
            return msg;
            //chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({msg.ToString(Newtonsoft.Json.Formatting.None)});");
        }

        public bool fnCheckPrinter()
        {
            bpac.PrinterClass printer = new bpac.PrinterClass();
            object[] printers = (object[])printer.GetInstalledPrinters();

            string printerName = (string)printers[0];

            bool isOnline = printer.IsPrinterOnline(printerName);
            return isOnline;
        }

        public void fnWebMessageReponse(JObject msg)
        {
            chromiumWebBrowser.ExecuteScriptAsync($"window.onWinFormMessage({msg.ToString(Newtonsoft.Json.Formatting.None)});");
        }
    }
}
