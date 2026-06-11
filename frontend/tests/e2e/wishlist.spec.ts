import { expect, test } from '@playwright/test'

const apiBase = process.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:9323'

test('cria, edita e alterna entre wishlist ativa e historico', async ({ page }) => {
  const items: Array<{
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
      const filtered = includePurchased ? items : items.filter((item) => item.status !== 'Purchased')
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
      items.push(created)
      await route.fulfill({ status: 201, json: created })
      return
    }

    if (endpoint.startsWith('/v1/wishlist-items/') && method === 'PATCH') {
      const itemId = endpoint.replace('/v1/wishlist-items/', '')
      const payload = (await request.postDataJSON()) as Record<string, unknown>
      const target = items.find((item) => item.id === itemId)

      if (!target) {
        await route.fulfill({ status: 404, json: { detail: 'Item nao encontrado.' } })
        return
      }

      target.category = String(payload.category)
      target.name = String(payload.name)
      target.brand = payload.brand ? String(payload.brand) : null
      target.targetPrice = Number(payload.targetPrice)
      target.inspirationImageAssetId = payload.inspirationImageAssetId ? String(payload.inspirationImageAssetId) : null
      target.links = Array.isArray(payload.links) ? payload.links.map((value) => String(value)) : []

      await route.fulfill({ status: 200, json: target })
      return
    }

    if (endpoint.startsWith('/v1/wishlist-items/') && endpoint.endsWith('/mark-purchased') && method === 'POST') {
      const itemId = endpoint.replace('/v1/wishlist-items/', '').replace('/mark-purchased', '')
      const target = items.find((item) => item.id === itemId)

      if (!target) {
        await route.fulfill({ status: 404, json: { detail: 'Item nao encontrado.' } })
        return
      }

      target.status = 'Purchased'
      target.purchasedAtUtc = new Date().toISOString()
      await route.fulfill({ status: 200, json: target })
      return
    }

    if (endpoint === '/v1/media/upload-url' && method === 'POST') {
      const payload = (await request.postDataJSON()) as { contentType: string }
      const mediaId = crypto.randomUUID()
      await route.fulfill({
        status: 200,
        json: {
          mediaAssetId: mediaId,
          uploadUrl: `${apiBase}/mock-upload/${mediaId}`,
          expiresAtUtc: new Date().toISOString(),
          requiredHeaders: {
            'content-type': payload.contentType,
          },
        },
      })
      return
    }

    if (endpoint.startsWith('/mock-upload/') && method === 'PUT') {
      await route.fulfill({ status: 200, body: '' })
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

  await expect(page.getByRole('heading', { name: 'Minha Wishlist', exact: true })).toBeVisible()

  await page.getByRole('button', { name: 'Novo desejo' }).click()
  await page.getByLabel('Nome do item').fill('Jaqueta Jeans')
  await page.getByLabel('Preco alvo (R$)').fill('299,90')
  await page.getByLabel('Links externos').fill('https://loja.exemplo/jaqueta')
  await page.getByLabel('Imagem de inspiracao (JPG, PNG ou WebP)').setInputFiles({
    name: 'insp.png',
    mimeType: 'image/png',
    buffer: Buffer.from(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMBAJ+X2pQAAAAASUVORK5CYII=',
      'base64',
    ),
  })

  await page.getByRole('button', { name: 'Salvar desejo' }).click()

  await expect(page.getByText('Jaqueta Jeans')).toBeVisible()
  await expect(page.getByText('R$ 299,90')).toBeVisible()

  await page.getByRole('button', { name: 'Editar' }).click()
  await page.getByLabel('Nome do item').fill('Jaqueta Jeans Premium')
  await page.getByRole('button', { name: 'Salvar alteracoes' }).click()

  await expect(page.getByText('Jaqueta Jeans Premium')).toBeVisible()

  await page.getByRole('button', { name: 'Marcar como comprado' }).click()

  await expect(page.getByText('Nenhum item encontrado nesta visualizacao.')).toBeVisible()

  await page.getByRole('tab', { name: 'Historico' }).click()
  await expect(page.getByText('Jaqueta Jeans Premium')).toBeVisible()
})
