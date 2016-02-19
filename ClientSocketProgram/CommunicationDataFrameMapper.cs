using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class CommunicationDataFrameMapper : IMapper<byte[], CommunicationDataFrame>
    {
        public CommunicationDataFrame Map(byte[] data)
        {
            CommunicationDataFrame model = new CommunicationDataFrame();

            model.LifeBit = data[0].GetBit(0);

            model.Insert = data[0].GetBit(1);
            model.Delete = data[0].GetBit(2);
            //model.NotFound = data[0].GetBit(3);

            model.Prefix = GetString(data, 2, 4);
            model.LBHD = GetString(data, 6, 12);
            model.Broadcast = GetString(data, 18, 60);
            model.Sequence = GetString(data, 78, 10);
            model.PLPID = GetString(data, 88, 2);
            model.Operation = GetString(data, 90, 2);

            return model;
        }

        public byte[] InverseMap(CommunicationDataFrame model)
        {
            byte[] outStream = new byte[CommunicationDataFrame.NumberOfBytes];

            outStream[0] = outStream[0].SetBit(0, model.LifeBit);

            outStream[0] = outStream[0].SetBit(1, model.Insert);
            outStream[0] = outStream[0].SetBit(2, model.Delete);

            outStream[0] = outStream[0].SetBit(3, model.NotFound);
            outStream[0] = outStream[0].SetBit(4, model.SOAP);
            outStream[0] = outStream[0].SetBit(5, model.NotInsert);
            outStream[0] = outStream[0].SetBit(6, model.NotDelete);
            outStream[0] = outStream[0].SetBit(7, model.UnknownError);


            GetBytes(model.Prefix, 4, ref outStream, 2);
            GetBytes(model.LBHD, 12, ref outStream, 6);
            GetBytes(model.Broadcast, 60, ref outStream, 18);
            GetBytes(model.Sequence, 10, ref outStream, 78);
            GetBytes(model.PLPID, 2, ref outStream, 88);
            GetBytes(model.Operation, 2, ref outStream, 90);

            return outStream;
        }

        private string GetString(byte[] input, int startindex, int num_bytes)
        {
            return ASCIIEncoding.ASCII.GetString(input, startindex, num_bytes).Replace("\0", " ");
        }

        private void GetBytes(string txt, int txtLength, ref byte[] stream, int startIndex)
        {
            if (txtLength > txt.Length)
                txtLength = txt.Length;

            var a = ASCIIEncoding.ASCII.GetBytes(txt, 0, txtLength, stream, startIndex);
        }

    }

}
