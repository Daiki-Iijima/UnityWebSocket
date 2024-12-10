using UnityEngine.UIElements;

public class ClientView
{
    private readonly UIDocument _document;
    
    //  クライアントのUI要素
    public TextField UrlTf { get;private set; }
    public TextField PortTf { get;private set; }
    public TextField PathTf { get;private set; }
    public Button ConnectBtn { get;private set; }
    public Label StateLl { get;private set; }
    public TextField MessageTf { get;private set; }
    public Button MessageSendBtn { get;private set; }

    public ClientView(UIDocument document)
    {
        _document = document;
        InitUIElements();
    }

    private void InitUIElements()
    {
        var clientRoot = _document.rootVisualElement.Q<VisualElement>("Client");
        var clientContent = clientRoot.Q<VisualElement>("Content");
        ConnectBtn =clientContent 
            .Q<VisualElement>("ConnectContent")
            .Q<Button>("StartBtn");
        StateLl =clientContent 
            .Q<VisualElement>("ConnectContent")
            .Q<Label>("ConnectionStateText");
        UrlTf = clientContent
            .Q<VisualElement>("URLTextField")
            .Q<TextField>("UrlTf");
        PortTf =clientContent 
            .Q<VisualElement>("PortTextField")
            .Q<TextField>("PortTf");
        PathTf =clientContent 
            .Q<VisualElement>("PathTextField")
            .Q<TextField>("PathTf");
        MessageTf =clientContent 
            .Q<VisualElement>("MessageTextField")
            .Q<TextField>("MsgTf");
        MessageSendBtn = clientContent
            .Q<Button>("MessageSendBtn");
    }
}
