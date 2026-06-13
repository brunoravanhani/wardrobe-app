import { expect, test } from '@playwright/test'

const apiBase = process.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:9323'

const CAPSULA_ID = 'a1000000-0000-0000-0000-000000000001'
const TRABALHO_ID = 'a1000000-0000-0000-0000-000000000002'

const TEMPLATES = [
  {
    id: CAPSULA_ID,
    name: 'Capsula',
    slotDefinitions: [
      { id: 'def-1', category: 'TShirt', quantity: 8 },
      { id: 'def-2', category: 'Shirt', quantity: 3 },
      { id: 'def-3', category: 'Pants', quantity: 3 },
      { id: 'def-4', category: 'Shorts', quantity: 3 },
      { id: 'def-5', category: 'Shoes', quantity: 3 },
    ],
  },
  {
    id: TRABALHO_ID,
    name: 'Trabalho',
    slotDefinitions: [
      { id: 'def-6', category: 'Shirt', quantity: 5 },
      { id: 'def-7', category: 'Trousers', quantity: 3 },
      { id: 'def-8', category: 'Shoes', quantity: 1 },
    ],
  },
]

function generateSlots(templateId: string, templateDefs: { category: string; quantity: number }[]) {
  const slots: Array<{
    id: string
    templateId: string
    category: string
    wardrobeItemId: string | null
    wishlistItemId: string | null
    isFulfilled: boolean
    fulfilledAtUtc: string | null
    createdAtUtc: string
  }> = []

  let counter = 0
  for (const def of templateDefs) {
    for (let i = 0; i < def.quantity; i++) {
      counter++
      slots.push({
        id: `slot-${counter}`,
        templateId,
        category: def.category,
        wardrobeItemId: null,
        wishlistItemId: null,
        isFulfilled: false,
        fulfilledAtUtc: null,
        createdAtUtc: new Date(Date.UTC(2026, 5, 12, 0, 0, counter)).toISOString(),
      })
    }
  }

  return slots
}

