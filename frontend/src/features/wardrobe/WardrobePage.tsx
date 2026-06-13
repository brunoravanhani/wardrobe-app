import { useCallback, useEffect, useMemo, useState } from 'react'
import { useAuthSession } from '../../app/providers/auth-context'
import {
  CLOTHING_CATEGORIES,
  createWardrobeApi,
  getCategoryLabelPtBr,
  type ClothingCategory,
  type WardrobeItem,
} from '../../services/wardrobeApi'
import {
  createTemplatesApi,
  type LinkSlotToWishlistInput,
  type TemplateSlot,
  type UserSlotsData,
  type WardrobeTemplate,
} from '../../services/wardrobeTemplatesApi'
import {
  WardrobeItemForm,
  type WardrobeItemFormInitialValues,
  type WardrobeItemFormValues,
} from './components/WardrobeItemForm'
import { TemplateSlotCard } from './components/TemplateSlotCard'
import { TemplateProgressBar } from './components/TemplateProgressBar'
import { LinkSlotToWishlistDialog } from './components/LinkSlotToWishlistDialog'
import { createMediaApi } from '../../services/mediaApi'
import { AssetImage } from '../../components/AssetImage'

type CategoryFilter = 'all' | ClothingCategory

type EditorState =
  | { mode: 'create' }
  | {
      mode: 'edit'
      item: WardrobeItem
    }

