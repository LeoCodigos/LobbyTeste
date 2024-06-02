using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class LobbyRelayConnection : MonoBehaviour
{
    private Lobby hostLobby;
    public string playerName;
    public string relayLobbyCode;
    [SerializeField] private float temporizadorAtivacaoLobby;

    public static LobbyRelayConnection instance;

    #region LobbyOperations

    // Mantem o Lobby atual sempre ativo --> evita o timeout de 3 segundos
    // A cada 15 segundos, manda um ping para o server da Unity
    private async void ManterLobbyAtivo()
    {
        if(hostLobby != null)
        {
            temporizadorAtivacaoLobby -= Time.deltaTime;

            if(temporizadorAtivacaoLobby < 0f)
            {
                temporizadorAtivacaoLobby = 15f;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);

            }

        }
    }

    public async void CriaLobby(string lobbyName, int maxPlayers, bool isPrivate)
    {
        try
        {
            //Define o nome do jogador no lobby
            string playerNameLobby = UIManager.instance.PlayerName;

            //Cria as opções do lobby
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", 
                            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerNameLobby)},
                        
                    }
                }
            };

            // Cria o Lobby
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            hostLobby = lobby;

            //Cria a alocação de Relay ==> código para conexão pela internet
            //CriaRelay(maxPlayers);
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

                relayLobbyCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                Debug.Log($"Criei uma nova alocação de relay com o código: {relayLobbyCode}");

            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }

            //Atualiza o Lobby para colocar o lobby code no dicionario
            Lobby newLobby = await LobbyService.Instance.UpdateLobbyAsync(
                lobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {"LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, lobby.LobbyCode)},
                        {"RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayLobbyCode)},
                    }
                });

            hostLobby = newLobby;

            Debug.Log($"O lobby {hostLobby.Name} foi criado por {playerNameLobby}\t" +
            $"Número de players: {hostLobby.MaxPlayers}" +
            $"\tID: {hostLobby.Id}" +
            $"\tToken do Lobby: {hostLobby.LobbyCode}\tPrivate? {hostLobby.IsPrivate}"+
            $"\tToken do Relay: {hostLobby.Data["RelayCode"].Value}");

        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e.Message);
        }        
    }

    public async void ListaLobbies(QueryLobbiesOptions queryLobbiesOptions = null)
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

    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            string playerName = UIHandler.instance.mainPanelPlayerNameInput.text;
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName)}
                    }
                }
            };

            Debug.Log($"Entrando o lobby {lobbyCode}");
            Lobby joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, options);

            //Acessa o metodo de visualizar as informações dos players
            MostraInformacoesPlayers(joinedLobby);
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

    #region RelayOperations

    public async void CriaRelay(int numberOfConnections)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(numberOfConnections);

            relayLobbyCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Criei uma nova alocação de relay com o código: {relayLobbyCode}");
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e.Message);
        }
    }

    public async void JoinRelayAs(string connectionType, string connectionRelayCode)
    {
        // Entrando com a variável da classe
        //var joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayLobbyCode);

        // Entrando com a variavel do parametro
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(connectionRelayCode);

        // Tipo da Conexão: DTLS --> Conexão Segura
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
    #endregion
    
    #region EventFunctions
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }        
    }

    async void Start()
    {
        //Coloca um nome inicial ao nome do jogador
        playerName = "Player1";
        UIManager.instance.PlayerName = playerName;

        //Cria coisas na Unity
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(this.playerName);

        // Dispara uma rotina de inicialização da API
        await UnityServices.InitializeAsync(initializationOptions);

        // Escuta pelo evento de login do jogador
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Conectado como: " + AuthenticationService.Instance.PlayerId);
        };

        // Dispara uma rotina de login do usuario (de forma anonima)
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    
    void Update()
    {
        ManterLobbyAtivo();
    }

    #endregion
}
