import { createContext, createElement, useContext, useMemo, useState, type PropsWithChildren } from 'react'

export type AuthBootstrapState = {
  sessionStorageKey: string
  locale: string
}

export type AuthSessionState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'authenticated'; email: string; accessToken: string }

export type AuthContextValue = {
  session: AuthSessionState
  signIn: (input: { email: string; accessToken: string }) => void
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function createInitialAuthState(bootstrap: AuthBootstrapState): AuthSessionState {
  const token = window.localStorage.getItem(bootstrap.sessionStorageKey)
  const email = window.localStorage.getItem(`${bootstrap.sessionStorageKey}:email`)

  if (token && email) {
    return { status: 'authenticated', email, accessToken: token }
  }

  return { status: 'anonymous' }
}

export function AuthProvider({
  bootstrap,
  children,
}: PropsWithChildren<{ bootstrap: AuthBootstrapState }>) {
  const [session, setSession] = useState<AuthSessionState>(() => createInitialAuthState(bootstrap))

  const contextValue = useMemo<AuthContextValue>(
    () => ({
      session,
      signIn: (input) => {
        window.localStorage.setItem(bootstrap.sessionStorageKey, input.accessToken)
        window.localStorage.setItem(`${bootstrap.sessionStorageKey}:email`, input.email)
        setSession({
          status: 'authenticated',
          email: input.email,
          accessToken: input.accessToken,
        })
      },
      signOut: () => {
        window.localStorage.removeItem(bootstrap.sessionStorageKey)
        window.localStorage.removeItem(`${bootstrap.sessionStorageKey}:email`)
        setSession({ status: 'anonymous' })
      },
    }),
    [bootstrap.sessionStorageKey, session],
  )

  return createElement(AuthContext.Provider, { value: contextValue }, children)
}

export function useAuthSession() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuthSession must be used within AuthProvider.')
  }

  return context.session
}

export function useAuthActions() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuthActions must be used within AuthProvider.')
  }

  return {
    signIn: context.signIn,
    signOut: context.signOut,
  }
}