export function WardrobePage() {
  const auth = useAuthSession()
  const accessToken = auth.status === 'authenticated' ? auth.accessToken : null
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

  const api = useMemo(
    () => createWardrobeApi({ baseUrl, getAccessToken: () => accessToken }),
    [baseUrl, accessToken],
  )
  const mediaApi = useMemo(
    () => createMediaApi({ baseUrl, getAccessToken: () => accessToken }),
    [baseUrl, accessToken],
  )
  const templatesApi = useMemo(
    () => createTemplatesApi({ baseUrl, getAccessToken: () => accessToken }),
    [baseUrl, accessToken],
  )

  const [items, setItems] = useState<WardrobeItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [selectedCategory, setSelectedCategory] = useState<CategoryFilter>('all')
  const [editor, setEditor] = useState<EditorState | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const [templates, setTemplates] = useState<WardrobeTemplate[]>([])
  const [slotsData, setSlotsData] = useState<UserSlotsData>({ activeTemplateId: null, slots: [] })
  const [pendingTemplateId, setPendingTemplateId] = useState<string | null>(null)
  const [isLoadingSlots, setIsLoadingSlots] = useState(false)
  const [linkSlotTarget, setLinkSlotTarget] = useState<TemplateSlot | null>(null)
  const [isLinkingSlot, setIsLinkingSlot] = useState(false)
  const [linkSlotError, setLinkSlotError] = useState<string | null>(null)

  const loadItems = useCallback(async () => {
    setIsLoading(true)
    setErrorMessage(null)
    try {
      const nextItems = await api.listWardrobeItems()
      setItems(nextItems)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Falha ao carregar pecas do guarda-roupa.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }, [api])

  const loadSlots = useCallback(async () => {
    setIsLoadingSlots(true)
    try {
      const data = await templatesApi.getUserSlots()
      setSlotsData(data)
    } catch {
      // silently ignore — slots are optional UI enhancement
    } finally {
      setIsLoadingSlots(false)
    }
  }, [templatesApi])

  const loadTemplates = useCallback(async () => {
    try {
      const data = await templatesApi.listTemplates()
      setTemplates(data)
    } catch {
      // silently ignore — templates are optional UI enhancement
    }
  }, [templatesApi])

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
      void Promise.all([loadItems(), loadSlots(), loadTemplates()])
    }, 0)
    return () => { window.clearTimeout(timer) }
  }, [loadItems, loadSlots, loadTemplates])

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
      }

      setEditor(null)
      await Promise.all([loadItems(), loadSlots()])
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Nao foi possivel salvar a peca.'
      setSubmitError(message)
    } finally {
      setIsSaving(false)
    }
  }

  async function uploadImage(file: File | null, purpose: 'WardrobeBodyImage' | 'WardrobeCareTagImage') {
    if (!file) return null
    const uploadPayload = await api.createUploadUrl({ file, purpose })
    await api.uploadFileToPresignedUrl(uploadPayload.uploadUrl, file, uploadPayload.requiredHeaders)
    return uploadPayload.mediaAssetId
  }

  async function handleTemplateChange(templateId: string | null) {
    if (!templateId || templateId === slotsData.activeTemplateId) return

    if (slotsData.activeTemplateId !== null) {
      setPendingTemplateId(templateId)
      return
    }

    await doSelectTemplate(templateId)
  }

  async function handleConfirmTemplateSwitch() {
    if (!pendingTemplateId) return
    const id = pendingTemplateId
    setPendingTemplateId(null)
    await doSelectTemplate(id)
  }

  async function doSelectTemplate(templateId: string) {
    try {
      await templatesApi.selectTemplate(templateId)
      await Promise.all([loadSlots(), loadItems()])
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Falha ao selecionar template.'
      setErrorMessage(message)
    }
  }

  function handleLinkWishlist(slot: TemplateSlot) {
    setLinkSlotTarget(slot)
    setLinkSlotError(null)
  }

  async function handleLinkWishlistSubmit(input: LinkSlotToWishlistInput) {
    if (!linkSlotTarget) return
    setIsLinkingSlot(true)
    setLinkSlotError(null)
    try {
      await templatesApi.linkSlotToWishlist(linkSlotTarget.id, input)
      setLinkSlotTarget(null)
      await loadSlots()
    } catch (error) {
      setLinkSlotError(error instanceof Error ? error.message : 'Falha ao adicionar a lista de desejos.')
    } finally {
      setIsLinkingSlot(false)
    }
  }

  const activeTemplate = templates.find((t) => t.id === slotsData.activeTemplateId)
  const activeSlots = slotsData.slots
  const isTemplateActive = slotsData.activeTemplateId !== null && activeTemplate !== undefined

  const displayedItems = useMemo(() => {
    if (isTemplateActive) return items
    if (selectedCategory === 'all') return items
    return items.filter((i) => i.category === selectedCategory)
  }, [items, selectedCategory, isTemplateActive])

  const pendingTemplateName = templates.find((t) => t.id === pendingTemplateId)?.name
  const currentTemplateName = activeTemplate?.name

  return (
    <section>
      {/* Header */}
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-3xl font-semibold text-slate-900">Meu Guarda-roupa</h2>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => void Promise.all([loadItems(), loadSlots()])}
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
            Nova peca
          </button>
        </div>
      </div>

      {/* Template selector */}
      {templates.length > 0 && (
        <div className="mb-5 flex flex-wrap items-center gap-3">
          <label htmlFor="template-select" className="text-sm font-medium text-slate-700">
            Template:
          </label>
          <select
            id="template-select"
            value={slotsData.activeTemplateId ?? ''}
            onChange={(e) => void handleTemplateChange(e.target.value || null)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none focus:ring-1 focus:ring-amber-500"
          >
            {!slotsData.activeTemplateId && <option value="">Sem template</option>}
            {templates.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
          {isLoadingSlots && <span className="text-sm text-slate-500">Carregando...</span>}
        </div>
      )}

      {/* Category tabs (only when no template active) */}
      {!isTemplateActive && (
        <div className="mb-5 flex flex-wrap gap-2" role="tablist" aria-label="Filtro de categorias">
          {categoryTabs.map((tab) => {
            const isActive = selectedCategory === tab.key
            return (
              <button
                key={tab.key}
                type="button"
                role="tab"
                aria-selected={isActive}
                onClick={() => setSelectedCategory(tab.key)}
                className={[
                  'rounded-full border px-4 py-1.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'border-amber-700 bg-amber-700 text-white'
                    : 'border-stone-300 bg-white text-slate-700 hover:border-amber-600 hover:text-amber-700',
                ].join(' ')}
              >
                {tab.label}
              </button>
            )
          })}
        </div>
      )}

      {/* Wardrobe item form */}
      {editor ? (
        <div className="mb-5">
          <WardrobeItemForm
            key={editor.mode === 'edit' ? `edit-${editor.item.id}` : 'create'}
            mode={editor.mode}
            initialValues={formInitialValues}
            busy={isSaving}
            submitError={submitError}
            onCancel={() => setEditor(null)}
            onSubmit={handleSubmitForm}
          />
        </div>
      ) : null}

      {errorMessage ? <p className="mb-3 text-sm text-red-700">{errorMessage}</p> : null}
      {isLoading ? <p className="text-slate-700">Carregando pecas...</p> : null}

      {/* Template view */}
      {isTemplateActive ? (
        <>
          <TemplateProgressBar
            fulfilled={activeSlots.filter((s) => s.isFulfilled).length}
            total={activeSlots.length}
          />

          {activeTemplate.slotDefinitions.map((def) => {
            const cat = def.category
            const slotsForCat = activeSlots
              .filter((s) => s.category === cat)
              .sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc))

            const fulfilledItemIds = new Set(
              slotsForCat.filter((s) => s.wardrobeItemId).map((s) => s.wardrobeItemId!),
            )
            const extraItems = items.filter((i) => i.category === cat && !fulfilledItemIds.has(i.id))

            return (
              <section key={cat} className="mb-8">
                <h3 className="mb-3 text-lg font-semibold text-slate-700">
                  {getCategoryLabelPtBr(cat)}
                </h3>
                <ul className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                  {slotsForCat.map((slot) => (
                    <TemplateSlotCard
                      key={slot.id}
                      slot={slot}
                      wardrobeItemName={
                        slot.wardrobeItemId
                          ? (items.find((i) => i.id === slot.wardrobeItemId)?.name ?? null)
                          : null
                      }
                      onLinkWishlist={handleLinkWishlist}
                    />
                  ))}
                  {extraItems.map((item) => (
                    <WardrobeItemCard
                      key={item.id}
                      item={item}
                      mediaApi={mediaApi}
                      onEdit={() => {
                        setSubmitError(null)
                        setEditor({ mode: 'edit', item })
                      }}
                    />
                  ))}
                </ul>
              </section>
            )
          })}

          {/* Extra categories not in the template */}
          {(() => {
            const templateCats = new Set(activeTemplate.slotDefinitions.map((d) => d.category))
            const extraCatItems = items.filter((i) => !templateCats.has(i.category))
            if (extraCatItems.length === 0) return null
            return (
              <section className="mb-8">
                <h3 className="mb-3 text-lg font-semibold text-slate-700">Outras pecas</h3>
                <ul className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                  {extraCatItems.map((item) => (
                    <WardrobeItemCard
                      key={item.id}
                      item={item}
                      mediaApi={mediaApi}
                      onEdit={() => {
                        setSubmitError(null)
                        setEditor({ mode: 'edit', item })
                      }}
                    />
                  ))}
                </ul>
              </section>
            )
          })()}
        </>
      ) : (
        /* Regular flat view */
        <>
          {!isLoading && displayedItems.length === 0 ? (
            <p className="rounded-xl border border-dashed border-stone-300 bg-white/60 p-8 text-center text-sm text-slate-600">
              Nenhuma peca encontrada para este filtro.
            </p>
          ) : null}

          <ul className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {displayedItems.map((item) => (
              <WardrobeItemCard
                key={item.id}
                item={item}
                mediaApi={mediaApi}
                onEdit={() => {
                  setSubmitError(null)
                  setEditor({ mode: 'edit', item })
                }}
              />
            ))}
          </ul>
        </>
      )}

      {/* Template switch confirmation modal */}
      {pendingTemplateId && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50"
          onClick={() => setPendingTemplateId(null)}
        >
          <div
            className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl"
            role="dialog"
            aria-modal="true"
            aria-labelledby="switch-template-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 id="switch-template-title" className="mb-3 text-lg font-semibold text-slate-900">
              Trocar template
            </h3>
            <p className="mb-5 text-sm text-slate-600">
              Trocar para <strong>{pendingTemplateName}</strong> removerá os slots não preenchidos de{' '}
              <strong>{currentTemplateName}</strong>. Continuar?
            </p>
            <div className="flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setPendingTemplateId(null)}
                className="rounded-md border border-stone-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-stone-50"
              >
                Cancelar
              </button>
              <button
                type="button"
                onClick={() => void handleConfirmTemplateSwitch()}
                className="rounded-md bg-amber-700 px-4 py-2 text-sm font-medium text-white hover:bg-amber-800"
              >
                Confirmar
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Link slot to wishlist dialog */}
      {linkSlotTarget && (
        <LinkSlotToWishlistDialog
          slot={linkSlotTarget}
          busy={isLinkingSlot}
          submitError={linkSlotError}
          onCancel={() => {
            setLinkSlotTarget(null)
            setLinkSlotError(null)
          }}
          onSubmit={handleLinkWishlistSubmit}
        />
      )}
    </section>
  )
}

