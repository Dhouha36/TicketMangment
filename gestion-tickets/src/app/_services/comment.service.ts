import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Comment } from '../_models/comment';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CommentService {
  private baseUrl = environment.apiUrl+"commentaires";
  constructor(private http: HttpClient) {}

  // Récupérer les commentaires associés à un ticket
  getCommentsByTicket(ticketId: number): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.baseUrl}/ticket/${ticketId}`);
  }

  // Ajouter un nouveau commentaire
  addComment(commentData: { contenu: string; ticketId: number }): Observable<Comment> {
    return this.http.post<Comment>(this.baseUrl, commentData);
  }
  addCommentFormData(formData: FormData): Observable<Comment> {
    return this.http.post<Comment>(`${this.baseUrl}`, formData);
  }
  
}
