export interface ContractCreate {
  dateDebut: string;
  dateFin:   string;
  type:      string;
}

export interface SocieteCreate {
  nom:       string;
  adresse:   string;
  telephone: string;
  paysId:    number;
  ville:     string;
  codePostal:string;
}