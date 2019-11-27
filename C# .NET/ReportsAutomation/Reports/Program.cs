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
using RestSharp;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Collections.Generic;
using MarketplaceWebService.Model;
using MarketplaceWebService;
using OtpNet;

namespace Reports
{
    class Program
    {
        // Azure Prerequisites
        private static DataLakeStoreFileSystemManagementClient adlsFileSystemClient;  // Data Lake Store File System Management Client
        private static string AzureClientId = ConfigurationManager.AppSettings["AzureClientID"];  // Application ID [Client ID]
        private static string AzureClientSecret = ConfigurationManager.AppSettings["AzureClientSecret"];  // Client Secret Key
        private static string AzureTenantId = ConfigurationManager.AppSettings["AzureTenantID"];  // Directory ID [Tenant ID]
        private static string AdlsAccountName = ConfigurationManager.AppSettings["AzureDataLakeName"];  // Name of the Azure Data Lake Store

        // Seller Central Prerequisites
        private static string SC_LoginID = ConfigurationManager.AppSettings["SC_LoginID"]; // Seller Central Login ID
        private static string SC_LoginPwd = ConfigurationManager.AppSettings["SC_LoginPwd"]; // Seller Central Password

        // Amazon Advertised Product Prerequisites
        private static string ADS_RefreshToken = ConfigurationManager.AppSettings["ADS_RefreshToken"]; // Amazon ADS Refresh Token
        private static string ADS_ClientID = ConfigurationManager.AppSettings["ADS_ClientID"]; // Amazon ADS Client ID
        private static string ADS_ClientSecret = ConfigurationManager.AppSettings["ADS_ClientSecret"]; // Amazon ADS Client Secret Key
        private static string ADS_Scope = ConfigurationManager.AppSettings["ADS_Scope"]; // Amazon ADS Scope

        // WorldPack Prerequisites
        private static string WP_Authorization = ConfigurationManager.AppSettings["WP_Authorization"]; // WorldPack Authorisation Token

        // Amazon MWS Prerequisites
        private static string MWS_AccessID = ConfigurationManager.AppSettings["MWS_AccessID"]; //Amazon MWS Access Key ID
        private static string MWS_SecretKey = ConfigurationManager.AppSettings["MWS_SecretKey"]; // Amazon MWS Secret Access Key
        private static string MWS_MerchantID = ConfigurationManager.AppSettings["MWS_MerchantID"]; // Amazon MWS Merchant ID
        private static string MWS_AuthToken = ConfigurationManager.AppSettings["MWS_AuthToken"]; // Amazon MWS Auth Token

