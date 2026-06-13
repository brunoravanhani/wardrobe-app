import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ConvertWishlistItemDialog } from '../../../src/features/wishlist/components/ConvertWishlistItemDialog'
import type { WishlistItem } from '../../../src/services/wishlistApi'

const mockItem: WishlistItem = {
  id: 'item-1',
  category: 'Shirt',
  name: 'Camisa casual',
  brand: 'Marca X',
  targetPrice: 150.0,
  inspirationImageAssetId: null,
  links: [],
  status: 'Active',
  purchasedAtUtc: null,
  convertedWardrobeItemId: null,
}

describe('ConvertWishlistItemDialog', () => {
  it('pre-fills name, brand, price, and category from wishlist item', () => {
    render(
      <ConvertWishlistItemDialog item={mockItem} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    expect(screen.getByLabelText('Nome')).toHaveValue('Camisa casual')
    expect(screen.getByLabelText('Marca')).toHaveValue('Marca X')
    expect(screen.getByLabelText('Preco (R$)')).toHaveValue('150,00')
    expect(screen.getByLabelText('Categoria')).toHaveValue('Shirt')
    expect(screen.getByLabelText('Tamanho')).toHaveValue('')
  })

  it('shows validation error when size is empty on submit', async () => {
    const user = userEvent.setup()
    render(
      <ConvertWishlistItemDialog item={mockItem} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    await user.click(screen.getByRole('button', { name: 'Confirmar conversao' }))

    await waitFor(() => {
      expect(screen.getByText('Tamanho e obrigatorio para conversao.')).toBeInTheDocument()
    })
  })

  it('calls onSubmit with mapped values when all required fields are filled', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <ConvertWishlistItemDialog item={mockItem} onCancel={vi.fn()} onSubmit={onSubmit} />,
    )

    await user.type(screen.getByLabelText('Tamanho'), 'M')
    await user.click(screen.getByRole('button', { name: 'Confirmar conversao' }))

    expect(onSubmit).toHaveBeenCalledWith({
      name: 'Camisa casual',
      category: 'Shirt',
      size: 'M',
      brand: 'Marca X',
      price: 150.0,
    })
  })

  it('calls onCancel when cancel button is clicked', async () => {
    const user = userEvent.setup()
    const onCancel = vi.fn()
    render(
      <ConvertWishlistItemDialog item={mockItem} onCancel={onCancel} onSubmit={vi.fn()} />,
    )

    await user.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
