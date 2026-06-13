import { useMemo, useState, type FormEvent } from 'react'
import {
  CLOTHING_CATEGORIES,
  getCategoryLabelPtBr,
  type ClothingCategory,
} from '../../../services/wardrobeApi'
import type { WishlistItem } from '../../../services/wishlistApi'

export type ConvertWishlistItemDialogValues = {
  name: string | null
  category: ClothingCategory | null
  size: string
  brand: string | null
  price: number | null
}

type ConvertWishlistItemDialogProps = {
  item: WishlistItem
  busy?: boolean
  submitError?: string | null
  onCancel: () => void
  onSubmit: (values: ConvertWishlistItemDialogValues) => Promise<void>
}

type FormErrors = {
  size?: string
  price?: string
}

export function ConvertWishlistItemDialog({
  item,
  busy = false,
  submitError,
  onCancel,
  onSubmit,
}: ConvertWishlistItemDialogProps) {
  const [name, setName] = useState(item.name)
  const [category, setCategory] = useState<ClothingCategory>(item.category)
  const [size, setSize] = useState('')
  const [brand, setBrand] = useState(item.brand ?? '')
  const [price, setPrice] = useState(formatEditablePrice(item.targetPrice))
  const [errors, setErrors] = useState<FormErrors>({})

  const categoryOptions = useMemo(
    () =>
      CLOTHING_CATEGORIES.map((value) => ({
        value,
        label: getCategoryLabelPtBr(value),
      })),
    [],
  )

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const nextErrors: FormErrors = {}

    if (!size.trim()) {
      nextErrors.size = 'Tamanho e obrigatorio para conversao.'
    }

    const parsedPrice = parsePrice(price)
    if (price.trim() && parsedPrice === null) {
      nextErrors.price = 'Preco invalido. Use numeros positivos, ex.: 399,90.'
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) {
      return
    }

    await onSubmit({
      name: normalizeOptional(name),
      category,
      size: size.trim(),
      brand: normalizeOptional(brand),
      price: parsedPrice,
    })
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      role="presentation"
      onClick={onCancel}
    >
      <div
        className="w-full max-w-lg rounded-xl border border-stone-200 bg-white p-6 shadow-xl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="convert-dialog-title"
        onClick={(event) => event.stopPropagation()}
      >
        <h3 id="convert-dialog-title" className="mb-4 text-lg font-semibold text-slate-900">
          Converter para guarda-roupa
        </h3>

        <form onSubmit={handleSubmit} noValidate className="grid gap-3 md:grid-cols-2" aria-label="Formulario de conversao da wishlist">
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="convert-name">
          Nome
          <input
            id="convert-name"
            name="name"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={name}
            onChange={(event) => setName(event.target.value)}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="convert-category">
          Categoria
          <select
            id="convert-category"
            name="category"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={category}
            onChange={(event) => setCategory(event.target.value as ClothingCategory)}
          >
            {categoryOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="convert-size">
          Tamanho
          <input
            id="convert-size"
            name="size"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={size}
            onChange={(event) => setSize(event.target.value)}
            placeholder="Ex.: M ou 39"
            required
          />
          {errors.size ? <span className="text-sm text-red-700">{errors.size}</span> : null}
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="convert-brand">
          Marca
          <input
            id="convert-brand"
            name="brand"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={brand}
            onChange={(event) => setBrand(event.target.value)}
            placeholder="Opcional"
          />
        </label>

        <label className="md:col-span-2 flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="convert-price">
          Preco (R$)
          <input
            id="convert-price"
            name="price"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={price}
            onChange={(event) => setPrice(event.target.value)}
            placeholder="Ex.: 499,90"
            inputMode="decimal"
          />
          {errors.price ? <span className="text-sm text-red-700">{errors.price}</span> : null}
        </label>

        {submitError ? <p className="md:col-span-2 text-sm text-red-700">{submitError}</p> : null}

        <div className="md:col-span-2 mt-2 flex flex-wrap gap-2">
          <button
            type="submit"
            disabled={busy}
            className="rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-70"
          >
            {busy ? 'Convertendo...' : 'Confirmar conversao'}
          </button>
          <button
            type="button"
            onClick={onCancel}
            className="rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-800"
          >
            Cancelar
          </button>
        </div>
        </form>
      </div>
    </div>
  )
}

function parsePrice(value: string): number | null {
  if (!value.trim()) {
    return null
  }

  const normalized = value.replace(/\./g, '').replace(',', '.').trim()
  const parsed = Number.parseFloat(normalized)

  if (Number.isNaN(parsed) || parsed < 0) {
    return null
  }

  return Number(parsed.toFixed(2))
}

function normalizeOptional(value: string): string | null {
  const normalized = value.trim()
  return normalized.length > 0 ? normalized : null
}

function formatEditablePrice(value: number) {
  return value.toFixed(2).replace('.', ',')
}
