import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable, from, throwError } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { Ticket } from '../_models/ticket';
import { PaginatedResult, Pagination } from '../_models/pagination';
import { TicketUpdateDto } from '../_models/ticketUpdateDto';
import { TicketValidationDto } from '../_models/ticket-validation.dto';
import { FinishTicketDto } from '../_models/finish-ticket-dto';
import { AccountService } from './account.service';
import { TicketCreateDto } from '../_models/ticketCreateDto';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TicketService {
  private baseUrl = environment.apiUrl+"tickets";
  constructor(
    private http: HttpClient,
    private accountService: AccountService
  ) { }

  getPaginatedTickets(
    pageNumber: number,
    pageSize: number,
    filters: any
  ): Observable<PaginatedResult<Ticket[]>> {
    const params = {
      pageNumber,
      pageSize,
      ...filters
    };
  
    return this.http.post<any>(`${this.baseUrl}/paged`, params, { observe: 'response' })
      .pipe(
        map(response => {
          const paginationHeader = response.headers.get('Pagination');
          const paginatedResult: PaginatedResult<Ticket[]> = {
            items: response.body || [],
            pagination: paginationHeader ? JSON.parse(paginationHeader) : {} as Pagination
          };
          return paginatedResult;
        })
      );
  }

  getTicket(id: number): Observable<Ticket> {
    return this.http.get<Ticket>(`${this.baseUrl}/${id}`);
  }

  // Convert file to base64 using a Promise; used internally by createTicket when needed.
  convertFileToBase64(file: File): Promise<string> {
    return new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        const base64String = (reader.result as string).split(',')[1];
        resolve(base64String);
      };
      reader.onerror = error => reject(error);
    });
  }

  // Create ticket returns an Observable.
  createTicket(ticket: TicketCreateDto, file?: File): Observable<any> {
    const formData = new FormData();
    
    // Envoyer le JSON sous la clé 'ticketJson' (et non 'ticket')
    formData.append('ticketJson', JSON.stringify(ticket));
    
    // Ajouter le fichier (s'il existe)
    if (file) {
        formData.append('file', file, file.name);
    }
    
    // Envoyer en multipart/form-data
    return this.http.post(this.baseUrl, formData);
}
  
  updateTicket(id: number, ticket: TicketUpdateDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, ticket);
  }

  deleteTicket(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  deleteMultipleTickets(ticketIds: number[]): Observable<any> {
    return this.http.request('delete', `${this.baseUrl}/bulk`, { body: ticketIds });
  }

  // For updating with attachment.
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

  getTicketCountByStatus(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/status-count`);
  }

  exportTickets(filters: any): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/export`, filters, { responseType: 'blob' });
  }
  
  
}