test('selecionar template Capsula exibe 20 slots agrupados por categoria', async ({ page }) => {
  let activeTemplateId: string | null = null
  let slots: ReturnType<typeof generateSlots> = []
  const wardrobeItems: Array<{ id: string; category: string; name: string; size: string; brand: string | null; price: number | null; bodyImageAssetId: string | null; careTagImageAssetId: string | null }> = []

  await page.route('**/*', async (route) => {
    const request = route.request()
    const url = request.url()

    if (!url.startsWith(apiBase)) {
      await route.continue()
      return
    }

    const method = request.method()
    const endpoint = url.slice(apiBase.length)

    if (endpoint === '/v1/wardrobe-templates' && method === 'GET') {
      await route.fulfill({ status: 200, json: TEMPLATES })
      return
    }

    if (endpoint === '/v1/wardrobe-templates/slots' && method === 'GET') {
      await route.fulfill({ status: 200, json: { activeTemplateId, slots } })
      return
    }

    if (endpoint === `/v1/wardrobe-templates/${CAPSULA_ID}/select` && method === 'POST') {
      activeTemplateId = CAPSULA_ID
      slots = generateSlots(CAPSULA_ID, TEMPLATES[0].slotDefinitions)
      await route.fulfill({ status: 204, body: '' })
      return
    }

    if (endpoint.startsWith('/v1/wardrobe-items') && method === 'GET') {
      await route.fulfill({ status: 200, json: wardrobeItems })
      return
    }

    await route.fulfill({
      status: 500,
      json: { detail: `Rota nao mockada: ${method} ${endpoint}` },
    })
  })

  await page.addInitScript(() => {
    window.localStorage.setItem('virtual-wardrobe/session-token', 'fake-token')
    window.localStorage.setItem('virtual-wardrobe/session-token:email', 'teste@virtualwardrobe.local')
  })

  await page.goto('http://127.0.0.1:4173')

  await expect(page.getByRole('heading', { name: 'Meu Guarda-roupa' })).toBeVisible()
  await expect(page.getByLabel('Template:')).toBeVisible()

  await page.getByLabel('Template:').selectOption({ value: CAPSULA_ID })

  await expect(page.getByText('0 de 20 pecas adquiridas')).toBeVisible()

  // 8 TShirt slots + 3 Shirt + 3 Pants + 3 Shorts + 3 Shoes = 20
  const slotButtons = page.getByRole('button', { name: 'Adicionar a Lista de Desejos' })
  await expect(slotButtons).toHaveCount(20)

  // Category headings appear
  await expect(page.getByRole('heading', { name: 'Camiseta' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Camisa social' })).toBeVisible()
})

test('adicionar peca do guarda-roupa cumpre slot mais antigo', async ({ page }) => {
  let activeTemplateId: string | null = CAPSULA_ID
  let slots = generateSlots(CAPSULA_ID, TEMPLATES[0].slotDefinitions)
  const wardrobeItems: Array<{ id: string; category: string; name: string; size: string; brand: string | null; price: number | null; bodyImageAssetId: string | null; careTagImageAssetId: string | null }> = []

  await page.route('**/*', async (route) => {
    const request = route.request()
    const url = request.url()

    if (!url.startsWith(apiBase)) {
      await route.continue()
      return
    }

    const method = request.method()
    const endpoint = url.slice(apiBase.length)

    if (endpoint === '/v1/wardrobe-templates' && method === 'GET') {
      await route.fulfill({ status: 200, json: TEMPLATES })
      return
    }

    if (endpoint === '/v1/wardrobe-templates/slots' && method === 'GET') {
      await route.fulfill({ status: 200, json: { activeTemplateId, slots } })
      return
    }

    if (endpoint.startsWith('/v1/wardrobe-items') && method === 'GET') {
      await route.fulfill({ status: 200, json: wardrobeItems })
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
        bodyImageAssetId: null,
        careTagImageAssetId: null,
      }
      wardrobeItems.push(created)

      // Simulate auto-fulfillment: fill oldest open slot of matching category
      const openSlot = slots.find((s) => s.category === created.category && !s.isFulfilled)
      if (openSlot) {
        openSlot.wardrobeItemId = created.id
        openSlot.isFulfilled = true
        openSlot.fulfilledAtUtc = new Date().toISOString()
      }

      await route.fulfill({ status: 201, json: created })
      return
    }

    if (endpoint === '/v1/media/upload-url' && method === 'POST') {
      const mediaId = crypto.randomUUID()
      await route.fulfill({
        status: 200,
        json: {
          mediaAssetId: mediaId,
          uploadUrl: `${apiBase}/mock-upload/${mediaId}`,
          expiresAtUtc: new Date().toISOString(),
          requiredHeaders: { 'content-type': 'image/png' },
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
      json: { detail: `Rota nao mockada: ${method} ${endpoint}` },
    })
  })

  await page.addInitScript(() => {
    window.localStorage.setItem('virtual-wardrobe/session-token', 'fake-token')
    window.localStorage.setItem('virtual-wardrobe/session-token:email', 'teste@virtualwardrobe.local')
  })

  await page.goto('http://127.0.0.1:4173')

  await expect(page.getByText('0 de 20 pecas adquiridas')).toBeVisible()

  await page.getByRole('button', { name: 'Nova peca' }).click()
  await page.getByLabel('Nome da peca').fill('Camiseta Azul')
  await page.getByLabel('Tamanho').fill('M')
  await page.getByRole('button', { name: 'Salvar peca' }).click()

  await expect(page.getByText('1 de 20 pecas adquiridas')).toBeVisible()
  await expect(page.getByText('Camiseta Azul')).toBeVisible()
  await expect(page.getByText('Adquirida', { exact: true })).toBeVisible()
})

test('trocar template mostra confirmacao e atualiza slots', async ({ page }) => {
  let activeTemplateId: string | null = CAPSULA_ID
  let slots = generateSlots(CAPSULA_ID, TEMPLATES[0].slotDefinitions)
  const wardrobeItems: Array<{ id: string; category: string; name: string; size: string; brand: string | null; price: number | null; bodyImageAssetId: string | null; careTagImageAssetId: string | null }> = []

  await page.route('**/*', async (route) => {
    const request = route.request()
    const url = request.url()

    if (!url.startsWith(apiBase)) {
      await route.continue()
      return
    }

    const method = request.method()
    const endpoint = url.slice(apiBase.length)

    if (endpoint === '/v1/wardrobe-templates' && method === 'GET') {
      await route.fulfill({ status: 200, json: TEMPLATES })
      return
    }

    if (endpoint === '/v1/wardrobe-templates/slots' && method === 'GET') {
      await route.fulfill({ status: 200, json: { activeTemplateId, slots } })
      return
    }

    if (endpoint === `/v1/wardrobe-templates/${TRABALHO_ID}/select` && method === 'POST') {
      activeTemplateId = TRABALHO_ID
      slots = generateSlots(TRABALHO_ID, TEMPLATES[1].slotDefinitions)
      await route.fulfill({ status: 204, body: '' })
      return
    }

    if (endpoint.startsWith('/v1/wardrobe-items') && method === 'GET') {
      await route.fulfill({ status: 200, json: wardrobeItems })
      return
    }

    await route.fulfill({
      status: 500,
      json: { detail: `Rota nao mockada: ${method} ${endpoint}` },
    })
  })

  await page.addInitScript(() => {
    window.localStorage.setItem('virtual-wardrobe/session-token', 'fake-token')
    window.localStorage.setItem('virtual-wardrobe/session-token:email', 'teste@virtualwardrobe.local')
  })

  await page.goto('http://127.0.0.1:4173')

  await expect(page.getByText('0 de 20 pecas adquiridas')).toBeVisible()

  await page.getByLabel('Template:').selectOption({ value: TRABALHO_ID })

  const confirmDialog = page.getByRole('dialog', { name: 'Trocar template' })
  await expect(confirmDialog).toBeVisible()
  await expect(confirmDialog.getByText(/Trabalho/)).toBeVisible()
  await expect(confirmDialog.getByText(/Capsula/)).toBeVisible()

  await page.getByRole('button', { name: 'Confirmar' }).click()

  await expect(page.getByText('0 de 9 pecas adquiridas')).toBeVisible()

  const slotButtons = page.getByRole('button', { name: 'Adicionar a Lista de Desejos' })
  await expect(slotButtons).toHaveCount(9)

  await expect(page.getByRole('heading', { name: 'Camisa social' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Calca social' })).toBeVisible()
})
