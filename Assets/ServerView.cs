using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ServerView
{
    private readonly UIDocument _document;
    
    public TextField IpTf {get; private set; }
    public Button StartBtn {get; private set; }
    public Label StateLabel {get; private set; }
    public Label UrlLabel {get; private set; }
    public ScrollView MessageList {get; private set; }
    
    public ServerView(UIDocument document)
    {
        _document = document;
         InitUIElements();
    }
    
    private void InitUIElements()
    {
        var serverRoot = _document.rootVisualElement.Q<VisualElement>("Server");
        StartBtn = serverRoot
            .Q<VisualElement>("Content")
            .Q<VisualElement>("ConnectContent")
            .Q<Button>("StartBtn");
        StateLabel = serverRoot
            .Q<VisualElement>("Content")
            .Q<VisualElement>("ConnectContent")
            .Q<Label>("ConnectionStateLabel");
        IpTf = serverRoot
            .Q<VisualElement>("Content")
            .Q<VisualElement>("PortTextField")
            .Q<TextField>("PortTf");
        UrlLabel = serverRoot
            .Q<VisualElement>("Content")
            .Q<Label>("ServerURL");
        MessageList = serverRoot
            .Q<VisualElement>("Content")
            .Q<ScrollView>("ReceiveMessageList");
    }
    
}
