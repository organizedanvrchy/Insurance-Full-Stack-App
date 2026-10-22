import { HttpErrorResponse } from '@angular/common/http';

/** Turns whatever the API sent back (ProblemDetails / validation errors) into one readable string. */
export function getErrorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) return 'Cannot reach the server. Is the API running?';

    const body = err.error;
    // ASP.NET validation failure (400): { errors: { Email: ["..."], Password: ["..."] } }
    if (body?.errors && typeof body.errors === 'object') {
      return (Object.values(body.errors) as string[][]).flat().join(' ');
    }
    // ProblemDetails from our 401 / 409 responses
    if (body?.title) return body.title;
  }
  return 'Something went wrong. Please try again.';
}
