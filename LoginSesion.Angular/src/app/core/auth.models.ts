export interface LoginRequest {
  usuario: string;
  password: string;
}

export interface SessionResponse {
  authenticated: boolean;
  usuario: string;
  nombre: string;
  expiresAt?: string | null;
}

export interface PerfilResponse {
  usuario: string;
  nombre: string;
  roles: string[];
  mensaje: string;
}
