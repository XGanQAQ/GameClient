using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;
public class MsgBase
{
    //协议名
    public string protoName = " ";
    //编码 对象->字符->bytes
    public static byte[] Encode(MsgBase msgBase)
    {
        string s = JsonUtility.ToJson(msgBase);
        return Encoding.UTF8.GetBytes(s); //以UTF8的格式(transform to 8 bit)转化为比特格式
    }
    
    //解码
    public static MsgBase Decode(string protoName, byte[] bytes, int offset, int count)
    {
        string s = Encoding.UTF8.GetString(bytes,offset,count);
        MsgBase msgBase = (MsgBase)JsonUtility.FromJson(s, Type.GetType(protoName));
        return msgBase;
    }

    //bytes->字符->对象
    public static byte[] EncodeName(MsgBase msgBase)
    {
        //名字与bytes长度
        byte[] nameBytes = Encoding.UTF8.GetBytes(msgBase.protoName);
        Int16 len = (Int16)nameBytes.Length;
        //申请bytes数值
        byte[] bytes = new byte[2 + len];
        //组装2字节长度信息
        bytes[0] = (byte)(len % 256); 
        bytes[1] = (byte)(len / 256);
        //组装名字bytes
        Array.Copy(nameBytes,0,bytes,2,len);
        return bytes;
    }

    public static string DecodeName(byte[] bytes, int offset, out int count)
    {
        count = 0;
        //必须大于2字节
        if (offset + 2 > bytes.Length)
        {
            return "";
        }
        
        //读取长度
        Int16 len = (Int16)((bytes[offset + 1] << 8) | bytes[offset]);
        if (len <= 0) return "";
        
        //长度必须足够
        if (offset + 2 + len > bytes.Length) return " ";
        
        //解析
        count = 2 + len;
        string name = Encoding.UTF8.GetString(bytes, offset + 2, len);
        return name;

    }
}
