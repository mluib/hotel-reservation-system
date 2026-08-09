// Mirrors HotelReservation.Domain.Enums.ReservationStatus.
export type ReservationStatus = 'Confirmed' | 'Cancelled';

// Mirrors the anonymous DTO shape returned by GetReservations / GetMyReservations /
// GetReservationById. Note there's no joined room/customer info from the API (bare
// roomId/customerId) — the mockup did the same join against its own mock data, so
// the frontend does it here too, against the already-fetched rooms/customers lists.
export interface Reservation {
  id: string;
  roomId: string;
  customerId: string;
  checkIn: string;
  checkOut: string;
  status: ReservationStatus;
  pricePerNight: number;
}

// Mirrors HotelReservation.Application.DTOs.CreateReservationRequest.
export interface CreateReservationRequest {
  roomId: string;
  checkIn: string;
  checkOut: string;
}
