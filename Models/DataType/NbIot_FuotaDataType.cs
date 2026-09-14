using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolSimulator.Models.DataType
{
    public enum NbIot_FuotaDataType
    {
        Response_AckForSetup,
        Response_AckForSegment,
        Response_AckForPeriodicMessage,
        Response_Nack
    }
}
