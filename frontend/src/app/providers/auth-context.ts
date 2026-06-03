import { createContext, useContext } from 'react'

export type AuthBootstrapState = {
  sessionStorageKey: string
  locale: string
}

export type AuthSessionState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'authenticated'; email: string; accessToken: string }

export const AuthContext = createContext<AuthSessionState>({ status: 'loading' })

export function createInitialAuthState(bootstrap: AuthBootstrapState): AuthSessionState {
  const token = window.localStorage.getItem(bootstrap.sessionStorageKey)
  const email = window.localStorage.getItem(`${bootstrap.sessionStorageKey}:email`)

  if (token && email) {
    return { status: 'authenticated', email, accessToken: token }
  }

  return { status: 'anonymous' }
}

export function useAuthSession() {
  return useContext(AuthContext)
}
