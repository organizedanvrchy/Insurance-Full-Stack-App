// The backend is reached through its API gateway (http://localhost:5080 locally):
//   docker compose -f deploy/docker-compose.yml up --build

import { environment } from "../../environments/environment";

export const API_URL = environment.apiUrl;
