import {
  useEffect,
  type PropsWithChildren,
} from 'react'
import { BrowserRouter } from 'react-router-dom'
import { AuthContext, createInitialAuthState, type AuthBootstrapState } from './auth-context'

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
      <BrowserRouter>{children}</BrowserRouter>
    </AuthContext.Provider>
  )
}
