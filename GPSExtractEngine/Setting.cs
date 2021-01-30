using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSExtractEngine
{
    class Setting
    {
        public SQLSERVERDB sqlserverdb = new SQLSERVERDB();
        public TempMongoDB tempmongodb = new TempMongoDB();
        public Config config = new Config();
        
    }
    class TempMongoDB
    {
        public string ServerAddr, DBName;
        public string gpsRawFile, gpsLogFile;
        public int enable; 
    }
    class SQLSERVERDB
    {
        public string SQLDBNm, SQLUserNm, SQLPwd, SQLIP;       
    }
    class Config
    {
        public int Amwell_id,Teltonika_id;
        public int maxThreadAmwell, maxThreadTelto;
    }
}
