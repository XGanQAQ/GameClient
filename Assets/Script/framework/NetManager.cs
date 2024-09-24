using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Cryptography;


public class NetManager : MonoBehaviour
{
    public enum NetEvent
    {
        ConnectSucc = 1,
        ConnectFail = 2,
        Close = 3,
    }
    
    private static Socket socket;
    //接收缓存区
    private static ByteArray readBuff;
    //写入队列
    private static Queue<ByteArray> writeQueue;

    private static bool isClosing = false;

    //初始化状态
    public static void InitState()
    {
        InitNetEventState();
        InitMsgListState();
        InitHeartState();
    }
    
    #region 网络事件

    //事件委托类型
    public delegate void EventListener(string err);
    //事件监听列表
    private static Dictionary<NetEvent, EventListener> 
        eventListeners = new Dictionary<NetEvent,EventListener>();
    
    //添加事件监听
    public static void AddEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent] += listener;
        }
        else
        {
            eventListeners[netEvent] = listener;
        }
    }
    
    //删除事件监听
    public static void RemoveEventListenner(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent] -= listener;
        }
        else
        {
            eventListeners.Remove(netEvent);
        }
    }
    
    //分发（触发）事件
    public static void FireEvent(NetEvent netEvent, string err)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent](err);
        }
        else
        {
            throw new Exception("不存在此事件");
        }
    }
    
    #endregion
    
    #region Connect

    private static bool isConnecting = false;

    public static void Connect(string ip, int port)
    {
        //状态判断
        if (socket != null && socket.Connected)
        {
            Debug.Log("Connect fail,is already connected");
        }

        if (isConnecting)
        {
            Debug.Log("Connect fail is connecting");
        }
        InitState();
        socket.NoDelay = true;
        socket.BeginConnect(ip, port,ConnectCallback, socket);

    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;
            socket.EndConnect(ar);
            Debug.Log("Socket Connect Succ ");
            FireEvent(NetEvent.ConnectSucc,"");
            isConnecting = false;
        }
        catch (SocketException ex)
        {
            Debug.Log("Socket connect fail" + ex.ToString());
            FireEvent(NetEvent.ConnectFail,ex.ToString());
            isConnecting = false;
        }
    }
    
    //初始化网络状态
    private static void InitNetEventState()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        readBuff = new ByteArray();
        writeQueue = new Queue<ByteArray>();
        isConnecting = false;
        isClosing = false;
    }
    

    #endregion

    #region Close

    public static void Close()
    {
        //状态判断
        if (socket == null || !socket.Connected)
        {
            return;
        }

        if (isConnecting)
        {
            return;
        }
        
        //还有数据在发送
        if (writeQueue.Count > 0)
        {
            isClosing = true;
        }
        else
        {
            socket.Close();
            FireEvent(NetEvent.Close,"");
        }
    }

    #endregion

    #region Send
    
    public static void Send(MsgBase msg)
    {
        if (socket == null || !socket.Connected)
        {
            return;
        }

        if (isConnecting)
        {
            return;
        }

        if (isClosing)
        {
            return;
        }
        //数据编码
        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[2 + len];
        //组装长度
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        //组装名字
        Array.Copy(nameBytes,0,sendBytes,2,nameBytes.Length);
        //组装消息体
        Array.Copy(bodyBytes,0,sendBytes,2+nameBytes.Length,bodyBytes.Length);
        //写入队列
        ByteArray ba = new ByteArray(sendBytes);
        int count = 0;
        lock (writeQueue)
        {
            writeQueue.Enqueue(ba);
            count = writeQueue.Count;
        }
        
        //send
        if (count == 1)
        {
            socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, socket);
        }

    }

    public static void SendCallback(IAsyncResult ar)
    {
        //获取state EndSend处理
        Socket socket = ar.AsyncState as Socket;
        //状态判断
        if (socket == null || !socket.Connected)
        {
            return;
        }
        //EndSend
        int count = socket.EndSend(ar);
        //获得写入队列第一条数据
        ByteArray ba;
        lock (writeQueue)
        {
            ba = writeQueue.First();
        }
        //完整发送
        ba.readIdx += count;
        if (ba.length == 0)
        {
            lock (writeQueue)
            {
                writeQueue.Dequeue();
                ba = writeQueue.First();
            }
        }
        
        //继续发送
        if (ba != null)
        {
            socket.BeginSend(ba.bytes, ba.readIdx, ba.length, 0, SendCallback, socket);
            
        }
        
        //正在关闭
        else if(isClosing)
        {
             socket.Close(9);
        }
    }
    
    #endregion


    #region 消息事件
    //消息委托类型
    public delegate void MsgListener(MsgBase msgBase);
    //消息监听列表
    public static Dictionary<string, MsgListener> MsgListeners = new Dictionary<string, MsgListener>();

    public static void AddMsgListener(string msgName, MsgListener msgListener)
    {
        //添加
        if (MsgListeners.ContainsKey(msgName))
        {
            MsgListeners[msgName] += msgListener;
        }
        //新增
        else
        {
            MsgListeners[msgName] = msgListener;
        }
    }

    public static void RemoveMsgListener(string msgName, MsgListener msgListener)
    {
        if (MsgListeners.ContainsKey(msgName))
        {
            MsgListeners[msgName] -= msgListener;
            //删除
            if (MsgListeners[msgName] == null)
            {
                MsgListeners.Remove(msgName);
            }
        }
    }

    public static void FireMsg(string msgName, MsgBase msgBase)
    {
        if (MsgListeners.ContainsKey(msgName))
        {
            MsgListeners[msgName](msgBase);
        }
    }
    #endregion

    #region 接收数据

    //消息列表
    private static List<MsgBase> msgList = new List<MsgBase>();
    //消息列表长度
    private static int msgCount = 0;
    //每一次update处理的消息量
    private readonly static int MAX_MESSAGE_FIRE = 10;
    
    //初始化状态
    private static void InitMsgListState()
    {
        msgList = new List<MsgBase>();
        msgCount = 0;
    }
    
    //Receive回调 在Connet中调用
    public static void ReceiveCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = ar.AsyncState as Socket;
            //获取接收数据长度
            int count = socket.EndReceive(ar);
            if (count == 0)
            {
                Close();
                return;
            }

            readBuff.writeIdx += count;
            //处理二进制消息
            OnReceiveData();
            //继续接收数据
            if (readBuff.remain < 8)
            {
                readBuff.MoveBytes();
                readBuff.ReSize(readBuff.length*2);
            }

            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallBack, socket);
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.ToString());
        }
    }
    
    //数据处理
    public static void OnReceiveData()
    {
        //消息长度
        if (readBuff.length <= 2)
        {
            return;
        }
        //获取消息体长度
        int readIdx = readBuff.readIdx;
        byte[] bytes = readBuff.bytes;
        Int16 bodyLength = (Int16)((bytes[readIdx + 1] << 8) | bytes[readIdx]);
        if (readBuff.length < bodyLength + 2)
        {
            return;
        }

        readBuff.readIdx += 2;
        //解析协议名
        int nameCount = 0;
        string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIdx, out nameCount);
        if (protoName == "")
        {
            Debug.Log("Fail Decode protoName");
            return;
        }

        readBuff.readIdx += nameCount;
        //解析协议体
        int bodyCount = bodyLength - nameCount;
        MsgBase msgBase = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIdx, bodyCount);
        readBuff.readIdx += bodyCount;
        readBuff.CheckAndMoveBytes();
        //添加到消息队列
        lock (msgList)
        {
            msgList.Add(msgBase);
        }

        msgCount++;
        //继续读取消息
        if (readBuff.length > 2)
        {
            OnReceiveData();
        }
    }
    #endregion

    #region Update

    public static void Update()
    {
        MsgUpdate();
        PingUpdate();
    }

    public static void MsgUpdate()
    {
        if (msgCount==0)
        {
            return;
        }
        //重复处理消息
        for (int i = 0; i < MAX_MESSAGE_FIRE; i++)
        {
            MsgBase msgBase = null;
            lock (msgBase)
            {
                if (msgList.Count>0)
                {
                    msgBase = msgList[0];
                    msgList.RemoveAt(0);
                    msgCount--;
                }
            }
            //分发消息
            if (msgBase != null)
            {
                FireMsg(msgBase.protoName,msgBase);
            }
            //没有消息了
            else
            {
                break;
            }
        }
    }

    #endregion

    #region 心跳机制

    //是否启用心跳
    public static bool isUsePing = true;
    //心跳间隔时间
    public static int PingInterval = 30;
    //上一次发送Ping的时间
    static float lastPingTime = 0;
    //上一次收到Pone的时间
    static float lastPongTime = 0;
    
    //初始状态
    private static void InitHeartState()
    {
        lastPingTime = Time.time;
        lastPongTime = Time.time;
        //监听Pong协议
        if (!MsgListeners.ContainsKey("MsgPone"))
        {
            AddMsgListener("MsgPone",OnMsgPone);
        }
    }
    
    //发送Ping协议
    private static void PingUpdate()
    {
        //是否启用
        if(!isUsePing) return;
        if (Time.time - lastPingTime > PingInterval)
        {
            SysMsg.MsgPing msgPing = new SysMsg.MsgPing();
            NetManager.Send(msgPing);
            lastPingTime = Time.time;
        }

        if (Time.time - lastPongTime > 4*PingInterval)
        {
            Close();
        }
    }
    
    //监听Pong协议
    private static void OnMsgPone(MsgBase msgBase)
    {
        lastPongTime = Time.time;
    }
    #endregion
}
