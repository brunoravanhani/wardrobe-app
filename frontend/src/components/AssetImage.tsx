import { useEffect, useState } from 'react'

type AssetImageProps = {
  assetId: string | null
  alt: string
  loadViewUrl: (assetId: string) => Promise<string>
  className?: string
  imageClassName?: string
}

type ResolvedAsset = { assetId: string; url: string | null }

/**
 * Renders a private media asset by exchanging its id for a short-lived
 * presigned view URL. Falls back to a neutral placeholder while loading,
 * when no asset is associated, or if the URL cannot be resolved.
 */
export function AssetImage({ assetId, alt, loadViewUrl, className, imageClassName }: AssetImageProps) {
  const [resolved, setResolved] = useState<ResolvedAsset | null>(null)

  useEffect(() => {
    if (!assetId) {
      return
    }

    let active = true

    loadViewUrl(assetId)
      .then((url) => {
        if (active) {
          setResolved({ assetId, url })
        }
      })
      .catch(() => {
        if (active) {
          setResolved({ assetId, url: null })
        }
      })

    return () => {
      active = false
    }
  }, [assetId, loadViewUrl])

  const isResolvedForCurrent = resolved?.assetId === assetId
  const url = isResolvedForCurrent ? resolved?.url ?? null : null
  const isLoading = Boolean(assetId) && !isResolvedForCurrent

  const wrapperClass = ['relative flex items-center justify-center overflow-hidden bg-stone-100', className]
    .filter(Boolean)
    .join(' ')

  if (url) {
    return (
      <div className={wrapperClass}>
        <img src={url} alt={alt} className={imageClassName ?? 'h-full w-full object-cover'} loading="lazy" />
      </div>
    )
  }

  return (
    <div className={wrapperClass} role="img" aria-label={alt}>
      {isLoading ? (
        <span className="h-6 w-6 animate-pulse rounded-full bg-stone-300" aria-hidden="true" />
      ) : (
        <PlaceholderIcon />
      )}
    </div>
  )
}

function PlaceholderIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-8 w-8 text-stone-400"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      aria-hidden="true"
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909M3.75 19.5h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25a.75.75 0 11-1.5 0 .75.75 0 011.5 0z"
      />
    </svg>
  )
}
