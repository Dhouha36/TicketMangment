import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Priorite } from '../_models/priorite.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PrioriteService {
  private baseUrl = environment.apiUrl + '/priorites'; // à adapter

  constructor(private http: HttpClient) { }

  getPriorites(): Observable<Priorite[]> {
    return this.http.get<Priorite[]>(this.baseUrl);
  }
}
