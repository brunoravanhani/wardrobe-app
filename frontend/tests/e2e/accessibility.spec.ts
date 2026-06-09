import { expect, test } from '@playwright/test'

const apiBase = process.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:9323'

function mockAuth(page: Parameters<typeof test>[1] extends (args: { page: infer P }) => unknown ? P : never) {
  return page.route(`${apiBase}/v1/auth/google/exchange`, (route) =>
    route.fulfill({
      status: 200,
      json: {
        accessToken: 'mock-token',
        expiresAtUtc: new Date(Date.now() + 3600_000).toISOString(),
        user: { userId: crypto.randomUUID(), email: 'user@example.com', displayName: 'User', locale: 'pt-BR' },
      },
    }),
  )
}

test.describe('acessibilidade - guarda-roupa', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/*', async (route) => {
      const url = route.request().url()
      if (!url.startsWith(apiBase)) return route.continue()

      const endpoint = url.slice(apiBase.length)
      if (endpoint.startsWith('/v1/wardrobe-items') && route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] })
      }
      return route.continue()
    })
  })

  test('botoes e links têm nome acessível', async ({ page }) => {
    await page.goto('/')

    const buttons = page.locator('button:visible')
    const count = await buttons.count()
    for (let i = 0; i < count; i++) {
      const btn = buttons.nth(i)
      const name = await btn.getAttribute('aria-label')
      const text = await btn.innerText()
      expect(name || text.trim(), `botão sem nome acessível na posição ${i}`).toBeTruthy()
    }
  })

  test('campos de formulário têm labels associados', async ({ page }) => {
    await page.goto('/')

    // Open the wardrobe item form if there is a creation button
    const addButton = page.locator('button', { hasText: /adicionar|novo|criar/i }).first()
    if (await addButton.isVisible()) {
      await addButton.click()

      const inputs = page.locator('input:visible, select:visible, textarea:visible')
      const inputCount = await inputs.count()
      for (let i = 0; i < inputCount; i++) {
        const input = inputs.nth(i)
        const id = await input.getAttribute('id')
        const ariaLabel = await input.getAttribute('aria-label')
        const ariaLabelledBy = await input.getAttribute('aria-labelledby')

        if (id) {
          const label = page.locator(`label[for="${id}"]`)
          const hasLabel = (await label.count()) > 0
          expect(
            hasLabel || !!ariaLabel || !!ariaLabelledBy,
            `campo de formulário sem label acessível (id: ${id})`,
          ).toBeTruthy()
        } else {
          expect(!!ariaLabel || !!ariaLabelledBy, 'campo de formulário sem label acessível (sem id)').toBeTruthy()
        }
      }
    }
  })

  test('navegação por teclado atinge elementos interativos', async ({ page }) => {
    await page.goto('/')

    await page.keyboard.press('Tab')
    const firstFocused = await page.evaluate(() => document.activeElement?.tagName)
    expect(['A', 'BUTTON', 'INPUT', 'SELECT', 'TEXTAREA', 'SUMMARY']).toContain(firstFocused)
  })

  test('imagens têm texto alternativo', async ({ page }) => {
    await page.goto('/')

    const images = page.locator('img:visible')
    const imgCount = await images.count()
    for (let i = 0; i < imgCount; i++) {
      const img = images.nth(i)
      const alt = await img.getAttribute('alt')
      // alt="" is acceptable for decorative images, but the attribute must be present
      expect(alt, `imagem sem atributo alt na posição ${i}`).not.toBeNull()
    }
  })
})

test.describe('acessibilidade - lista de desejos', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/*', async (route) => {
      const url = route.request().url()
      if (!url.startsWith(apiBase)) return route.continue()

      const endpoint = url.slice(apiBase.length)
      if (endpoint.startsWith('/v1/wishlist-items') && route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: [] })
      }
      return route.continue()
    })
  })

  test('dialogo de conversão tem papel e título corretos', async ({ page }) => {
    const wishlistItems = [
      {
        id: crypto.randomUUID(),
        category: 'TopWear',
        name: 'Camisa Azul',
        brand: null,
        targetPrice: 150,
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
      if (endpoint.startsWith('/v1/wishlist-items') && route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: wishlistItems })
      }
      return route.continue()
    })

    await page.goto('/wishlist')

    const convertButton = page.locator('button', { hasText: /converter|wardrobe/i }).first()
    if (await convertButton.isVisible()) {
      await convertButton.click()

      const dialog = page.locator('[role="dialog"]')
      if (await dialog.isVisible()) {
        await expect(dialog).toBeVisible()
        // Dialog must have an accessible name via aria-label or aria-labelledby
        const ariaLabel = await dialog.getAttribute('aria-label')
        const ariaLabelledBy = await dialog.getAttribute('aria-labelledby')
        expect(ariaLabel || ariaLabelledBy, 'diálogo sem nome acessível').toBeTruthy()
      }
    }
  })

  test('tabs de categorias têm papel tablist e tab corretos', async ({ page }) => {
    await page.goto('/wishlist')

    const tablist = page.locator('[role="tablist"]')
    if (await tablist.isVisible()) {
      const tabs = tablist.locator('[role="tab"]')
      const tabCount = await tabs.count()
      expect(tabCount).toBeGreaterThan(0)

      for (let i = 0; i < tabCount; i++) {
        const tab = tabs.nth(i)
        const text = await tab.innerText()
        expect(text.trim()).toBeTruthy()
      }
    }
  })
})
