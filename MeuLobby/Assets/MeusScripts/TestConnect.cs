using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;

public class TestConnect : MonoBehaviour
{
    #region Atributes
    [SerializeField] private Lobby hostlobby;
    [SerializeField] private string relayLobbyCode;
    [SerializeField] private string playerName;
    [SerializeField] private float timerLobby;

    public static TestConnect instance;

    public string PlayerName{
        set{
            playerName=value;
        }
    }


    #endregion

    #region LobbyFunctions

    private async void ManterLobbyAtivo(){
        if(hostlobby != null)
        {
            timerLobby -= Time.deltaTime;

            if(timerLobby < 0.0f){
                timerLobby = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostlobby.Id);
            }
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate){

        try{
            string player= UIManager.instance.PlayerName;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions{
                IsPrivate = isPrivate,
                Player = new Player{
                    Data= new Dictionary<string, PlayerDataObject>{
                        {"PlayerName",
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public,playerName)},    
                    }
                },
            };

            //Cria o Lobby

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostlobby= lobby;

            try{
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                relayLobbyCode= await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"Criei uma nova alocacao de relay com o codigo:{relayLobbyCode}");

            }

            catch(RelayServiceException e){ Debug.Log(e);}


            //Atualiza o Lobby para colocar o lobby code no dicionario
            Lobby newLobby = await LobbyService.Instance.UpdateLobbyAsync(
                lobby.Id,
                new UpdateLobbyOptions{
                    Data = new Dictionary<string, DataObject> {
                        {"LobbyCode", new DataObject(DataObject.VisibilityOptions.Public,lobby.LobbyCode)},
                        {"RelayCode", new DataObject(DataObject.VisibilityOptions.Public,relayLobbyCode)},

                    }
                }
                );

            hostlobby= newLobby;

            Debug.Log($"O lobby {hostlobby.Name} foi criado por {playerName}\t" +
                $"N�mero de players: {hostlobby.MaxPlayers}" +
                $"\tID: {hostlobby.Id}" +
                $"\tToken do Lobby: {hostlobby.LobbyCode}\tPrivate? {hostlobby.IsPrivate}"+
                $"\tToken do Relay: {hostlobby.Data["RelayCode"].Value}"
                );
        }catch(LobbyServiceException e){Debug.Log(e.Message);}

    }

    public async void CriaRelay(int numberOfConnections){
        try{
            Allocation allocation= await RelayService.Instance.CreateAllocationAsync(numberOfConnections);
            Debug.Log($"Criei uma nova alocacao de relay:{relayLobbyCode}");
        }
        catch(RelayServiceException e){
            Debug.Log(e);
        }
    }

    public async void JoinRelayAs(string connectionType, string connectionRelayCode)
    {
        // Entrando com a variavel da classe
        //var joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayLobbyCode);

        // Entrando com a variavel do parametro
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(connectionRelayCode);

        // Tipo da Conexao: DTLS --> Conexao Segura
        //RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
       // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        if (connectionType == "host")
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                ipAddress:joinAllocation.RelayServer.IpV4,
                port:(ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData
                );
            
            NetworkManager.Singleton.StartHost();
        }
        else if (connectionType == "server")
        {
           // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            NetworkManager.Singleton.StartServer();
        }
        else
        {
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                ipAddress: joinAllocation.RelayServer.IpV4,
                port: (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );
            
            NetworkManager.Singleton.StartClient();
        }
        
    }

     public async void ListaLobbies(QueryLobbiesOptions queryLobbiesOptions=null)
    {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Debug.Log($"Encontrei {queryResponse.Results.Count} lobbie(s):");
            int i = 1;
            foreach(Lobby lobby in queryResponse.Results)
            {
                Debug.Log($"\tLobby[{i}]: {lobby.Name}" +
                    $"\tLobby Code: {lobby.Data["LobbyCode"].Value}" +
                    $"\tRelay Code: {lobby.Data["RelayCode"].Value}");
                MostraInformacoesPlayers(lobby);
                i++;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e.Message);
        }

    }

    private void MostraInformacoesPlayers(Lobby lobby)
    {
        Debug.Log($"Mostrando informacoes dos jogadores do lobby {lobby.Name}");
        foreach(Player player in lobby.Players)
        {
             Debug.Log($"\tNome: {player.Data["PlayerName"].Value}\tID: {player.Id}");
            // Debug.Log($"\tID: {player.Id}");
        }
    
    }
    #endregion

    #region EventFunctions
    private void Awake(){
        instance=this;
    }

    // Start is called before the first frame update
    private async void Start()
    {
        this.playerName = "SpyMaster"+ Random.Range(1,501);
        UIManager.instance.PlayerName = this.playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(this.playerName);

        //Dispara um rotina da API
        await UnityServices.InitializeAsync(initializationOptions);

        //Escuta o evento de login
        AuthenticationService.Instance.SignedIn+=()=>{
            Debug.Log("Conectado como:"+ AuthenticationService.Instance.PlayerId);
        };

        //Dispara um login de usuario anonimo
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }


    // Update is called once per frame
    void Update()
    {
        ManterLobbyAtivo();
    }

    #endregion
}
