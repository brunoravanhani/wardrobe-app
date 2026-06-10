import { expect, test } from '@playwright/test'

test('recupera rascunhos nao salvos no formulario da wishlist', async ({ page }) => {
  await page.addInitScript(() => {
    window.localStorage.setItem('virtual-wardrobe/session-token', 'fake-token')
    window.localStorage.setItem('virtual-wardrobe/session-token:email', 'teste@virtualwardrobe.local')
  })

  await page.goto('http://127.0.0.1:4173/wishlist')

  await page.getByRole('button', { name: 'Novo desejo' }).click()
  await page.getByLabel('Nome do item').fill('Tenis Branco')
  await page.getByLabel('Preco alvo (R$)').fill('399,90')

  await page.reload()

  await page.getByRole('button', { name: 'Novo desejo' }).click()
  await expect(page.getByLabel('Nome do item')).toHaveValue('Tenis Branco')
  await expect(page.getByLabel('Preco alvo (R$)')).toHaveValue('399,90')
})

test('recupera rascunhos nao salvos no formulario do guarda-roupa', async ({ page }) => {
  await page.addInitScript(() => {
    window.localStorage.setItem('virtual-wardrobe/session-token', 'fake-token')
    window.localStorage.setItem('virtual-wardrobe/session-token:email', 'teste@virtualwardrobe.local')
  })

  await page.goto('http://127.0.0.1:4173')

  await page.getByRole('button', { name: 'Nova peca' }).click()
  await page.getByLabel('Nome da peca').fill('Camisa Linho')
  await page.getByLabel('Preco (R$)').fill('249,90')

  await page.reload()

  await page.getByRole('button', { name: 'Nova peca' }).click()
  await expect(page.getByLabel('Nome da peca')).toHaveValue('Camisa Linho')
  await expect(page.getByLabel('Preco (R$)')).toHaveValue('249,90')
})
