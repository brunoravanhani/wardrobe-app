export const CLOTHING_CATEGORIES = [
  'TShirt',
  'Shirt',
  'Pants',
  'Trousers',
  'Shorts',
  'Coats',
  'Shoes',
  'Polo',
  'Accessories',
] as const

export type ClothingCategory = (typeof CLOTHING_CATEGORIES)[number]

const CATEGORY_LABELS_PT_BR: Record<ClothingCategory, string> = {
  TShirt: 'Camiseta',
  Shirt: 'Camisa social',
  Pants: 'Calça',
  Trousers: 'Calça social',
  Shorts: 'Bermuda',
  Coats: 'Casacos',
  Shoes: 'Calçados',
  Polo: 'Polo',
  Accessories: 'Acessórios',
}

export function getCategoryLabelPtBr(category: ClothingCategory): string {
  return CATEGORY_LABELS_PT_BR[category]
}

const NUMERIC_TO_CATEGORY: Record<number, ClothingCategory> = {
  1: 'TShirt',
  2: 'Shirt',
  3: 'Pants',
  4: 'Trousers',
  5: 'Shorts',
  6: 'Coats',
  7: 'Shoes',
  8: 'Polo',
  9: 'Accessories',
}

export function coerceCategoryString(raw: ClothingCategory | number): ClothingCategory {
  if (typeof raw === 'number') return NUMERIC_TO_CATEGORY[raw] ?? CLOTHING_CATEGORIES[0]
  return raw
}

export type WardrobeItem = {
  id: string
  category: ClothingCategory
  name: string
  brand: string | null
  size: string
  price: number | null
  bodyImageAssetId: string | null
  careTagImageAssetId: string | null
}

export type UpsertWardrobeItemInput = {
  category: ClothingCategory
  name: string
  size: string
  brand?: string | null
  price?: number | null
  bodyImageAssetId?: string | null
  careTagImageAssetId?: string | null
}

export type UploadPurpose = 'WardrobeBodyImage' | 'WardrobeCareTagImage'

export type CreateUploadUrlRequest = {
  file: File
  purpose: UploadPurpose
}

export type CreateUploadUrlResponse = {
  mediaAssetId: string
  uploadUrl: string
  expiresAtUtc: string
  requiredHeaders: Record<string, string>
}

type ApiErrorPayload = {
  title?: string
  detail?: string
  message?: string
}

type WardrobeApiClientOptions = {
  baseUrl: string
  getAccessToken: () => string | null
}

export type WardrobeApiClient = ReturnType<typeof createWardrobeApi>

export function createWardrobeApi(options: WardrobeApiClientOptions) {
  const baseUrl = options.baseUrl.replace(/\/$/, '')

  async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
    const token = options.getAccessToken()
    const response = await fetch(`${baseUrl}${path}`, {
      ...init,
      headers: {
        'content-type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(init?.headers ?? {}),
      },
    })

    if (!response.ok) {
      throw new Error(await parseApiError(response))
    }

    if (response.status === 204) {
      return undefined as T
    }

    return (await response.json()) as T
  }

  return {
    listWardrobeItems(category?: ClothingCategory) {
      const search = category ? `?category=${encodeURIComponent(category)}` : ''
      return requestJson<WardrobeItem[]>(`/v1/wardrobe-items${search}`)
    },

    createWardrobeItem(input: UpsertWardrobeItemInput) {
      return requestJson<WardrobeItem>('/v1/wardrobe-items', {
        method: 'POST',
        body: JSON.stringify(toApiPayload(input)),
      })
    },

    updateWardrobeItem(itemId: string, input: UpsertWardrobeItemInput) {
      return requestJson<WardrobeItem>(`/v1/wardrobe-items/${itemId}`, {
        method: 'PATCH',
        body: JSON.stringify(toApiPayload(input)),
      })
    },

    deleteWardrobeItem(itemId: string) {
      return requestJson<void>(`/v1/wardrobe-items/${itemId}`, {
        method: 'DELETE',
      })
    },

    createUploadUrl(input: CreateUploadUrlRequest) {
      return requestJson<CreateUploadUrlResponse>('/v1/media/upload-url', {
        method: 'POST',
        body: JSON.stringify({
          fileName: input.file.name,
          contentType: input.file.type,
          fileSizeBytes: input.file.size,
          purpose: input.purpose,
        }),
      })
    },

    async uploadFileToPresignedUrl(uploadUrl: string, file: File, requiredHeaders: Record<string, string>) {
      const response = await fetch(uploadUrl, {
        method: 'PUT',
        body: file,
        headers: requiredHeaders,
      })

      if (!response.ok) {
        throw new Error('Falha ao enviar imagem para o armazenamento privado.')
      }
    },
  }
}

function toApiPayload(input: UpsertWardrobeItemInput) {
  return {
    category: input.category,
    name: input.name,
    size: input.size,
    brand: normalizeText(input.brand),
    price: typeof input.price === 'number' ? input.price : null,
    bodyImageAssetId: input.bodyImageAssetId ?? null,
    careTagImageAssetId: input.careTagImageAssetId ?? null,
  }
}

function normalizeText(value?: string | null) {
  if (!value) {
    return null
  }

  const normalized = value.trim()
  return normalized.length > 0 ? normalized : null
}

async function parseApiError(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as ApiErrorPayload
    return payload.detail ?? payload.message ?? payload.title ?? `Erro HTTP ${response.status}`
  } catch {
    return `Erro HTTP ${response.status}`
  }
}
