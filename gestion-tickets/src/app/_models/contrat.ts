export interface Contrat {
  id: number;
  dateDebut: string;
  dateFin?: string;
  type: string;
  societePartenaireId?: number;
  clientId?: number;
}
