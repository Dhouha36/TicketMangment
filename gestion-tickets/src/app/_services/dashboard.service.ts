import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardCountsDto } from '../_models/dashboardCountsDto';
import { environment } from '../../environments/environment';
import { TicketStatDto } from '../_models/ticket-stat.dto';
import { TicketFilterRequest } from '../_models/TicketFilterRequest';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private baseUrl = environment.apiUrl+"dashboard";
  constructor(private http: HttpClient) {}

  getDashboardCounts(): Observable<DashboardCountsDto> {
    return this.http.get<DashboardCountsDto>(`${this.baseUrl}/counts`);
  }
  getTicketsByUser(req: TicketFilterRequest): Observable<TicketStatDto[]> {
    return this.http.post<TicketStatDto[]>(`${this.baseUrl}/tickets-by-user`, req);
  }

  getTicketsByStatus(req: TicketFilterRequest): Observable<TicketStatDto[]> {
    return this.http.post<TicketStatDto[]>(`${this.baseUrl}/tickets-by-status`, req);
  }
  getTicketsFiltered(req: TicketFilterRequest): Observable<TicketStatDto[]> {
    return this.http.post<TicketStatDto[]>(`${this.baseUrl}/tickets-filtered`, req);
  }
  getMyTicketsCount(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/my-tickets-count`);
  }
  
  getTicketsCountAsResponsible(userId: number): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/count/responsible/${userId}`);
  }

  getTicketsCountAsProjectMember(userId: number): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/count/projectmember/${userId}`);
  }

  getTimeSpent(req: TicketFilterRequest): Observable<TicketStatDto[]> {
    return this.http.post<TicketStatDto[]>(`${this.baseUrl}/timespent`, req);
  }

}
