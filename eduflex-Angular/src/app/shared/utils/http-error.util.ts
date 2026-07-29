import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extracts a backend-provided message from a plain Angular HttpErrorResponse — for services that
 * call the API directly via HttpClient rather than through the NSwag-generated Client (see
 * api-error.util.ts for the NSwag ApiException equivalent). ASP.NET's BadRequest(ex.Message)
 * serializes the string straight into the JSON body, so HttpClient parses it as `error` already.
 */
export function extractHttpErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    if (typeof err.error === 'string' && err.error) return err.error;
    if (err.error && typeof err.error === 'object' && typeof err.error.message === 'string') return err.error.message;
  }
  return fallback;
}
