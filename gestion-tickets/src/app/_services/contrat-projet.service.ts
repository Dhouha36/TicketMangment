import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ContratProjet } from '../_models/contrat-projet';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class ContratProjetService {
  private baseUrl = `${environment.apiUrl}contratprojet`;

  constructor(private http: HttpClient) {}

  getById(id: number): Observable<ContratProjet> {
    return this.http.get<ContratProjet>(`${this.baseUrl}/${id}`);
  }

  create(dto: Partial<ContratProjet>): Observable<ContratProjet> {
    return this.http.post<ContratProjet>(this.baseUrl, dto);
  }

  update(id: number, dto: Partial<ContratProjet>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
