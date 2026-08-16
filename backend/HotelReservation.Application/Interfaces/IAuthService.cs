using HotelReservation.Application.DTOs;

namespace HotelReservation.Application.Interfaces;

public interface IAuthService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);

    Task<AuthenticationResponse> LoginAsync(LoginRequest request);
}
