namespace HotelReservation.Application.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string userName, System.Collections.Generic.IList<string> roles);
}
