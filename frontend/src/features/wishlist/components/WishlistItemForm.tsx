import { useMemo, useState, type FormEvent } from 'react'
import {
  CLOTHING_CATEGORIES,
  getCategoryLabelPtBr,
  type ClothingCategory,
} from '../../../services/wardrobeApi'
import type { WishlistLink } from '../../../services/wishlistApi'

const MAX_IMAGE_SIZE_BYTES = 10 * 1024 * 1024
const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])

export type WishlistItemFormValues = {
  category: ClothingCategory
  name: string
  brand: string | null
  targetPrice: number
  links: WishlistLink[]
  inspirationImageFile: File | null
}

export type WishlistItemFormInitialValues = {
  category: ClothingCategory
  name: string
  brand: string | null
  targetPrice: number
  links: WishlistLink[]
}

type WishlistItemFormProps = {
  mode: 'create' | 'edit'
  initialValues?: WishlistItemFormInitialValues
  busy?: boolean
  submitError?: string | null
  onCancel: () => void
  onSubmit: (values: WishlistItemFormValues) => Promise<void>
}

type LinkRow = { url: string; label: string }

type FormErrors = {
  name?: string
  targetPrice?: string
  inspirationImageFile?: string
  links?: string
}

const defaultValues: WishlistItemFormInitialValues = {
  category: 'TShirt',
  name: '',
  brand: null,
  targetPrice: 0,
  links: [],
}

function toLinkRows(links: WishlistLink[]): LinkRow[] {
  if (links.length === 0) return [{ url: '', label: '' }]
  return links.map((l) => ({ url: l.url, label: l.label ?? '' }))
}

