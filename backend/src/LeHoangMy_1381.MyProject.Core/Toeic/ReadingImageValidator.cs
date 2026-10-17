using System;
using System.Buffers.Binary;
using System.Linq;

namespace LeHoangMy_1381.MyProject.Toeic;

public static class ReadingImageValidator
{
    public static string Mime(byte[] data)
    {
        if(data==null || data.Length<12 || data.Length>10*1024*1024)return null;
        if(data[0]==255 && data[1]==216)return ValidJpeg(data) ? "image/jpeg" : null;
        if(!data.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))return null;
        var position=8;var header=false;var pixels=false;
        while(position<=data.Length-12)
        {
            var length=BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position,4));
            if(length>data.Length-position-12)return null;
            var count=(int)length;var type=System.Text.Encoding.ASCII.GetString(data,position+4,4);
            if(!header)
            {
                if(type!="IHDR" || count!=13)return null;
                var width=BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position+8,4));var height=BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position+12,4));
                if(width==0 || height==0 || width>20000 || height>20000 || (ulong)width*height>80_000_000)return null;
                header=true;
            }
            uint crc=0xffffffff;
            for(var i=position+4;i<position+8+count;i++)
            {
                crc^=data[i];for(var bit=0;bit<8;bit++)crc=(crc&1)!=0 ? (crc>>1)^0xedb88320 : crc>>1;
            }
            crc^=0xffffffff;
            if(crc!=BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position+8+count,4)))return null;
            position+=count+12;
            if(type=="IDAT")pixels=true;
            if(type=="IEND")return count==0 && position==data.Length && pixels ? "image/png" : null;
        }
        return null;
    }

    private static bool ValidJpeg(byte[] data)
    {
        var position=2;var frame=false;var scan=false;
        while(position<data.Length)
        {
            if(data[position++]!=255)return false;
            while(position<data.Length && data[position]==255)position++;
            if(position>=data.Length)return false;
            var marker=data[position++];
            if(marker==217)return frame && scan && position==data.Length;
            if(marker is 0 or 216 || marker is >=208 and <=215 || position>data.Length-2)return false;
            var length=BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position,2));
            if(length<2 || length>data.Length-position)return false;
            var isFrame=marker is >=192 and <=207 && marker is not (196 or 200 or 204);
            if(isFrame)
            {
                if(length<8)return false;
                var height=BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position+3,2));
                var width=BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position+5,2));
                var components=data[position+7];
                if(width==0 || height==0 || width>20000 || height>20000 || (ulong)width*height>80_000_000 ||
                   components is <1 or >4 || length!=8+3*components)return false;
                frame=true;
            }
            if(marker!=218){position+=length;continue;}
            if(!frame || length<6)return false;
            var scanComponents=data[position+2];
            if(scanComponents is <1 or >4 || length!=6+2*scanComponents)return false;
            position+=length;var entropy=false;
            while(position<data.Length-1)
            {
                if(data[position]!=255){entropy=true;position++;continue;}
                var next=data[position+1];
                if(next==0 || next is >=208 and <=215){entropy=true;position+=2;continue;}
                break;
            }
            if(!entropy)return false;
            scan=true;
        }
        return false;
    }
}