        static void Main(string[] args)
        {
            // Azure Data Lake Initialisation
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var clientCredential = new ClientCredential(AzureClientId, AzureClientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(AzureTenantId, clientCredential).Result;
            adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            // Azure Source and Destination Variable 
            string Azuresource = "", Azuredestination = "";

            // Creating a Folder in the App Path
            var directoryFullPath = AppDomain.CurrentDomain.BaseDirectory + @"DownloadedReports\";    // Creating a Folder in Current Directory if not exists
            Directory.CreateDirectory(directoryFullPath);

            // Web Driver Object
            IWebDriver driver;

            // Setting the needed Chrome Options 
            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("headless"); // Setting the Browser as Headless
            options.AddUserProfilePreference("download.default_directory", directoryFullPath);  // Setting the default directory for the report to be downloaded
            options.AddUserProfilePreference("intl.accept_languages", "en-us"); // Setting the language as English
            //options.AddUserProfilePreference("disable-popup-blocking", "true"); // Setting to Allow Flash Screens
            //List<string> allowFlashUrls = new List<string>() { "http://www.flexwebportal.com:8080/WebSynapse/CrystalReportViewer.jsp"};
            //options.AddUserProfilePreference("profile.managed_plugins_allowed_for_urls", allowFlashUrls);

            // Path of ChromeDrive.exe
            driver = new ChromeDriver(AppDomain.CurrentDomain.BaseDirectory, options); // Assigning all the above options to Chrome Driver
            Console.WriteLine("Started");


            // Amazon Seller Central Login Page
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/");
            driver.FindElement(By.ClassName("secondary")).Click();
            driver.FindElement(By.Id("ap_email")).SendKeys(SC_LoginID);
            driver.FindElement(By.Id("ap_password")).SendKeys(SC_LoginPwd);
            driver.FindElement(By.Id("signInSubmit")).Click();

            //// Generating OTP
            //var otpKeyStr = "WGHSYZEE2RJOO6OFMCBMB6HKNHDZRADH4TK2FDJ77YWQ5YFRWDBQ"; //  2FA secret key.
            //var otpKeyBytes = Base32Encoding.ToBytes(otpKeyStr);
            //var totp = new Totp(otpKeyBytes);
            //var twoFactorCode = totp.ComputeTotp(DateTime.UtcNow); // 2FA code at this time!
            //Console.WriteLine(twoFactorCode);

            // OTP
            Console.WriteLine("Please enter the OTP : ");
            var input = Console.ReadLine();
            driver.FindElement(By.Id("auth-mfa-otpcode")).SendKeys(input);
            driver.FindElement(By.Id("auth-mfa-remember-device")).Click();
            driver.FindElement(By.Id("auth-signin-button")).Click();

            // ------------------ REPORT 1 ------------------ // ---- Automating Scraping Method ----
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
                string BusRepfilePath = "";
                foreach (string filename in Directory.GetFiles(directoryFullPath))
                {
                    BusRepfilePath = directoryFullPath + Path.GetFileName(filename);  // Getting the FileName   
                }
                Thread.Sleep(1000);

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
                File.Move(BusRepfilePath, directoryFullPath + "BusinessReport" + i + ".csv");
            }

            // Combining all the CSV Files into 1
            string BussourceFolder = directoryFullPath;
            string BusdestinationFile = directoryFullPath + @"\BusinessReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";

            // Matches the CSV files by searching the specified wildcard and combines them to single file
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
            Azuresource = directoryFullPath + @"BusinessReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            Azuredestination = "Meal prep haven/CA_AO/Business Reports/" + "BusinessReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(AdlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Business Report");

            // Deleting the Business Report Files
            Array.ForEach(Directory.GetFiles(directoryFullPath), File.Delete);


            // ------------------ REPORT 2 ------------------ // ---- Automating Scraping Method ----
            // Downloading Reserved Inventory Report
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/gp/ssof/reports/search.html#orderAscending=&recordType=ReserveBreakdown&noResultType=&merchantSku=&fnSku=&FnSkuXORMSku=&reimbursementId=&orderId=&genericOrderId=&asin=&lpn=&shipmentId=&problemType=ALL_DEFECT_TYPES&hazmatStatus=&inventoryEventTransactionType=&fulfillmentCenterId=&transactionItemId=&inventoryAdjustmentReasonGroup=&eventDateOption=1&fromDate=mm%2Fdd%2Fyyyy&toDate=mm%2Fdd%2Fyyyy&startDate=&endDate=&fromMonth=1&fromYear=2019&toMonth=1&toYear=2019&startMonth=&startYear=&endMonth=&endYear=&specificMonth=1&specificYear=2019");
            Thread.Sleep(1000);
            driver.FindElement(By.XPath("//*[@id=\"requestCsvTsvDownload\"]/tr[1]/td[2]/button")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            Thread.Sleep(10000);
            driver.FindElement(By.XPath("//*[@id=\"downloadArchive\"]/table/tbody/tr[1]/td[5]/a")).Click();

            // Editing the Reserved Inventory CSV
            // Appending Columns and Values in Existing CSV File
            string ResRepfilePath = "";
            foreach (string filename in Directory.GetFiles(directoryFullPath))
            {
                ResRepfilePath = directoryFullPath + Path.GetFileName(filename);  // Getting the File Name
            }
                
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
            File.Move(ResRepfilePath, directoryFullPath + "ReservedInventoryReport.csv");

            // Uploading Reserved Inventory Report to Azure Data Lake
            Azuresource = directoryFullPath + "ReservedInventoryReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Reserved Inventory Reports/" + "ReservedInventoryReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(AdlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Reserved Inventory Report");

            // Deleting the Reserved Inventory Report File
            Array.ForEach(Directory.GetFiles(directoryFullPath), File.Delete);


            // ------------------ REPORT 3 ------------------ // ---- Automating Scraping Method ----
            // Downloading Managed Inventory Report
            driver.Navigate().GoToUrl("https://sellercentral.amazon.com/gp/ssof/reports/search.html?#orderAscending=&recordType=FBA_MYI_UNSUPPRESSED_INVENTORY&noResultType=&merchantSku=&fnSku=&FnSkuXORMSku=&reimbursementId=&orderId=&genericOrderId=&asin=&lpn=&shipmentId=&problemType=ALL_DEFECT_TYPES&hazmatStatus=&inventoryEventTransactionType=&fulfillmentCenterId=&transactionItemId=&inventoryAdjustmentReasonGroup=&eventDateOption=1&fromDate=mm%2Fdd%2Fyyyy&toDate=mm%2Fdd%2Fyyyy&startDate=&endDate=&fromMonth=1&fromYear=2019&toMonth=1&toYear=2019&startMonth=&startYear=&endMonth=&endYear=&specificMonth=1&specificYear=2019");
            Thread.Sleep(1000);
            driver.FindElement(By.XPath("//*[@id=\"requestCsvTsvDownload\"]/tr[1]/td[2]/button")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            Thread.Sleep(10000);
            driver.FindElement(By.XPath("//*[@id=\"downloadArchive\"]/table/tbody/tr[1]/td[5]/a")).Click();

            // Editing the Managed Inventory CSV
            // Appending Columns and Values in Existing CSV File
            string ManRepfilePath = "";
            foreach (string filename in Directory.GetFiles(directoryFullPath))
            {
                ManRepfilePath = directoryFullPath + Path.GetFileName(filename);  // Getting the File Name
            }

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
            File.Move(ManRepfilePath, directoryFullPath + "ManagedInventoryReport.csv");

            // Uploading Reserved Inventory Report to Azure Data Lake
            Azuresource = directoryFullPath + "ManagedInventoryReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Managed Inventory Reports/" + "ManagedInventoryReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(AdlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Managed Inventory Report");

            // Deleting the Managed Inventory Report File
            Array.ForEach(Directory.GetFiles(directoryFullPath), File.Delete);


            // ------------------ REPORT 4 ------------------ // ---- Automating Scraping Method ----
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
            string PayRepfilePath = "";
            foreach (string filename in Directory.GetFiles(directoryFullPath))
            {
                PayRepfilePath = directoryFullPath + Path.GetFileName(filename);  // Getting the File Name
            }

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
            File.Move(PayRepfilePath, directoryFullPath + "PaymentReport.csv");


            // Uploading Payment Report to Azure Data Lake
            Azuresource = directoryFullPath + "PaymentReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Payment Reports/" + "PaymentReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(AdlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Payment Report");

            // Deleting the Payment Report File
            Array.ForEach(Directory.GetFiles(directoryFullPath), File.Delete);


            // ------------------ REPORT 5 ------------------ // ---- Automating Scraping Method ----
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
            string StoRepfilePath = "";
            foreach (string filename in Directory.GetFiles(directoryFullPath))
            {
                StoRepfilePath = directoryFullPath + Path.GetFileName(filename);  // Getting the File Name
            }

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
            File.Move(StoRepfilePath, directoryFullPath + "StorageFeesReport.csv");

            // Uploading Storage Fees Report to Azure Data Lake
            Azuresource = directoryFullPath + "StorageFeesReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Storage Fees Reports/" + "StorageFeesReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(AdlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Storage Fees Report");

