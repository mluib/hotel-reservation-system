export function nightsBetween(checkIn: string, checkOut: string): number {
  const ms = new Date(checkOut).getTime() - new Date(checkIn).getTime();
  return Math.round(ms / 86_400_000);
}

// The backend stores a Reservation's PricePerNight (the room's rate at booking time),
// not a precomputed total — the total is nights x that rate, computed client-side
// wherever a reservation's price needs to be shown (My Reservations, Admin's
// Reservations tab), matching what the design mockup displays as "price".
export function reservationTotal(checkIn: string, checkOut: string, pricePerNight: number): number {
  return nightsBetween(checkIn, checkOut) * pricePerNight;
}
