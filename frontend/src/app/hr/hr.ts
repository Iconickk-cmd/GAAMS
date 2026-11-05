
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

interface HrStatistics {
  totalEmployees: number;
  activeEmployees: number;
  inactiveEmployees: number;
  totalDepartments: number;
  activeDepartments: number;
}

@Component({
  selector: 'app-hr',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './hr.html',
  styleUrl: './hr.css'
})
export class HrComponent implements OnInit {
  protected readonly totalEmployees = signal<number | null>(null);
  protected readonly activeEmployees = signal<number | null>(null);
  protected readonly inactiveEmployees = signal<number | null>(null);
  protected readonly totalDepartments = signal<number | null>(null);
  protected readonly activeDepartments = signal<number | null>(null);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');

  private readonly apiUrl = 'http://localhost:5081/api/hr/statistics';

  constructor(private readonly http: HttpClient) { }

  ngOnInit(): void {
    this.loadOverview();
  }

  protected loadOverview(): void {
    if (this.loading()) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.http.get<HrStatistics>(this.apiUrl).subscribe({
      next: (statistics) => {
        this.totalEmployees.set(statistics.totalEmployees);
        this.activeEmployees.set(statistics.activeEmployees);
        this.inactiveEmployees.set(statistics.inactiveEmployees);
        this.totalDepartments.set(statistics.totalDepartments);
        this.activeDepartments.set(statistics.activeDepartments);

        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to load HR statistics:', error);

        this.errorMessage.set(
          'Unable to load HR statistics. Please check that the GAAMS API is running and try refreshing.'
        );

        this.loading.set(false);
      }
    });
  }
}