            // Deleting the Storage Fees Report File
            Array.ForEach(Directory.GetFiles(directoryFullPath), File.Delete);


            // ------------------ REPORT 6 ------------------ // ---- API Method ----
            // Downloading Amazon ADS Product Report through API
            // Refreshing Access Token
            var client1 = new RestClient("https://api.amazon.com/auth/o2/token");
            var request1 = new RestRequest(Method.POST);
            request1.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request1.AddParameter("grant_type", "refresh_token");
            request1.AddParameter("refresh_token", ADS_RefreshToken);
            request1.AddParameter("client_id", ADS_ClientID);
            request1.AddParameter("client_secret", ADS_ClientSecret);
            IRestResponse response1 = client1.Execute(request1);
            dynamic result1 = JsonConvert.DeserializeObject(response1.Content);
            string ADS_AccessToken = result1.access_token;

            // Getting the Report ID
            var client2 = new RestClient("https://advertising-api.amazon.com/v2/sp/productAds/report");
            var request2 = new RestRequest(Method.POST);
            request2.AddHeader("Content-Type", "application/json");
            request2.AddHeader("Authorization", "Bearer " + ADS_AccessToken);
            request2.AddHeader("Amazon-Advertising-API-ClientId", ADS_ClientID);
            request2.AddHeader("Amazon-Advertising-API-Scope", ADS_Scope);
            request2.AddParameter("undefined", "{\"reportDate\":\"20190930\", \"metrics\":\"campaignName,campaignId,adGroupName,adGroupId,currency,asin,sku,impressions,clicks,cost,attributedConversions1d,attributedConversions7d,attributedConversions14d,attributedConversions30d,attributedConversions1dSameSKU,attributedConversions7dSameSKU,attributedConversions14dSameSKU,attributedConversions30dSameSKU,attributedUnitsOrdered1d,attributedUnitsOrdered7d,attributedUnitsOrdered14d,attributedUnitsOrdered30d,attributedSales1d,attributedSales7d,attributedSales14d,attributedSales30d,attributedSales1dSameSKU,attributedSales7dSameSKU,attributedSales14dSameSKU,attributedSales30dSameSKU\"}", ParameterType.RequestBody);
            IRestResponse response2 = client2.Execute(request2);
            dynamic result2 = JsonConvert.DeserializeObject(response2.Content);
            string ReportID = result2.reportId;
            Thread.Sleep(3000);

