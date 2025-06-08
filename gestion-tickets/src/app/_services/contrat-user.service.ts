import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "src/environments/environment";
import { ContratUser } from "../_models/contrat-user";

@Injectable({
  providedIn: 'root'
})
export class ContratUserService {
  private baseUrl = `${environment.apiUrl}ContratUsers`;

  constructor(private http: HttpClient) {}


  updateContrat(dto: ContratUser): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${dto.id}`, dto);
  }

  /**
   * Récupère un contrat par son ID
   */
  getContratById(id: number): Observable<ContratUser> {
    return this.http.get<ContratUser>(`${this.baseUrl}/${id}`);
  }

  /**
   * Crée un nouveau contrat utilisateur
   */
  createContrat(dto: Omit<ContratUser, 'id'>): Observable<ContratUser> {
    return this.http.post<ContratUser>(this.baseUrl, dto);
  }

  /**
   * Supprime un contrat utilisateur
   */
  deleteContrat(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}