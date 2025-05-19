import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Contrat } from '../_models/contrat';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class ContratService {
  baseUrl = environment.apiUrl+"contrat";
  constructor(private http: HttpClient) {}

  getContract(id: number): Observable<Contrat> {
    return this.http.get<Contrat>(`${this.baseUrl}/${id}`);
  }

  updateContract(id: number, contract: Contrat): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, contract);
  }

  addContract(contract: Contrat): Observable<Contrat> {
    return this.http.post<Contrat>(this.baseUrl, contract);
  }
}
