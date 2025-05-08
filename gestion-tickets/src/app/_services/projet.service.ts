import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { Projet } from '../_models/Projet';
import { ProjetMember } from '../_models/projet-member';
import { PaginatedResult, Pagination } from '../_models/pagination';
import { environment } from '../../environments/environment';
import { ProjetCreate } from '../DTOs/projet-create.model';

@Injectable({
  providedIn: 'root'
})
export class ProjetService {
  private baseUrl = environment.apiUrl+"projets"; 

  paginatedResult: PaginatedResult<Projet[]> | null = null;
  constructor(private http: HttpClient) { }

  // Récupérer tous les projets
  getProjets(filters: any): Observable<Projet[]> {
    return this.http.post<Projet[]>(`${this.baseUrl}/search`, filters);
  }

  // Méthode pour récupérer les projets paginés
  getPaginatedProjets(
    pageNumber: number,
    pageSize: number,
    searchTerm?: string,
    extraFilters?: any
  ): Observable<PaginatedResult<Projet[]>> {
    const params = {
      pageNumber,
      pageSize,
      searchTerm: searchTerm ? searchTerm : '',
      ...extraFilters
    };
  
    return this.http.post<any>(this.baseUrl + '/paged', params, { observe: 'response' })
      .pipe(
        map((response: HttpResponse<Projet[]>) => {
          const paginationHeader = response.headers.get('Pagination');
          const paginatedResult: PaginatedResult<Projet[]> = {
            items: response.body || [],
            pagination: paginationHeader ? JSON.parse(paginationHeader) : {} as Pagination
          };
          return paginatedResult;
        })
      );
  }
  
  // Récupérer un projet par ID
  getProjetById(id: number): Observable<Projet> {
    return this.http.get<Projet>(`${this.baseUrl}/${id}`);
  }

  // Ajouter un nouveau projet
  addProjet(payload: ProjetCreate): Observable<Projet> {
    return this.http.post<Projet>(`${this.baseUrl}/ajouterProjet`, payload);
  }

  // Mettre à jour un projet
  updateProjet(projet: Projet): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/modifierProjet/${projet.id}`, projet);
  }


  // Supprimer un projet
  deleteProjet(id: number): Observable<void> {
    return this.http.get<void>(`${this.baseUrl}/supprimerProjet/${id}`);
  }

  deleteSelectedProjets(ids: number[]): Observable<any> {
    // On utilise http.request pour pouvoir envoyer un body avec la méthode DELETE
    return this.http.request('get', `${this.baseUrl}/supprimerProjets`, { body: ids });
  }

  // Ajouter un utilisateur à un projet
  ajouterUtilisateurAuProjet(projetId: number, userId: number, role: string): Observable<string> {
    const body = { userId: userId, role: role };
    return this.http.post(`${this.baseUrl}/${projetId}/utilisateurs`, body, { responseType: 'text' });
  }  

  // Récupérer les membres d'un projet
  getMembresProjet(projetId: number): Observable<ProjetMember[]> {
    return this.http.get<ProjetMember[]>(`${this.baseUrl}/membres/${projetId}`);
  }

  getUserProjets(): Observable<Projet[]> {
    return this.http.get<Projet[]>(`${this.baseUrl}/user`);
  }

  // Assigner ou mettre à jour le rôle d'un utilisateur dans un projet
  assignerRole(projetId: number, userId: number, role: string): Observable<any> {
    // Ici, selon votre API, vous pouvez soit utiliser un endpoint dédié (par exemple assigner-role)
    // ou réutiliser l'endpoint d'ajout d'utilisateur. Dans cet exemple, on suppose un endpoint dédié.
    const body = { projetId, userId, role };
    return this.http.post<any>(`${this.baseUrl}/assigner-role`, null, {
      params: { projetId: projetId.toString(), userId: userId.toString(), role }
    });
  }

  // Supprimer un utilisateur d'un projet
  supprimerUtilisateurDuProjet(projetId: number, userId: number): Observable<void> {
    return this.http.get<void>(`${this.baseUrl}/delete/${projetId}/utilisateurs/${userId}`);
  }


  deleteSelectedProjectMembers(projetId: number, userIds: number[]): Observable<any> {
    // On utilise http.request('delete') pour envoyer un body avec la méthode DELETE.
    return this.http.request('get', `${this.baseUrl}/supprimerUtilisateursDuProjet`, {
      body: { projetId, userIds }
    });
  }

  getProjetsBySocieteId(societeId: number): Observable<Projet[]> {
    return this.http.get<Projet[]>(`${this.baseUrl}/societe/${societeId}`);
  }

  exportProjets(filters: any): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/export`, filters, { responseType: 'blob' });
  }

}
