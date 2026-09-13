using Unity.Netcode;
using UnityEngine;

namespace NKY.Scripts
{
    public class NetworkDebugUI : MonoBehaviour
    {
        private void OnGUI()
        {
            if(NetworkManager.Singleton == null) return;
            // 화면 좌측 상단에 디버그용 버튼 영역 배치
            GUILayout.BeginArea(new Rect(10, 10, 200, 200));

            // 아직 서버/클라이언트로 접속하지 않은 상태일 때만 버튼 표시
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Start Host (방 만들기)", GUILayout.Height(40)))
                {
                    NetworkManager.Singleton.StartHost();
                }

                if (GUILayout.Button("Start Client (참가하기)", GUILayout.Height(40)))
                {
                    NetworkManager.Singleton.StartClient();
                }
                if (GUILayout.Button("Shutdown"))
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
            else
            {
                // 접속 후 현재 상태 표시
                string mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";
                GUILayout.Label($"현재 접속 상태: {mode}");
            }

            GUILayout.EndArea();
        }
        
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
    }
}