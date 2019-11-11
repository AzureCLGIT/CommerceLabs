using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using System.IO;
using System.Linq;
using System.Configuration;
using OtpNet;

namespace Reports
{
    class Program
    {
        // Data Lake Store File System Management Client
        private static DataLakeStoreFileSystemManagementClient adlsFileSystemClient;

        // Application ID [Client ID]
        private static string clientId = ConfigurationManager.AppSettings["ClientID"];

        // Client Secret Key
        private static string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];

        // Directory ID [Tenant ID]
        private static string tenantId = ConfigurationManager.AppSettings["TenantID"];

        // Name of the Azure Data Lake Store
        private static string adlsAccountName = ConfigurationManager.AppSettings["AzureDataLakeName"];

        static void Main(string[] args)
        {
            // Azure Data Lake Initialisation
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var clientCredential = new ClientCredential(clientId, clientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(tenantId, clientCredential).Result;
            adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            // Azure Source and Destination Variable 
            string Azuresource = "", Azuredestination = "";

            // Web Driver Object
            IWebDriver driver;
            
            // Setting the default directory for the report to be downloaded
            ChromeOptions options = new ChromeOptions();                     
            options.AddUserProfilePreference("download.default_directory", @"G:\CommerceLabs\Reports\Reports\App_Data\\");
            options.AddUserProfilePreference("intl.accept_languages", "nl");
            options.AddUserProfilePreference("disable-popup-blocking", "true");

            // Path of ChromeDrive.exe
            driver = new ChromeDriver(@"G:\CommerceLabs\Reports", options);
            Console.WriteLine("Started");

            // Seller Central Login Page
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/");
            driver.FindElement(By.ClassName("secondary")).Click();
            driver.FindElement(By.Id("ap_email")).SendKeys("geethanjali@jumpstartninja.com");
            driver.FindElement(By.Id("ap_password")).SendKeys("C@d3U$^#7");
            driver.FindElement(By.Id("signInSubmit")).Click();

            /*// Generating OTP
            var otpKeyStr = "WGHSYZEE2RJOO6OFMCBMB6HKNHDZRADH4TK2FDJ77YWQ5YFRWDBQ"; //  2FA secret key.
            var otpKeyBytes = Base32Encoding.ToBytes(otpKeyStr);
            var totp = new Totp(otpKeyBytes);
            var twoFactorCode = totp.ComputeTotp(DateTime.UtcNow); // 2FA code at this time!
            Console.WriteLine(twoFactorCode);*/

            // OTP
            Console.WriteLine("Please enter the OTP : ");
            var input = Console.ReadLine();
            driver.FindElement(By.Id("auth-mfa-otpcode")).SendKeys(input);
            driver.FindElement(By.Id("auth-mfa-remember-device")).Click();
            driver.FindElement(By.Id("auth-signin-button")).Click();
            Console.ReadLine();

            // Downloading Business Report
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/gp/site-metrics/report.html#reportID=102%3ADetailSalesTrafficBySKU&runDate=&fromDate=&toDate=");
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(100);
            Thread.Sleep(1000);

            // Downloading 3 Reports by looping through dates
            for (int i = -1; i > -4; i--)
            {
                // Looping through Dates and setting the Date Value
                var DateValue = System.DateTime.Now.AddDays(i).ToString("MM/dd/yyyy").Replace("-", "/");

                // Setting From Date
                IWebElement FromDateBox = driver.FindElement(By.Id("fromDate2"));
                FromDateBox.Clear();
                FromDateBox.SendKeys(DateValue + Keys.Enter);
                // Setting To Date
                IWebElement ToDateBox = driver.FindElement(By.Id("toDate2"));
                ToDateBox.Clear();
                ToDateBox.SendKeys(DateValue + Keys.Enter);

                // Downloading the Report
                Thread.Sleep(1000);
                driver.FindElement(By.Id("export")).Click();
                Thread.Sleep(1000);
                driver.FindElement(By.Id("downloadCSV")).Click();
                Thread.Sleep(1000);

                // Editing the Business Report CSV
                String BusRepfilePath = @"G:\CommerceLabs\Reports\Reports\App_Data\BusinessReport-" + System.DateTime.Now.ToString("MM/d/yy") + ".csv";
                Thread.Sleep(2000);

                // Transaction Date
                var Buscol1 = File.ReadLines(BusRepfilePath).Select((line, index) => index == 0 ? line + ", Transaction Date"
                                                                                             : line + "," + DateValue.Replace("/", "-")).ToList();
                File.WriteAllLines(BusRepfilePath, Buscol1);

                // Account
                var Buscol2 = File.ReadLines(BusRepfilePath).Select((line, index) => index == 0 ? line + ",Account"
                                                                                             : line + ",Meal Prep Haven").ToList();
                File.WriteAllLines(BusRepfilePath, Buscol2);

                // Country
                var Buscol3 = File.ReadLines(BusRepfilePath).Select((line, index) => index == 0 ? line + ",Country"
                                                                                             : line + ",US").ToList();
                File.WriteAllLines(BusRepfilePath, Buscol3);

                // Reference URL
                var Buscol4 = File.ReadLines(BusRepfilePath).Select((line, index) => index == 0 ? line + ",Reference"
                                                                                       : line + "," + driver.Url).ToList();
                File.WriteAllLines(BusRepfilePath, Buscol4);

                // Renaming the Report File
                File.Move(BusRepfilePath, @"G:\CommerceLabs\Reports\Reports\App_Data\" + "BusinessReport" + i + ".csv");
            }

            // Combining all the CSV Files into 1
            string BussourceFolder = @"G:\CommerceLabs\Reports\Reports\App_Data\";
            string BusdestinationFile = @"G:\CommerceLabs\Reports\Reports\App_Data\" + "BusinessReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";

            // Matches the CSV files by searching the specified wildcard and combines them to 1
            string[] filePaths = Directory.GetFiles(BussourceFolder, "BusinessReport-?.csv");
            StreamWriter fileDest = new StreamWriter(BusdestinationFile, true);

            for (int j = 0; j < filePaths.Length; j++)
            {
                string file = filePaths[j];

                string[] lines = File.ReadAllLines(file);

                if (j > 0)
                {
                    lines = lines.Skip(1).ToArray(); // Skipping Header row for all except first file
                }

                foreach (string line in lines)
                {
                    fileDest.WriteLine(line);
                }
            }

            fileDest.Close();

            // Uploading Business Report to Azure Data Lake
            Azuresource = @"G:\CommerceLabs\Reports\Reports\App_Data\" + "BusinessReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            Azuredestination = "Meal prep haven/CA_AO/Business Reports/" + "BusinessReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Business Report");

            // Downloading Reserved Inventory Report
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/gp/ssof/reports/search.html#orderAscending=&recordType=ReserveBreakdown&noResultType=&merchantSku=&fnSku=&FnSkuXORMSku=&reimbursementId=&orderId=&genericOrderId=&asin=&lpn=&shipmentId=&problemType=ALL_DEFECT_TYPES&hazmatStatus=&inventoryEventTransactionType=&fulfillmentCenterId=&transactionItemId=&inventoryAdjustmentReasonGroup=&eventDateOption=1&fromDate=mm%2Fdd%2Fyyyy&toDate=mm%2Fdd%2Fyyyy&startDate=&endDate=&fromMonth=1&fromYear=2019&toMonth=1&toYear=2019&startMonth=&startYear=&endMonth=&endYear=&specificMonth=1&specificYear=2019");
            Thread.Sleep(1000);
            driver.FindElement(By.XPath("//*[@id=\"requestCsvTsvDownload\"]/tr[1]/td[2]/button")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            Thread.Sleep(10000);
            driver.FindElement(By.XPath("//*[@id=\"downloadArchive\"]/table/tbody/tr[1]/td[5]/a")).Click();

            // Editing the Reserved Inventory CSV
            // Appending Columns and Values in Existing CSV File
            String ResRepfilePath = @"G:\CommerceLabs\Reports\Reports\App_Data\17366271616018202.csv";

            // Account
            var Rescol1 = File.ReadLines(ResRepfilePath).Select((line, index) => index == 0 ? line + ",Account"
                                                                                         : line + ",Meal Prep Haven").ToList();
            File.WriteAllLines(ResRepfilePath, Rescol1);

            // Country
            var Rescol2 = File.ReadLines(ResRepfilePath).Select((line, index) => index == 0 ? line + ",Country"
                                                                                         : line + ",US").ToList();
            File.WriteAllLines(ResRepfilePath, Rescol2);

            // Reference URL
            var Rescol3 = File.ReadLines(ResRepfilePath).Select((line, index) => index == 0 ? line + ",Reference"
                                                                                         : line + "," + driver.Url).ToList();
            File.WriteAllLines(ResRepfilePath, Rescol3);

            // Renaming the Report File
            File.Move(ResRepfilePath, @"G:\CommerceLabs\Reports\Reports\App_Data\" + "ReservedInventoryReport.csv");


            // Uploading Reserved Inventory Report to Azure Data Lake
            Azuresource = @"G:\CommerceLabs\Reports\Reports\App_Data\ReservedInventoryReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Reserved Inventory Reports/" + "ReservedInventoryReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Reserved Inventory Report");


            // Downloading Managed Inventory Report
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/gp/ssof/reports/search.html?#orderAscending=&recordType=FBA_MYI_UNSUPPRESSED_INVENTORY&noResultType=&merchantSku=&fnSku=&FnSkuXORMSku=&reimbursementId=&orderId=&genericOrderId=&asin=&lpn=&shipmentId=&problemType=ALL_DEFECT_TYPES&hazmatStatus=&inventoryEventTransactionType=&fulfillmentCenterId=&transactionItemId=&inventoryAdjustmentReasonGroup=&eventDateOption=1&fromDate=mm%2Fdd%2Fyyyy&toDate=mm%2Fdd%2Fyyyy&startDate=&endDate=&fromMonth=1&fromYear=2019&toMonth=1&toYear=2019&startMonth=&startYear=&endMonth=&endYear=&specificMonth=1&specificYear=2019");
            Thread.Sleep(1000);
            driver.FindElement(By.XPath("//*[@id=\"requestCsvTsvDownload\"]/tr[1]/td[2]/button")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            Thread.Sleep(10000);
            driver.FindElement(By.XPath("//*[@id=\"downloadArchive\"]/table/tbody/tr[1]/td[5]/a")).Click();

            // Editing the Managed Inventory CSV
            // Appending Columns and Values in Existing CSV File
            String ManRepfilePath = @"G:\CommerceLabs\Reports\Reports\App_Data\17358619756018202.csv";

            // Account
            var Mancol1 = File.ReadLines(ManRepfilePath).Select((line, index) => index == 0 ? line + ",Account"
                                                                                            : line + ",Meal Prep Haven").ToList();
            File.WriteAllLines(ManRepfilePath, Mancol1);

            // Country
            var Mancol2 = File.ReadLines(ManRepfilePath).Select((line, index) => index == 0 ? line + ",Country"
                                                                                            : line + ",US").ToList();
            File.WriteAllLines(ManRepfilePath, Mancol2);

            // Reference URL
            var Mancol3 = File.ReadLines(ManRepfilePath).Select((line, index) => index == 0 ? line + ",Reference"
                                                                                            : line + "," + driver.Url).ToList();
            File.WriteAllLines(ManRepfilePath, Mancol3);

            // Renaming the Report File
            File.Move(ManRepfilePath, @"G:\CommerceLabs\Reports\Reports\App_Data\" + "ManagedInventoryReport.csv");


            // Uploading Reserved Inventory Report to Azure Data Lake
            Azuresource = @"G:\CommerceLabs\Reports\Reports\App_Data\ManagedInventoryReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Managed Inventory Reports/" + "ManagedInventoryReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Managed Inventory Report"); 


            // Downloading Payment Report
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/payments/reports/custom/request?tbla_daterangereportstable=sort:%7B%22sortOrder%22%3A%22DESCENDING%22%7D;search:undefined;pagination:1;");
            Thread.Sleep(1000);
            driver.FindElement(By.XPath("//*[@id=\"drrGenerateReportButton\"]/span/input")).Click();
            driver.FindElement(By.XPath("//*[@id=\"drrReportTypeRadioTransaction\"]")).Click();
            driver.FindElement(By.XPath("//*[@id=\"drrReportRangeTypeRadioCustom\"]")).Click();
            driver.FindElement(By.XPath("//*[@id=\"drrFromDate\"]")).SendKeys(System.DateTime.Now.AddDays(-1).ToString("MM/dd/yyyy").Replace("-", "/") + Keys.Enter);
            driver.FindElement(By.XPath("//*[@id=\"drrToDate\"]")).SendKeys(System.DateTime.Now.AddDays(-1).ToString("MM/dd/yyyy").Replace("-", "/") + Keys.Enter);
            driver.FindElement(By.XPath("//*[@id=\"drrGenerateReportsGenerateButton\"]/span/input")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            driver.FindElement(By.ClassName("drrRefreshTable")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            Thread.Sleep(10000);
            driver.FindElement(By.XPath("//*[@id=\"downloadButton\"]")).Click();

            // Editing the Payment CSV
            // Appending Columns and Values in Existing CSV File
            String PayRepfilePath = @"G:\CommerceLabs\Reports\Reports\App_Data\2019Nov1-2019Nov1CustomTransaction.csv";

            // Account
            var Paycol1 = File.ReadLines(PayRepfilePath).Select((line, index) => index == 0 ? line + ",Account"
                                                                                            : line + ",Meal Prep Haven").ToList();
            File.WriteAllLines(PayRepfilePath, Paycol1);

            // Country
            var Paycol2 = File.ReadLines(PayRepfilePath).Select((line, index) => index == 0 ? line + ",Country"
                                                                                            : line + ",US").ToList();
            File.WriteAllLines(PayRepfilePath, Paycol2);

            // Reference URL
            var Paycol3 = File.ReadLines(PayRepfilePath).Select((line, index) => index == 0 ? line + ",Reference"
                                                                                            : line + "," + driver.Url).ToList();
            File.WriteAllLines(PayRepfilePath, Paycol3);

            // Renaming the Report File
            File.Move(PayRepfilePath, @"G:\CommerceLabs\Reports\Reports\App_Data\" + "PaymentReport.csv");


            // Uploading Reserved Inventory Report to Azure Data Lake
            Azuresource = @"G:\CommerceLabs\Reports\Reports\App_Data\PaymentReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Payment Reports/" + "PaymentReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Payment Report");


            // Downloading Storage Fees Report
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/gp/ssof/reports/search.html#orderAscending=&recordType=STORAGE_FEE_CHARGES&noResultType=&merchantSku=&fnSku=&FnSkuXORMSku=&reimbursementId=&orderId=&genericOrderId=&asin=&lpn=&shipmentId=&problemType=ALL_DEFECT_TYPES&hazmatStatus=&inventoryEventTransactionType=&fulfillmentCenterId=&transactionItemId=&inventoryAdjustmentReasonGroup=&eventDateOption=1&fromDate=mm%2Fdd%2Fyyyy&toDate=mm%2Fdd%2Fyyyy&startDate=&endDate=&fromMonth=1&fromYear=2019&toMonth=1&toYear=2019&startMonth=&startYear=&endMonth=&endYear=&specificMonth=1&specificYear=2019");
            Thread.Sleep(1000);
            driver.FindElement(By.XPath("//*[@id=\"specificMonthDownload\"]/option[9]")).Click();
            driver.FindElement(By.XPath("//*[@id=\"specificYearDownload\"]/option[1]")).Click();
            driver.FindElement(By.XPath("//*[@id=\"requestCsvTsvDownload\"]/tr[1]/td[2]/button")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(4000);
            Thread.Sleep(10000);
            driver.FindElement(By.XPath("//*[@id=\"downloadArchive\"]/table/tbody/tr[1]/td[5]/a")).Click();

            // Editing the Storage Fees CSV
            // Appending Columns and Values in Existing CSV File
            String StoRepfilePath = @"G:\CommerceLabs\Reports\Reports\App_Data\17375924800018202.csv";

            // Account
            var Stocol1 = File.ReadLines(StoRepfilePath).Select((line, index) => index == 0 ? line + ",Account"
                                                                                            : line + ",Meal Prep Haven").ToList();
            File.WriteAllLines(StoRepfilePath, Stocol1);

            // Country
            var Stocol2 = File.ReadLines(StoRepfilePath).Select((line, index) => index == 0 ? line + ",Country"
                                                                                            : line + ",US").ToList();
            File.WriteAllLines(StoRepfilePath, Stocol2);

            // Reference URL
            var Stocol3 = File.ReadLines(StoRepfilePath).Select((line, index) => index == 0 ? line + ",Reference"
                                                                                            : line + "," + driver.Url).ToList();
            File.WriteAllLines(StoRepfilePath, Stocol3);

            // Renaming the Report File
            File.Move(StoRepfilePath, @"G:\CommerceLabs\Reports\Reports\App_Data\" + "StorageFeesReport.csv");


            // Uploading Reserved Inventory Report to Azure Data Lake
            Azuresource = @"G:\CommerceLabs\Reports\Reports\App_Data\StorageFeesReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Storage Fees Reports/" + "StorageFeesReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Storage Fees Report");

            // ----------------------------------------------------------------------------------------------------------------------------------------------------------- 
            // Essa Login Page
            /*driver.Navigate().GoToUrl("https://secure-wms.com/webui/login?callbackUri=https://secure-wms.com/smartui/&tplguid={494c9efa-39c6-4b26-9228-22c5feda6b2a}");
            driver.FindElement(By.Id("login")).SendKeys("Commerce1");
            driver.FindElement(By.Id("password")).SendKeys("Breakv1");
            driver.FindElement(By.XPath("/html/body/main/div/div[2]/form/input[2]")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            Thread.Sleep(500);

            // Downloading Essa Report
            driver.FindElement(By.XPath("//*[@id=\"wms_Misc\"]/div/a")).Click();
            driver.FindElement(By.XPath("//*[@id=\"wms_Misc\"]/ul/li[2]/a")).Click();

            driver.FindElement(By.ClassName("k-widget")).Click();

            //driver.FindElement(By.XPath("//*[@id=\"GridManageInventorymenu\"]/li/span/span[2]")).Click();

            // Uploading Essa Report to Azure Data Lake

            Console.WriteLine("");*/
        }
    }
}