            // Retrieving the Report Status
            var client3 = new RestClient("https://advertising-api.amazon.com/v2/reports/" + ReportID);
            var request3 = new RestRequest(Method.GET);
            request3.AddHeader("Content-Type", "application/json");
            request3.AddHeader("Authorization", "Bearer " + ADS_AccessToken);
            request3.AddHeader("Amazon-Advertising-API-ClientId", ADS_ClientID);
            request3.AddHeader("Amazon-Advertising-API-Scope", ADS_Scope);
            IRestResponse response3 = client3.Execute(request3);
            dynamic result3 = JsonConvert.DeserializeObject(response3.Content);
            string DownloadLocation = result3.location;

            if(result3.status == "SUCCESS")
            {
                // Retrieving Report Download URL
                var client4 = new RestClient(DownloadLocation);
                var request4 = new RestRequest(Method.GET);
                request4.AddHeader("Authorization", "Bearer " + ADS_AccessToken);
                request4.AddHeader("Amazon-Advertising-API-Scope", ADS_Scope);
                IRestResponse response4 = client4.Execute(request4);
                string DownloadURL = response4.ResponseUri.ToString();

                // Downloading the Report
                driver.Navigate().GoToUrl(DownloadURL);
            }

            // Extracting .gz Zip file
            DirectoryInfo directorySelected = new DirectoryInfo(directoryFullPath);
            foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.gz"))
            {
                Decompress(fileToDecompress);
            }

            // Converting the Json File to CSV File


            // Uploading Amazon ADS Product Report to Azure Data Lake
            Azuresource = directoryFullPath + "ADSProductReport.csv";
            Azuredestination = "Meal prep haven/CA_AO/Advertised Product Reports/" + "AmazonADSProductReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".csv";
            adlsFileSystemClient.FileSystem.UploadFile(AdlsAccountName, Azuresource, Azuredestination, 1, false, true);
            Console.WriteLine("Uploaded Amazon ADS Product Report");


