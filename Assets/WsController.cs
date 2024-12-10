using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using WebSocketSharp.Server;
using TextField = UnityEngine.UIElements.TextField;

public class WsController : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    [SerializeField] private string myIp;

    //  サーバーのUI要素
    private TextField _serverIpTf;
    private Button _serverStartBtn;
    private Label _serverStateLabel;
    private Label _serverUrlLabel;
    private ScrollView _receiveMessageList;
    
    //  クライアントのUI要素
    private TextField _clientUrlTf;
    private TextField _clientPortTf;
    private TextField _clientPathTf;
    private Button _clientConnectBtn;
    private Label _clientStateLabel;
    private TextField _clientMessageTf;
    private Button _clientMessageSendBtn;
    private static readonly ConcurrentQueue<Action> clientActions = new ConcurrentQueue<Action>();
    
    private WebSocketServer _wssr;
    private Queue<Action> _mainThreadActions = new Queue<Action>();

    private WebSocket wscr;
    
    void Start()
    {
        FindServerElements();
        FindClientElements();
        
        _serverStartBtn.clicked += () =>
        {
            switch (_wssr)
            {
                case { IsListening: true }:
                    _wssr.Stop();
                    _serverStateLabel.text = "状態 : 停止中";
                    _serverStateLabel.style.color = Color.red;
                    _serverStartBtn.text = "サーバー起動";
                    _serverUrlLabel.text = "URL : ";
                    _wssr = null;
                    return;
                case null:
                    StartServer();
                    _serverStateLabel.text = "状態 : 起動中";
                    _serverStateLabel.style.color = Color.green;
                    _serverStartBtn.text = "サーバー停止";
                    return;
            }
        };
        _serverStateLabel.text = "状態 : 起動していません";
        _serverStateLabel.style.color = Color.red;
        _serverUrlLabel.text = "URL : ";
        
        _clientConnectBtn.clicked += () =>
        {
            switch (wscr)
            {
                case { IsAlive: true }:
                    wscr.Close();
                    _clientStateLabel.text = "状態 : 切断中";
                    _clientStateLabel.style.color = Color.red;
                    _clientConnectBtn.text = "接続";
                    wscr = null;
                    return;
                case null:
                    StartClient();
                    _clientStateLabel.text = "状態 : 接続中";
                    _clientStateLabel.style.color = Color.green;
                    _clientConnectBtn.text = "切断";
                    return;
            }
        };
        
        _clientStateLabel.text = "状態 : 接続していません";
        _clientStateLabel.style.color = Color.red;
        
        _clientMessageSendBtn.clicked += () =>
        {
            if (wscr == null || !wscr.IsAlive)
            {
                Debug.Log("接続されていません");
                return;
            }
            wscr.Send(_clientMessageTf.text);
        };
    }

    void FindServerElements()
    {
        var serverRoot = document.rootVisualElement.Q<VisualElement>("Server");
        _serverStartBtn = serverRoot
            .Q<VisualElement>("Content")
            .Q<VisualElement>("ConnectContent")
            .Q<Button>("StartBtn");
        _serverStateLabel = serverRoot
            .Q<VisualElement>("Content")
            .Q<VisualElement>("ConnectContent")
            .Q<Label>("ConnectionStateLabel");
        _serverIpTf = serverRoot
            .Q<VisualElement>("Content")
            .Q<VisualElement>("PortTextField")
            .Q<TextField>("PortTf");
        _serverUrlLabel = serverRoot
            .Q<VisualElement>("Content")
            .Q<Label>("ServerURL");
        _receiveMessageList = serverRoot
            .Q<VisualElement>("Content")
            .Q<ScrollView>("ReceiveMessageList");
    }
    
    void FindClientElements()
    {
        var clientRoot = document.rootVisualElement.Q<VisualElement>("Client");
        var clientContent = clientRoot.Q<VisualElement>("Content");
        _clientConnectBtn =clientContent 
            .Q<VisualElement>("ConnectContent")
            .Q<Button>("StartBtn");
        _clientStateLabel =clientContent 
            .Q<VisualElement>("ConnectContent")
            .Q<Label>("ConnectionStateText");
        _clientUrlTf = clientContent
            .Q<VisualElement>("URLTextField")
            .Q<TextField>("UrlTf");
        _clientPortTf =clientContent 
            .Q<VisualElement>("PortTextField")
            .Q<TextField>("PortTf");
        _clientPathTf =clientContent 
            .Q<VisualElement>("PathTextField")
            .Q<TextField>("PathTf");
        _clientMessageTf =clientContent 
            .Q<VisualElement>("MessageTextField")
            .Q<TextField>("MsgTf");
        _clientMessageSendBtn = clientContent
            .Q<Button>("MessageSendBtn");
    }

    private void StartServer()
    {
        string url = "ws://"+myIp+":"+_serverIpTf.value;
        _wssr =new WebSocketServer(url);
        _wssr.AddWebSocketService<MyWs>("/ws",myWs =>
        {
            myWs.OnMessageReceived = AddMessageToUI;
        }); 
        
        _serverUrlLabel.text = "URL : " + url;
       
        _wssr.Start();
    }

    private void StartClient()
    {
        string url = _clientUrlTf.value+":"+_clientPortTf.value+_clientPathTf.value ;
        Debug.Log(url);
        wscr = new WebSocket(url);

        wscr.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket Open");
        };

        wscr.OnMessage += (sender, e) =>
        {
            Debug.Log("WebSocket Data: " + e.Data);
        };

        wscr.OnError += (sender, e) =>
        {
            Debug.Log("WebSocket Error Message: " + e.Message);
        };

        wscr.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket Close");
            
            wscr.Close();
            wscr = null;
            clientActions.Enqueue(() =>
            {
                _clientStateLabel.text = "状態 : 接続失敗";
                _clientStateLabel.style.color = Color.yellow;
                _clientConnectBtn.text = "接続";
            });
        };

        wscr.Connect();
    }
    
    private void AddMessageToUI(string message)
    {
        // メインスレッドで実行するようにキューに追加
        _mainThreadActions.Enqueue(() =>
        {
            Label label = new Label(message);
            label.style.color = Color.white;
            _receiveMessageList.Add(label);
        });
    }

    // Update is called once per frame
    void Update()
    {
        // キューからアクションを取り出して実行
        while (_mainThreadActions.Count > 0)
        {
            var action = _mainThreadActions.Dequeue();
            action.Invoke();
        }   
        
        //  クライアントの処理をメインスレッドで行いたい
        while (clientActions.TryDequeue(out var action))
        {
            action();
        }
    }
}
public class MyWs : WebSocketBehavior
{
    public Action<string> OnMessageReceived;

    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);
        Debug.Log("ReceiveMessage : " + e.Data);
        OnMessageReceived?.Invoke(e.Data);
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        Debug.Log("Open");
        OnMessageReceived?.Invoke("接続されました");
    }
}