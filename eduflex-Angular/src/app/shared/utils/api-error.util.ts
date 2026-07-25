interface ApiExceptionLike {
  message: string;
  status: number;
  response: string;
  headers: { [key: string]: any };
  result: any;
}

function isApiException(err: unknown): err is ApiExceptionLike {
  return typeof err === 'object' && err !== null && (err as any).isApiException === true;
}

/**
 * Extracts the backend's message from an NSwag ApiException's raw response body.
 * Falls back to the given message if err isn't an ApiException or has no body.
 */
export function extractApiErrorMessage(err: unknown, fallback: string): string {
  if (isApiException(err) && err.response) {
    try {
      return JSON.parse(err.response);
    } catch {
      return err.response;
    }
  }
  return fallback;
}
