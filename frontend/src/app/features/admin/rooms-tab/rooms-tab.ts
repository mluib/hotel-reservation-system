import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RoomsService } from '../../../core/services/rooms.service';
import { AdminHighlightService } from '../admin-highlight.service';
import { Room } from '../../../core/models/room.model';
import { extractErrorMessage } from '../../../core/utils/http-error';
import { RoomDialog } from '../dialogs/room-dialog/room-dialog';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';
import { ErrorDialog } from '../../../shared/error-dialog/error-dialog';

type SortKey = 'name' | 'number' | 'type' | 'pricePerNight';

@Component({
  selector: 'app-rooms-tab',
  imports: [RoomDialog, ConfirmDialog, ErrorDialog],
  templateUrl: './rooms-tab.html',
  styleUrl: './rooms-tab.css',
})
export class RoomsTab implements OnInit {
  private readonly roomsService = inject(RoomsService);
  protected readonly highlight = inject(AdminHighlightService);

  private readonly rooms = signal<Room[]>([]);
  private readonly sort = signal<{ key: SortKey; dir: 'asc' | 'desc' }>({
    key: 'number',
    dir: 'asc',
  });

  protected readonly sortedRooms = computed(() => {
    const { key, dir } = this.sort();
    return [...this.rooms()].sort((a, b) => {
      const av = a[key];
      const bv = b[key];
      const cmp = typeof av === 'number' && typeof bv === 'number' ? av - bv : String(av).localeCompare(String(bv));
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  protected readonly dialogOpen = signal(false);
  protected readonly editingRoom = signal<Room | null>(null);
  protected readonly pendingDelete = signal<Room | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected sortArrow(key: SortKey): string {
    const s = this.sort();
    return s.key !== key ? '' : s.dir === 'asc' ? '▲' : '▼';
  }

  protected toggleSort(key: SortKey): void {
    this.sort.update((s) => ({
      key,
      dir: s.key === key && s.dir === 'asc' ? 'desc' : 'asc',
    }));
  }

  protected openNew(): void {
    this.editingRoom.set(null);
    this.dialogOpen.set(true);
  }

  protected openEdit(room: Room): void {
    this.editingRoom.set(room);
    this.dialogOpen.set(true);
  }

  protected onDialogSaved(): void {
    this.dialogOpen.set(false);
    this.load();
  }

  protected confirmDelete(): void {
    const room = this.pendingDelete();
    if (!room) return;
    this.roomsService.delete(room.id).subscribe({
      next: () => {
        this.pendingDelete.set(null);
        this.load();
      },
      error: (err) => {
        this.pendingDelete.set(null);
        this.deleteError.set(extractErrorMessage(err));
      },
    });
  }

  private load(): void {
    this.roomsService.getAll().subscribe((rooms) => this.rooms.set(rooms));
  }
}
