type MediaApiClientOptions = {
  baseUrl: string
  getAccessToken: () => string | null
}

type ViewUrlResponse = {
  url: string
  expiresAtUtc: string
}

export type MediaApiClient = ReturnType<typeof createMediaApi>

export function createMediaApi(options: MediaApiClientOptions) {
  const baseUrl = options.baseUrl.replace(/\/$/, '')

  return {
    async createViewUrl(mediaAssetId: string): Promise<string> {
      const token = options.getAccessToken()
      const response = await fetch(`${baseUrl}/v1/media/${mediaAssetId}/view-url`, {
        method: 'POST',
        headers: {
          'content-type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
      })

      if (!response.ok) {
        throw new Error(`Falha ao obter URL de visualizacao (HTTP ${response.status}).`)
      }

      const payload = (await response.json()) as ViewUrlResponse
      return payload.url
    },
  }
}
