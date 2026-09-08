using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace RYU._01.Script.Auth
{
    public class AuthBase : MonoBehaviour
    {
        public virtual async void Awake()
        {
            UnityServices.Initialized += () => Debug.Log("UGS 초기화 완료");
            UnityServices.InitializeFailed += ctx => Debug.Log("UGS 초기화 실패" + ctx);
            
            await UnityServices.InitializeAsync();

            BindingUGSEvents();
        }

        public virtual void BindingUGSEvents()
        {
            AuthenticationService.Instance.SignedIn += async () =>
            {
                var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();

                Debug.Log("로그인 성공");
                Debug.Log("플레이어 id" + AuthenticationService.Instance.PlayerId);
                Debug.Log("플레이어 Nmae" + playerName);
                Debug.Log("Player AccessToken" + AuthenticationService.Instance.AccessToken);
            };
            
            AuthenticationService.Instance.SignedOut += () => Debug.Log("로그아웃");
            AuthenticationService.Instance.Expired += () => Debug.Log("세션 만료");
        }
    }
}