type WardrobeItemCardProps = {
  item: WardrobeItem
  mediaApi: ReturnType<typeof createMediaApi>
  onEdit: () => void
}

function WardrobeItemCard({ item, mediaApi, onEdit }: WardrobeItemCardProps) {
  return (
    <li className="flex flex-col overflow-hidden rounded-xl border border-stone-200 bg-white shadow-sm transition-shadow hover:shadow-md">
      <AssetImage
        assetId={item.bodyImageAssetId}
        alt={item.name}
        loadViewUrl={mediaApi.createViewUrl}
        className="aspect-[4/5] w-full"
      />
      <div className="flex flex-1 flex-col gap-3 p-4">
        <div>
          <h3 className="font-semibold text-slate-900">{item.name}</h3>
          <p className="text-sm text-slate-500">{getCategoryLabelPtBr(item.category)}</p>
        </div>
        <dl className="space-y-0.5 text-sm text-slate-700">
          <div>
            <dt className="inline font-medium">Tamanho:</dt> <dd className="inline">{item.size}</dd>
          </div>
          <div>
            <dt className="inline font-medium">Marca:</dt>{' '}
            <dd className="inline">{item.brand ?? 'Nao informada'}</dd>
          </div>
          <div>
            <dt className="inline font-medium">Preco:</dt>{' '}
            <dd className="inline">{formatPrice(item.price)}</dd>
          </div>
        </dl>
        <button
          type="button"
          onClick={onEdit}
          className="mt-auto w-full rounded-md border border-amber-600 px-3 py-1.5 text-sm font-medium text-amber-700 hover:bg-amber-50"
        >
          Editar
        </button>
      </div>
    </li>
  )
}

function formatPrice(value: number | null) {
  if (value === null) return 'Nao informado'
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}
