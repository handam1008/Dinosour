using System;
using System.Threading.Tasks;
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
            await OnServicesReadyAsync();
        }

        // 초기화가 끝난 뒤에 실행됨. 자동 로그인처럼 서비스가 준비돼야 하는 일은 여기서
        protected virtual Task OnServicesReadyAsync() => Task.CompletedTask;

        // 예전에 로그인한 기록(세션 토큰)이 있으면 같은 계정으로 다시 로그인
        protected static async Task<bool> TrySignInWithCachedSessionAsync()
        {
            if (!AuthenticationService.Instance.SessionTokenExists) return false;

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"자동 로그인 실패, 저장된 로그인 기록을 지웁니다: {e.Message}");
                AuthenticationService.Instance.ClearSessionToken();
                return false;
            }
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