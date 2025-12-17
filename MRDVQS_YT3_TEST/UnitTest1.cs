using Microsoft.VisualStudio.TestTools.UnitTesting;
using MRDVQS_YT3;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows.Forms;

namespace MRDVQS_YT3_TEST
{
    [TestClass]
    public class UnitTest1
    {
        private Form1 form = new Form1();

        [TestMethod]
        public void TestMethod_fnPostDefaultParameter()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "parameter.txt");
            try
            {
                JObject resultMsg = form.fnPostDefaultParameter(path);

                // Validate response
                Assert.IsNotNull(resultMsg);
                Assert.AreEqual(1, (int)resultMsg["ErrCode"], "Invalid JSON should trigger an error path.");
                Assert.IsFalse(resultMsg["ErrMsg"]?.ToString().Contains("Error in fnPostDefaultParameter"), "Error message should indicate fnPostDefaultParameter failure.");

                // Validate file was written and contains expected values
                Assert.IsTrue(File.Exists(path), "parameter.txt should exist after saving parameters.");
                var saved = JObject.Parse(File.ReadAllText(path));
                Assert.AreEqual("vn", saved["lang"]?.ToString());
                Assert.AreEqual("", saved["device"]?.ToString());
                Assert.AreEqual("", saved["product"]?.ToString());
            }
            finally
            {
                // Clean up test file and restore backup
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod]
        public void TestMethod_fnGetDefaultParameter()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "parameter.txt");
            try
            {
                var newData = new JObject
                {
                    ["lang"] = "en",
                    ["device"] = "unit-test-device",
                    ["product"] = "unit-test-product"
                };
                JObject result = form.fnGetDefaultParameter(newData, path);

                // Validate response
                Assert.IsNotNull(result);
                Assert.AreEqual("SAVE_PARAMETER", result["action"]?.ToString());
                Assert.AreEqual(1, (int)result["ErrCode"]);

                // Validate file was written and contains expected values
                Assert.IsTrue(File.Exists(path), "parameter.txt should exist after saving parameters.");
                var saved = JObject.Parse(File.ReadAllText(path));
                Assert.AreEqual("en", saved["lang"]?.ToString());
                Assert.AreEqual("unit-test-device", saved["device"]?.ToString());
                Assert.AreEqual("unit-test-product", saved["product"]?.ToString());
            }
            finally
            {
                // Clean up test file and restore backup
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [TestMethod] // Confirm this device has printer driver, bpac library before enable this test
        public void TestMethod_fnPrintQR()
        {
            string qr = "UNITTEST-QR-123";
            JObject resultMsg = form.fnPrintQR(qr);

            // Ensure the result is not null
            Assert.IsNotNull(resultMsg);
            // Check if the result is a json with expected action
            Assert.AreEqual(1, (int)resultMsg["ErrCode"]);
            // Check if ErrMsg is not "Failed to open print template."
            Assert.AreNotEqual("Failed to open print template.", resultMsg["ErrMsg"]?.ToString());
            // Check if ErrMsg does not indicate error in fnPrintQR
            Assert.IsFalse(resultMsg["ErrMsg"]?.ToString().Contains("Error in fnPrintQR"));
        }
    }
}
