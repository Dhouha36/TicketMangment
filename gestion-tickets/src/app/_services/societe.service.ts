import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { Societe } from '../_models/societe';
import { PaginatedResult } from '../_models/pagination';
import { User } from '../_models/user';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SocieteService {
  getSocieteById(societeId: number) {
    throw new Error('Method not implemented.');
  }
  private apiUrl = environment.apiUrl + '/societe'; 
  paginatedResult: PaginatedResult<Societe[]> | null = null;

  constructor(private http: HttpClient) {}

  getSocietes(searchTerm?: string): Observable<Societe[]> {
    let params = new HttpParams();
    if (searchTerm && searchTerm.trim() !== '') {
      params = params.append('searchTerm', searchTerm);
    }
    return this.http.get<Societe[]>(this.apiUrl, { params });
  }

  // Méthode pour récupérer les projets paginés
  getPaginatedSocietes(pageNumber?: number, pageSize?: number, searchTerm?: string): Observable<PaginatedResult<Societe[]>> {
    let params = new HttpParams();
    if (pageNumber != null && pageSize != null) {
      params = params.append('pageNumber', pageNumber.toString());
      params = params.append('pageSize', pageSize.toString());
    }
    if (searchTerm && searchTerm.trim() !== '') {
      params = params.append('searchTerm', searchTerm);
    }
  
    return this.http.get<Societe[]>(this.apiUrl + '/paged', { observe: 'response', params })
      .pipe(
        map((response: HttpResponse<Societe[]>) => {
          const paginatedResult: PaginatedResult<Societe[]> = {
            items: response.body || [],
            pagination: response.headers.get('Pagination') ? JSON.parse(response.headers.get('Pagination')!) : null!
          };
          this.paginatedResult = paginatedResult;
          return paginatedResult;
        })
      );
  }
  
  

  getSociete(id: number): Observable<Societe> {
    return this.http.get<Societe>(`${this.apiUrl}/${id}`);
  }

  getSocieteDetails(id: number): Observable<Societe> {
    return this.http.get<Societe>(`${this.apiUrl}/details/${id}`);
  }

  addSociete(societe: Societe): Observable<Societe> {
    return this.http.post<Societe>(this.apiUrl, societe);
  }

  updateSociete(id: number, societe: Societe): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, societe);
  }

  deleteSociete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  deleteSelectedSocietes(ids: number[]): Observable<void> {
    return this.http.request<void>('delete', `${this.apiUrl}/supprimerSocietes`, { body: ids });
  }

  getSocieteUsersPaged(societeId: number, pageNumber?: number, pageSize?: number, searchTerm?: string): Observable<PaginatedResult<User[]>> {
    let params = new HttpParams();
    if (pageNumber != null && pageSize != null) {
      params = params.append('pageNumber', pageNumber.toString());
      params = params.append('pageSize', pageSize.toString());
    }
    if (searchTerm && searchTerm.trim() !== '') {
      params = params.append('searchTerm', searchTerm);
    }
    
    return this.http.get<User[]>(`${this.apiUrl}/${societeId}/users/paged`, { 
      observe: 'response', 
      params 
    }).pipe(
      map((response: HttpResponse<User[]>) => {
        const paginatedResult: PaginatedResult<User[]> = {
          items: response.body || [],
          pagination: response.headers.get('Pagination') 
            ? JSON.parse(response.headers.get('Pagination')!) 
            : null
        };
        return paginatedResult;
      })
    );
  }

  attachUser(societeId: number, userId: number): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/${societeId}/users/${userId}`,
      null,
      { responseType: 'text' }  // Précise que la réponse sera du texte
    ); 
  }
  
  
  detachUser(societeId: number, userId: number): Observable<any> {
  return this.http.delete(`${this.apiUrl}/${societeId}/users/${userId}`, { responseType: 'text' });
}  
  
}
