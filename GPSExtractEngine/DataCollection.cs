using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSExtractEngine
{
    class Amwell_Data //:IDisposable
    {
        public string _device_id { get; set; }
        public string _datatype { get; set; }
        public string _gpstime { get; set; }
        public string _lat { get; set; }
        public string _lon { get; set; }
        public string _speed { get; set; }
        public string _direction { get; set; }
        public string _reportstatus { get; set; }
        public int _acc { get; set; }
        public int _sos { get; set; }
        public string _alarm_state { get; set; }
        public float _mileage { get; set; }
        public string _commandid_reply { get; set; }
        public string _rawdata { get; set; }
        public string _camerano { get; set; }
        public string _imagebase64 { get; set; }
        public int _input1 { get; set; }
        public int _input2 { get; set; }
        public int _input3 { get; set; }
        public int _input4 { get; set; }
      
        public int _output1 { get; set; }
        public int _output2 { get; set; }

        public int _gsmsignal { get; set; }
        public int _gpssatelit { get; set; }
        public float? _temperatur1 { get; set; }
        public float? _temperatur2 { get; set; }
        public float? _inputanalog { get; set; }
        public float _mainpowervoltage { get; set; }
        public float _batterypercent { get; set; }
        public float _fuellevel { get; set; }
        public int _overspeed { get; set; }
        public string _rfid { get; set; } 
    }
}
