import { Injectable } from '@angular/core';
import { HttpClient} from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ClientDto } from '../DTOs/ClientDto';
import { ClientParams } from '../DTOs/ClientParams';
import { RegisterClientDto } from '../DTOs/RegisterClientDto';
import { PaginatedResult } from '../_models/pagination';
import { environment } from 'src/environments/environment';
import { Ticket } from '../_models/ticket';
import { Projet } from '../_models/Projet';


@Injectable({
  providedIn: 'root'
})
export class ClientService {
  private baseUrl = environment.apiUrl+ "clients";

  constructor(private http: HttpClient) {}

  register(dto: RegisterClientDto): Observable<ClientDto> {
    return this.http.post<ClientDto>(`${this.baseUrl}/register`, dto);
  }

  validateClient(dto: RegisterClientDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/validateClient`, dto);
  }

  getAll(): Observable<ClientDto[]> {
    return this.http.get<ClientDto[]>(this.baseUrl);
  }

  getPaged(params: ClientParams)
: Observable<PaginatedResult<ClientDto[]>> {
  return this.http.post<ClientDto[]>(`${this.baseUrl}/paged`, params, { observe: 'response' })
    .pipe(
      map(resp => {
        const paginatedResult = new PaginatedResult<ClientDto[]>();
        // Ici resp.body est déjà un ClientDto[] comprenant `projets: ProjetMiniDto[]`
        const clients = resp.body!;

        // (Optionnel) si vous voulez un champ projetsIds pour le template
        const enriched: ClientDto[] = clients.map(c => ({
          ...c,
          // ajoute une propriété calculée
          projetsIds: c.projets.map(p => p.id)
        }));

        paginatedResult.items = enriched;
        const header = resp.headers.get('Pagination');
        if (header) paginatedResult.pagination = JSON.parse(header);
        return paginatedResult;
      })
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
  getClientTickets(clientId: number): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(`${this.baseUrl}/${clientId}/tickets`);
  }
  getClientProjects(clientId: number): Observable<Projet[]> {
    return this.http.get<Projet[]>(`${this.baseUrl}/${clientId}/projects`);
  }

  addClientToProject(clientId: number, projectId: number): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/${clientId}/projects/${projectId}`,
      {}
    );
  }
  
  detachProjectFromClient(clientId: number, projectId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/${clientId}/projects/${projectId}`
    );
  }
}