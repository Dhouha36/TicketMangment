import { ContratProjet } from "../_models/contrat-projet";
import { TypeContrat } from "./type-contrat.enum";

export interface ProjetCreate {
  nom: string;
  description?: string;
  chefProjetId: number;
  societeId: number;
  contratProjet?: Omit<ContratProjet, 'id' | 'projetId'>;
}
