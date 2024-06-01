using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.NetworkManager;
using Random = UnityEngine.Random;

public class RandomPositionPlayerSpawner : MonoBehaviour
{
    int m_RoundRobinIndex = 0;

    [SerializeField]
    SpawnMethod m_SpawnMethod;

    [SerializeField]
    List<Vector3> m_SpawnPositions;



    public Vector3 GetNextSpawnPosition()
    {
        switch (m_SpawnMethod)
        {
            case SpawnMethod.Random:
                var index = Random.Range(0, m_SpawnPositions.Count);
                return m_SpawnPositions[index];
            case SpawnMethod.RoundRobin:
                m_RoundRobinIndex = (m_RoundRobinIndex + 1) % m_SpawnPositions.Count;
                return m_SpawnPositions[m_RoundRobinIndex];
            default:
                throw new NotImplementedException();
        }
    }

    private void Awake()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalWithRandomSpawnPos;
    }

    void ConnectionApprovalWithRandomSpawnPos(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
    {
        // Here we are only using ConnectionApproval to set the player's spawn position. Connections are always approved.
        response.CreatePlayerObject = true;
        response.Position = GetNextSpawnPosition();
        response.Rotation = Quaternion.identity;
        response.Approved = true;
    }
}

enum SpawnMethod
{
    Random = 0,
    RoundRobin = 1,
}