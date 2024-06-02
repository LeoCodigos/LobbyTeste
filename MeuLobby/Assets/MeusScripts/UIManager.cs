using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager:MonoBehaviour{

#region Atritubtes

    [SerializeField] private TMP_InputField playerNameIF;
    [SerializeField] private TMP_InputField lobbyNameIF;
    [SerializeField] private TMP_InputField lobbyCodeIF;
    public static UIManager instance;  

#endregion

#region Get/Set

public string PlayerName{
    get => playerNameIF.text;
    set{
        playerNameIF.text = value;
        
    }
}
public string LobbyName{
    get => lobbyNameIF.text;
    set{
        lobbyNameIF.text = value;
    }
}

public string LobbyCode{
    get => lobbyCodeIF.text;
    set{
        lobbyCodeIF.text = value;
    }
}

#endregion

#region Methods
    public void UpdateName(){
        TestConnect.instance.PlayerName = PlayerName;
    }

    public void CreateLobbyPublicButton(){
        
        TestConnect.instance.CreateLobby(LobbyName, 8, false);
    }

    public void CreateLobbyPrivateButton(){

        TestConnect.instance.CreateLobby(LobbyName, 8, true);
    }
    
#endregion

    private void Awake(){
        instance = this;
    }
     private void Update()
    {
              
    }
}