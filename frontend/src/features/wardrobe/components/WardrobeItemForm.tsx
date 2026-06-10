import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  CLOTHING_CATEGORIES,
  getCategoryLabelPtBr,
  type ClothingCategory,
} from '../../../services/wardrobeApi'
import { useDraftState } from '../../../app/providers/DraftStateProvider'

const MAX_IMAGE_SIZE_BYTES = 10 * 1024 * 1024
const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])

export type WardrobeItemFormValues = {
  category: ClothingCategory
  name: string
  size: string
  brand: string | null
  price: number | null
  bodyImageFile: File | null
  careTagImageFile: File | null
}

export type WardrobeItemFormInitialValues = {
  category: ClothingCategory
  name: string
  size: string
  brand: string | null
  price: number | null
}

type WardrobeItemFormProps = {
  mode: 'create' | 'edit'
  initialValues?: WardrobeItemFormInitialValues
  draftStorageKey?: string
  busy?: boolean
  submitError?: string | null
  onCancel: () => void
  onSubmit: (values: WardrobeItemFormValues) => Promise<void>
}

type FormErrors = {
  category?: string
  name?: string
  size?: string
  price?: string
  bodyImageFile?: string
  careTagImageFile?: string
}

const defaultValues: WardrobeItemFormInitialValues = {
  category: 'TShirt',
  name: '',
  size: '',
  brand: null,
  price: null,
}

export function WardrobeItemForm({
  mode,
  initialValues,
  draftStorageKey,
  busy = false,
  submitError,
  onCancel,
  onSubmit,
}: WardrobeItemFormProps) {
  const draftState = useDraftState()
  const values = useMemo(() => {
    if (initialValues) {
      return initialValues
    }

    if (!draftStorageKey) {
      return defaultValues
    }

    const rawDraft = draftState.readDraft(draftStorageKey)
    if (!rawDraft) {
      return defaultValues
    }

    try {
      const parsed = JSON.parse(rawDraft) as Partial<WardrobeItemFormInitialValues>
      return {
        category: isCategory(parsed.category) ? parsed.category : defaultValues.category,
        name: typeof parsed.name === 'string' ? parsed.name : defaultValues.name,
        size: typeof parsed.size === 'string' ? parsed.size : defaultValues.size,
        brand: typeof parsed.brand === 'string' || parsed.brand === null ? parsed.brand : defaultValues.brand,
        price: typeof parsed.price === 'number' || parsed.price === null ? parsed.price : defaultValues.price,
      }
    } catch {
      return defaultValues
    }
  }, [draftState, draftStorageKey, initialValues])

  const [category, setCategory] = useState<ClothingCategory>(values.category)
  const [name, setName] = useState(values.name)
  const [size, setSize] = useState(values.size)
  const [brand, setBrand] = useState(values.brand ?? '')
  const [price, setPrice] = useState(values.price !== null ? formatEditablePrice(values.price) : '')
  const [bodyImageFile, setBodyImageFile] = useState<File | null>(null)
  const [careTagImageFile, setCareTagImageFile] = useState<File | null>(null)
  const [errors, setErrors] = useState<FormErrors>({})

  useEffect(() => {
    if (!draftStorageKey || mode !== 'create') {
      return
    }

    draftState.writeDraft(
      draftStorageKey,
      JSON.stringify({
        category,
        name,
        size,
        brand: brand.trim() ? brand.trim() : null,
        price: parsePrice(price),
      }),
    )
  }, [brand, category, draftState, draftStorageKey, mode, name, price, size])

  const title = mode === 'create' ? 'Nova peca do guarda-roupa' : 'Editar peca do guarda-roupa'

  const submitLabel = mode === 'create' ? 'Salvar peca' : 'Salvar alteracoes'

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
      nextErrors.name = 'Nome da peca e obrigatorio.'
    }

    if (!size.trim()) {
      nextErrors.size = 'Tamanho e obrigatorio.'
    }

    const parsedPrice = parsePrice(price)
    if (price.trim().length > 0 && parsedPrice === null) {
      nextErrors.price = 'Preco invalido. Use numeros positivos, ex.: 199,90.'
    }

    if (parsedPrice !== null && parsedPrice < 0) {
      nextErrors.price = 'Preco nao pode ser negativo.'
    }

    const bodyImageError = validateImage(bodyImageFile)
    if (bodyImageError) {
      nextErrors.bodyImageFile = bodyImageError
    }

    const careTagError = validateImage(careTagImageFile)
    if (careTagError) {
      nextErrors.careTagImageFile = careTagError
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) {
      return
    }

    await onSubmit({
      category,
      name: name.trim(),
      size: size.trim(),
      brand: brand.trim() ? brand.trim() : null,
      price: parsedPrice,
      bodyImageFile,
      careTagImageFile,
    })
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-xl border border-slate-300 bg-white p-4 shadow-sm"
      aria-label="Formulario de peca"
    >
      <h3 className="mb-4 text-lg font-semibold text-slate-900">{title}</h3>

      <div className="grid gap-3 md:grid-cols-2">
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="category">
          Categoria
          <select
            id="category"
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
          {errors.category ? <span className="text-sm text-red-700">{errors.category}</span> : null}
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="name">
          Nome da peca
          <input
            id="name"
            name="name"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Ex.: Camiseta basica azul"
            required
          />
          {errors.name ? <span className="text-sm text-red-700">{errors.name}</span> : null}
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="size">
          Tamanho
          <input
            id="size"
            name="size"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={size}
            onChange={(event) => setSize(event.target.value)}
            placeholder="Ex.: M"
            required
          />
          {errors.size ? <span className="text-sm text-red-700">{errors.size}</span> : null}
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="brand">
          Marca
          <input
            id="brand"
            name="brand"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={brand}
            onChange={(event) => setBrand(event.target.value)}
            placeholder="Opcional"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="price">
          Preco (R$)
          <input
            id="price"
            name="price"
            className="rounded-md border border-slate-300 px-3 py-2"
            value={price}
            onChange={(event) => setPrice(event.target.value)}
            placeholder="Ex.: 199,90"
            inputMode="decimal"
          />
          {errors.price ? <span className="text-sm text-red-700">{errors.price}</span> : null}
        </label>
      </div>

      <div className="mt-4 grid gap-3 md:grid-cols-2">
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="bodyImageFile">
          Foto da peca (JPG, PNG ou WebP)
          <input
            id="bodyImageFile"
            name="bodyImageFile"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            onChange={(event) => setBodyImageFile(event.target.files?.[0] ?? null)}
          />
          {errors.bodyImageFile ? <span className="text-sm text-red-700">{errors.bodyImageFile}</span> : null}
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-800" htmlFor="careTagImageFile">
          Foto da etiqueta de cuidado (JPG, PNG ou WebP)
          <input
            id="careTagImageFile"
            name="careTagImageFile"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            onChange={(event) => setCareTagImageFile(event.target.files?.[0] ?? null)}
          />
          {errors.careTagImageFile ? <span className="text-sm text-red-700">{errors.careTagImageFile}</span> : null}
        </label>
      </div>

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

function isCategory(value: unknown): value is ClothingCategory {
  return typeof value === 'string' && CLOTHING_CATEGORIES.includes(value as ClothingCategory)
}
