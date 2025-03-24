// status.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StatutDesTicket } from '../_models/statut-des-ticket.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StatusService {
  private apiUrl = environment.apiUrl + '/statutDesTickets'; // Remplacez par l'URL de votre API

  constructor(private http: HttpClient) {}

  getStatuses(): Observable<StatutDesTicket[]> {
    return this.http.get<StatutDesTicket[]>(this.apiUrl);
  }

  
}
