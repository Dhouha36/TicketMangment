import { TypeContrat } from "./type-contrat.enum";

export interface RegisterUserDto {
  firstname:    string;
  lastname:     string;
  email:        string;
  numtelephone: string;
  pays:         number;
  role:         string;     
  actif:        boolean;
  contract?: {
    dateDebut: string;        // ISO 8601
    dateFin?:   string;       // ISO 8601 ou null
    type:      TypeContrat;   // 'CDD' | 'CDI' | 'Projet'
    projetId?: number;        // si type === 'Projet'
  };
}