import { getCategoryLabelPtBr, type ClothingCategory } from '../../../services/wardrobeApi'
import type { TemplateSlot } from '../../../services/wardrobeTemplatesApi'

type TemplateSlotCardProps = {
  slot: TemplateSlot
  wardrobeItemName?: string | null
  onLinkWishlist: (slot: TemplateSlot) => void
}

export function TemplateSlotCard({ slot, wardrobeItemName, onLinkWishlist }: TemplateSlotCardProps) {
  const categoryLabel = getCategoryLabelPtBr(slot.category as ClothingCategory)

  if (slot.isFulfilled) {
    return (
      <li className="flex flex-col overflow-hidden rounded-xl border border-emerald-200 bg-emerald-50 shadow-sm">
        <div className="flex flex-1 flex-col gap-2 p-4">
          <p className="text-xs font-semibold uppercase tracking-wide text-emerald-600">{categoryLabel}</p>
          <p className="font-semibold text-slate-900">{wardrobeItemName ?? 'Peça adquirida'}</p>
          <span className="mt-auto inline-flex w-fit items-center rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700">
            Adquirida
          </span>
        </div>
      </li>
    )
  }

  return (
    <li className="flex flex-col overflow-hidden rounded-xl border border-dashed border-stone-300 bg-white/60 shadow-sm">
      <div className="flex flex-1 flex-col gap-2 p-4">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">{categoryLabel}</p>
        <p className="font-medium text-slate-400">Slot disponível</p>
        <button
          type="button"
          onClick={() => onLinkWishlist(slot)}
          className="mt-auto w-full rounded-md border border-amber-600 px-3 py-1.5 text-sm font-medium text-amber-700 hover:bg-amber-50"
        >
          Adicionar à Lista de Desejos
        </button>
      </div>
    </li>
  )
}