            // ------------------ REPORT 7 ------------------ // ---- API Method ----
            // Downloading WorldPack Report through API
            // Getting the Access Token
            var clientw1 = new RestClient("http://secure-wms.com/AuthServer/api/Token");
            var requestw1 = new RestRequest(Method.POST);
            requestw1.AddHeader("Content-Type", "application/json; charset=utf-8");
            requestw1.AddHeader("Accept", "application/json");
            requestw1.AddHeader("Authorization", "Basic " + WP_Authorization);
            requestw1.AddParameter("undefined", "{\"grant_type\": \"client_credentials\",\"tpl\": \"{1ddbea91-a4ff-4b42-a25d-81a25b8cb727}\",\"user_login_id\": \"759\"}", ParameterType.RequestBody);
            IRestResponse responsew1 = clientw1.Execute(requestw1);
            dynamic resultw1 = JsonConvert.DeserializeObject(responsew1.Content);
            string WP_AccessToken = resultw1.access_token;

            // Getting the Inventory Details
            var clientw2 = new RestClient("https://secure-wms.com/inventory/stockdetails?customerid=194&facilityid=4");
            var requestw2 = new RestRequest(Method.GET);
            requestw2.AddHeader("Content-Type", "application/hal+json; charset=utf-8");
            requestw2.AddHeader("Accept", "application/hal+json");
            requestw2.AddHeader("Authorization", "Bearer " + WP_AccessToken);
            IRestResponse responsew2 = clientw2.Execute(requestw2);
            dynamic resultw2 = JsonConvert.DeserializeObject(responsew2.Content);
            string jsonstr = resultw2._embedded.ToString();

            // Json String to CSV

            // Uploading WorldPack Report to Azure Data Lake


            // ------------------ REPORT 8 ------------------ // ---- API Method ----
            // Downloading Amazon MWS Reports - FBA_StorageFees Report through API
            string accessKeyId = MWS_AccessID;
            string secretAccessKey = MWS_SecretKey;
            MarketplaceWebServiceConfig config = new MarketplaceWebServiceConfig();
            config.ServiceURL = "https://mws.amazonservices.com";
            const string applicationName = "ApplicationName";
            const string applicationVersion = "0.1a";

            MarketplaceWebServiceClient service =
            new MarketplaceWebServiceClient(
                   accessKeyId,
                   secretAccessKey,
                   applicationName,
                   applicationVersion,
                   config);

            string merchantID = MWS_MerchantID;
            string mwsauthtoken = MWS_AuthToken;

            // Requesting the Report
            RequestReportRequest reportRequest = new RequestReportRequest();
            reportRequest.Merchant = merchantID;
            reportRequest.MWSAuthToken = mwsauthtoken;
            reportRequest.ReportType = "_GET_FBA_STORAGE_FEE_CHARGES_DATA_";
            reportRequest.StartDate = DateTime.Parse("01-09-2019");
            reportRequest.EndDate = DateTime.Now;
            reportRequest.ReportOptions = "true"; //shows sales channel to certain reports

            // Handling the Response of RequestReport
            RequestReportResponse reportResponse = service.RequestReport(reportRequest);
            System.Threading.Thread.Sleep(30000); //sleep for 30 seconds to allow report request to generate prior to requesting the reportRequestID

            string requestID = reportResponse.RequestReportResult.ReportRequestInfo.ReportRequestId;

            string reportStatus = ""; // holds the status of the report

            // Getting the Report Request List
            GetReportRequestListRequest reportRequestList = new GetReportRequestListRequest();
            reportRequestList.Merchant = merchantID;
            reportRequestList.MWSAuthToken = mwsauthtoken;

            // Handling the Response of Report Request List
            GetReportRequestListResponse reportRequestListReponse = service.GetReportRequestList(reportRequestList);

