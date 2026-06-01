import { Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { InterviewService } from './interview.service';
import { AvailabilitySlot } from './interview.models';
import { Card } from '../../shared/ui/card/card';
import { Button } from '../../shared/ui/button/button';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { ToastService } from '../../shared/ui/toast/toast.service';

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

@Component({
  selector: 'app-interview-availability',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, Card, Button, Spinner],
  template: `
    <div class="max-w-4xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-900">Weekly Availability</h1>
        <div class="flex gap-2">
          <input
            type="text"
            [(ngModel)]="interviewerId"
            placeholder="Interviewer ID"
            class="rounded-md border border-gray-300 px-3 py-2 text-sm w-64"
          />
          <app-button variant="secondary" size="sm" (click)="loadAvailability()">
            Load
          </app-button>
        </div>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-8">
          <app-spinner />
        </div>
      } @else {
        <div class="space-y-4">
          @for (day of days; track day.index) {
            <app-card [title]="day.name">
              <div class="space-y-2">
                @for (slot of getSlotsForDay(day.index); track slot.id) {
                  <div class="flex items-center gap-3">
                    <span class="text-sm text-gray-700">{{ slot.startTime }} - {{ slot.endTime }}</span>
                    <app-button variant="danger" size="sm" (click)="removeSlot(day.index, slot.id)">
                      Remove
                    </app-button>
                  </div>
                } @empty {
                  <p class="text-sm text-gray-500">No slots configured</p>
                }
                <div class="flex items-center gap-3 pt-2 border-t border-gray-100">
                  <input
                    type="time"
                    [(ngModel)]="newSlots[day.index].startTime"
                    class="rounded-md border border-gray-300 px-3 py-2 text-sm"
                  />
                  <span class="text-sm text-gray-500">to</span>
                  <input
                    type="time"
                    [(ngModel)]="newSlots[day.index].endTime"
                    class="rounded-md border border-gray-300 px-3 py-2 text-sm"
                  />
                  <app-button size="sm" (click)="addSlot(day.index)">
                    Add Slot
                  </app-button>
                </div>
              </div>
            </app-card>
          }
        </div>

        <div class="flex gap-3 mt-6">
          <app-button [loading]="saving()" (click)="saveAvailability()">
            Save Availability
          </app-button>
        </div>
      }
    </div>
  `,
  styles: `:host { display: block; }`,
})
export class InterviewAvailability implements OnInit, OnDestroy {
  private interviewService = inject(InterviewService);
  private toastService = inject(ToastService);
  private destroy$ = new Subject<void>();

  loading = signal(false);
  saving = signal(false);
  interviewerId = '';
  slots = signal<AvailabilitySlot[]>([]);

  days = DAY_NAMES.map((name, index) => ({ name, index }));

  newSlots: Record<number, { startTime: string; endTime: string }> = {};
  private newIdCounter = -1;

  ngOnInit(): void {
    for (let i = 0; i < 7; i++) {
      this.newSlots[i] = { startTime: '09:00', endTime: '17:00' };
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAvailability(): void {
    if (!this.interviewerId) {
      this.toastService.error('Please enter an interviewer ID');
      return;
    }
    this.loading.set(true);
    this.interviewService
      .getAvailability(this.interviewerId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.slots.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.toastService.error('Failed to load availability');
          this.loading.set(false);
        },
      });
  }

  getSlotsForDay(dayOfWeek: number): AvailabilitySlot[] {
    return this.slots().filter((s) => s.dayOfWeek === dayOfWeek);
  }

  addSlot(dayOfWeek: number): void {
    const times = this.newSlots[dayOfWeek];
    if (!times.startTime || !times.endTime) {
      this.toastService.error('Please enter start and end time');
      return;
    }
    if (times.startTime >= times.endTime) {
      this.toastService.error('End time must be after start time');
      return;
    }

    const newSlot: AvailabilitySlot = {
      id: (this.newIdCounter--).toString(),
      dayOfWeek,
      startTime: times.startTime,
      endTime: times.endTime,
    };
    this.slots.update((current) => [...current, newSlot]);
  }

  removeSlot(dayOfWeek: number, slotId: string): void {
    this.slots.update((current) => current.filter((s) => s.id !== slotId));
  }

  saveAvailability(): void {
    if (!this.interviewerId) {
      this.toastService.error('Please enter an interviewer ID');
      return;
    }
    this.saving.set(true);
    const payload = this.slots().map((s) => ({
      dayOfWeek: s.dayOfWeek,
      startTime: s.startTime,
      endTime: s.endTime,
    }));
    this.interviewService
      .setAvailability(payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.success('Availability saved');
          this.saving.set(false);
          this.loadAvailability();
        },
        error: () => {
          this.toastService.error('Failed to save availability');
          this.saving.set(false);
        },
      });
  }
}
