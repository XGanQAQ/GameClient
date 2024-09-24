using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ByteArray : MonoBehaviour
{
    //默认大小
    private const int DEFAULT_SIZE = 1024;
    //初始大小
    private int iniSize = 0;
    //缓冲区
    public byte[] bytes;
    //读写位置
    public int readIdx = 0;
    public int writeIdx = 0;
    
    //容量
    private int capacity = 0;
    //剩余空间
    public int remain
    {
        get { return capacity - writeIdx; }
    }
    public int length
    {
        get { return writeIdx - readIdx; }
    }

    //构造函数
    public ByteArray(int size = DEFAULT_SIZE)
    {
        bytes = new byte[size];
        capacity = size;
        iniSize = size;
        readIdx = 0;
        writeIdx = 0;
    }
    
    //构造函数
    public ByteArray(byte[] defaultBytes)
    {
        bytes = defaultBytes;
        readIdx = 0;
        writeIdx = defaultBytes.Length;
    }

    public void ReSize(int size)
    {
        if(size<length) return;
        if(size<iniSize) return;
        int n = 1;
        while (n<size)
        {
            n *= 2;
        }

        capacity = n;
        byte[] newBytes = new byte[capacity];
        Array.Copy(bytes,readIdx,newBytes,0,writeIdx-readIdx);
        bytes = newBytes;
        writeIdx = length;
        readIdx = 0;
    }

    public void CheckAndMoveBytes()
    {
        if (length<8)
        {
            
        }
    }

    public void MoveBytes()
    {
        if (length>0)
        {
            Array.Copy(bytes,readIdx,bytes,0,length);
        }

        writeIdx = length;
        readIdx = 0;
    }

    public int Write(byte[] bs, int offset, int count)
    {
        if (remain < count)
        {
            ReSize(length+count);
        }
        Array.Copy(bs,offset,bytes,writeIdx,count);
        writeIdx += count;
        return count;
    }

    public int Read(byte[] bs, int offset, int count)
    {
        count = Math.Min(count, length);
        Array.Copy(bytes,readIdx,bs,offset,count);
        readIdx += count;
        CheckAndMoveBytes();
        return count;
    }
}
