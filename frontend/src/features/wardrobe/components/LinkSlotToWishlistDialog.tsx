import { useState } from 'react'
import { getCategoryLabelPtBr, type ClothingCategory } from '../../../services/wardrobeApi'
import type { LinkSlotToWishlistInput, TemplateSlot } from '../../../services/wardrobeTemplatesApi'

type FormErrors = {
  name?: string
  targetPrice?: string
}

type LinkSlotToWishlistDialogProps = {
  slot: TemplateSlot
  busy: boolean
  submitError?: string | null
  onCancel: () => void
  onSubmit: (input: LinkSlotToWishlistInput) => Promise<void>
}

export function LinkSlotToWishlistDialog({
  slot,
  busy,
  submitError,
  onCancel,
  onSubmit,
}: LinkSlotToWishlistDialogProps) {
  const [name, setName] = useState('')
  const [brand, setBrand] = useState('')
  const [price, setPrice] = useState('')
  const [errors, setErrors] = useState<FormErrors>({})
  const categoryLabel = getCategoryLabelPtBr(slot.category as ClothingCategory)

  function stopPropagation(e: React.MouseEvent) {
    e.stopPropagation()
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const nextErrors: FormErrors = {}

    if (!name.trim()) {
      nextErrors.name = 'Nome é obrigatório.'
    }

    const parsedPrice = parsePrice(price)
    if (parsedPrice === null || isNaN(parsedPrice) || parsedPrice <= 0) {
      nextErrors.targetPrice = 'Preço deve ser maior que zero.'
    }

    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors)
      return
    }

    setErrors({})
    await onSubmit({
      name: name.trim(),
      brand: brand.trim() || null,
      targetPrice: parsedPrice!,
    })
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50"
      onClick={onCancel}
    >
      <div
        className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="link-slot-dialog-title"
        onClick={stopPropagation}
      >
        <h2 id="link-slot-dialog-title" className="mb-1 text-xl font-semibold text-slate-900">
          Adicionar à Lista de Desejos
        </h2>
        <p className="mb-5 text-sm text-slate-500">Categoria: {categoryLabel}</p>

        <form onSubmit={(e) => void handleSubmit(e)} noValidate>
          <div className="grid gap-3">
            <div>
              <label htmlFor="link-slot-name" className="mb-1 block text-sm font-medium text-slate-700">
                Nome <span aria-hidden="true">*</span>
              </label>
              <input
                id="link-slot-name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none focus:ring-1 focus:ring-amber-500"
                placeholder="Ex.: Camiseta branca"
              />
              {errors.name && (
                <span className="mt-1 block text-sm text-red-700">{errors.name}</span>
              )}
            </div>

            <div>
              <label htmlFor="link-slot-brand" className="mb-1 block text-sm font-medium text-slate-700">
                Marca
              </label>
              <input
                id="link-slot-brand"
                type="text"
                value={brand}
                onChange={(e) => setBrand(e.target.value)}
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none focus:ring-1 focus:ring-amber-500"
                placeholder="Ex.: Nike"
              />
            </div>

            <div>
              <label htmlFor="link-slot-price" className="mb-1 block text-sm font-medium text-slate-700">
                Preço desejado (R$) <span aria-hidden="true">*</span>
              </label>
              <input
                id="link-slot-price"
                type="text"
                inputMode="decimal"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none focus:ring-1 focus:ring-amber-500"
                placeholder="0,00"
              />
              {errors.targetPrice && (
                <span className="mt-1 block text-sm text-red-700">{errors.targetPrice}</span>
              )}
            </div>
          </div>

          {submitError && <p className="mt-4 text-sm text-red-700">{submitError}</p>}

          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              onClick={onCancel}
              className="rounded-md border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-stone-50"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={busy}
              className="rounded-md bg-amber-700 px-4 py-2 text-sm font-medium text-white hover:bg-amber-800 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {busy ? 'Adicionando...' : 'Adicionar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

function parsePrice(value: string): number | null {
  const normalized = value.replace(',', '.')
  const parsed = Number.parseFloat(normalized)
  return isNaN(parsed) ? null : parsed
}
