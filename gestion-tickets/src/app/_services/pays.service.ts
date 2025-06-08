import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { Pays } from '../_models/pays';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PaysService {
  private baseUrl = environment.apiUrl;
  private REST_URL = 'https://restcountries.com/v3.1/all?fields=cca3,name,idd,flags';

  constructor(private http: HttpClient) {}

  // Récupère les pays depuis le backend en gérant les backslashes et le préfixe
  getPays(searchTerm?: string): Observable<Pays[]> {
    const body = { searchTerm: searchTerm || '' };
    return this.http.post<Pays[]>(`${this.baseUrl}pays/getPays`, body).pipe(
      map(paysList =>
        paysList.map(pays => this.normalizeAndPrefix(pays))
      )
    );
  }

  getPaysById(idPays: number): Observable<Pays> {
    return this.http.get<Pays>(`${this.baseUrl}pays/${idPays}`).pipe(
      map(pays => this.normalizeAndPrefix(pays))
    );
  }

  // Convertit n'importe quel fichier en DataURL Base64
  private toBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = error => reject(error);
    });
  }

  // Ajoute un nouveau pays en envoyant la photo encodée en Base64
  async addPays(nom: string, codeTel: string, file: File | string): Promise<Observable<Pays>> {
    let fileBase64: string;
    if (typeof file === 'string') {
      fileBase64 = file; // déjà encodé (ou chaîne vide)
    } else {
      fileBase64 = await this.toBase64(file);
    }
    const body = { nom, codeTel, file: fileBase64 };
    return this.http.post<Pays>(`${this.baseUrl}pays/ajouterPays`, body).pipe(
      map(pays => this.normalizeAndPrefix(pays))
    );
  }

  // Mettez à jour un pays, possibilité de fournir une nouvelle photo
  async updatePays(
    idPays: number,
    paysUpdateDto: { nom: string; codeTel: string },
    file?: File
  ): Promise<Observable<Pays>> {
    let fileBase64: string | undefined;
    if (file) {
      fileBase64 = await this.toBase64(file);
    }
    const body: any = {
      nom: paysUpdateDto.nom,
      codeTel: paysUpdateDto.codeTel,
      file: fileBase64 ?? ''
    };
    return this.http.put<Pays>(`${this.baseUrl}pays/ModifierPays/${idPays}`, body).pipe(
      map(pays => this.normalizeAndPrefix(pays))
    );
  }

  // Supprime un pays
  deletePays(idPays: number): Observable<any> {
    return this.http.get(`${this.baseUrl}pays/supprimerPays/${idPays}`);
  }

  // Renvoie tous les pays du monde via REST Countries
  getAllCountries(): Observable<Pays[]> {
    return this.http.get<any[]>(this.REST_URL).pipe(
      map(list =>
        list.map(item => ({
          idPays: item.cca3,
          nom: item.name.common,
          codeTel: item.idd.root + (item.idd.suffixes?.[0] || ''),
          photoUrl: item.flags?.svg || item.flag
        }))
      )
    );
  }

  // Normalise les backslashes simples en slashs et préfixe l'URL
  private normalizeAndPrefix(pays: Pays & any): Pays {
    let raw = pays.photoUrl ?? '';
  
    // 1) DataURL ?
    if (raw.startsWith('data:')) {
      pays.photoUrl = raw;
      return pays;
    }
  
    // 2) remplace backslashes
    raw = raw.split('\\').join('/');
  
    // 3) URL absolue ?
    if (/^https?:\/\//.test(raw)) {
      pays.photoUrl = raw;
      return pays;
    }
  
    // 4) c’est un fichier dans wwwroot/assets
    const host = environment.assetsUrl.replace(/\/$/, '');
    pays.photoUrl = `${host}/${raw}`;  // -> https://localhost:5001/assets/xxx.jpg
    return pays;
  }  
}
