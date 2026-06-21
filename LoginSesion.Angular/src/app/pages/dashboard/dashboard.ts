import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { PerfilResponse } from '../../core/auth.models';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardPage implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly perfil = signal<PerfilResponse | null>(null);
  readonly error = signal('');
  readonly storedUserName = this.authService.storedUserName;

  ngOnInit() {
    this.authService.loadPerfil().subscribe({
      next: (perfil) => this.perfil.set(perfil),
      error: () => this.error.set('La sesion expiro o no es valida. Vuelva a iniciar sesion.')
    });
  }

  logout() {
    this.authService.logout().subscribe(() => this.router.navigateByUrl('/login'));
  }
}
