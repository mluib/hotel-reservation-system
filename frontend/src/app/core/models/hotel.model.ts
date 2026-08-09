// Mirrors HotelReservation.Application.DTOs.HotelDto.
export interface Hotel {
  id: string;
  name: string;
  address: string;
  imageUrl: string | null;
}

// Mirrors HotelReservation.Application.DTOs.UpdateHotelRequest.
export interface HotelUpdate {
  name: string;
  address: string;
}
