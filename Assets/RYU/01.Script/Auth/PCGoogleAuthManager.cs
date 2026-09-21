using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RYU._01.Script.Auth
{
    public class PCGoogleAuthManager : AuthBase
    {
        [Header("UI Components")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button savePlayerNameButton;
        [SerializeField] private TMP_InputField playerNameIF;

        [SerializeField] private string sceneName = "Leaderboard";

        [Header("Google Desktop Credentials")]
        [SerializeField] private GoogleOAuthConfig oauthConfig;

        [Header("Login")]
        [SerializeField] private float loginTimeoutSeconds = 120f;

        private const string GoogleClientIdSuffix = ".apps.googleusercontent.com";

        private HttpListener _listener;

        public override void Awake()
        {
            base.Awake();
            BindUIEvents();
        }

        private void Start()
        {
            savePlayerNameButton.gameObject.SetActive(false);
            playerNameIF.gameObject.SetActive(false);
            ValidateConfig();
        }

        private void OnDestroy()
        {
            // 로그인 도중 씬이 바뀌거나 플레이가 멈춰도 포트를 반납한다
            StopListener();
        }

        private void BindUIEvents()
        {
            loginButton.onClick.AddListener(OnGoogleLoginButtonClicked);
            savePlayerNameButton.onClick.AddListener(async () => await SavePlayerName(playerNameIF.text));
        }

        private bool ValidateConfig()
        {
            if (oauthConfig == null)
            {
                Debug.LogError("Secrets/GoogleOAuthConfig가 없습니다. 팀장에게 파일을 받으세요.");
                return false;
            }

            string clientId = oauthConfig.clientId?.Trim();
            if (string.IsNullOrEmpty(clientId) || !clientId.EndsWith(GoogleClientIdSuffix))
            {
                Debug.LogError($"Client ID가 올바르지 않습니다. '{GoogleClientIdSuffix}'까지 전부 입력했는지 확인하세요.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(oauthConfig.clientSecret))
            {
                Debug.LogError("Client Secret이 비어 있습니다.");
                return false;
            }

            return true;
        }

        private async Task SavePlayerName(string playerName)
        {
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

                var _playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
                playerNameIF.text = _playerName.Split('#')[0];
                Debug.Log("플레이어 이름 변경 성공" + AuthenticationService.Instance.PlayerName);
                playerNameIF.gameObject.SetActive(false);
                savePlayerNameButton.gameObject.SetActive(false);
                SceneManager.LoadScene(sceneName);
            }
            catch (AuthenticationException e)
            {
                Debug.Log(e.Message);
            }
        }

        private async void OnGoogleLoginButtonClicked()
        {
            if (!ValidateConfig()) return;

            // 로그인 중 중복 클릭 방지
            loginButton.interactable = false;

            try
            {
                Debug.Log("구글 로그인 시도 중...");

                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    InitializationOptions options = new InitializationOptions();
                    options.SetEnvironmentName("production");
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
            finally
            {
                if (loginButton != null) loginButton.interactable = true;
            }
        }

        private async Task<string> RequestGoogleIdTokenAsync()
        {
            StopListener();

            // 고정 포트는 다른 프로그램과 부딪힐 수 있어서 매번 비어 있는 포트를 쓴다.
            // 구글 데스크톱 OAuth는 루프백 주소면 포트를 자유롭게 허용한다.
            string redirectUri = $"http://{IPAddress.Loopback}:{GetFreePort()}/";

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(redirectUri);
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Debug.LogError($"로그인 대기 서버 시작 실패 (코드 {ex.ErrorCode}): {ex.Message}");
                StopListener();
                return null;
            }

            try
            {
                string authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                                 $"client_id={Uri.EscapeDataString(oauthConfig.clientId.Trim())}&" +
                                 $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                                 "response_type=code&" +
                                 "scope=openid%20email%20profile";

                Application.OpenURL(authUrl);

                // 브라우저 응답 대기 (창을 그냥 닫아버린 경우를 위해 시간 제한)
                Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
                Task finished = await Task.WhenAny(contextTask, Task.Delay(TimeSpan.FromSeconds(loginTimeoutSeconds)));

                if (finished != contextTask)
                {
                    // 리스너를 끄면 대기 중이던 작업이 예외로 끝나므로 조용히 처리한다
                    _ = contextTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    Debug.LogWarning("구글 로그인 시간이 초과되었습니다. 다시 시도해주세요.");
                    return null;
                }

                HttpListenerContext context = await contextTask;
                string code = context.Request.QueryString.Get("code");

                WriteResponsePage(context.Response, !string.IsNullOrEmpty(code));

                if (string.IsNullOrEmpty(code))
                {
                    string error = context.Request.QueryString.Get("error");
                    Debug.LogError($"구글 인증 코드를 수신하지 못했습니다. {error}");
                    return null;
                }

                return await ExchangeCodeForIdTokenAsync(code, redirectUri);
            }
            finally
            {
                // 성공·실패·시간초과 모두 포트를 반납한다
                StopListener();
            }
        }

        private static void WriteResponsePage(HttpListenerResponse response, bool success)
        {
            string message = success
                ? "<h2>로그인이 완료되었습니다!</h2><p>이 창을 닫고 게임으로 돌아가세요.</p>"
                : "<h2>로그인에 실패했습니다.</h2><p>이 창을 닫고 게임에서 다시 시도해주세요.</p>";

            string html = "<html><head><meta charset='utf-8'></head>" +
                          "<body style='font-family:sans-serif; text-align:center; padding-top:50px;'>" +
                          message + "</body></html>";

            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private static int GetFreePort()
        {
            // 포트 0으로 열면 OS가 비어 있는 포트를 배정해준다
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private void StopListener()
        {
            if (_listener == null) return;

            try
            {
                if (_listener.IsListening) _listener.Stop();
                _listener.Close();
            }
            catch (ObjectDisposedException)
            {
            }

            _listener = null;
        }

        private async Task<string> ExchangeCodeForIdTokenAsync(string code, string redirectUri)
        {
            WWWForm form = new WWWForm();
            form.AddField("code", code);
            form.AddField("client_id", oauthConfig.clientId.Trim());
            form.AddField("client_secret", oauthConfig.clientSecret.Trim());
            form.AddField("redirect_uri", redirectUri);
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
                loginButton.gameObject.SetActive(false);
                playerNameIF.gameObject.SetActive(true);
                savePlayerNameButton.gameObject.SetActive(true);

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
