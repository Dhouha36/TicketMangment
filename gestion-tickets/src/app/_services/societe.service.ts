import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { Societe } from '../_models/societe';
import { PaginatedResult, Pagination } from '../_models/pagination';
import { User } from '../_models/user';
import { environment } from '../../environments/environment';
import { SocieteCreate } from '../_models/societe-create.model';

@Injectable({
  providedIn: 'root'
})
export class SocieteService {
  getSocieteById(societeId: number) {
    throw new Error('Method not implemented.');
  }
  public apiUrl = environment.apiUrl+"societe";
  paginatedResult: PaginatedResult<Societe[]> | null = null;

  constructor(private http: HttpClient) { }

  getSocietes(searchTerm?: string): Observable<Societe[]> {
    const body = { searchTerm: searchTerm || '' };
    return this.http.post<Societe[]>(`${this.apiUrl}/search`, body);
  }

  // Méthode pour récupérer les projets paginés
  getPaginatedSocietes(
    pageNumber: number, 
    pageSize: number, 
    searchTerm?: string, 
    extraFilters?: any
  ): Observable<PaginatedResult<Societe[]>> {
    const params = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ? searchTerm : '',
      ...extraFilters
    };
  
    return this.http.post<any>(this.apiUrl + '/paged', params, { observe: 'response' })
      .pipe(
        map((response: HttpResponse<Societe[]>) => {
          const paginationHeader = response.headers.get('Pagination');
          const paginatedResult: PaginatedResult<Societe[]> = {
            items: response.body || [],
            pagination: paginationHeader ? JSON.parse(paginationHeader) : {} as Pagination
          };
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

  addSociete(societeDto: SocieteCreate): Observable<Societe> {
    return this.http.post<Societe>(this.apiUrl, societeDto);
  }

  validateSociete(dto: SocieteCreate): Observable<void> {
    return this.http
      .post<void>(`${this.apiUrl}/validateSociete`, dto);
  }
  updateSociete(id: number, societe: Societe): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, societe);
  }

  deleteSociete(id: number): Observable<void> {
    const headers = new HttpHeaders()
  .set('Content-Type', 'application/json; charset=utf-8')
  .set('Accept', 'application/json; charset=utf-8');
    return this.http.get<void>(`${this.apiUrl}/delet/${id}`, { headers });
  }

  deleteSelectedSocietes(ids: number[]): Observable<void> {
    return this.http.request<void>('get', `${this.apiUrl}supprimerSocietes`, { body: ids });
  }

  getSocieteUsersPaged(
    societeId: number, 
    pageNumber?: number, 
    pageSize?: number, 
    searchTerm?: string,
    extraFilters?: any
  ): Observable<PaginatedResult<User[]>> {
    const params = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ? searchTerm : '',
      ...extraFilters
    };
  
    return this.http.post<any>(`${this.apiUrl}/${societeId}/users/paged`, params, { observe: 'response' })
      .pipe(
        map((response: HttpResponse<User[]>) => {
          const paginationHeader = response.headers.get('Pagination');
          const paginatedResult: PaginatedResult<User[]> = {
            items: response.body || [],
            pagination: paginationHeader ? JSON.parse(paginationHeader) : {} as Pagination
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


  detachUser(societeId: number, userId: number): Observable<string> {
    const url = `${this.apiUrl}/${societeId}/users/${userId}`;
    return this.http
      .delete(url, { responseType: 'text' });
  }
  

  exportSocietes(searchTerm: string, extraFilters: any): Observable<Blob> {
    const body: any = { searchTerm };
    if (extraFilters && extraFilters.pays) {
      body.pays = extraFilters.pays;
    }
    return this.http.post(`${this.apiUrl}/export`, body, { responseType: 'blob' });
  }

}
