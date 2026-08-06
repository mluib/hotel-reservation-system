// Mirrors HotelReservation.Application.Authentication.LoginRequest.
export interface LoginRequest {
  email: string;
  password: string;
}

// Mirrors HotelReservation.Application.Authentication.RegisterRequest.
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

// Mirrors HotelReservation.Application.Authentication.AuthenticationResponse.
export interface AuthenticationResponse {
  token: string;
}

export type Role = 'Admin' | 'Customer';

// What's decoded out of the JWT payload — enough to drive the UI (role, id, display
// name) without a dedicated "whoami" call for anything but the customer's own name
// (see CustomerProfileService, which covers that separately).
export interface DecodedUser {
  userId: string;
  userName: string;
  roles: Role[];
  expiresAtEpochSeconds: number;
}
