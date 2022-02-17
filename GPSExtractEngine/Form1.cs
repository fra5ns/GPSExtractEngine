using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoCollectionLibrary;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Data.SqlClient;
 

namespace GPSExtractEngine
{
    public partial class Form1 : Form
    {
        Setting setting = new Setting(); CommonFunction fnc = new CommonFunction();
        const int vendor_amwell = 2, vendor_telto = 5;
        CancellationTokenSource ctsCancelExtract; CancellationToken tokenCancelExtract;
        CancellationTokenSource ctsBreakMongoLoop; CancellationToken tokenMongoBreak;
        string logpath, logfilenm, strConnDB;
        int iNoThreadExtractAmwell = 0, iNoThreadExtractTelto =0 ;
        bool isBusyExtractAmwell = false, isBusyExtractTelto = false;
        protected static IMongoClient _clientTempMongoRead; protected static IMongoDatabase _dbTempMongoRead; bool isTempMongoReadConnected = false;
        protected static IMongoClient _clientTempMongoWrite; protected static IMongoDatabase _dbTempMongoWrite; bool isTempMongoWriteConnected = false;

        ConcurrentDictionary<string, string> ListReportIDAmwell = new ConcurrentDictionary<string, string>();

        bool isQueryMongoOnProgress = false, isBusyOnSaveCache = false ; 
        ConcurrentQueue<BsonDocument> logGPSRawAmwell = new ConcurrentQueue<BsonDocument>();
        ConcurrentQueue<BsonDocument> logGPSRawTeltonika = new ConcurrentQueue<BsonDocument>();
        ConcurrentQueue<logGPS> logGPSFinal = new ConcurrentQueue<logGPS>();

        int total_perMin_query = 0, total_perMin_Extract = 0;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSetting(); Loading_ReportID();
            this.logpath = Directory.GetCurrentDirectory() + "\\log";          
            if (!Directory.Exists(this.logpath))
                Directory.CreateDirectory(this.logpath);
            this.Height = 650;
            this.Width = 760;

            ctsCancelExtract = new CancellationTokenSource();
            tokenCancelExtract = ctsCancelExtract.Token;
        }

        void LoadSetting()
        {
            try
            {
                string jsonSetting = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "setting.json"));
                JObject jObject = JObject.Parse(jsonSetting);

                setting.tempmongodb.gpsRawFile = (string)jObject["db"]["TempMongoDB"]["gpsRawFile"];
                setting.tempmongodb.gpsLogFile = (string)jObject["db"]["TempMongoDB"]["gpsLogFile"];
                setting.tempmongodb.ServerAddr = (string)jObject["db"]["TempMongoDB"]["ServerAddr"];
                setting.tempmongodb.DBName = (string)jObject["db"]["TempMongoDB"]["DBName"];
                setting.tempmongodb.enable = (int)jObject["db"]["TempMongoDB"]["enable"];

                setting.sqlserverdb.SQLDBNm = (string)jObject["db"]["SQLSERVER"]["DB"];
                setting.sqlserverdb.SQLUserNm = (string)jObject["db"]["SQLSERVER"]["User"];
                setting.sqlserverdb.SQLPwd = (string)jObject["db"]["SQLSERVER"]["Pwd"];
                setting.sqlserverdb.SQLIP = (string)jObject["db"]["SQLSERVER"]["IP"];
                strConnDB = "Data Source=" + setting.sqlserverdb.SQLIP + ";Initial Catalog=" + setting.sqlserverdb.SQLDBNm + ";User Id=" + setting.sqlserverdb.SQLUserNm + ";Password=" + setting.sqlserverdb.SQLPwd + ";MultipleActiveResultSets=True;Application Name=ExtractEngine";

