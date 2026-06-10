import { expect, test } from '@playwright/test'

const apiBase = process.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:9323'

test('marca item como comprado e converte para o guarda-roupa', async ({ page }) => {
  const wishlistItems: Array<{
    id: string
    category: string
    name: string
    brand: string | null
    targetPrice: number
    inspirationImageAssetId: string | null
    links: string[]
    status: 'Active' | 'Purchased'
    purchasedAtUtc: string | null
    convertedWardrobeItemId: string | null
  }> = []

  const wardrobeItems: Array<{
    id: string
    category: string
    name: string
    brand: string | null
    size: string
    price: number | null
    bodyImageAssetId: string | null
    careTagImageAssetId: string | null
  }> = []

  await page.route('**/*', async (route) => {
    const request = route.request()
    const url = request.url()

    if (!url.startsWith(apiBase)) {
      await route.continue()
      return
    }

    const method = request.method()
    const endpoint = url.slice(apiBase.length)

    if (endpoint.startsWith('/v1/wishlist-items') && method === 'GET') {
      const includePurchased = new URL(url).searchParams.get('includePurchased') === 'true'
      const filtered = includePurchased ? wishlistItems : wishlistItems.filter((item) => item.status !== 'Purchased')
      await route.fulfill({ status: 200, json: filtered })
      return
    }

    if (endpoint === '/v1/wishlist-items' && method === 'POST') {
      const payload = (await request.postDataJSON()) as Record<string, unknown>
      const created = {
        id: crypto.randomUUID(),
        category: String(payload.category),
        name: String(payload.name),
        brand: payload.brand ? String(payload.brand) : null,
        targetPrice: Number(payload.targetPrice),
        inspirationImageAssetId: payload.inspirationImageAssetId ? String(payload.inspirationImageAssetId) : null,
        links: Array.isArray(payload.links) ? payload.links.map((value) => String(value)) : [],
        status: 'Active' as const,
        purchasedAtUtc: null,
        convertedWardrobeItemId: null,
      }

      wishlistItems.push(created)
      await route.fulfill({ status: 201, json: created })
      return
    }

    if (endpoint.startsWith('/v1/wishlist-items/') && endpoint.endsWith('/mark-purchased') && method === 'POST') {
      const itemId = endpoint.replace('/v1/wishlist-items/', '').replace('/mark-purchased', '')
      const target = wishlistItems.find((item) => item.id === itemId)

      if (!target) {
        await route.fulfill({ status: 404, json: { detail: 'Item nao encontrado.' } })
        return
      }

      target.status = 'Purchased'
      target.purchasedAtUtc = new Date().toISOString()
      await route.fulfill({ status: 200, json: target })
      return
    }

    if (endpoint.startsWith('/v1/wishlist-items/') && endpoint.endsWith('/convert') && method === 'POST') {
      const itemId = endpoint.replace('/v1/wishlist-items/', '').replace('/convert', '')
      const payload = (await request.postDataJSON()) as Record<string, unknown>
      const target = wishlistItems.find((item) => item.id === itemId)

      if (!target) {
        await route.fulfill({ status: 404, json: { detail: 'Item nao encontrado.' } })
        return
      }

      if (!target.convertedWardrobeItemId) {
        const wardrobe = {
          id: crypto.randomUUID(),
          category: String(payload.category ?? target.category),
          name: String(payload.name ?? target.name),
          brand: payload.brand ? String(payload.brand) : target.brand,
          size: String(payload.size),
          price: typeof payload.price === 'number' ? Number(payload.price) : target.targetPrice,
          bodyImageAssetId: payload.bodyImageAssetId ? String(payload.bodyImageAssetId) : target.inspirationImageAssetId,
          careTagImageAssetId: payload.careTagImageAssetId ? String(payload.careTagImageAssetId) : null,
        }

        wardrobeItems.push(wardrobe)
        target.convertedWardrobeItemId = wardrobe.id
      }

      const converted = wardrobeItems.find((item) => item.id === target.convertedWardrobeItemId)

      await route.fulfill({
        status: 200,
        json: {
          wishlistItemId: target.id,
          wardrobeItem: converted,
        },
      })
      return
    }

    if (endpoint.startsWith('/v1/wardrobe-items') && method === 'GET') {
      await route.fulfill({ status: 200, json: wardrobeItems })
      return
    }

    await route.fulfill({
      status: 500,
      json: { detail: `Rota nao mockada no teste: ${method} ${endpoint}` },
    })
  })

  await page.addInitScript(() => {
    window.localStorage.setItem('virtual-wardrobe/session-token', 'fake-token')
    window.localStorage.setItem('virtual-wardrobe/session-token:email', 'teste@virtualwardrobe.local')
  })

  await page.goto('http://127.0.0.1:4173/wishlist')

  await page.getByRole('button', { name: 'Novo desejo' }).click()
  await page.getByLabel('Nome do item').fill('Bota de couro')
  await page.getByLabel('Preco alvo (R$)').fill('499,90')
  await page.getByLabel('Links externos').fill('https://loja.exemplo/bota')
  await page.getByRole('button', { name: 'Salvar desejo' }).click()

  await expect(page.getByText('Bota de couro')).toBeVisible()

  await page.getByRole('button', { name: 'Marcar como comprado' }).click()
  await page.getByRole('tab', { name: 'Historico' }).click()

  await expect(page.getByText('Bota de couro')).toBeVisible()
  await page.getByRole('button', { name: 'Converter para guarda-roupa' }).click()

  await expect(page.getByRole('heading', { name: 'Converter para guarda-roupa' })).toBeVisible()
  await page.getByLabel('Tamanho').fill('39')
  await page.getByRole('button', { name: 'Confirmar conversao' }).click()

  await expect(page.getByText('Convertido para guarda-roupa com sucesso.')).toBeVisible()

  await page.getByRole('link', { name: 'Guarda-roupa' }).click()
  await expect(page.getByText('Bota de couro')).toBeVisible()
  await expect(page.getByText('39')).toBeVisible()
})