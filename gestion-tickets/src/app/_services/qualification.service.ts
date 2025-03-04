import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Qualification } from '../_models/qualification.model';

@Injectable({
  providedIn: 'root'
})
export class QualificationService {
  private baseUrl = 'https://localhost:5001/api/qualifications'; // à adapter

  constructor(private http: HttpClient) { }

  getQualifications(): Observable<Qualification[]> {
    return this.http.get<Qualification[]>(this.baseUrl);
  }
}
