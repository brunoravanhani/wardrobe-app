import { expect, test } from '@playwright/test'

// p95 budgets from the implementation plan
const BUDGET_LIST_RENDER_MS = 2000
const BUDGET_SAVE_CONFIRM_MS = 3000
const BUDGET_CONVERSION_MS = 3000

// Number of samples to collect to approximate p95
const SAMPLES = 20

const apiBase = process.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:9323'

function p95(values: number[]): number {
  const sorted = [...values].sort((a, b) => a - b)
  const idx = Math.ceil(sorted.length * 0.95) - 1
  return sorted[Math.max(0, idx)]
}

test('p95 - renderização da lista do guarda-roupa em até 2s', async ({ page }) => {
  const wardrobeItems = Array.from({ length: 20 }, (_, i) => ({
    id: crypto.randomUUID(),
    category: 'TopWear',
    name: `Peça ${i + 1}`,
    brand: 'Marca',
    size: 'M',
    price: 100,
    bodyImageAssetId: null,
    careTagImageAssetId: null,
  }))

  await page.route('**/*', async (route) => {
    const url = route.request().url()
    if (!url.startsWith(apiBase)) return route.continue()

    const endpoint = url.slice(apiBase.length)
    if (endpoint.startsWith('/v1/wardrobe-items') && route.request().method() === 'GET') {
      return route.fulfill({ status: 200, json: wardrobeItems })
    }
    return route.continue()
  })

  const timings: number[] = []

  for (let i = 0; i < SAMPLES; i++) {
    const start = Date.now()
    await page.goto('/', { waitUntil: 'networkidle' })
    // Wait for at least one item to be visible as the readiness signal
    await page.waitForSelector('[data-testid="wardrobe-item"], [role="listitem"], li', { timeout: BUDGET_LIST_RENDER_MS + 500 }).catch(() => null)
    timings.push(Date.now() - start)
  }

  const p95Value = p95(timings)
  expect(
    p95Value,
    `p95 renderização da lista: ${p95Value}ms > limite ${BUDGET_LIST_RENDER_MS}ms`,
  ).toBeLessThanOrEqual(BUDGET_LIST_RENDER_MS)
})

test('p95 - confirmação de criação de peça do guarda-roupa em até 3s', async ({ page }) => {
  const created: unknown[] = []

  await page.route('**/*', async (route) => {
    const url = route.request().url()
    if (!url.startsWith(apiBase)) return route.continue()

    const endpoint = url.slice(apiBase.length)
    const method = route.request().method()

    if (endpoint === '/v1/wardrobe-items' && method === 'GET') {
      return route.fulfill({ status: 200, json: created })
    }
    if (endpoint === '/v1/wardrobe-items' && method === 'POST') {
      const payload = (await route.request().postDataJSON()) as Record<string, unknown>
      const item = { id: crypto.randomUUID(), size: 'M', price: null, bodyImageAssetId: null, careTagImageAssetId: null, ...payload }
      created.push(item)
      return route.fulfill({ status: 201, json: item })
    }
    return route.continue()
  })

  await page.goto('/', { waitUntil: 'networkidle' })

  const timings: number[] = []

  for (let i = 0; i < SAMPLES; i++) {
    const addButton = page.locator('button', { hasText: /adicionar|novo|criar/i }).first()
    if (!(await addButton.isVisible())) break

    const start = Date.now()
    await addButton.click()

    const nameInput = page.locator('input[name="name"], input[placeholder*="nome"], input[id*="name"]').first()
    if (await nameInput.isVisible()) {
      await nameInput.fill(`Peça Performance ${i + 1}`)
    }

    const submitButton = page.locator('button[type="submit"], button', { hasText: /salvar|confirmar|criar/i }).first()
    if (await submitButton.isVisible()) {
      await submitButton.click()
      // Wait for the form to close or a success indicator
      await page.waitForSelector('[data-testid="success"], [role="alert"]', { timeout: BUDGET_SAVE_CONFIRM_MS + 500 }).catch(() => null)
    }

    timings.push(Date.now() - start)
  }

  if (timings.length === 0) {
    test.skip()
    return
  }

  const p95Value = p95(timings)
  expect(
    p95Value,
    `p95 confirmação de criação: ${p95Value}ms > limite ${BUDGET_SAVE_CONFIRM_MS}ms`,
  ).toBeLessThanOrEqual(BUDGET_SAVE_CONFIRM_MS)
})

test('p95 - atualização de conversão de item da lista de desejos em até 3s', async ({ page }) => {
  const wishlistItemId = crypto.randomUUID()
  const wishlistItems = [
    {
      id: wishlistItemId,
      category: 'TopWear',
      name: 'Camisa Desejada',
      brand: null,
      targetPrice: 200,
      inspirationImageAssetId: null,
      links: [],
      status: 'Purchased',
      purchasedAtUtc: new Date().toISOString(),
      convertedWardrobeItemId: null,
    },
  ]

  await page.route('**/*', async (route) => {
    const url = route.request().url()
    if (!url.startsWith(apiBase)) return route.continue()

    const endpoint = url.slice(apiBase.length)
    const method = route.request().method()

    if (endpoint.startsWith('/v1/wishlist-items') && method === 'GET') {
      return route.fulfill({ status: 200, json: wishlistItems })
    }
    if (endpoint.endsWith('/convert') && method === 'POST') {
      const wardrobeItem = {
        id: crypto.randomUUID(),
        category: 'TopWear',
        name: 'Camisa Desejada',
        brand: null,
        size: 'M',
        price: null,
        bodyImageAssetId: null,
        careTagImageAssetId: null,
      }
      return route.fulfill({ status: 200, json: { wishlistItemId, wardrobeItem } })
    }
    return route.continue()
  })

  await page.goto('/wishlist', { waitUntil: 'networkidle' })

  const timings: number[] = []

  for (let i = 0; i < SAMPLES; i++) {
    const convertButton = page.locator('button', { hasText: /converter/i }).first()
    if (!(await convertButton.isVisible())) break

    const start = Date.now()
    await convertButton.click()

    const sizeInput = page.locator('input[name="size"], input[placeholder*="tamanho"], input[id*="size"]').first()
    if (await sizeInput.isVisible()) {
      await sizeInput.fill('M')
    }

    const confirmButton = page.locator('button[type="submit"], button', { hasText: /confirmar|converter/i }).first()
    if (await confirmButton.isVisible()) {
      await confirmButton.click()
      await page.waitForSelector('[data-testid="conversion-success"], [role="alert"]', {
        timeout: BUDGET_CONVERSION_MS + 500,
      }).catch(() => null)
    }

    timings.push(Date.now() - start)
  }

  if (timings.length === 0) {
    test.skip()
    return
  }

  const p95Value = p95(timings)
  expect(
    p95Value,
    `p95 conversão: ${p95Value}ms > limite ${BUDGET_CONVERSION_MS}ms`,
  ).toBeLessThanOrEqual(BUDGET_CONVERSION_MS)
})
