import type { ClothingCategory } from './wardrobeApi'

export type WardrobeTemplate = {
  id: string
  name: string
  slotDefinitions: TemplateSlotDefinition[]
}

export type TemplateSlotDefinition = {
  id: string
  category: ClothingCategory
  quantity: number
}

export type TemplateSlot = {
  id: string
  templateId: string
  category: ClothingCategory
  wardrobeItemId: string | null
  wishlistItemId: string | null
  isFulfilled: boolean
  fulfilledAtUtc: string | null
  createdAtUtc: string
}

export type UserSlotsData = {
  activeTemplateId: string | null
  slots: TemplateSlot[]
}

export type LinkSlotToWishlistInput = {
  name: string
  brand?: string | null
  targetPrice: number
}

export type LinkedWishlistItem = {
  id: string
  category: ClothingCategory
  name: string
  brand: string | null
  targetPrice: number
}

export type TemplatesApiClient = ReturnType<typeof createTemplatesApi>

type ApiErrorPayload = {
  title?: string
  detail?: string
  message?: string
}

type TemplatesApiClientOptions = {
  baseUrl: string
  getAccessToken: () => string | null
}

export function createTemplatesApi(options: TemplatesApiClientOptions) {
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
    listTemplates() {
      return requestJson<WardrobeTemplate[]>('/v1/wardrobe-templates')
    },

    getUserSlots() {
      return requestJson<UserSlotsData>('/v1/wardrobe-templates/slots')
    },

    selectTemplate(templateId: string) {
      return requestJson<void>(`/v1/wardrobe-templates/${templateId}/select`, {
        method: 'POST',
      })
    },

    linkSlotToWishlist(slotId: string, input: LinkSlotToWishlistInput) {
      return requestJson<LinkedWishlistItem>(`/v1/wardrobe-templates/slots/${slotId}/link-wishlist`, {
        method: 'POST',
        body: JSON.stringify({
          name: input.name.trim(),
          brand: normalizeText(input.brand),
          targetPrice: input.targetPrice,
        }),
      })
    },
  }
}

function normalizeText(value?: string | null) {
  if (!value) return null
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
