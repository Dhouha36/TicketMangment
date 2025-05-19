export interface ClientUpdateDto {
  id:            number;
  email:         string;
  firstName:     string;
  lastName:      string;
  numTelephone:  string;
  paysId:        number;   // le code du pays (ex. `"1"`)
  actif:         boolean;
  societeId:     number;
  nouveauPassword?:          string;
  confirmNouveauPassword?:   string;
}
