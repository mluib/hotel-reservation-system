namespace HotelReservation.Application.Authentication;

public interface IAuthService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);

    Task<AuthenticationResponse> LoginAsync(LoginRequest request);
}
