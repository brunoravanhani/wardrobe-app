import {
  useEffect,
  type PropsWithChildren,
} from 'react'
import { BrowserRouter } from 'react-router-dom'
import { AuthContext, createInitialAuthState, type AuthBootstrapState } from './auth-context'
import { DraftStateProvider } from './DraftStateProvider'

export function AppProviders({
  bootstrap,
  children,
}: PropsWithChildren<{ bootstrap: AuthBootstrapState }>) {
  const authState = createInitialAuthState(bootstrap)

  useEffect(() => {
    document.documentElement.lang = bootstrap.locale
  }, [bootstrap])

  return (
    <AuthContext.Provider value={authState}>
      <DraftStateProvider>
        <BrowserRouter>{children}</BrowserRouter>
      </DraftStateProvider>
    </AuthContext.Provider>
  )
}
