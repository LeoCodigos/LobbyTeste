using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : NetworkBehaviour
{

    [SerializeField]
    private string m_SceneName;

    public override void OnNetworkSpawn()
    {
        if (IsHost && !string.IsNullOrEmpty(m_SceneName))
        {
            var status = NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning($"Failed to load {m_SceneName} " +
                      $"with a {nameof(SceneEventProgressStatus)}: {status}");
            }

            /*
            status = NetworkManager.SceneManager.UnloadScene();
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning($"Failed to load {m_SceneName} " +
                      $"with a {nameof(SceneEventProgressStatus)}: {status}");
            }
            */
        }
    }
}