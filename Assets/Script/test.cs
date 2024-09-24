using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
   
    void Start()
    {
        NetManager.AddEventListener(NetManager.NetEvent.ConnectSucc,OnconnectSucc);
        NetManager.AddEventListener(NetManager.NetEvent.ConnectFail,OnConnectFail);
        NetManager.AddEventListener(NetManager.NetEvent.Close,OnConnectClose);
        //消息事件test
        NetManager.AddMsgListener("MsgMove",OnMsgMove);
        BattleMsg.MsgMove msgMove = new BattleMsg.MsgMove();
        msgMove.x = 222;
        NetManager.FireMsg("MsgMove",msgMove);
    }

    private void Update()
    {
        NetManager.Update();
    }

    //收到MsgMove协议
    public void OnMsgMove(MsgBase msgBase)
    {
        BattleMsg.MsgMove msg = msgBase as BattleMsg.MsgMove;
        Debug.Log("OnMsgMove msg.x = " + msg.x);
    }
    public void OnConnectClick()
    {
        NetManager.Connect("127.0.0.1",8888);    
    }

    public void OnCloseClick()
    {
        NetManager.Close();
    }

    void OnconnectSucc(string err)
    {
        Debug.Log("OnccnnectSucc");
        //TODO
    }

    void OnConnectFail(string err)
    {
        Debug.Log("OnconnectFail" + err);
    }

    void OnConnectClose(string err)
    {
        Debug.Log("OnConnectClose");
    }
    // Update is called once per frame

    public void OnMoveClick()
    {
        BattleMsg.MsgMove msgMove = new BattleMsg.MsgMove();
        msgMove.x = 120;
        msgMove.y = 100;
        msgMove.z = -2;
        NetManager.Send(msgMove);
        Debug.Log("SendMsgMove");
    }
}
