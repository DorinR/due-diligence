using rag_experiment.Domain;
using rag_experiment.Services.Auth.Models;

namespace rag_experiment.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<AuthResponse> ConfirmResetPasswordAsync(ConfirmResetPasswordRequest request);
        Task<User?> GetUserByIdAsync(int id);
    }
}