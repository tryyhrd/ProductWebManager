using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProductWebManager.Services
{
    public class AuthService
    {
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        // Синхронные свойства для обратной совместимости.
        // Так как это ServerAuthenticationStateProvider, задача уже завершена.
        private AuthenticationState CurrentState => _authStateProvider.GetAuthenticationStateAsync().Result;

        public int CurrentUserId 
        {
            get
            {
                var user = CurrentState.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    if (idClaim != null && int.TryParse(idClaim.Value, out int id))
                        return id;
                }
                return 0;
            }
        }

        public string CurrentUserName 
        {
            get
            {
                var user = CurrentState.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    return user.Identity.Name ?? "";
                }
                return "";
            }
        }

        public bool IsAuthenticated => CurrentState.User.Identity?.IsAuthenticated == true;
        
        // Для обратной совместимости, чтобы Routes.razor не ругался (инициализация теперь не требуется)
        public bool IsInitialized => true;

        // Эти методы теперь не делают ничего, так как авторизация идет через /api/auth/login
        public Task Login(int userId, string userName) => Task.CompletedTask;
        public Task Logout() => Task.CompletedTask;
    }
}