                setting.config.maxThreadAmwell = (int)jObject["config"]["maxThreadAmwell"];
                setting.config.maxThreadTelto = (int)jObject["config"]["maxThreadTelto"];
                setting.config.Amwell_id = (int)jObject["config"]["amwell_id"];
                setting.config.Teltonika_id = (int)jObject["config"]["teltonika_id"];

            }
            catch (Exception ex){ MessageBox.Show(ex.Message); }
        }
        void Loading_ReportID()
        {
            try
            {
                ListReportIDAmwell.Clear();
                DataTable ds = GetDataSql("select * from reportid_master where vendor_id= " + vendor_amwell);
                foreach (DataRow row in ds.Rows)
                {
                    ListReportIDAmwell.TryAdd(row["report_id"].ToString() + ";" + row["vendor_id"].ToString() + ";" + row["company_id"].ToString(), row["report_nm"].ToString());
                }
                ds.Dispose();
            }
            catch { }
        }
        private DataTable GetDataSql(string sql)
        {

            DataTable dt = new DataTable();         

            try
            {
               
                using (SqlConnection con = new SqlConnection(strConnDB))
                {
                    con.Open();
                    if (con.State == ConnectionState.Open)
                    {
                        //using (SqlCommand command = new SqlCommand(sql, con))
                        //{
                        using (SqlDataAdapter da = new SqlDataAdapter(sql, con))
                        {
                            if (da != null) da.Fill(dt);
                        }
                        //}
                    }
                    try { con.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("GetDataSQL : " + sql + " error : " + ex.Message));
                if (ex.Message.Contains("Physical connection is not usable")) SqlConnection.ClearAllPools();
            }
            return dt;
        }
        public void ClusterTempMongoRead_DescriptionChanged(object sender, ClusterDescriptionChangedEventArgs e)
        {
            switch (e.NewClusterDescription.State)
            {
                case ClusterState.Disconnected:
                    isTempMongoReadConnected = false;
                    break;
                case ClusterState.Connected:
                    isTempMongoReadConnected = true;
                    break;
            }
        }

        public void ClusterTempMongoWrite_DescriptionChanged(object sender, ClusterDescriptionChangedEventArgs e)
        {
            switch (e.NewClusterDescription.State)
            {
                case ClusterState.Disconnected:
                    isTempMongoWriteConnected = false;
                    break;
                case ClusterState.Connected:
                    isTempMongoWriteConnected = true;
                    break;
            }
        }
        void ConnectTempMongoRead()
        {
            try
            {
                _clientTempMongoRead = new MongoClient(setting.tempmongodb.ServerAddr);
                _dbTempMongoRead = _clientTempMongoRead.GetDatabase(setting.tempmongodb.DBName);
                _clientTempMongoRead.Cluster.DescriptionChanged += ClusterTempMongoRead_DescriptionChanged;

            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("Connect Mongo Temp Read err : " + ex.Message));
            }
        }
        void ConnectTempMongoWrite()
        {
            try
            {
                _clientTempMongoWrite = new MongoClient(setting.tempmongodb.ServerAddr);
                _dbTempMongoWrite = _clientTempMongoWrite.GetDatabase(setting.tempmongodb.DBName);
                _clientTempMongoWrite.Cluster.DescriptionChanged += ClusterTempMongoWrite_DescriptionChanged;
            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("Connect Mongo Write Temp err : " + ex.Message));
            }
        }
        public void AddMessage(string msg)
        {
            try
            {
                int length = txtMsg.Text.Length;
                if (length > 100000)
                {
                    txtMsg.Text = txtMsg.Text.Substring(txtMsg.TextLength - 4097);
                    length = txtMsg.Text.Length;
                }
                string str = string.Concat(new object[] { DateTime.Now.ToString("HH:mm:ss.fff"), " :   ", msg });
                txtMsg.AppendText(str);
                txtMsg.AppendText(Environment.NewLine);
                WriteLog(str);
            }
            catch
            {
            }
        }

        private void tmrQueryMongo_Tick(object sender, EventArgs e)
        {
            if (setting.tempmongodb.enable == 0) return;
            tmrQueryMongo.Enabled = false;
            try
            {
                if (!bwQueryMongo1.IsBusy && !isQueryMongoOnProgress ) bwQueryMongo1.RunWorkerAsync();
            }
            catch { }
            tmrQueryMongo.Enabled = true;
        }

        async Task QueryRawData ( int vendor_id)
        {
            await Task.Run(async () =>
          {
          
            try
            {
                if (!isTempMongoReadConnected) ConnectTempMongoRead();
                CancellationTokenSource ctsBreakLoop = new CancellationTokenSource();
                CancellationToken tokenBreakLoop = ctsBreakLoop.Token;
                var collection = _dbTempMongoRead.GetCollection<BsonDocument>(setting.tempmongodb.gpsRawFile);

                var filter = Builders<BsonDocument>.Filter.Eq("vendor_id", vendor_id);
                var sort = Builders<BsonDocument>.Sort.Ascending("_id");
                var options = new FindOptions<BsonDocument> { BatchSize = 500 };
                IList<FilterDefinition<BsonDocument>> filters = new List<FilterDefinition<BsonDocument>>();
                this.SafeInvoke<Form1>(d => d.toolStripLastQuery.Text = "Query: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                
                using (IAsyncCursor<BsonDocument> cursor = await collection.FindAsync(filter, options, tokenBreakLoop))
                {
                      if (!isTempMongoWriteConnected) ConnectTempMongoWrite();
                      var collwrite = _dbTempMongoWrite.GetCollection<BsonDocument>(setting.tempmongodb.gpsRawFile);
                      while (!tokenBreakLoop.IsCancellationRequested && await cursor.MoveNextAsync())
                      {
                          IEnumerable<BsonDocument> batch = cursor.Current;
                          foreach (BsonDocument doc in batch)
                          {
                              filters.Add(Builders<BsonDocument>.Filter.Eq("_id", doc["_id"].AsObjectId));
                              total_perMin_query++;
                              if (vendor_id == 2) logGPSRawAmwell.Enqueue(doc);
                              else if (vendor_id == 5) logGPSRawTeltonika.Enqueue(doc);
                              this.SafeInvoke<Form1>(d => d.toolStripLastObjectID.Text = "Id: " + doc["_id"].AsObjectId.CreationTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
                              //this.SafeInvoke<Form1>(d => d.lblAmwellQueue.Text = "Amwell : " + logGPSRawAmwell.Count.ToString());
                              this.SafeInvoke<Form1>(d => d.toolStripRecPerMinute.Text = "Per Min Query:" + total_perMin_query.ToString());
                          }
                          if (filters.Count > 0)
                          {
                              Stopwatch sw = new Stopwatch(); sw.Start();
                              var filDel = Builders<BsonDocument>.Filter.Or(filters);
                              DeleteResult xDel = await collwrite.DeleteManyAsync(filDel);
                              filters.Clear(); //kl gak diclear nanti makin banyak dan bkin makin slow
                              sw.Stop();
                              this.SafeInvoke<Form1>(d => d.toolStripDel.Text = "Del " + xDel.DeletedCount.ToString() + " =" + sw.ElapsedMilliseconds.ToString());
                          }
                          if (logGPSRawAmwell.Count > 30000 || logGPSRawTeltonika.Count > 5000) ctsBreakLoop.Cancel();
                          if (tokenMongoBreak.IsCancellationRequested) ctsBreakLoop.Cancel();
                          if (vendor_id == 2)
                              this.SafeInvoke<Form1>(d => d.lblAmwellQueue.Text = "Amwell : " + logGPSRawAmwell.Count.ToString());
                          else if (vendor_id == 5)
                              this.SafeInvoke<Form1>(d => d.lblTeltoQueue.Text = "Telto : " + logGPSRawTeltonika.Count.ToString());

                      }
                }
            }
            catch(Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("QueryRawData " + vendor_id + " err:" + ex.Message));
            }
          });
        }
        private async void bwQueryMongo1_DoWork(object sender, DoWorkEventArgs e)
        {
            isQueryMongoOnProgress = true;
             

                if (setting.config.Amwell_id != 0 && logGPSRawAmwell.Count < 20000 ) await QueryRawData(setting.config.Amwell_id);
                if (setting.config.Teltonika_id != 0 && logGPSRawTeltonika.Count < 5000) await QueryRawData(setting.config.Teltonika_id);


            isQueryMongoOnProgress = false; 
        }

        private void lblTotalRaw_Click(object sender, EventArgs e)
        {

        }

        private void btnStartProses_Click(object sender, EventArgs e)
        {
            this.Text = Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location).ToString().Replace(".exe", "");
            if (btnStartProses.Text.Contains("Start"))
            {
                btnStartProses.Text = "Stop Proses";                
                tmrQueryMongo.Enabled = true; isQueryMongoOnProgress = false;            
                tmrSaveCache.Enabled = true;
              
                ctsBreakMongoLoop = new CancellationTokenSource();
                tokenMongoBreak = ctsBreakMongoLoop.Token;
                ctsCancelExtract = new CancellationTokenSource();
                tokenCancelExtract = ctsCancelExtract.Token;
            }
            else
            {
                btnStartProses.Text = "Start Proses"; 
                tmrQueryMongo.Enabled = false; isQueryMongoOnProgress = false;
                ctsBreakMongoLoop.Cancel();
            }
        }

        public void WriteLog(string msg)
        {
            try
            {
                this.logfilenm = "log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                using (StreamWriter writer = System.IO.File.AppendText(this.logpath + @"\" + this.logfilenm))
                {
                    writer.WriteLine(msg);
                }
            }
            catch { }
        }

        private void tmrExtract_Tick(object sender, EventArgs e)
        {
            tmrExtract.Enabled = false;
            if (!isBusyExtractAmwell && !bwExtractAmwell.IsBusy && iNoThreadExtractAmwell < setting.config.maxThreadAmwell) bwExtractAmwell.RunWorkerAsync();
            if (!isBusyExtractTelto && !bwExtractTelto.IsBusy &&  iNoThreadExtractTelto < setting.config.maxThreadTelto) bwExtractTelto.RunWorkerAsync();
            tmrExtract.Enabled = true;
        }

        private async  void bwExtractAmwell_DoWork(object sender, DoWorkEventArgs e)
        {
            isBusyExtractAmwell = true;
            try
            {
                await Task.Run ( () =>
                {
                    BsonDocument doc; int iCount = 0;
                    while (iNoThreadExtractAmwell < setting.config.maxThreadAmwell && logGPSRawAmwell.TryDequeue(out doc))
                    {
                        Interlocked.Increment(ref iNoThreadExtractAmwell); Debug.WriteLine(iNoThreadExtractAmwell);
                        this.SafeInvoke<Form1>(d => d.toolStripTotalThread.Text = "Thread: " + iNoThreadExtractAmwell);
                        ExtractAmwell(doc);
                        this.SafeInvoke<Form1>(d => d.lblAmwellQueue.Text = "Amwell : " + logGPSRawAmwell.Count.ToString());
                        iCount++;
                        if (iCount > setting.config.maxThreadAmwell) break;
                        Application.DoEvents(); //mesti ditambahkan ini supaya tidak not responding
                    }
                });
            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("extract amwell err : " + ex.Message));
            }
            isBusyExtractAmwell = false;
        }

        private void bwExtractTelto_DoWork(object sender, DoWorkEventArgs e)
        {
            isBusyExtractTelto = true;
            try
            {

            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("extract telto err : " + ex.Message));
            }
            isBusyExtractTelto = false;

        }

        void ExtractAmwell ( BsonDocument doc)
        {
            try
            {
                Task.Factory.StartNew(() =>
               {
                   string strData = doc["data"].AsString;
                   string sDataType = "";

                   string sMsgContent = "";
                   if (doc["image_base64"].AsString != "")
                   {
                       sMsgContent = doc["data"].AsString; // utk camera content lokasinya = rawdata
                        string sn = doc["sn"].AsString;
                       Extract_AmwellData(sMsgContent, sDataType, sn, doc);
                   }
                   else
                   {
                       int sMsgLen = (Convert.ToInt16(strData.Substring(6, 4), 16) - 6) * 2; // dikurangi device id, checksum dan end
                        sMsgContent = strData.Substring(18, sMsgLen);
                       sDataType = strData.Substring(4, 2);
                       string sn = fnc.Reverse_DeviceID(strData.Substring(10, 8));
                       if (sDataType == "82" || sDataType == "83") //alarm  82 start, 83 off (khusus T20)
                            Extract_AmwellAlarm(sMsgContent, sDataType, sn, doc);
                       else
                           Extract_AmwellData(sMsgContent, sDataType, sn, doc);
                   }
                   Interlocked.Decrement(ref iNoThreadExtractAmwell);
               });
            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("Extract Amwell err : " + ex.Message + Environment.NewLine + doc["data"].AsString));
            }
            
            this.SafeInvoke<Form1>(d => d.toolStripTotalThread.Text = "Thread: " + iNoThreadExtractAmwell);
        }

        void Extract_AmwellAlarm(string sMsgContent, string sDataType,  string gps_sn, BsonDocument doc )
        {
            Amwell_Data result = null;
            try
            {
                //2.	ymdhms wwww jjjj ssff p load sign  (for alarm packet)
                string sPositionInfo = sMsgContent.Substring(0, 36); //byte 0-byte 17  digit dari 0 sampai (17*2)+1, panjang 18 byte = 36 digit
                string sGPSstate = sMsgContent.Substring(36, 6);  // byte 18-20 panjang 3 byte = 6 digit    p load sign
                string sAlarmState = sMsgContent.Substring(42, 6); // byte 21-23 panjang 3 byte
                string sAlarmParam = sMsgContent.Substring(48, 10); // byte 24-28 panjang 5 byte

                string sGPSTime = sPositionInfo.Substring(0, 12);
                string sLat = sPositionInfo.Substring(12, 8);
                string sLon = sPositionInfo.Substring(20, 8);
                string sSpeed = sPositionInfo.Substring(28, 4);
                string sDirection = sPositionInfo.Substring(32, 4);
                string sAntennaStatus = sGPSstate.Substring(0, 2); //gps antena & power state
                float sMileage = 0;  //tidak ada info mileage di alarm

                string sCommandIDReply = "";
                string sReportStatus = "", sAlarmStatus = "";
                string sBinerAntenna = fnc.HexToBin(sAntennaStatus);
                if (sBinerAntenna.Substring(0, 1) == "0") sReportStatus += "10,";  // gps unlocated 

                int sACC = 0, sos = 0, overspeed = 0;
                int input1 = 0, input2 = 0, input3 = 0, input4 = 0;
                string sAlarmStatus1 = fnc.HexToBin(sAlarmState.Substring(0, 2));
                string sAlarmStatus2 = fnc.HexToBin(sAlarmState.Substring(2, 2));
                string sAlarmStatus3 = fnc.HexToBin(sAlarmState.Substring(4, 2));
                string sAttachAlarmByte1 = sAlarmParam.Substring(0, 2);
                string sAttachAlarmByte2 = fnc.HexToBin(sAlarmParam.Substring(2, 2));
                string sAttachAlarmByte3 = sAlarmParam.Substring(4, 2);
                string sAttachAlarmByte4 = fnc.HexToBin(sAlarmParam.Substring(6, 2));
                string sAttachAlarmByte5 = sAlarmParam.Substring(8, 2);

                if (sAlarmStatus1.Substring(0, 1) == "1") sAlarmStatus += "35,"; // enter geofence alarm
                if (sAlarmStatus1.Substring(1, 1) == "1") sAlarmStatus += "34,";  //exit geofence alarm
                if (sAlarmStatus1.Substring(3, 1) == "1") sAlarmStatus += "42,";  // low power alarm
                //if (sAlarmStatus1.Substring(4, 1) == "1") { sAlarmStatus += "25,"; input5 = 1; }  //input 5 alarm
                if (sAlarmStatus1.Substring(5, 1) == "1") { sAlarmStatus += "22,"; input2 = 1; }  //input 2 alarm
                if (sAlarmStatus1.Substring(6, 1) == "1") { sAlarmStatus += "21,"; input1 = 1; } //input 1 alarm
                if (sAlarmStatus1.Substring(7, 1) == "1") sAlarmStatus += "43,";  //illegal ignition alarm

                if (sAlarmStatus2.Substring(0, 1) == "1") { sAlarmStatus += "23,"; input3 = 1; } // input 3 alarm
                if (sAlarmStatus2.Substring(1, 1) == "1") sAlarmStatus += "44,";  //movement alarm
                if (sAlarmStatus2.Substring(2, 1) == "1") sAlarmStatus += "106,";  // G sensor alarm
                if (sAlarmStatus2.Substring(3, 1) == "1") { sAlarmStatus += "24,"; input4 = 1; }  //input 4 alarm
                if (sAlarmStatus2.Substring(4, 1) == "1") sAlarmStatus += "14,";  //power cut alarm
                if (sAlarmStatus2.Substring(5, 1) == "1") sAlarmStatus += "33,";  //overtime parking alarm
                if (sAlarmStatus2.Substring(6, 1) == "1") { sAlarmStatus += "32,"; overspeed = 1; } //overspeed alarm
                if (sAlarmStatus2.Substring(7, 1) == "1")
                {
                    sAlarmStatus += "31,";  //sos alarm
                    sos = 1;
                }

                if (sAlarmStatus3.Substring(0, 1) == "1") sAlarmStatus += "47,"; // idle alarm
                if (sAlarmStatus3.Substring(1, 1) == "1") sAlarmStatus += "41,";  //fatique alarm
                if (sAlarmStatus3.Substring(3, 1) == "1") sAlarmStatus += "48,";  // housing dismantled alarm
                if (sAlarmStatus3.Substring(5, 1) == "1") sAlarmStatus += "49,";  //temperature abnormal
                if (sAlarmStatus3.Substring(6, 1) == "1") sAlarmStatus += "50,";  //gsm jamming alarm 
                if (sAlarmStatus3.Substring(7, 1) == "1") sAlarmStatus += "40,";  //fuel sensor disconnect alarm 

                if (sAttachAlarmByte2.Substring(3, 1) == "1") { sAlarmStatus += "69,"; } // protection alarm
                if (sAttachAlarmByte2.Substring(4, 1) == "1") { sAlarmStatus += "68,"; } // eseal wire cut/disconnect
                //if (sAttachAlarmByte2.Substring(5, 1) == "1") { sAlarmStatus += "27,"; input7 = 1; } // input 7 alarm
                //if (sAttachAlarmByte2.Substring(6, 1) == "1") { sAlarmStatus += "26,"; input6 = 1; } // input 7 alarm
                if (sAttachAlarmByte2.Substring(7, 1) == "1") sACC = 0; else sACC = 1;

                if (sAttachAlarmByte4.Substring(0, 1) == "1") sAlarmStatus += "51,"; // crash alarm
                if (sAttachAlarmByte4.Substring(1, 1) == "1") sAlarmStatus += "52,"; // turn over
                if (sAttachAlarmByte4.Substring(2, 1) == "1") sAlarmStatus += "104,"; // harsh acceleration 
                if (sAttachAlarmByte4.Substring(3, 1) == "1") sAlarmStatus += "103,"; // harsh deceleration
                if (sAttachAlarmByte4.Substring(4, 1) == "1") sAlarmStatus += "55,"; // shake alarm
                if (sAttachAlarmByte4.Substring(5, 1) == "1") sAlarmStatus += "56,"; // re-fuel alarm
                if (sAttachAlarmByte4.Substring(6, 1) == "1") sAlarmStatus += "57,"; // steal fuel alarm
                if (sAttachAlarmByte4.Substring(7, 1) == "1") sAlarmStatus += "58,"; // low fuel alarm

                if (sAlarmStatus != "") { sAlarmStatus = sAlarmStatus.Substring(0, sAlarmStatus.Length - 1); }
                sReportStatus += sAlarmStatus;

                result = new Amwell_Data
                {
                    _gpstime = sGPSTime,
                    _datatype = sDataType,
                    _device_id = gps_sn,
                    _lon = sLon,
                    _lat = sLat,
                    _speed = sSpeed,
                    _direction = sDirection,
                    _mileage = sMileage,
                    _commandid_reply = sCommandIDReply,
                    _reportstatus = sReportStatus,
                    _acc = sACC,
                    _sos = sos,
                    _alarm_state = sAlarmStatus,
                    _rawdata = doc["data"].AsString,
                    _input1 = input1,
                    _input2 = input2,
                    _input3 = input3,
                    _input4 = input4,                  
                    _fuellevel = 0,
                    _overspeed = overspeed
                };

                SaveAmwellToCollection(result, doc);
            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("extract alarm " + ex.Message));
            }
        }
        void Extract_AmwellData(string strContent, string sDataType, string gps_sn, BsonDocument doc)
        {
            Amwell_Data result = null;
            try
            { //	ymdhms wwww jjjj ssff st lichen1 lichen2 lichen3 st1st2st3st4 v1v2v3v4v5v6v7v8
                string sPositionInfo = strContent.Substring(0, 36); //byte 0-byte 17  digit dari 0 sampai (17*2)+1, panjang 18 byte = 36 digit
                string sAttachinfo = strContent.Substring(36, 32);  // byte 18-33 panjang 16 byte = 32 digit 
                string sSensorInfo = "";
                if (strContent.Length > 68) // > 34 byte berarti ada peripheral data
                    sSensorInfo = strContent.Substring(68);
                if (strContent.Length > 68) sSensorInfo = strContent.Substring(68);
                string sGPSTime = sPositionInfo.Substring(0, 12);
                string sLat = sPositionInfo.Substring(12, 8);
                string sLon = sPositionInfo.Substring(20, 8);
                string sSpeed = sPositionInfo.Substring(28, 4);
                string sDirection = sPositionInfo.Substring(32, 4);
                string sAntennaStatus = fnc.HexToBin(sAttachinfo.Substring(0, 2)); //gps antena & power state
                double sMileage = Convert.ToInt64(sAttachinfo.Substring(2, 6), 16);
                string sStatus = sAttachinfo.Substring(8, 8);
                int gps_satelit = Convert.ToInt16(sAttachinfo.Substring(22, 2), 16);
                string sCommandIDReply = sAttachinfo.Substring(30, 2);
                if (sDataType != "85") sCommandIDReply = "";
                string sReportStatus = ""; float iMileage = 0;
                int input1 = 0, input2 = 0, input3 = 0, input4 = 0, output1 = 0, output2 = 0, overspeed = 0;
                float? temperatur1 = null, temperatur2 = null, input_analog=null;
                float main_power_voltage = 0, battery_percent = 0, fuel_level = 0;
                string sBarcode = ""; string rfid = "";
                 

                if (sDataType == "AB") sMileage = 0; //krn nilainya ngaco jika camera
                if (sAntennaStatus.Substring(0, 1) == "0") sReportStatus += "10,";  // not located
                if (sAntennaStatus.Substring(1, 2) == "10") sReportStatus += "11,";  // gps antena short circuit
                if (sAntennaStatus.Substring(1, 2) == "01") sReportStatus += "12,";  //gps antena open circuit

                if (sAntennaStatus.Substring(3, 2) == "10") sReportStatus += "14,";  // main power cut
                if (sAntennaStatus.Substring(3, 2) == "01") sReportStatus += "15,";  // main power low
                if (sAntennaStatus.Substring(3, 2) == "00") sReportStatus += "14,";  // main power off (AT09)
                // AT09 firmware baru km = 011 , per 100m = 111
                //  T360-AW101D-AW-EN-V1.0.4@ Jan  6 2016  firmware lama km = 010, 
                // 001 ?
                // D2 (bit ke 6) : 0 = mileage unit KM, 1 = mileage unit 0.1 km
                if (sAntennaStatus.Substring(5, 1) == "1") // per 100 m
                    iMileage = Convert.ToSingle(sMileage) / 10;
                else
                {
                    if (sAntennaStatus.Substring(6, 2) == "00" || sAntennaStatus.Substring(6, 2) == "01") //meter
                        iMileage = Convert.ToSingle(sMileage) / 1000;
                    else iMileage = Convert.ToSingle(sMileage);  //km
                }

                int sACC = 0, sos = 0;  //status 4 byte :St1St2St3St4 
                string sStatusByteSt1 = fnc.HexToBin(sStatus.Substring(0, 2));
                string sStatusByteSt2 = fnc.HexToBin(sStatus.Substring(2, 2));
                string sStatusByteSt3 = fnc.HexToBin(sStatus.Substring(4, 2));
                string sStatusByteSt4 = fnc.HexToBin(sStatus.Substring(6, 2));
                string sAlarmState = ""; int gsm_signal = 0;
                if (sStatusByteSt1.Substring(0, 1) == "1") sACC = 0; else sACC = 1;
                // input status di paket data location bukan alarm, jika ada perubahan status input hanya ada di data alarm sekali saja.
                if (sStatusByteSt1.Substring(1, 1) == "0") { sReportStatus += "21,"; input1 = 1; } //input 1 
                if (sStatusByteSt1.Substring(2, 1) == "0") { sReportStatus += "22,"; input2 = 1; } //input 2 alarm
                if (sStatusByteSt1.Substring(3, 1) == "0") { sReportStatus += "23,"; input3 = 1; } //input 3 alarm
                if (sStatusByteSt1.Substring(4, 1) == "0") { sReportStatus += "24,"; input4 = 1; }  //input 4 alarm
                if (sStatusByteSt1.Substring(5, 1) == "0") { sReportStatus += "28,"; } //Fuel cut alarm
                if (sStatusByteSt2.Substring(1, 1) == "0") { sReportStatus += "32,"; overspeed = 1; } //overspeed
                if (sStatusByteSt2.Substring(2, 1) == "0") sReportStatus += "33,";  //overtime parking
                if (sStatusByteSt2.Substring(3, 1) == "0") sReportStatus += "34,";  //1= no exit geofence, 0=exit geofence alarm
                if (sStatusByteSt2.Substring(4, 1) == "0") sReportStatus += "35,";  //1= no enter geofence ,0=enter geofence alarm
                //if (sStatusByteSt2.Substring(5, 1) == "0") { sReportStatus += "25,"; input5 = 1; } //input 5 alarm
                //if (sStatusByteSt2.Substring(6, 1) == "0") { sReportStatus += "26,"; input6 = 1; }//input 6 alarm
                //if (sStatusByteSt2.Substring(7, 1) == "0") { sReportStatus += "27,"; input7 = 1; }//input 7 alarm
                if (sStatusByteSt3.Substring(0, 1) == "1") { sReportStatus += "41,"; }//fatique alarm
                if (sStatusByteSt3.Substring(1, 1) == "1")
                {//need reply 0x21 ack
                }
                if (sStatusByteSt3.Substring(2, 1) == "1") sReportStatus += "49,";  //temperature abnormal
                gsm_signal = Convert.ToInt16(sStatusByteSt3.Substring(3, 5), 2);

                if (sStatusByteSt4.Substring(2, 1) == "1") { sReportStatus += "68,"; sAlarmState += "68,"; }  // lock wire cut
                if (sReportStatus != "") sReportStatus = sReportStatus.Substring(0, sReportStatus.Length - 1);
                if (sAlarmState != "") sAlarmState = sAlarmState.Substring(0, sAlarmState.Length - 1);

                if (sSensorInfo != "" && sSensorInfo.Length > 4)
                {
                    try
                    {
                        int iLen = Convert.ToInt16(sSensorInfo.Substring(0, 4), 16);
                        //string sKLV = ""; // ada firmware versi pertama AT09i bug tanpa ada Len nya
                        //if ( sSensorInfo.Substring(4).Length == (iLen*2)) sKLV = 
                        //string sKLV = sSensorInfo.Substring(4, iLen * 2);
                        string sKLV = sSensorInfo.Substring(4);
                        bool bLoop = true;
                        while (bLoop)
                        {
                            string sK = sKLV.Substring(0, 2);
                            int sL = Convert.ToInt16(sKLV.Substring(2, 2), 16);
                            string sV = sKLV.Substring(4, sL * 2);
                            switch (sK)
                            {
                                case "0D":
                                    string st = fnc.HexToBin(sV);
                                    if (st.Substring(0, 1) == "1")
                                    {
                                        st = "0" + st.Substring(1);
                                        temperatur1 = -1 * Convert.ToInt16(st, 2);
                                    }
                                    else temperatur1 = Convert.ToInt16(sV, 16);
                                    temperatur1 = Convert.ToSingle(temperatur1 * 0.1);
                                    break;
                                case "0B": //barcode
                                    sBarcode = fnc.HexNumericStringToASCII(sV);
                                    break;
                                case "10": // main power voltage
                                    main_power_voltage = Convert.ToSingle(Convert.ToInt16(sV, 16) / 10.0);
                                    break;
                                case "20":// battery power percent
                                    battery_percent = Convert.ToInt16(sV, 16);
                                    break;
                                case "0E": // dlm satuan 0-100
                                    fuel_level = Convert.ToInt16(sV, 16);
                                    break;
                                case "24":  //AD voltage , sensor load, disimpan dulu sementara field fuel_level
                                    fuel_level = Convert.ToInt16(sV, 16);
                                    break;

                                case "19": // dlm satuan 0.1%  , set fuel level dlm satuan 0.1% : *269#*Y,1
                                    fuel_level = Convert.ToSingle(Convert.ToInt16(sV, 16) / 10.0); // harus dibagi 10.0 baru bisa dpt float, kl dibagi 10 dptnya integer
                                    break;

                                case "16": // RFID N+1
                                    rfid = fnc.HexNumericStringToASCII(sV.Substring(2));
                                    break;
                                case "2F":
                                    input_analog = Convert.ToSingle(Convert.ToInt16(sV, 16) / 100.0);
                                    break;
                            }
                            if (sKLV.Length > ((sL + 2) * 2))
                            {
                                sKLV = sKLV.Substring((sL + 2) * 2);//sisanya                                   
                            }
                            else bLoop = false;
                        }
                    }
                    catch (Exception exSensor )
                    {
                        Debug.WriteLine(exSensor.Message);
                     };
                }
                result = new Amwell_Data
                {
                    _gpstime = sGPSTime,
                    _datatype = sDataType,
                    _device_id = gps_sn,
                    _lon = sLon,
                    _lat = sLat,
                    _speed = sSpeed,
                    _direction = sDirection,
                    _mileage = iMileage,
                    _commandid_reply = sCommandIDReply,
                    _reportstatus = sReportStatus,
                    _acc = sACC,
                    _sos = sos,
                    _alarm_state = sAlarmState,
                    _rawdata = doc["data"].AsString,
                    _imagebase64 = doc["image_base64"].AsString,
                    _camerano = doc["camera_no"].AsString,
                    _input1 = input1,
                    _input2 = input2,
                    _input3 = input3,
                    _input4 = input4,                   
                    _output1 = output1,
                    _output2 = output2,
                    _gsmsignal = gsm_signal,
                    _gpssatelit = gps_satelit,
                    _temperatur1 = temperatur1,
                    _temperatur2 = temperatur2,
                    _mainpowervoltage = main_power_voltage,
                    _batterypercent = battery_percent,
                    _fuellevel = fuel_level,
                    _inputanalog = input_analog,
                    _overspeed = overspeed
                };

                if (result != null)
                {                    
                    SaveAmwellToCollection(result,doc);
                }
                //this.SafeInvoke<Form1>(d => d.AddMessage(sMSISDN + " acc : " + result._acc.ToString() + " input : " + result._input1.ToString() + result._input2.ToString() + result._input3.ToString() + result._input4.ToString() + result._input5.ToString() + " output : " + result._output1.ToString() + result._output2.ToString()));

            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("Extract AmwellData err : " + ex.Message + "," + doc["data"].AsString));
            }
        }
        void SaveAmwellToCollection ( Amwell_Data data, BsonDocument doc)
        {
            try
            {
                DateTime gpstime = default(DateTime);
                float lon = 0, lat = 0, speed = 0;
                speed = Convert.ToSingle(data._speed);
                try
                {
                    gpstime = new DateTime(System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.ToFourDigitYear(Convert.ToInt16(data._gpstime.Substring(0, 2))), Convert.ToInt16(data._gpstime.Substring(2, 2)), Convert.ToInt16(data._gpstime.Substring(4, 2)), Convert.ToInt16(data._gpstime.Substring(6, 2)), Convert.ToInt16(data._gpstime.Substring(8, 2)), Convert.ToInt16(data._gpstime.Substring(10, 2)));
                }
                catch { return; }
                string tmp = fnc.HexToBin(data._lat.Substring(0, 1));
                //minute nya dibagi 60000 ( bukan 600000) karena  satuan ' nya dalam 0.001 (biasanya MM.MMMM, tapi yg amwell MM.MMM)
                if (tmp.Substring(0, 1) == "1")
                {
                    // south latitude
                    tmp = Convert.ToInt16(tmp.Substring(1), 2).ToString() + data._lat.Substring(1, 2); // lat degree
                    lat = Convert.ToSingle(Math.Round(-1 * (Convert.ToInt16(tmp) + Convert.ToSingle(data._lat.Substring(3)) / 60000), 6));
                }
                else
                {
                    tmp = Convert.ToInt16(data._lat.Substring(0, 3)).ToString(); // lat degree
                    lat = Convert.ToSingle(Math.Round((Convert.ToInt16(tmp) + Convert.ToSingle(data._lat.Substring(3)) / 60000), 6));
                }

                tmp = fnc.HexToBin(data._lon.Substring(0, 1));
                if (tmp.Substring(0, 1) == "1")
                {
                    // west longtitude
                    tmp = Convert.ToInt16(tmp.Substring(1), 2).ToString() + data._lon.Substring(1, 2); // lon degree
                    lon = Convert.ToSingle(Math.Round(-1 * (Convert.ToInt16(tmp) + Convert.ToSingle(data._lon.Substring(3)) / 60000), 6));
                }
                else
                {
                    tmp = Convert.ToInt16(data._lon.Substring(0, 3)).ToString(); // lon degree
                    lon = Convert.ToSingle(Math.Round((Convert.ToInt16(tmp) + Convert.ToSingle(data._lon.Substring(3)) / 60000), 6));
                }
                TimeSpan span1 = DateTime.Now - gpstime;
                if (  Math.Abs(span1.TotalDays) >= 30 || span1.TotalDays < -1) { return; }
                if (lon == 0 || lat == 0) { return; }
                bool bLocated = true;
                string t = "," + data._reportstatus + ",";

                if (t.Contains(",10,")) // unallocated
                {
                    bLocated = false; 
                }
                string sReportStatus = data._reportstatus;
                string sAlarmStatus = data._alarm_state;

                if (doc["gps_type"].AsInt32 == 22) // AT-10   // utk AT10, wire cut dipackage statusbyte 4, dan di alarm
                {
                    if (data._acc == 1)   // 66 = lock, 67= unlocked
                    {
                        AddNewAlarmEventStatus("66", ref sReportStatus);
                    }
                    else
                    {
                        AddNewAlarmEventStatus("67", ref sReportStatus);
                    }
                }

                logGPS log = new logGPS(); log.gps_sn = data._device_id; log.nopol = doc["nopol"].AsString;
                log.acc = data._acc; log.gps_time = gpstime.AddHours(doc["gmt"].AsInt32); log.stime = doc["_id"].AsObjectId.CreationTime.ToLocalTime();
                log.alarm_id = sAlarmStatus; log.alarm_nm = GetEventNm_Amwell(sAlarmStatus, doc["company_id"].AsInt32.ToString());
                log.report_id = sReportStatus; log.report_nm = GetEventNm_Amwell(sReportStatus, doc["company_id"].AsInt32.ToString());
                log.company_id = doc["company_id"].AsInt32; log.data_type = data._datatype; log.rfid = data._rfid;
                log.direction = data._direction; log.fuel_level = data._fuellevel; log.gps_satelit = data._gpssatelit;
                log.gsm_signal = data._gsmsignal; log.input1 = data._input1; log.input2 =data._input2; log.input3 = data._input3; log.input4 = data._input4;
                log.lat = lat; log.lon = lon; log.data_in = doc["data"].AsString;
                log.main_power_voltage = data._mainpowervoltage; log.battery_percent =data._batterypercent;
                log.odometer = data._mileage; log.output1 = data._output1; log.output2 = data._output2;
               
                log.camera_no = string.IsNullOrEmpty( data._camerano) ? "": data._camerano;
                log.photo_base64 = string.IsNullOrEmpty( data._imagebase64) ? "": data._imagebase64 ;
                log.sos = data._sos; log.speed = speed; log.hdop = (bLocated == false ? 99 : 0);
                log.temperatur1 = data._temperatur1; log.temperatur2 = data._temperatur2;
                log.upline_companyid = doc["upline_companyid"].AsInt32; log.upline_master = doc["upline_master"].AsInt32;
                log.vendor_id = doc["vendor_id"].AsInt32;
                log.cluster_processing = doc["cluster_processing"].AsInt32;
                log.input_analog = data._inputanalog;
                log.Id = ObjectId.GenerateNewId();
                logGPSFinal.Enqueue(log);
                total_perMin_Extract++;
                this.SafeInvoke<Form1>(d => d.lblLogFinal.Text = "LogDone: " + logGPSFinal.Count.ToString());
                this.SafeInvoke<Form1>(d => d.toolStripExtractPerMin.Text = "Per Min Extrc: " + total_perMin_Extract.ToString());
            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("Save amwell collection err : " + ex.Message + Environment.NewLine + doc["data"].AsString));
            }
        }
        private void AddNewAlarmEventStatus(string value, ref string sReturnValue)
        {
            sReturnValue = value + "," + sReturnValue;
            if (!sReturnValue.EndsWith(","))
                return;
            sReturnValue = sReturnValue.Substring(0, sReturnValue.Length - 1);
        }

        private  void tmrSaveCache_Tick(object sender, EventArgs e)
        {
            tmrSaveCache.Enabled = false;
            if (!bwSaveCache.IsBusy && !isBusyOnSaveCache && logGPSFinal.Count > 0) bwSaveCache.RunWorkerAsync();  
            tmrSaveCache.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (btnExtract.Text.Contains("Start"))
            {
                btnExtract.Text = "Stop Extract";
                tmrExtract.Enabled = true; isBusyExtractAmwell = false; isBusyExtractTelto = false;              
                
                ctsCancelExtract = new CancellationTokenSource();
                tokenCancelExtract = ctsCancelExtract.Token;
            }
            else
            {
                btnExtract.Text = "Start Extract";
                tmrExtract.Enabled = false; isBusyExtractAmwell = false; isBusyExtractTelto = false;
                ctsCancelExtract.Cancel();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AddMessage("isQueryMongoOnProgres: " + isQueryMongoOnProgress.ToString());
            AddMessage("timer query: " + tmrQueryMongo.Enabled.ToString());
            AddMessage("isBusyOnSaveCache : " + isBusyOnSaveCache.ToString());
            AddMessage("timer save : " + tmrSaveCache.Enabled.ToString());
            AddMessage("Log Raw Amwell : " + logGPSRawAmwell.Count.ToString());
            AddMessage("Log Raw Telto : " + logGPSRawTeltonika.Count.ToString());
            AddMessage("Log Final : " + logGPSFinal.Count.ToString());
            AddMessage("Total Thread Amwell : " + iNoThreadExtractAmwell.ToString());
            AddMessage(Environment.NewLine);
                 
        }

        private async  void bwSaveCache_DoWork(object sender, DoWorkEventArgs e)
        {
            isBusyOnSaveCache = true;
           await  Task.Run(async () =>
          {
              try
              {
                  int iCount = 0;
                  List<logGPS> lstData = new List<logGPS>();
                  logGPS item;
                  if (!isTempMongoWriteConnected) ConnectTempMongoWrite();
                  while (logGPSFinal.TryDequeue(out item))
                  {
                      iCount++;
                      lstData.Add(item);
                      if (iCount > 5000)
                      {
                          iCount = 0;
                          var col = _dbTempMongoWrite.GetCollection<logGPS>(setting.tempmongodb.gpsLogFile);
                          await col.InsertManyAsync(lstData, new InsertManyOptions() { IsOrdered = false });
                          lstData = new List<logGPS>();
                      }
                      this.SafeInvoke<Form1>(d => d.lblLogFinal.Text = "LogDone: " + logGPSFinal.Count.ToString());
                  }
                  if (lstData.Count > 0)
                  {
                      if (!isTempMongoWriteConnected) ConnectTempMongoWrite();
                      var col = _dbTempMongoWrite.GetCollection<logGPS>(setting.tempmongodb.gpsLogFile);
                      await col.InsertManyAsync(lstData, new InsertManyOptions() { IsOrdered = false });
                  }

              }
              catch (Exception ex)
              {
                  this.SafeInvoke<Form1>(d => d.AddMessage("Save queue : " + ex.Message));
              }
          });
            isBusyOnSaveCache = false;
        }

        void ExtractTelto(string strData)
        {
            try
            {

            }
            catch (Exception ex)
            {
                this.SafeInvoke<Form1>(d => d.AddMessage("Extract Telto err : " + ex.Message + Environment.NewLine + strData));
            }
            Interlocked.Decrement(ref iNoThreadExtractTelto);
        }
        async Task GetTotalGPSLog()
        { 
            try
            {
                if (!isTempMongoReadConnected) ConnectTempMongoRead();
                var totalCount = await _dbTempMongoRead.GetCollection<BsonDocument>(setting.tempmongodb.gpsRawFile).EstimatedDocumentCountAsync();
                this.SafeInvoke<Form1>(d => d.lblTotalRaw.Text = "Raw : "+  totalCount.ToString());
                
            }
            catch { }
             
        }
        private async void tmrRunningTime_Tick(object sender, EventArgs e)
        {
            await GetTotalGPSLog();
            total_perMin_query = 0;  total_perMin_Extract = 0;
            this.SafeInvoke<Form1>(d => d.toolStripRecPerMinute.Text = "Per Min Query:" + total_perMin_query.ToString());
            this.SafeInvoke<Form1>(d => d.toolStripExtractPerMin.Text = "Per Min Extrc: " + total_perMin_Extract.ToString());
        }

        public void WriteLog_Nm(string msg, string _prefix)
        {
            try
            {
                string logfilenm = "log_" + _prefix + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                using (StreamWriter writer = System.IO.File.AppendText(this.logpath + @"\" + logfilenm))
                {
                    writer.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg);
                    //writer.WriteLine( msg);
                }
            }
            catch { }
        }
        string GetEventNm_Amwell(string ilist, string company_id)
        {
            string res = "";
            string nm = "";
            try
            {
                string[] sList = ilist.Split(',');
                foreach (string str in sList)
                {
                    nm = "";
                    if (ListReportIDAmwell.TryGetValue(str + ";" + vendor_amwell + ";" + company_id, out nm)) res += nm + ",";
                    else if (ListReportIDAmwell.TryGetValue(str + ";" + vendor_amwell + ";0", out nm)) res += nm + ",";

                }
                if (res != "") res = res.Substring(0, res.Length - 1);
            }
            catch { }

            return res;
        }

    }

    public static class ExtensionMethod
    {
        // Methods
        public static void SafeInvoke<T>(this T isi, Action<T> call) where T : ISynchronizeInvoke
        {
            if (isi.InvokeRequired)
            {
                isi.BeginInvoke(call, new object[] { isi });
            }
            else
            {
                call(isi);
            }
        }

        public static TResult SafeInvoke<T, TResult>(this T isi, Func<T, TResult> call) where T : ISynchronizeInvoke
        {
            if (isi.InvokeRequired)
            {
                IAsyncResult result = isi.BeginInvoke(call, new object[] { isi });
                return (TResult)isi.EndInvoke(result);
            }
            return call(isi);
        }

        public static IEnumerable<Exception> GetInnerExceptions(this Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            var innerException = ex;
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            }
            while (innerException != null);
        }

    }
}
