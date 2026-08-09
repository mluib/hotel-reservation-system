// Mirrors HotelReservation.Domain.Enums.RoomType, serialized as its name since the
// backend registers JsonStringEnumConverter globally.
export type RoomType = 'Single' | 'Double' | 'Suite';

export const ROOM_TYPES: RoomType[] = ['Single', 'Double', 'Suite'];

// Mirrors HotelReservation.Application.DTOs.RoomDto.
export interface Room {
  id: string;
  number: string;
  type: RoomType;
  pricePerNight: number;
  hotelId: string;
  imageUrl: string | null;
}

// Mirrors HotelReservation.Application.DTOs.RoomFilterRequest (all optional query params).
export interface RoomFilter {
  type?: RoomType | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  checkIn?: string | null;
  checkOut?: string | null;
}

// Mirrors HotelReservation.Application.DTOs.CreateRoomRequest / UpdateRoomRequest
// (the two happen to have the same shape today).
export interface RoomUpsert {
  number: string;
  type: RoomType;
  pricePerNight: number;
  hotelId: string;
}
