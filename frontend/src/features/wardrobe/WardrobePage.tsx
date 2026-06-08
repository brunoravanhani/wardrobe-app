import { useCallback, useEffect, useMemo, useState } from 'react'
import { useAuthSession } from '../../app/providers/auth-context'
import { useDraftState } from '../../app/providers/DraftStateProvider'
import {
  CLOTHING_CATEGORIES,
  createWardrobeApi,
  getCategoryLabelPtBr,
  type ClothingCategory,
  type WardrobeItem,
} from '../../services/wardrobeApi'
import {
  WardrobeItemForm,
  type WardrobeItemFormInitialValues,
  type WardrobeItemFormValues,
} from './components/WardrobeItemForm'

type CategoryFilter = 'all' | ClothingCategory

type EditorState =
  | { mode: 'create' }
  | {
      mode: 'edit'
      item: WardrobeItem
    }

export function WardrobePage() {
  const auth = useAuthSession()
  const draftState = useDraftState()
  const accessToken = auth.status === 'authenticated' ? auth.accessToken : null
  const api = useMemo(
    () =>
      createWardrobeApi({
        baseUrl: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
        getAccessToken: () => accessToken,
      }),
    [accessToken],
  )

  const [items, setItems] = useState<WardrobeItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [selectedCategory, setSelectedCategory] = useState<CategoryFilter>('all')
  const [editor, setEditor] = useState<EditorState | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const loadItems = useCallback(async () => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const category = selectedCategory === 'all' ? undefined : selectedCategory
      const nextItems = await api.listWardrobeItems(category)
      setItems(nextItems)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Falha ao carregar pecas do guarda-roupa.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }, [api, selectedCategory])

  const categoryTabs = useMemo(
    () => [
      { key: 'all' as const, label: 'Todas' },
      ...CLOTHING_CATEGORIES.map((category) => ({
        key: category,
        label: getCategoryLabelPtBr(category),
      })),
    ],
    [],
  )

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadItems()
    }, 0)

    return () => {
      window.clearTimeout(timer)
    }
  }, [loadItems])

  if (auth.status !== 'authenticated') {
    return null
  }

  const formInitialValues: WardrobeItemFormInitialValues | undefined =
    editor?.mode === 'edit'
      ? {
          category: editor.item.category,
          name: editor.item.name,
          size: editor.item.size,
          brand: editor.item.brand,
          price: editor.item.price,
        }
      : undefined

  async function handleSubmitForm(values: WardrobeItemFormValues) {
    setSubmitError(null)
    setIsSaving(true)

    try {
      const bodyImageAssetId = await uploadImage(values.bodyImageFile, 'WardrobeBodyImage')
      const careTagImageAssetId = await uploadImage(values.careTagImageFile, 'WardrobeCareTagImage')

      if (editor?.mode === 'edit') {
        await api.updateWardrobeItem(editor.item.id, {
          category: values.category,
          name: values.name,
          size: values.size,
          brand: values.brand,
          price: values.price,
          bodyImageAssetId: bodyImageAssetId ?? editor.item.bodyImageAssetId,
          careTagImageAssetId: careTagImageAssetId ?? editor.item.careTagImageAssetId,
        })
      } else {
        await api.createWardrobeItem({
          category: values.category,
          name: values.name,
          size: values.size,
          brand: values.brand,
          price: values.price,
          bodyImageAssetId,
          careTagImageAssetId,
        })
        draftState.clearDraft('wardrobe-item:create')
      }

      setEditor(null)
      await loadItems()
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Nao foi possivel salvar a peca.'
      setSubmitError(message)
    } finally {
      setIsSaving(false)
    }
  }

  async function uploadImage(file: File | null, purpose: 'WardrobeBodyImage' | 'WardrobeCareTagImage') {
    if (!file) {
      return null
    }

    const uploadPayload = await api.createUploadUrl({ file, purpose })
    await api.uploadFileToPresignedUrl(uploadPayload.uploadUrl, file, uploadPayload.requiredHeaders)
    return uploadPayload.mediaAssetId
  }

  return (
    <section className="rounded-xl border border-amber-300 bg-white/85 p-5 shadow-sm">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-semibold text-slate-900">Guarda-roupa</h2>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => void loadItems()}
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
          >
            Atualizar
          </button>
          <button
            type="button"
            onClick={() => {
              setSubmitError(null)
              setEditor({ mode: 'create' })
            }}
            className="rounded-md bg-amber-700 px-3 py-2 text-sm font-medium text-white"
          >
            Nova peca
          </button>
        </div>
      </div>

      <div className="mb-4 flex flex-wrap gap-2" role="tablist" aria-label="Filtro de categorias">
        {categoryTabs.map((tab) => {
          const isActive = selectedCategory === tab.key
          return (
            <button
              key={tab.key}
              type="button"
              role="tab"
              aria-selected={isActive}
              onClick={() => {
                setSelectedCategory(tab.key)
              }}
              className={[
                'rounded-md border px-3 py-1.5 text-sm',
                isActive
                  ? 'border-amber-700 bg-amber-700 text-white'
                  : 'border-slate-300 bg-white text-slate-800 hover:border-amber-700 hover:text-amber-700',
              ].join(' ')}
            >
              {tab.label}
            </button>
          )
        })}
      </div>

      {editor ? (
        <div className="mb-4">
          <WardrobeItemForm
            key={editor.mode === 'edit' ? `edit-${editor.item.id}` : 'create'}
            mode={editor.mode}
            initialValues={formInitialValues}
            draftStorageKey={editor.mode === 'create' ? 'wardrobe-item:create' : undefined}
            busy={isSaving}
            submitError={submitError}
            onCancel={() => setEditor(null)}
            onSubmit={handleSubmitForm}
          />
        </div>
      ) : null}

      {errorMessage ? <p className="mb-3 text-sm text-red-700">{errorMessage}</p> : null}
      {isLoading ? <p className="text-slate-700">Carregando pecas...</p> : null}

      {!isLoading && items.length === 0 ? (
        <p className="rounded-md border border-dashed border-slate-300 p-4 text-sm text-slate-700">
          Nenhuma peca encontrada para este filtro.
        </p>
      ) : null}

      <ul className="grid gap-3 md:grid-cols-2">
        {items.map((item) => (
          <li key={item.id} className="rounded-lg border border-slate-200 bg-slate-50 p-4">
            <div className="mb-2 flex items-start justify-between gap-2">
              <div>
                <h3 className="font-semibold text-slate-900">{item.name}</h3>
                <p className="text-sm text-slate-600">{getCategoryLabelPtBr(item.category)}</p>
              </div>
              <button
                type="button"
                onClick={() => {
                  setSubmitError(null)
                  setEditor({ mode: 'edit', item })
                }}
                className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-800"
              >
                Editar
              </button>
            </div>
            <dl className="space-y-1 text-sm text-slate-700">
              <div>
                <dt className="inline font-medium">Tamanho:</dt> <dd className="inline">{item.size}</dd>
              </div>
              <div>
                <dt className="inline font-medium">Marca:</dt> <dd className="inline">{item.brand ?? 'Nao informada'}</dd>
              </div>
              <div>
                <dt className="inline font-medium">Preco:</dt>{' '}
                <dd className="inline">{formatPrice(item.price)}</dd>
              </div>
            </dl>
          </li>
        ))}
      </ul>
    </section>
  )
}

function formatPrice(value: number | null) {
  if (value === null) {
    return 'Nao informado'
  }

  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value)
}
