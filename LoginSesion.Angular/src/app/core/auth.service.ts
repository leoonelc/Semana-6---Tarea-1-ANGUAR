import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, map, of, switchMap, tap } from 'rxjs';

import { LoginRequest, PerfilResponse, SessionResponse } from './auth.models';

const API_URL = 'http://localhost:5222/api';
const STORAGE_KEY = 'usuarioNombre';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessionSignal = signal<SessionResponse | null>(null);

  readonly session = this.sessionSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.sessionSignal()?.authenticated === true);
  readonly storedUserName = signal(localStorage.getItem(STORAGE_KEY) ?? '');

  prepareCsrf() {
    return this.http.get(`${API_URL}/auth/csrf`);
  }

  login(credentials: LoginRequest) {
    return this.prepareCsrf().pipe(
      switchMap(() => this.http.post<SessionResponse>(`${API_URL}/auth/login`, credentials)),
      tap((session) => this.saveSession(session))
    );
  }

  validateSession() {
    return this.http.get<SessionResponse>(`${API_URL}/auth/session`).pipe(
      tap((session) => this.saveSession(session)),
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      })
    );
  }

  loadPerfil() {
    return this.http.get<PerfilResponse>(`${API_URL}/perfil`);
  }

  logout() {
    return this.prepareCsrf().pipe(
      switchMap(() => this.http.post(`${API_URL}/auth/logout`, {})),
      tap(() => this.clearSession()),
      catchError(() => {
        this.clearSession();
        return of(null);
      })
    );
  }

  private saveSession(session: SessionResponse) {
    this.sessionSignal.set(session);
    localStorage.setItem(STORAGE_KEY, session.nombre);
    this.storedUserName.set(session.nombre);
  }

  clearSession() {
    this.sessionSignal.set(null);
    localStorage.removeItem(STORAGE_KEY);
    this.storedUserName.set('');
  }
}
