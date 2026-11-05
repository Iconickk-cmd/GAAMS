
import { Routes } from '@angular/router';

import { DashboardComponent } from './dashboard/dashboard';
import { EmployeesComponent } from './employees/employees';
import { DepartmentsComponent } from './departments/departments';
import { HrComponent } from './hr/hr';
import { LeaveManagementComponent } from './leave-management/leave-management';
import { PositionsComponent } from './positions/positions';
import { PositionAssignmentsComponent } from './position-assignments/position-assignments';

export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'employees', component: EmployeesComponent },
  { path: 'departments', component: DepartmentsComponent },
  { path: 'hr', component: HrComponent },
  { path: 'leave-management', component: LeaveManagementComponent },
  { path: 'positions', component: PositionsComponent },
  { path: 'position-assignments', component: PositionAssignmentsComponent },
  { path: '**', redirectTo: '' }
];
