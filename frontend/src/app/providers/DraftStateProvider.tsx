import {
  createContext,
  useCallback,
  useContext,
  type PropsWithChildren,
} from 'react'

type DraftStateContextValue = {
  readDraft: (key: string) => string | null
  writeDraft: (key: string, value: string) => void
  clearDraft: (key: string) => void
}

const STORAGE_PREFIX = 'virtual-wardrobe/drafts/'

const DraftStateContext = createContext<DraftStateContextValue>({
  readDraft: () => null,
  writeDraft: () => undefined,
  clearDraft: () => undefined,
})

export function DraftStateProvider({ children }: PropsWithChildren) {
  const readDraft = useCallback((key: string) => {
    return window.localStorage.getItem(`${STORAGE_PREFIX}${key}`)
  }, [])

  const writeDraft = useCallback((key: string, value: string) => {
    window.localStorage.setItem(`${STORAGE_PREFIX}${key}`, value)
  }, [])

  const clearDraft = useCallback((key: string) => {
    window.localStorage.removeItem(`${STORAGE_PREFIX}${key}`)
  }, [])

  return (
    <DraftStateContext.Provider
      value={{
        readDraft,
        writeDraft,
        clearDraft,
      }}
    >
      {children}
    </DraftStateContext.Provider>
  )
}

export function useDraftState() {
  return useContext(DraftStateContext)
}
