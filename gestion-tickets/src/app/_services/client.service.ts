import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ClientDto } from '../DTOs/ClientDto';
import { ClientParams } from '../DTOs/ClientParams';
import { RegisterClientDto } from '../DTOs/RegisterClientDto';
import { PaginatedResult } from '../_models/pagination';
import { environment } from 'src/environments/environment';


@Injectable({
  providedIn: 'root'
})
export class ClientService {
  private baseUrl = environment.apiUrl+ "clients";

  constructor(private http: HttpClient) {}

  register(dto: RegisterClientDto): Observable<ClientDto> {
    return this.http.post<ClientDto>(`${this.baseUrl}/register`, dto);
  }

  getAll(): Observable<ClientDto[]> {
    return this.http.get<ClientDto[]>(this.baseUrl);
  }

  getPaged(params: ClientParams): Observable<PaginatedResult<ClientDto>> {
    return this.http.post<PaginatedResult<ClientDto>>(
      `${this.baseUrl}/paged`,
      params
    );
  }

  getById(id: number): Observable<ClientDto> {
    return this.http.get<ClientDto>(`${this.baseUrl}/${id}`);
  }

  update(id: number, dto: Partial<ClientDto>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getBySociete(societeId: number): Observable<ClientDto[]> {
    return this.http.get<ClientDto[]>(`${this.baseUrl}/societe/${societeId}`);
  }

  export(params: ClientParams): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/export`, params, { responseType: 'blob' });
  }
}