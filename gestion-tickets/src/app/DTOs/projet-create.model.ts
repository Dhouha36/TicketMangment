import { TypeContrat } from "./type-contrat.enum";

export interface ProjetCreate {
  nom: string;
  description?: string;
  chefProjetId: number;
  societeId: number;
  contract?: {
    dateDebut: string;        // ISO 8601
    dateFin?:   string;       // ISO 8601 ou null
    type:      TypeContrat;   // 'CDD' | 'CDI' | 'Projet'
    projetId?: number;        // si type === 'Projet'
  };
}
