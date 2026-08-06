import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HotelService } from '../../core/services/hotel.service';
import { resolveImageUrl } from '../../core/utils/image-url';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  protected readonly hotelService = inject(HotelService);
  protected readonly heroImageUrl = computed(() => resolveImageUrl(this.hotelService.hotel()?.imageUrl));

  ngOnInit(): void {
    this.hotelService.ensureLoaded();
  }
}
