type TemplateProgressBarProps = {
  fulfilled: number
  total: number
}

export function TemplateProgressBar({ fulfilled, total }: TemplateProgressBarProps) {
  const percent = total > 0 ? Math.round((fulfilled / total) * 100) : 0

  return (
    <div className="mb-5">
      <div className="mb-1 flex items-center justify-between text-sm">
        <span className="font-medium text-slate-700">Progresso</span>
        <span className="text-slate-600">{fulfilled} de {total} peças adquiridas</span>
      </div>
      <div className="h-2 overflow-hidden rounded-full bg-stone-200">
        <div
          className="h-full rounded-full bg-emerald-500 transition-all duration-300"
          style={{ width: `${percent}%` }}
          role="progressbar"
          aria-valuenow={fulfilled}
          aria-valuemin={0}
          aria-valuemax={total}
          aria-label={`${fulfilled} de ${total} peças adquiridas`}
        />
      </div>
    </div>
  )
}
