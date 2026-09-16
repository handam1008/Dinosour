using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RYU._01.Script.Auth
{
    public class PCGoogleAuthManager : AuthBase
    {
        [Header("UI Components")]
        [SerializeField] private Button loginButton;
        
        [Header("Google Desktop Credentials")]
        [SerializeField] private GoogleOAuthConfig oauthConfig;
        
        private const string RedirectUri = "http://localhost:5000/";

        public override void Awake()
        {
            base.Awake();
            BindingUGSEvents();
        }

        private void Start()
        {
            if (oauthConfig == null)
            {
                Debug.LogError("Secrets/GoogleOAuthConfig가 없습니다. 팀장에게 파일을 받으세요.");
                return;
            }
        }

        private void BindingUGSEvents()
        {
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(() => OnGoogleLoginButtonClicked());
            }
        }

        private async void OnGoogleLoginButtonClicked()
        {
            try
            {
                Debug.Log("구글 로그인 시도 중...");

                // UGS 초기화 상태 및 환경 명시 재확인
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    InitializationOptions options = new InitializationOptions();
                    options.SetEnvironmentName("production"); // 대시보드 환경 이름 지정
                    await UnityServices.InitializeAsync(options);
                }

                string idToken = await RequestGoogleIdTokenAsync();

                if (!string.IsNullOrEmpty(idToken))
                {
                    await SignInUGSAsync(idToken);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[구글 로그인 전체 프로세스 실패] {ex.Message}");
            }
        }

        private async Task<string> RequestGoogleIdTokenAsync()
        {
            HttpListener listener = new HttpListener();
            
            try
            {
                listener.Prefixes.Add(RedirectUri);
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Debug.LogError($"포트(5000)가 이미 사용 중이거나 방화벽에 의해 차단되었습니다: {ex.Message}");
                return null;
            }

            // 구글 로그인 브라우저 열기
            string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                             $"client_id={oauthConfig.clientId}&" +
                             $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
                             $"response_type=code&" +
                             $"scope=openid%20email%20profile";

            Application.OpenURL(authUrl);

            // 브라우저 응답 대기
            HttpListenerContext context = await listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            string code = request.QueryString.Get("code");

            // UTF-8 한글 깨짐 방지 응답
            HttpListenerResponse response = context.Response;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;

            string responseString = "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif; text-align:center; padding-top:50px;'><h2>로그인이 완료되었습니다!</h2><p>이 창을 닫고 게임으로 돌아가세요.</p></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            listener.Stop();

            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("구글 인증 코드를 수신하지 못했습니다.");
                return null;
            }

            return await ExchangeCodeForIdTokenAsync(code);
        }

        private async Task<string> ExchangeCodeForIdTokenAsync(string code)
        {
            WWWForm form = new WWWForm();
            form.AddField("code", code);
            form.AddField("client_id", oauthConfig.clientId);
            form.AddField("client_secret", oauthConfig.clientSecret);
            form.AddField("redirect_uri", RedirectUri);
            form.AddField("grant_type", "authorization_code");

            using (UnityWebRequest www = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"토큰 교환 실패: {www.error}\n{www.downloadHandler.text}");
                    return null;
                }
                
                GoogleTokenResponse tokenData = JsonUtility.FromJson<GoogleTokenResponse>(www.downloadHandler.text);
                return tokenData.id_token;
            }
        }

        private async Task SignInUGSAsync(string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithGoogleAsync(idToken);
                Debug.Log($"<color=green>[성공] UGS 구글 로그인 완료! Player ID: {AuthenticationService.Instance.PlayerId}</color>");
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"[UGS 인증 에러] {ex.Message}\n* 대시보드와 유니티 에디터의 Project ID 및 Environment(production)가 일치하는지 확인하세요.");
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"[UGS 요청 실패] {ex.Message}");
            }
        }
        
        [Serializable]
        private class GoogleTokenResponse
        {
            public string access_token;
            public string id_token;
            public int expires_in;
            public string token_type;
        }
    }
}