import {
  useEffect,
  type PropsWithChildren,
} from 'react'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider, type AuthBootstrapState } from './auth-context'
import { DraftStateProvider } from './DraftStateProvider'

export function AppProviders({
  bootstrap,
  children,
}: PropsWithChildren<{ bootstrap: AuthBootstrapState }>) {
  useEffect(() => {
    document.documentElement.lang = bootstrap.locale
  }, [bootstrap])

  return (
    <AuthProvider bootstrap={bootstrap}>
      <DraftStateProvider>
        <BrowserRouter>{children}</BrowserRouter>
      </DraftStateProvider>
    </AuthProvider>
  )
}
