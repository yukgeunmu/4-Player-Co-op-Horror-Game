using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Network
{
    public class NetworkBootstrap : MonoBehaviour
    {
        private void OnGUI()
        {
            if (NetworkManager.Singleton == null)
                return;

            if (!NetworkManager.Singleton.IsClient &&
                !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Host", GUILayout.Width(200), GUILayout.Height(50)))
                {
                    StartHost();
                }

                if (GUILayout.Button("Client", GUILayout.Width(200), GUILayout.Height(50)))
                {
                    StartClient();
                }
            }
            else
            {
                GUILayout.Label(
                    $"Mode : {(NetworkManager.Singleton.IsHost ? "Host" : "Client")}"
                );
            }
        }

        private void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        private void StartClient()
        {
            NetworkManager.Singleton.StartClient();
        }
    }
}