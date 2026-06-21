import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginPage {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly demoAccounts = [
    { usuario: 'maria', password: '123456', nombre: 'Maria Camila', badge: 'MC' },
    { usuario: 'admin', password: 'admin123', nombre: 'Administrador', badge: 'AD' },
    { usuario: 'leonel', password: 'leonel123', nombre: 'Leonel Castillo', badge: 'LC' }
  ];

  readonly form = this.fb.nonNullable.group({
    usuario: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  fillDemo(usuario: string, password: string) {
    this.form.setValue({ usuario, password });
    this.error.set('');
  }

  submit() {
    this.error.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (error) => {
        this.loading.set(false);
        this.error.set(
          error.status === 401
            ? 'Usuario o contrasena incorrectos.'
            : 'No fue posible iniciar sesion. Intente nuevamente.'
        );
      }
    });
  }
}
