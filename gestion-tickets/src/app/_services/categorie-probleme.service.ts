import { Injectable } from '@angular/core'; 
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { CategorieProbleme } from '../_models/categorie-probleme.model';
import { PaginatedResult } from '../_models/pagination';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CategorieProblemeService {
  private baseUrl = environment.apiUrl + '/CategorieProbleme';

  constructor(private http: HttpClient) {}

  // Récupère toutes les catégories (non paginées)
  getCategories(): Observable<CategorieProbleme[]> {
    return this.http.get<CategorieProbleme[]>(this.baseUrl);
  }

  // Récupère les catégories paginées
  getCategoriesPaginated(pageNumber: number, pageSize: number, searchTerm: string = ''): Observable<PaginatedResult<CategorieProbleme[]>> {
    const paginatedResult: PaginatedResult<CategorieProbleme[]> = new PaginatedResult<CategorieProbleme[]>();
    
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm);

    return this.http.get<CategorieProbleme[]>(`${this.baseUrl}/paged`, { observe: 'response', params })
      .pipe(
        map((response: HttpResponse<CategorieProbleme[]>) => {
          paginatedResult.items = response.body || [];
          const paginationHeader = response.headers.get('Pagination');
          if (paginationHeader) {
            paginatedResult.pagination = JSON.parse(paginationHeader);
          }
          return paginatedResult;
        })
      );
  }

  // Récupère une catégorie par ID
  getCategoryById(id: number): Observable<CategorieProbleme> {
    return this.http.get<CategorieProbleme>(`${this.baseUrl}/${id}`);
  }

  // Ajoute une nouvelle catégorie
  addCategory(category: CategorieProbleme): Observable<CategorieProbleme> {
    return this.http.post<CategorieProbleme>(this.baseUrl, category);
  }

  // Met à jour une catégorie existante
  updateCategory(category: CategorieProbleme): Observable<any> {
    return this.http.put(`${this.baseUrl}/${category.id}`, category);
  }

  // Supprime une catégorie par ID
  deleteCategory(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  // Supprime plusieurs catégories à la fois
  deleteSelectedCategories(ids: number[]): Observable<any> {
    // Utilisation de la méthode http.request afin d'envoyer un body avec la requête DELETE
    return this.http.request('delete', `${this.baseUrl}/deleteMultiple`, { body: ids });
  }
}
