import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Ticket } from '../_models/ticket';
import { PaginatedResult } from '../_models/pagination';
import { TicketUpdateDto } from '../_models/ticketUpdateDto';
import { TicketValidationDto } from '../_models/ticket-validation.dto';
import { FinishTicketDto } from '../_models/finish-ticket-dto';
import { AccountService } from './account.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TicketService {
  private baseUrl = environment.apiUrl + '/tickets';

  constructor(
    private http: HttpClient,
    private accountService: AccountService
  ) { }

  getPaginatedTickets(pageNumber?: number, pageSize?: number, searchTerm?: string): Observable<PaginatedResult<Ticket[]>> {
    let params = new HttpParams();
    
    if (pageNumber != null && pageSize != null) {
      params = params.append('pageNumber', pageNumber.toString());
      params = params.append('pageSize', pageSize.toString());
    }
    
    if (searchTerm && searchTerm.trim() !== '') {
      params = params.append('searchTerm', searchTerm);
    }
    
    // Récupération de l'utilisateur courant via AccountService
    const currentUser = this.accountService.currentUser();
    if (currentUser) {
      params = params.append('userId', currentUser.id.toString());
      params = params.append('role', currentUser.role);
    }
    
    return this.http.get<Ticket[]>(this.baseUrl, { observe: 'response', params })
      .pipe(
        map((response: HttpResponse<any>) => {
          const body = response.body;
          const items = Array.isArray(body) ? body : body?.items ?? [];
          const pagination = response.headers.get('Pagination')
            ? JSON.parse(response.headers.get('Pagination')!)
            : { currentPage: 1, pageSize: items.length, totalCount: items.length, totalPages: 1 };
          const paginatedResult: PaginatedResult<Ticket[]> = {
            items: items,
            pagination: pagination
          };
          return paginatedResult;
        })
      );
  }


  getTicket(id: number): Observable<Ticket> {
    return this.http.get<Ticket>(`${this.baseUrl}/${id}`);
  }

  createTicket(formData: FormData): Observable<Ticket> {
    return this.http.post<Ticket>(this.baseUrl, formData);
  }

  updateTicket(id: number, ticket: TicketUpdateDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, ticket);
  }


  deleteTicket(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  // Méthode pour supprimer plusieurs tickets (appel vers DELETE api/tickets/bulk)
  deleteMultipleTickets(ticketIds: number[]): Observable<any> {
    return this.http.request('delete', `${this.baseUrl}/bulk`, { body: ticketIds });
  }

  // Pour la mise à jour avec attachment
  uploadAttachment(formData: FormData): Observable<{ secureUrl: string }> {
    return this.http.post<{ secureUrl: string }>(`${this.baseUrl}/upload`, formData);
  }

  validateTicket(id: number, validationData: TicketValidationDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/validate/${id}`, validationData);
  }

  finishTicket(ticketId: number, finishData: FinishTicketDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/finish/${ticketId}`, finishData);
  }

  updateResponsible(ticketId: number, responsibleDto: { responsibleId: number }): Observable<any> {
    return this.http.post(`${this.baseUrl}/updateResponsible/${ticketId}`, responsibleDto);
  }
  getTicketCountByStatus() {
    return this.http.get<any[]>(`${this.baseUrl}/status-count`);
  }

}
