import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { PaginatedResult, Pagination } from './../_models/pagination';
import { User } from '../_models/user';
import { Pays } from '../_models/pays';
import { Projet } from '../_models/Projet';
import { Ticket } from '../_models/ticket';
import { environment } from '../../environments/environment';
import { ClientCreate } from '../DTOs/client-create.model';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  currentUser = signal<User | null>(this.getUserFromLocalStorage());
  paginatedResult = signal<PaginatedResult<User[]> | null>(null);

  private getUserFromLocalStorage(): User | null {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  }

  login(model: any) {
    return this.http.post<User>(this.baseUrl + 'account/login', model).pipe(
      map(user => {
        if (user) {
          localStorage.setItem('user', JSON.stringify(user));
          this.currentUser.set(user);
        }
      })
    );
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUser.set(null);
  }

  register(dto: ClientCreate): Observable<User> {
    return this.http.post<User>(this.baseUrl + 'account/register', dto).pipe(
      map(user => user)
    );
  }

  getPays(): Observable<Pays[]> {
    return this.http.get<Pays[]>(this.baseUrl + 'users/pays');
  }

  setCurrentUser(user: User) {
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.baseUrl + 'users');
  }

  // Méthode pour récupérer les utilisateurs paginés avec filtres dans le body
  getUsers(
    pageNumber: number,
    pageSize: number,
    searchTerm?: string,
    extraFilters?: any
  ): Observable<PaginatedResult<User[]>> {
    const params = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ? searchTerm : '',
      ...extraFilters
    };

    return this.http.post<any>(this.baseUrl + 'users/paged', params, { observe: 'response' })

      .pipe(

        map((response: HttpResponse<any>) => {

          // Now response.headers is available

          const paginationHeader = response.headers.get('Pagination');

          const paginatedResult: PaginatedResult<User[]> = {

            items: response.body || [],

            pagination: paginationHeader ? JSON.parse(paginationHeader) : {} as Pagination

          };

          return paginatedResult;

        })

      );
  }
  

  getUser(id: number): Observable<User> {
    return this.http.get<User>(this.baseUrl + 'users/' + id);
  }

  updateUser(user: User): Observable<any> {
    return this.http.post(this.baseUrl + 'users/' + user.id, user);
  }
  

  getUserProjects(
    userId: number,
    pageNumber: number,
    pageSize: number,
    searchTerm?: string
  ): Observable<PaginatedResult<Projet[]>> {
    const params = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ? searchTerm : ''
    };
  
    return this.http.post<any>(
      `${this.baseUrl}users/${userId}/projects/paged`,
      params,
      { observe: 'response' }
    ).pipe(
      map((response: HttpResponse<any>) => {
        const paginationHeader = response.headers.get('Pagination');
  
        const paginatedResult: PaginatedResult<Projet[]> = {
          items: response.body || [],
          pagination: paginationHeader ? JSON.parse(paginationHeader) : {} as Pagination
        };
        return paginatedResult;
      })
    );
  }
  

  getUserTickets(
    userId: number,
    pageNumber: number,
    pageSize: number,
    searchTerm?: string
  ): Observable<PaginatedResult<Ticket[]>> {
    const params = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ? searchTerm : ''
    };
  
    return this.http.post<any>(
      `${this.baseUrl}users/${userId}/tickets/paged`,
      params,
      { observe: 'response' }
    ).pipe(
      map((response: HttpResponse<any>) => {
        const paginationHeader = response.headers.get('Pagination');
  
        const paginatedResult: PaginatedResult<Ticket[]> = {
          items: response.body || [],
          pagination: paginationHeader ? JSON.parse(paginationHeader) : {} as Pagination
        };
        return paginatedResult;
      })
    );
  }
  

  deleteUser(id: number): Observable<any> {
    return this.http.get(this.baseUrl + 'users/delete/' + id).pipe(
      tap(() => {
        if (this.currentUser()?.id === id) {
          this.logout();
        }
      })
    );
  }

  validateToken(): Observable<void> {
    return this.http.get<void>(this.baseUrl + 'account/validate');
  }

  getUsersByRole(roleName: string): Observable<User[]> {
    return this.http.get<User[]>(`${this.baseUrl}users/role/${roleName}`);
  }

  exportUsers(searchTerm: string, extraFilters: any): Observable<Blob> {
    // On regroupe les filtres dans un seul objet
    const filters = {
      searchTerm: searchTerm?.trim() !== '' ? searchTerm : undefined,
      role: extraFilters?.role,
      actif: extraFilters?.actif,
      hasContract: extraFilters?.hasContract
    };
    
    // Optionnel : on peut nettoyer l'objet en supprimant les propriétés undefined
    const cleanedFilters = Object.fromEntries(Object.entries(filters).filter(([_, v]) => v !== undefined));
  
    return this.http.post(this.baseUrl + 'users/export', cleanedFilters, { responseType: 'blob' });
  }

  forgotPassword(model: any): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(this.baseUrl + 'account/forgot-password', model);
  }

  resetPassword(model: any): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(this.baseUrl + 'account/reset-password', model);
  }
  
  removeAccents(input: string): string {

    const accentsMap: { [key: string]: string } = {

      'á': 'a', 'à': 'a', 'ä': 'a', 'â': 'a', 'ã': 'a', 'å': 'a',

      'é': 'e', 'è': 'e', 'ë': 'e', 'ê': 'e',

      'í': 'i', 'ì': 'i', 'ï': 'i', 'î': 'i',

      'ó': 'o', 'ò': 'o', 'ö': 'o', 'ô': 'o', 'õ': 'o',

      'ú': 'u', 'ù': 'u', 'ü': 'u', 'û': 'u',

      'ç': 'c', 'ñ': 'n',

      'Á': 'A', 'À': 'A', 'Ä': 'A', 'Â': 'A', 'Ã': 'A', 'Å': 'A',

      'É': 'E', 'È': 'E', 'Ë': 'E', 'Ê': 'E',

      'Í': 'I', 'Ì': 'I', 'Ï': 'I', 'Î': 'I',

      'Ó': 'O', 'Ò': 'O', 'Ö': 'O', 'Ô': 'O', 'Õ': 'O',

      'Ú': 'U', 'Ù': 'U', 'Ü': 'U', 'Û': 'U',

      'Ç': 'C', 'Ñ': 'N'

    };

 
    return input.replace(/[áàäâãåéèëêíìïîóòöôõúùüûçñÁÀÄÂÃÅÉÈËÊÍÌÏÎÓÒÖÔÕÚÙÜÛÇÑ]/g, match => accentsMap[match]);

  }

  removeSpecial(input:string): string{

    let data = this.removeAccents(input);

    data = data.replace(/"/g, "'");  // Replace double quotes with single quotes

    data = data.replace(/°/g, ' ');  // Replace degree symbol with space

    return data;

  }
}
