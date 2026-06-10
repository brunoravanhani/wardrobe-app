import { useCallback, useEffect, useMemo, useState } from 'react'
import { useAuthSession } from '../../app/providers/auth-context'
import { useDraftState } from '../../app/providers/DraftStateProvider'
import {
  createWishlistApi,
  type WishlistItem,
  type WishlistItemStatus,
} from '../../services/wishlistApi'
import { getCategoryLabelPtBr } from '../../services/wardrobeApi'
import {
  WishlistItemForm,
  type WishlistItemFormInitialValues,
  type WishlistItemFormValues,
} from './components/WishlistItemForm'
import {
  ConvertWishlistItemDialog,
  type ConvertWishlistItemDialogValues,
} from './components/ConvertWishlistItemDialog'

type ViewMode = 'active' | 'history'

type EditorState =
  | { mode: 'create' }
  | {
      mode: 'edit'
      item: WishlistItem
    }

export function WishlistPage() {
  const auth = useAuthSession()
  const draftState = useDraftState()
  const accessToken = auth.status === 'authenticated' ? auth.accessToken : null

  const api = useMemo(
    () =>
      createWishlistApi({
        baseUrl: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
        getAccessToken: () => accessToken,
      }),
    [accessToken],
  )

  const [items, setItems] = useState<WishlistItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [conversionError, setConversionError] = useState<string | null>(null)
  const [conversionSuccessMessage, setConversionSuccessMessage] = useState<string | null>(null)
  const [editor, setEditor] = useState<EditorState | null>(null)
  const [itemToConvert, setItemToConvert] = useState<WishlistItem | null>(null)
  const [viewMode, setViewMode] = useState<ViewMode>('active')

  const loadItems = useCallback(async () => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const includePurchased = viewMode === 'history'
      const listedItems = await api.listWishlistItems(includePurchased)
      setItems(viewMode === 'history' ? listedItems.filter((item) => item.status === 'Purchased') : listedItems)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Falha ao carregar wishlist.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }, [api, viewMode])

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

  const formInitialValues: WishlistItemFormInitialValues | undefined =
    editor?.mode === 'edit'
      ? {
          category: editor.item.category,
          name: editor.item.name,
          brand: editor.item.brand,
          targetPrice: editor.item.targetPrice,
          links: editor.item.links,
        }
      : undefined

  async function handleSubmit(values: WishlistItemFormValues) {
    setSubmitError(null)
    setIsSaving(true)

    try {
      const inspirationImageAssetId = await uploadImage(values.inspirationImageFile)

      if (editor?.mode === 'edit') {
        await api.updateWishlistItem(editor.item.id, {
          category: values.category,
          name: values.name,
          brand: values.brand,
          targetPrice: values.targetPrice,
          links: values.links,
          inspirationImageAssetId: inspirationImageAssetId ?? editor.item.inspirationImageAssetId,
        })
      } else {
        await api.createWishlistItem({
          category: values.category,
          name: values.name,
          brand: values.brand,
          targetPrice: values.targetPrice,
          links: values.links,
          inspirationImageAssetId,
        })
        draftState.clearDraft('wishlist-item:create')
      }

      setEditor(null)
      await loadItems()
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Nao foi possivel salvar o item da wishlist.'
      setSubmitError(message)
    } finally {
      setIsSaving(false)
    }
  }

  async function handleMarkAsPurchased(item: WishlistItem) {
    try {
      await api.markAsPurchased(item.id)
      await loadItems()
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Nao foi possivel atualizar o status do item.'
      setErrorMessage(message)
    }
  }

  async function handleConvertToWardrobe(values: ConvertWishlistItemDialogValues) {
    if (!itemToConvert) {
      return
    }

    setConversionError(null)
    setIsSaving(true)

    try {
      await api.convertToWardrobe(itemToConvert.id, {
        name: values.name,
        category: values.category,
        size: values.size,
        brand: values.brand,
        price: values.price,
      })

      setItemToConvert(null)
      setConversionSuccessMessage('Convertido para guarda-roupa com sucesso.')
      await loadItems()
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Nao foi possivel converter o item.'
      setConversionError(message)
    } finally {
      setIsSaving(false)
    }
  }

  async function uploadImage(file: File | null) {
    if (!file) {
      return null
    }

    const uploadPayload = await api.createInspirationUploadUrl(file)
    await api.uploadFileToPresignedUrl(uploadPayload.uploadUrl, file, uploadPayload.requiredHeaders)
    return uploadPayload.mediaAssetId
  }

  return (
    <section className="rounded-xl border border-amber-300 bg-white/85 p-5 shadow-sm">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-semibold text-slate-900">Wishlist</h2>

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
            Novo desejo
          </button>
        </div>
      </div>

      <div className="mb-4 flex flex-wrap gap-2" role="tablist" aria-label="Visualizacao da wishlist">
        <ViewTab
          active={viewMode === 'active'}
          label="Ativos"
          onClick={() => {
            setViewMode('active')
          }}
        />
        <ViewTab
          active={viewMode === 'history'}
          label="Historico"
          onClick={() => {
            setViewMode('history')
          }}
        />
      </div>

      {editor ? (
        <div className="mb-4">
          <WishlistItemForm
            key={editor.mode === 'edit' ? `edit-${editor.item.id}` : 'create'}
            mode={editor.mode}
            initialValues={formInitialValues}
            draftStorageKey={editor.mode === 'create' ? 'wishlist-item:create' : undefined}
            busy={isSaving}
            submitError={submitError}
            onCancel={() => setEditor(null)}
            onSubmit={handleSubmit}
          />
        </div>
      ) : null}

      {itemToConvert ? (
        <ConvertWishlistItemDialog
          item={itemToConvert}
          busy={isSaving}
          submitError={conversionError}
          onCancel={() => {
            setItemToConvert(null)
            setConversionError(null)
          }}
          onSubmit={handleConvertToWardrobe}
        />
      ) : null}

      {conversionSuccessMessage ? <p className="mb-3 text-sm text-emerald-800">{conversionSuccessMessage}</p> : null}

      {errorMessage ? <p className="mb-3 text-sm text-red-700">{errorMessage}</p> : null}
      {isLoading ? <p className="text-slate-700">Carregando wishlist...</p> : null}

      {!isLoading && items.length === 0 ? (
        <p className="rounded-md border border-dashed border-slate-300 p-4 text-sm text-slate-700">
          Nenhum item encontrado nesta visualizacao.
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

              <div className="flex gap-2">
                {item.status === 'Active' ? (
                  <button
                    type="button"
                    onClick={() => {
                      void handleMarkAsPurchased(item)
                    }}
                    className="rounded-md border border-emerald-600 bg-emerald-600 px-3 py-1.5 text-sm text-white"
                  >
                    Marcar como comprado
                  </button>
                ) : null}

                {item.status === 'Purchased' && !item.convertedWardrobeItemId ? (
                  <button
                    type="button"
                    onClick={() => {
                      setItemToConvert(item)
                      setConversionError(null)
                      setConversionSuccessMessage(null)
                    }}
                    className="rounded-md border border-emerald-700 bg-emerald-700 px-3 py-1.5 text-sm text-white"
                  >
                    Converter para guarda-roupa
                  </button>
                ) : null}

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
            </div>

            <dl className="space-y-1 text-sm text-slate-700">
              <MetaRow label="Status" value={toStatusLabel(item.status)} />
              <MetaRow
                label="Conversao"
                value={item.convertedWardrobeItemId ? 'Convertido' : 'Nao convertido'}
              />
              <MetaRow label="Marca" value={item.brand ?? 'Nao informada'} />
              <MetaRow label="Preco alvo" value={formatPrice(item.targetPrice)} />
              <MetaRow label="Links" value={item.links.length > 0 ? item.links.join(', ') : 'Nao informados'} />
            </dl>
          </li>
        ))}
      </ul>
    </section>
  )
}

function ViewTab({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={[
        'rounded-md border px-3 py-1.5 text-sm',
        active
          ? 'border-amber-700 bg-amber-700 text-white'
          : 'border-slate-300 bg-white text-slate-800 hover:border-amber-700 hover:text-amber-700',
      ].join(' ')}
    >
      {label}
    </button>
  )
}

function MetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="inline font-medium">{label}:</dt> <dd className="inline">{value}</dd>
    </div>
  )
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value)
}

function toStatusLabel(status: WishlistItemStatus) {
  return status === 'Purchased' ? 'Comprado' : 'Ativo'
}
