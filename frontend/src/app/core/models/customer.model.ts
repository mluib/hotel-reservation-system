// Mirrors HotelReservation.Application.DTOs.CustomerDto.
export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
}

// Mirrors HotelReservation.Application.DTOs.UpdateCustomerRequest.
export interface CustomerUpdate {
  firstName: string;
  lastName: string;
  email: string;
}
