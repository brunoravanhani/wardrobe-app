import {
  CLOTHING_CATEGORIES,
  type WardrobeItem,
  type ClothingCategory,
} from './wardrobeApi'

export type WishlistItemStatus = 'Active' | 'Purchased'

export type WishlistLink = {
  url: string
  label: string | null
}

export type WishlistItem = {
  id: string
  category: ClothingCategory
  name: string
  brand: string | null
  targetPrice: number
  inspirationImageAssetId: string | null
  links: WishlistLink[]
  status: WishlistItemStatus
  purchasedAtUtc: string | null
  convertedWardrobeItemId: string | null
}

export type UpsertWishlistItemInput = {
  category: ClothingCategory
  name: string
  brand?: string | null
  targetPrice: number
  inspirationImageAssetId?: string | null
  links: WishlistLink[]
}

export type WishlistApiClient = ReturnType<typeof createWishlistApi>

export type ConvertWishlistItemInput = {
  name?: string | null
  category?: ClothingCategory | null
  size: string
  brand?: string | null
  price?: number | null
  bodyImageAssetId?: string | null
  careTagImageAssetId?: string | null
}

export type WishlistConversionResult = {
  wishlistItemId: string
  wardrobeItem: WardrobeItem
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

type WishlistApiClientOptions = {
  baseUrl: string
  getAccessToken: () => string | null
}

const WISHLIST_INSPIRATION_PURPOSE = 'WishlistInspirationImage'

export function createWishlistApi(options: WishlistApiClientOptions) {
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
    listWishlistItems(includePurchased = false) {
      const search = includePurchased ? '?includePurchased=true' : ''
      return requestJson<WishlistItem[]>(`/v1/wishlist-items${search}`)
    },

    createWishlistItem(input: UpsertWishlistItemInput) {
      return requestJson<WishlistItem>('/v1/wishlist-items', {
        method: 'POST',
        body: JSON.stringify(toApiPayload(input)),
      })
    },

    updateWishlistItem(itemId: string, input: UpsertWishlistItemInput) {
      return requestJson<WishlistItem>(`/v1/wishlist-items/${itemId}`, {
        method: 'PATCH',
        body: JSON.stringify(toApiPayload(input)),
      })
    },

    deleteWishlistItem(itemId: string) {
      return requestJson<void>(`/v1/wishlist-items/${itemId}`, {
        method: 'DELETE',
      })
    },

    markAsPurchased(itemId: string) {
      return requestJson<WishlistItem>(`/v1/wishlist-items/${itemId}/mark-purchased`, {
        method: 'POST',
      })
    },

    convertToWardrobe(itemId: string, input: ConvertWishlistItemInput) {
      return requestJson<WishlistConversionResult>(`/v1/wishlist-items/${itemId}/convert`, {
        method: 'POST',
        body: JSON.stringify({
          name: normalizeText(input.name),
          category: input.category ?? null,
          size: input.size.trim(),
          brand: normalizeText(input.brand),
          price: typeof input.price === 'number' ? input.price : null,
          bodyImageAssetId: input.bodyImageAssetId ?? null,
          careTagImageAssetId: input.careTagImageAssetId ?? null,
        }),
      })
    },

    createInspirationUploadUrl(file: File) {
      return requestJson<CreateUploadUrlResponse>('/v1/media/upload-url', {
        method: 'POST',
        body: JSON.stringify({
          fileName: file.name,
          contentType: file.type,
          fileSizeBytes: file.size,
          purpose: WISHLIST_INSPIRATION_PURPOSE,
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

function toApiPayload(input: UpsertWishlistItemInput) {
  return {
    category: input.category,
    name: input.name,
    brand: normalizeText(input.brand),
    targetPrice: input.targetPrice,
    inspirationImageAssetId: input.inspirationImageAssetId ?? null,
    links: input.links,
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

export function isClothingCategory(value: unknown): value is ClothingCategory {
  return typeof value === 'string' && CLOTHING_CATEGORIES.includes(value as ClothingCategory)
}
