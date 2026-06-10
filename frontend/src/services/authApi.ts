export type AuthSessionResponse = {
  accessToken: string
  expiresAtUtc: string
  user: {
    userId: string
    email: string
    displayName: string | null
    locale: string
  }
}

type AuthApiClientOptions = {
  baseUrl: string
}

type ApiErrorPayload = {
  title?: string
  detail?: string
  message?: string
}

export function createAuthApi(options: AuthApiClientOptions) {
  const baseUrl = options.baseUrl.replace(/\/$/, '')

  return {
    async exchangeGoogleToken(idToken: string): Promise<AuthSessionResponse> {
      const response = await fetch(`${baseUrl}/v1/auth/google/exchange`, {
        method: 'POST',
        headers: {
          'content-type': 'application/json',
        },
        body: JSON.stringify({ idToken }),
      })

      if (!response.ok) {
        throw new Error(await parseApiError(response))
      }

      return (await response.json()) as AuthSessionResponse
    },
  }
}

async function parseApiError(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as ApiErrorPayload
    return payload.detail ?? payload.message ?? payload.title ?? `Erro HTTP ${response.status}`
  } catch {
    return `Erro HTTP ${response.status}`
  }
}
