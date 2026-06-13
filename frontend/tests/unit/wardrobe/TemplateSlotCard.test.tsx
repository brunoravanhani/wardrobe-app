import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { TemplateSlotCard } from '../../../src/features/wardrobe/components/TemplateSlotCard'
import type { TemplateSlot } from '../../../src/services/wardrobeTemplatesApi'

const openSlot: TemplateSlot = {
  id: 'slot-1',
  templateId: 'template-1',
  category: 'TShirt',
  wardrobeItemId: null,
  wishlistItemId: null,
  isFulfilled: false,
  fulfilledAtUtc: null,
  createdAtUtc: '2026-06-12T00:00:00Z',
}

const fulfilledSlot: TemplateSlot = {
  id: 'slot-2',
  templateId: 'template-1',
  category: 'Shirt',
  wardrobeItemId: 'item-99',
  wishlistItemId: null,
  isFulfilled: true,
  fulfilledAtUtc: '2026-06-12T10:00:00Z',
  createdAtUtc: '2026-06-12T00:00:00Z',
}

describe('TemplateSlotCard', () => {
  it('renders category label in pt-BR when unfulfilled', () => {
    render(<TemplateSlotCard slot={openSlot} onLinkWishlist={vi.fn()} />)
    expect(screen.getByText('Camiseta')).toBeInTheDocument()
  })

  it('renders "Adicionar a Lista de Desejos" button when unfulfilled', () => {
    render(<TemplateSlotCard slot={openSlot} onLinkWishlist={vi.fn()} />)
    expect(screen.getByRole('button', { name: 'Adicionar a Lista de Desejos' })).toBeInTheDocument()
  })

  it('calls onLinkWishlist with the slot when button is clicked', async () => {
    const user = userEvent.setup()
    const onLinkWishlist = vi.fn()
    render(<TemplateSlotCard slot={openSlot} onLinkWishlist={onLinkWishlist} />)
    await user.click(screen.getByRole('button', { name: 'Adicionar a Lista de Desejos' }))
    expect(onLinkWishlist).toHaveBeenCalledWith(openSlot)
  })

  it('renders wardrobe item name when fulfilled', () => {
    render(
      <TemplateSlotCard slot={fulfilledSlot} wardrobeItemName="Camisa Social" onLinkWishlist={vi.fn()} />,
    )
    expect(screen.getByText('Camisa Social')).toBeInTheDocument()
  })

  it('renders category label in pt-BR when fulfilled', () => {
    render(
      <TemplateSlotCard slot={fulfilledSlot} wardrobeItemName="Camisa Social" onLinkWishlist={vi.fn()} />,
    )
    expect(screen.getByText('Camisa social')).toBeInTheDocument()
  })

  it('does not render "Adicionar a Lista de Desejos" button when fulfilled', () => {
    render(
      <TemplateSlotCard slot={fulfilledSlot} wardrobeItemName="Camisa Social" onLinkWishlist={vi.fn()} />,
    )
    expect(screen.queryByRole('button', { name: 'Adicionar a Lista de Desejos' })).not.toBeInTheDocument()
  })
})