            foreach (ReportRequestInfo info in reportRequestListReponse.GetReportRequestListResult.ReportRequestInfo)
            {
                if (info.ReportRequestId.Equals(requestID))
                {
                    reportStatus = info.ReportProcessingStatus;

                    if (reportStatus == "_DONE_")
                    {
                        // Getting the Report using Report ID
                        GetReportRequest getReport = new GetReportRequest();
                        getReport.ReportId = info.GeneratedReportId;
                        getReport.Merchant = merchantID;
                        getReport.MWSAuthToken = mwsauthtoken;

                        // Writting the Report as a File
                        string filename = Path.Combine(directoryFullPath, "FBA_StorageFeesReport-" + DateTime.Now.ToString("dd/MM/yyyy") + ".txt");
                        getReport.Report = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        GetReportResponse report_response = service.GetReport(getReport);
                        Console.WriteLine("Sleeping for 1 minute to allow processes to settle. ");
                        Thread.Sleep(60000);
                        getReport.Report.Close();

                        // Uploading FBA Storage Fees to Azure Data Lake
                        Azuresource = filename;
                        Azuredestination = "Amazon MWS Reports/" + "FBA_StorageFeesReport-" + System.DateTime.Now.ToString("dd/MM/yyyy") + ".txt";
                        adlsFileSystemClient.FileSystem.UploadFile(AdlsAccountName, Azuresource, Azuredestination, 1, false, true);
                        Console.WriteLine("Uploaded Amazon MWS FBA_StorageFees Report");

                        break;
                    }
                    
                }
                else
                {
                    continue; // Only concerning about the requested report, not all the reports being generated
                }
            }

            /*// ------------------ REPORT 9 ------------------ //
            // Flex Login Page
            driver.Navigate().GoToUrl("http://www.flexwebportal.com:8080/WebSynapse/");
            driver.FindElement(By.Name("username")).SendKeys("COMMERCE");
            driver.FindElement(By.Name("password")).SendKeys("COMLABS696");
            driver.FindElement(By.Name("submit")).Click();
            driver.Navigate().GoToUrl("http://www.flexwebportal.com:8080/WebSynapse/reports.screen");
            driver.FindElement(By.Name("reportType")).SendKeys("Inventory As Of Date" + Keys.Enter);
            driver.FindElement(By.Name("Update")).Click();
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(4000);
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            Thread.Sleep(50000);
            driver.Navigate().Refresh();
            Thread.Sleep(50000);
            driver.FindElement(By.XPath("//*[@id=\"CrystalViewer_page\"]")).Click();
            Thread.Sleep(1000);
            driver.FindElement(By.XPath("//*[@id=\"CrystalViewer_page\"]")).Click();

            Console.ReadLine();

            // ------------------ REPORT 10 ------------------ //
            // Essa Login Page
            driver.Navigate().GoToUrl("https://secure-wms.com/webui/login?callbackUri=https://secure-wms.com/smartui/?page=manageinventory&tplguid={494c9efa-39c6-4b26-9228-22c5feda6b2a}");
            driver.FindElement(By.Id("login")).SendKeys("Commerce1");
            driver.FindElement(By.Id("password")).SendKeys("Breakv1");
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2000);
            driver.FindElement(By.XPath("/html/body/main/div/div[2]/form/input[2]")).Click();

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1000);
            Thread.Sleep(500);

            // Downloading Essa Report
            driver.FindElement(By.XPath("//*[@id=\"wms_Inventiry\"]/div/a")).Click();
            driver.FindElement(By.XPath("//*[@id=\"wms_Inventiry\"]/ul/li[2]/a")).Click();

            driver.FindElement(By.XPath("//*[@id=\"inventory_proto_selectedInvetoryFacilityId_listbox\"]/li")).Click();


            //driver.FindElement(By.XPath("//*[@id=\"GridManageInventorymenu\"]/li/span/span[2]")).Click();

            // Uploading Essa Report to Azure Data Lake

            Console.WriteLine("");*/
        }



        // Function to extract the Zip File [Amazon ADS Product Report]
        public static void Decompress(FileInfo fileToDecompress)
        {
            string Filename1 = "";

            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);
                Filename1 = newFileName;

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }

            File.Move(Filename1, AppDomain.CurrentDomain.BaseDirectory + @"DownloadedReports\" + "ADSProductReport.json");
        }



    }
}