export function WishlistItemForm({
  mode,
  initialValues,
  busy = false,
  submitError,
  onCancel,
  onSubmit,
}: WishlistItemFormProps) {
  const values = useMemo(() => {
    return initialValues ?? defaultValues
  }, [initialValues])

  const [category, setCategory] = useState<ClothingCategory>(values.category)
  const [name, setName] = useState(values.name)
  const [brand, setBrand] = useState(values.brand ?? '')
  const [targetPrice, setTargetPrice] = useState(values.targetPrice > 0 ? formatEditablePrice(values.targetPrice) : '')
  const [linkRows, setLinkRows] = useState<LinkRow[]>(() => toLinkRows(values.links))
  const [inspirationImageFile, setInspirationImageFile] = useState<File | null>(null)
  const [errors, setErrors] = useState<FormErrors>({})

  const title = mode === 'create' ? 'Novo item da wishlist' : 'Editar item da wishlist'
  const submitLabel = mode === 'create' ? 'Salvar desejo' : 'Salvar alteracoes'

  const categoryOptions = useMemo(
    () =>
      CLOTHING_CATEGORIES.map((item) => ({
        value: item,
        label: getCategoryLabelPtBr(item),
      })),
    [],
  )

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const nextErrors: FormErrors = {}

    if (!name.trim()) {
      nextErrors.name = 'Nome do item e obrigatorio.'
    }

    const parsedPrice = parsePrice(targetPrice)
    if (parsedPrice === null) {
      nextErrors.targetPrice = 'Preco alvo obrigatorio. Use numeros positivos, ex.: 299,90.'
    } else if (parsedPrice < 0) {
      nextErrors.targetPrice = 'Preco alvo nao pode ser negativo.'
    }

    const filledRows = linkRows.filter((r) => r.url.trim().length > 0)
    const linkValidationError = validateLinkRows(filledRows)
    if (linkValidationError) {
      nextErrors.links = linkValidationError
    }

    const imageError = validateImage(inspirationImageFile)
    if (imageError) {
      nextErrors.inspirationImageFile = imageError
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0 || parsedPrice === null) {
      return
    }

    await onSubmit({
      category,
      name: name.trim(),
      brand: brand.trim() ? brand.trim() : null,
      targetPrice: parsedPrice,
      links: filledRows.map((r) => ({ url: r.url.trim(), label: r.label.trim() || null })),
      inspirationImageFile,
    })
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-xl border border-slate-300 bg-white p-4 shadow-sm"
      aria-label="Formulario de item da wishlist"
    >
      <h3 className="mb-4 text-lg font-semibold text-slate-900">{title}</h3>

      <div className="grid gap-3 md:grid-cols-2">
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="wishlist-category">
          Categoria
          <select
            id="wishlist-category"
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

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="wishlist-name">
          Nome do item
          <input
            id="wishlist-name"
            name="name"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Ex.: Jaqueta jeans"
            required
          />
          {errors.name ? <span className="text-sm text-red-700">{errors.name}</span> : null}
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="wishlist-brand">
          Marca
          <input
            id="wishlist-brand"
            name="brand"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={brand}
            onChange={(event) => setBrand(event.target.value)}
            placeholder="Opcional"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="wishlist-targetPrice">
          Preco alvo (R$)
          <input
            id="wishlist-targetPrice"
            name="targetPrice"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={targetPrice}
            onChange={(event) => setTargetPrice(event.target.value)}
            placeholder="Ex.: 299,90"
            inputMode="decimal"
            required
          />
          {errors.targetPrice ? <span className="text-sm text-red-700">{errors.targetPrice}</span> : null}
        </label>
      </div>

      <fieldset className="mt-3">
        <legend className="mb-1 text-sm font-medium text-slate-800">Links externos</legend>
        <div className="flex flex-col gap-2">
          {linkRows.map((row, index) => (
            <div key={index} className="flex gap-2">
              <input
                type="url"
                aria-label={`URL do link ${index + 1}`}
                className="min-w-0 flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm"
                value={row.url}
                onChange={(event) => {
                  const next = [...linkRows]
                  next[index] = { ...next[index], url: event.target.value }
                  setLinkRows(next)
                }}
                placeholder="https://loja.exemplo/item"
              />
              <input
                type="text"
                aria-label={`Etiqueta do link ${index + 1}`}
                className="w-36 rounded-md border border-slate-300 px-3 py-2 text-sm"
                value={row.label}
                onChange={(event) => {
                  const next = [...linkRows]
                  next[index] = { ...next[index], label: event.target.value }
                  setLinkRows(next)
                }}
                placeholder="Ex.: Ver na Loja"
                maxLength={80}
              />
              {linkRows.length > 1 ? (
                <button
                  type="button"
                  aria-label={`Remover link ${index + 1}`}
                  onClick={() => setLinkRows(linkRows.filter((_, i) => i !== index))}
                  className="rounded-md border border-slate-300 px-2 py-1 text-sm text-slate-600 hover:bg-stone-50"
                >
                  ×
                </button>
              ) : null}
            </div>
          ))}
        </div>
        <button
          type="button"
          onClick={() => setLinkRows([...linkRows, { url: '', label: '' }])}
          className="mt-2 text-sm text-amber-700 hover:underline"
        >
          + Adicionar link
        </button>
        {errors.links ? <span className="mt-1 block text-sm text-red-700">{errors.links}</span> : null}
      </fieldset>

      <label className="mt-3 flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="wishlist-inspirationImageFile">
        Imagem de inspiracao (JPG, PNG ou WebP)
        <input
          id="wishlist-inspirationImageFile"
          name="inspirationImageFile"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          onChange={(event) => setInspirationImageFile(event.target.files?.[0] ?? null)}
        />
        {errors.inspirationImageFile ? <span className="text-sm text-red-700">{errors.inspirationImageFile}</span> : null}
      </label>

      {submitError ? <p className="mt-3 text-sm text-red-700">{submitError}</p> : null}

      <div className="mt-5 flex flex-wrap gap-2">
        <button
          type="submit"
          disabled={busy}
          className="rounded-md bg-amber-700 px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-70"
        >
          {busy ? 'Salvando...' : submitLabel}
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
  )
}

function parsePrice(value: string): number | null {
  if (!value.trim()) {
    return null
  }

  const normalized = value.replace(/\./g, '').replace(',', '.').trim()
  const parsed = Number.parseFloat(normalized)

  if (Number.isNaN(parsed)) {
    return null
  }

  return Number(parsed.toFixed(2))
}

function formatEditablePrice(value: number) {
  return value.toFixed(2).replace('.', ',')
}

function validateLinkRows(rows: LinkRow[]): string | null {
  for (const row of rows) {
    try {
      const parsed = new URL(row.url.trim())
      if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
        return 'Use apenas links com http:// ou https://.'
      }
    } catch {
      return 'Informe uma URL valida em cada linha de link.'
    }
  }

  return null
}

function validateImage(file: File | null): string | null {
  if (!file) {
    return null
  }

  if (!ALLOWED_IMAGE_TYPES.has(file.type)) {
    return 'Formato invalido. Use somente JPG, PNG ou WebP.'
  }

  if (file.size > MAX_IMAGE_SIZE_BYTES) {
    return 'Arquivo acima do limite de 10 MB.'
  }

  return null
}
