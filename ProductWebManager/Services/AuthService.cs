using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace ProductWebManager.Services
{
    public class AuthService : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage;
        private bool _isInitialized = false;
        private bool _isAuthenticated = false;
        private string _userName = "";
        private int _userId = 0;

        public int CurrentUserId => _userId;
        public string CurrentUserName => _userName;
        public bool IsAuthenticated => _isAuthenticated;

        public AuthService(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
        }

        // Этот метод Blazor вызывает автоматически для проверки прав доступа
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            return CreateAuthenticationState();
        }

        // Вынесли создание состояния в отдельный метод для переиспользования
        private AuthenticationState CreateAuthenticationState()
        {
            ClaimsIdentity identity;

            if (_isAuthenticated)
            {
                identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, _userName),
                    new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                    new Claim(ClaimTypes.Role, "User")
                }, "CustomAuth");
            }
            else
            {
                identity = new ClaimsIdentity(); // Пустой identity означает "Не авторизован"
            }

            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                _isInitialized = true;
                var result = await _localStorage.GetAsync<AuthState>("auth");
                if (result.Success && result.Value != null)
                {
                    _isAuthenticated = result.Value.IsAuthenticated;
                    _userId = result.Value.UserId;
                    _userName = result.Value.UserName ?? "";
                }
            }
            catch
            {
                // Защита от пререндеринга на сервере, когда JS еще недоступен
                _isAuthenticated = false;
                _userId = 0;
                _userName = "";
                _isInitialized = false;
            }
        }

        public async Task Login(int userId, string userName)
        {
            _isAuthenticated = true;
            _userId = userId;
            _userName = userName;
            _isInitialized = true;

            try
            {
                await _localStorage.SetAsync("auth", new AuthState
                {
                    IsAuthenticated = true,
                    UserId = userId,
                    UserName = userName
                });
            }
            catch { }

            // КРИТИЧЕСКИЙ СТЕП: Создаем новое состояние и уведомляем ВСЕ компоненты Blazor
            var newState = Task.FromResult(CreateAuthenticationState());
            NotifyAuthenticationStateChanged(newState);
        }

        public async Task Logout()
        {
            _isAuthenticated = false;
            _userId = 0;
            _userName = "";
            _isInitialized = true;

            try
            {
                await _localStorage.DeleteAsync("auth");
            }
            catch { }

            // КРИТИЧЕСКИЙ СТЕП: Оповещаем систему о выходе пользователя
            var newState = Task.FromResult(CreateAuthenticationState());
            NotifyAuthenticationStateChanged(newState);
        }
    }

    public class AuthState
    {
        public bool IsAuthenticated { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
    }
}
