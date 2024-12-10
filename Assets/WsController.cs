using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WsController : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private ClientView _clientView;
    private ServerView _serverView;
    
    private WebSocketServer _wssr;
    private WebSocket wscr;

    private readonly ConcurrentQueue<Action> _clientActions = new ();
    private readonly Queue<Action> _mainThreadActions = new ();

    [SerializeField] private Font jpFont;
    
    void Start()
    {
        _serverView = new ServerView(document);
        _clientView = new ClientView(document);
        
       _serverView.StartBtn.clicked += () =>
        {
            switch (_wssr)
            {
                case { IsListening: true }:
                    _wssr.Stop();
                    _serverView.StateLabel.text = "状態 : 停止中";
                    _serverView.StateLabel.style.color = Color.red;
                    _serverView.StartBtn.text = "サーバー起動";
                    _serverView.UrlLabel.text = "URL : ";
                    _wssr = null;
                    return;
                case null:
                    StartServer();
                    _serverView.StateLabel.text = "状態 : 起動中";
                    _serverView.StateLabel.style.color = Color.green;
                    _serverView.StartBtn.text = "サーバー停止";
                    return;
            }
        };
        _serverView.StateLabel.text = "状態 : 起動していません";
        _serverView.StateLabel.style.color = Color.red;
        _serverView.UrlLabel.text = "URL : ";
        
        _clientView.ConnectBtn.clicked += () =>
        {
            switch (wscr)
            {
                case { IsAlive: true }:
                    wscr.Close();
                    _clientView.StateLl.text = "状態 : 切断中";
                    _clientView.StateLl.style.color = Color.red;
                    _clientView.StateLl.text = "接続";
                    wscr = null;
                    return;
                case null:
                    StartClient();
                    _clientView.StateLl.text = "状態 : 接続中";
                    _clientView.StateLl.style.color = Color.green;
                    _clientView.ConnectBtn.text = "切断";
                    return;
            }
        };
        
        _clientView.StateLl.text = "状態 : 接続していません";
        _clientView.StateLl.style.color = Color.red;
        
        _clientView.MessageSendBtn.clicked += () =>
        {
            if (wscr == null || !wscr.IsAlive)
            {
                Debug.Log("接続されていません");
                return;
            }
            wscr.Send(_clientView.MessageTf.text);
        };
    }

    private void StartServer()
    {
        string url = "ws://"+NetworkUtils.GetIPFromInterface()+":"+_serverView.IpTf.value;
        _wssr =new WebSocketServer(url);
        _wssr.AddWebSocketService<MyWs>("/ws",myWs =>
        {
            myWs.OnMessageReceived = AddMessageToUI;
        }); 
        
        _serverView.UrlLabel.text = "URL : " + url;
       
        _wssr.Start();
    }

    private void StartClient()
    {
        string url = _clientView.UrlTf.value+":"+_clientView.PortTf.value+_clientView.PathTf.value ;
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
            _clientActions.Enqueue(() =>
            {
                _clientView.StateLl.text = "状態 : 接続失敗";
                _clientView.StateLl.style.color = Color.yellow;
                _clientView.ConnectBtn.text = "接続";
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
            label.style.unityFont = jpFont;
           _serverView.MessageList.Add(label);
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
        while (_clientActions.TryDequeue(out var action))
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
    
    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        Debug.Log("Close");
        OnMessageReceived?.Invoke("切断されました");
    }
}