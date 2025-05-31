import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { PaginatedResult, Pagination } from './../_models/pagination';
import { User } from '../_models/user';
import { Pays } from '../_models/pays';
import { Projet } from '../_models/Projet';
import { Ticket } from '../_models/ticket';
import { environment } from '../../environments/environment';
import { ClientCreate } from '../DTOs/client-create.model';
import { ClientDto } from '../DTOs/ClientDto';
import { RegisterUserDto } from '../DTOs/RegisterUserDto';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  paginatedResult = signal<PaginatedResult<User[]> | null>(null);

  // On stocke le user (User ou ClientDto) dans un BehaviorSubject
  private currentUserSubject = new BehaviorSubject<User | ClientDto | null>(null);

  // On expose un observable en lecture seule
  public currentUser$: Observable<User | ClientDto | null> =
    this.currentUserSubject.asObservable();
    constructor(/* injecter HttpClient, etc. */) {
      // Au démarrage du service, on peut initialiser le BehaviorSubject
      // avec le contenu du localStorage (s’il existe) :
      const stored = localStorage.getItem('user');
      if (stored) {
        try {
          const parsed = JSON.parse(stored);
          this.currentUserSubject.next(parsed);
        } catch {
          this.currentUserSubject.next(null);
        }
      }
    } 

  private getUserFromLocalStorage(): User | ClientDto | null {
    const json = localStorage.getItem('user');
    return json ? JSON.parse(json) : null;
  }

  login(credentials: { email: string; password: string }) {
    return this.http.post<User>('/api/account/login', credentials).pipe(
      tap(user => {
        localStorage.setItem('token', user.token);
        this.setCurrentUser(user); 
      })
    );
  }

  logout() {
    localStorage.removeItem('user');
    localStorage.removeItem('token');
    this.setCurrentUser(null);
  }

  register(dto: RegisterUserDto): Observable<User> {
    return this.http.post<User>(
      `${this.baseUrl}account/register`,
      dto
    );
  }

  getPays(): Observable<Pays[]> {
    return this.http.get<Pays[]>(this.baseUrl + 'users/pays');
  }


  // À appeler systématiquement quand on veut changer le user
  setCurrentUser(user: User | ClientDto | null): void {
    if (user) {
      // 1) On met à jour le BehaviorSubject
      this.currentUserSubject.next(user);
      // 2) On synchronise le localStorage
      localStorage.setItem('user', JSON.stringify(user));
    } else {
      // Quand on logout : on next(null) et on nettoie localStorage
      this.currentUserSubject.next(null);
      localStorage.removeItem('user');
      localStorage.removeItem('token');
    }
  }

  public currentUser(): User | ClientDto | null {
    return this.currentUserSubject.value;
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

  updateUser(id: number, formData: FormData): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}users/${id}`, formData);
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
  
}
