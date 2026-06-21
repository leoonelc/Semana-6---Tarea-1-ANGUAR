import { HttpInterceptorFn } from '@angular/common/http';

function readCookie(name: string): string | null {
  const match = document.cookie
    .split('; ')
    .find((cookie) => cookie.startsWith(`${name}=`));

  return match ? decodeURIComponent(match.split('=')[1]) : null;
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const xsrfToken = readCookie('XSRF-TOKEN');
  const headers = xsrfToken ? request.headers.set('X-XSRF-TOKEN', xsrfToken) : request.headers;

  return next(
    request.clone({
      headers,
      withCredentials: true
    })
  );
};
