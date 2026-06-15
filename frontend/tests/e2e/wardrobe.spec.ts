import { expect, test } from '@playwright/test'

const apiBase = process.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:9323'

test('cria, edita e filtra pecas do guarda-roupa', async ({ page }) => {
  const items: Array<{
    id: string
    category: string
    name: string
    size: string
    brand: string | null
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

    if (endpoint.startsWith('/v1/wardrobe-items') && method === 'GET') {
      const categoryQuery = new URL(url).searchParams.get('category')
      const filtered = categoryQuery ? items.filter((item) => item.category === categoryQuery) : items
      await route.fulfill({ status: 200, json: filtered })
      return
    }

    if (endpoint === '/v1/wardrobe-items' && method === 'POST') {
      const payload = (await request.postDataJSON()) as Record<string, unknown>
      const created = {
        id: crypto.randomUUID(),
        category: String(payload.category),
        name: String(payload.name),
        size: String(payload.size),
        brand: payload.brand ? String(payload.brand) : null,
        price: typeof payload.price === 'number' ? payload.price : null,
        bodyImageAssetId: payload.bodyImageAssetId ? String(payload.bodyImageAssetId) : null,
        careTagImageAssetId: payload.careTagImageAssetId ? String(payload.careTagImageAssetId) : null,
      }
      items.push(created)
      await route.fulfill({ status: 201, json: created })
      return
    }

    if (endpoint.startsWith('/v1/wardrobe-items/') && method === 'PATCH') {
      const itemId = endpoint.replace('/v1/wardrobe-items/', '')
      const payload = (await request.postDataJSON()) as Record<string, unknown>
      const target = items.find((item) => item.id === itemId)

      if (!target) {
        await route.fulfill({ status: 404, json: { detail: 'Item nao encontrado.' } })
        return
      }

      target.category = String(payload.category)
      target.name = String(payload.name)
      target.size = String(payload.size)
      target.brand = payload.brand ? String(payload.brand) : null
      target.price = typeof payload.price === 'number' ? payload.price : null
      target.bodyImageAssetId = payload.bodyImageAssetId ? String(payload.bodyImageAssetId) : null
      target.careTagImageAssetId = payload.careTagImageAssetId ? String(payload.careTagImageAssetId) : null

      await route.fulfill({ status: 200, json: target })
      return
    }

    if (endpoint === '/v1/wardrobe-templates' && method === 'GET') {
      await route.fulfill({ status: 200, json: [] })
      return
    }

    if (endpoint === '/v1/wardrobe-templates/slots' && method === 'GET') {
      await route.fulfill({ status: 200, json: { activeTemplateId: null, slots: [] } })
      return
    }

    if (endpoint === '/v1/media/upload-url' && method === 'POST') {
      const payload = (await request.postDataJSON()) as {
        fileName: string
        contentType: string
      }
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

  await page.goto('http://127.0.0.1:4173')

  await expect(page.getByRole('heading', { name: 'Meu Guarda-roupa' })).toBeVisible()

  await page.getByRole('button', { name: 'Nova peça' }).click()
  await page.getByLabel('Nome da peca').fill('Camiseta Azul')
  await page.getByLabel('Tamanho').fill('M')
  await page.getByLabel('Marca').fill('Marca X')
  await page.getByLabel('Preco (R$)').fill('129,90')
  await page.getByLabel('Foto da peca (JPG, PNG ou WebP)').setInputFiles({
    name: 'body.png',
    mimeType: 'image/png',
    buffer: Buffer.from(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMBAJ+X2pQAAAAASUVORK5CYII=',
      'base64',
    ),
  })

  await page.getByRole('button', { name: 'Salvar peca' }).click()

  await expect(page.getByText('Camiseta Azul')).toBeVisible()
  await expect(page.getByText('Marca X')).toBeVisible()

  await page.getByRole('button', { name: 'Editar' }).click()
  await page.getByLabel('Nome da peca').fill('Camiseta Azul Editada')
  await page.getByRole('button', { name: 'Salvar alteracoes' }).click()

  await expect(page.getByText('Camiseta Azul Editada')).toBeVisible()

  await page.getByRole('tab', { name: 'Camisa social' }).click()
  await expect(page.getByText('Nenhuma peca encontrada para este filtro.')).toBeVisible()

  await page.getByRole('tab', { name: 'Camiseta' }).click()
  await expect(page.getByText('Camiseta Azul Editada')).toBeVisible()
})
