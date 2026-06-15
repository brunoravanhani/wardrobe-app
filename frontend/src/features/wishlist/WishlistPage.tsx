import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuthSession } from '../../app/providers/auth-context'
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
import { createMediaApi } from '../../services/mediaApi'
import { AssetImage } from '../../components/AssetImage'

type ViewMode = 'active' | 'history'

type EditorState =
  | { mode: 'create' }
  | {
      mode: 'edit'
      item: WishlistItem
    }

export function WishlistPage() {
  const auth = useAuthSession()
  const accessToken = auth.status === 'authenticated' ? auth.accessToken : null

  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'
  const api = useMemo(
    () =>
      createWishlistApi({
        baseUrl,
        getAccessToken: () => accessToken,
      }),
    [baseUrl, accessToken],
  )
  const mediaApi = useMemo(
    () => createMediaApi({ baseUrl, getAccessToken: () => accessToken }),
    [baseUrl, accessToken],
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
      }

      setEditor(null)
      await loadItems()
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Não foi possível salvar o item da wishlist.'
      setSubmitError(message)
    } finally {
      setIsSaving(false)
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
      const message = error instanceof Error ? error.message : 'Não foi possível converter o item.'
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
    <section>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-3xl font-semibold text-slate-900">Minha Wishlist</h2>

        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => void loadItems()}
            className="rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-stone-50"
          >
            Atualizar
          </button>
          <button
            type="button"
            onClick={() => {
              setSubmitError(null)
              setEditor({ mode: 'create' })
            }}
            className="rounded-md bg-amber-700 px-3 py-2 text-sm font-medium text-white hover:bg-amber-800"
          >
            Novo desejo
          </button>
        </div>
      </div>

      <div
        className="mb-5 flex flex-wrap gap-6 border-b border-stone-200"
        role="tablist"
        aria-label="Visualização da wishlist"
      >
        <ViewTab
          active={viewMode === 'active'}
          label="Ativos"
          onClick={() => {
            setViewMode('active')
          }}
        />
        <ViewTab
          active={viewMode === 'history'}
          label="Histórico"
          onClick={() => {
            setViewMode('history')
          }}
        />
      </div>

      {editor ? (
        <div className="mb-5">
          <WishlistItemForm
            key={editor.mode === 'edit' ? `edit-${editor.item.id}` : 'create'}
            mode={editor.mode}
            initialValues={formInitialValues}
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
        <p className="rounded-xl border border-dashed border-stone-300 bg-white/60 p-8 text-center text-sm text-slate-600">
          Nenhum item encontrado nesta visualização.
        </p>
      ) : null}

      <ul className="grid gap-5 md:grid-cols-2">
        {items.map((item) => (
          <li
            key={item.id}
            className="flex gap-4 overflow-hidden rounded-xl border border-stone-200 bg-white p-4 shadow-sm transition-shadow hover:shadow-md"
          >
            <AssetImage
              assetId={item.inspirationImageAssetId}
              alt={item.name}
              loadViewUrl={mediaApi.createViewUrl}
              className="h-32 w-24 shrink-0 rounded-lg"
            />

            <div className="flex min-w-0 flex-1 flex-col gap-1.5">
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <h3 className="truncate font-semibold text-slate-900">{item.name}</h3>
                  <p className="text-sm text-slate-500">{getCategoryLabelPtBr(item.category)}</p>
                </div>
                <StatusBadge status={item.status} />
              </div>

              {item.brand ? <p className="text-sm text-slate-600">{item.brand}</p> : null}
              <p className="text-sm font-medium text-slate-800">Preço alvo: {formatPrice(item.targetPrice)}</p>

              {item.links.length > 0 ? (
                <p className="flex flex-wrap gap-x-3 gap-y-1 text-sm">
                  {item.links.map((link) => (
                    <a
                      key={link.url}
                      href={link.url}
                      target="_blank"
                      rel="noreferrer"
                      className="text-amber-700 underline-offset-2 hover:underline"
                    >
                      {link.label ?? deriveLinkLabel(link.url)}
                    </a>
                  ))}
                </p>
              ) : null}

              <div className="mt-auto flex flex-wrap gap-2 pt-2">
                {!item.convertedWardrobeItemId ? (
                  <button
                    type="button"
                    onClick={() => {
                      setItemToConvert(item)
                      setConversionError(null)
                      setConversionSuccessMessage(null)
                    }}
                    className="rounded-md bg-emerald-700 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-800"
                  >
                    Converter para guarda-roupa
                  </button>
                ) : null}

                {item.convertedWardrobeItemId ? (
                  <Link
                    to="/"
                    className="rounded-md bg-amber-700 px-3 py-1.5 text-sm font-medium text-white hover:bg-amber-800"
                  >
                    Ver no Guarda-Roupa
                  </Link>
                ) : null}

                <button
                  type="button"
                  onClick={() => {
                    setSubmitError(null)
                    setEditor({ mode: 'edit', item })
                  }}
                  className="rounded-md border border-stone-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-stone-50"
                >
                  Editar
                </button>
              </div>
            </div>
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
        '-mb-px border-b-2 px-1 pb-2 text-sm font-medium transition-colors',
        active
          ? 'border-amber-700 text-amber-800'
          : 'border-transparent text-slate-500 hover:text-amber-700',
      ].join(' ')}
    >
      {label}
    </button>
  )
}

function StatusBadge({ status }: { status: WishlistItemStatus }) {
  const isPurchased = status === 'Purchased'
  return (
    <span
      className={[
        'shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium',
        isPurchased ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800',
      ].join(' ')}
    >
      {toStatusLabel(status)}
    </span>
  )
}

function deriveLinkLabel(url: string): string {
  try {
    return new URL(url).hostname.replace(/^www\./, '')
  } catch {
    return url
  }
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
