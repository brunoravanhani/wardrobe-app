import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { useEffect, useRef, useState, type ReactElement } from 'react'
import { AppProviders } from './providers/AppProviders'
import { useAuthActions, useAuthSession, type AuthBootstrapState } from './providers/auth-context'
import { WardrobePage } from '../features/wardrobe/WardrobePage'
import { WishlistPage } from '../features/wishlist/WishlistPage'
import { LoginPage } from '../features/auth/LoginPage'
import { HangerIcon } from '../components/BrandLogo'

function RequireAuth({ children }: { children: ReactElement }) {
  const auth = useAuthSession()
  const location = useLocation()

  if (auth.status === 'loading') {
    return <p className="text-center text-slate-700">Carregando sessao...</p>
  }

  if (auth.status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return children
}

function TopNav({ email, onSignOut }: { email: string; onSignOut: () => void }) {
  return (
    <header className="border-b border-slate-800 bg-slate-800 text-slate-100 shadow-sm">
      <div className="mx-auto flex w-full max-w-5xl items-center gap-4 px-4 py-3 md:px-8">
        <div className="flex items-center gap-2 text-amber-400">
          <HangerIcon className="h-6 w-6" />
          <span className="hidden text-sm font-semibold tracking-wide text-slate-100 sm:inline">
            Guarda-Roupa &amp; Wishlist
          </span>
        </div>

        <nav className="ml-2 flex items-center gap-1" aria-label="Navegacao principal">
          <NavBarLink to="/">Guarda-roupa</NavBarLink>
          <NavBarLink to="/wishlist">Wishlist</NavBarLink>
        </nav>

        <div className="ml-auto">
          <AccountMenu email={email} onSignOut={onSignOut} />
        </div>
      </div>
    </header>
  )
}

function NavBarLink({ to, children }: { to: string; children: string }) {
  return (
    <NavLink
      to={to}
      end={to === '/'}
      className={({ isActive }) =>
        [
          'rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
          isActive ? 'bg-slate-700 text-white' : 'text-slate-300 hover:bg-slate-700/60 hover:text-white',
        ].join(' ')
      }
    >
      {children}
    </NavLink>
  )
}

function AccountMenu({ email, onSignOut }: { email: string; onSignOut: () => void }) {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement | null>(null)
  const initial = email.trim().charAt(0).toUpperCase() || 'U'

  useEffect(() => {
    if (!open) {
      return
    }

    function handlePointerDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        className="flex items-center gap-2 rounded-full border border-slate-600 bg-slate-700 py-1 pl-1 pr-2 text-sm text-slate-100 hover:border-slate-500"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label="Menu da conta"
      >
        <span className="flex h-7 w-7 items-center justify-center rounded-full bg-amber-600 text-xs font-semibold text-white">
          {initial}
        </span>
        <span aria-hidden="true" className="text-xs text-slate-300">
          ▾
        </span>
      </button>

      {open ? (
        <div
          role="menu"
          className="absolute right-0 z-20 mt-2 w-56 rounded-lg border border-slate-200 bg-white p-2 text-slate-800 shadow-lg"
        >
          <p className="truncate px-2 py-1 text-xs text-slate-500" title={email}>
            {email}
          </p>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false)
              onSignOut()
            }}
            className="mt-1 flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm font-medium text-slate-800 hover:bg-slate-100"
          >
            Sair
          </button>
        </div>
      ) : null}
    </div>
  )
}

function AppFrame() {
  const auth = useAuthSession()
  const { signOut } = useAuthActions()
  const isAuthenticated = auth.status === 'authenticated'

  return (
    <div className="min-h-screen bg-stone-100 text-slate-900">
      {isAuthenticated ? <TopNav email={auth.email} onSignOut={() => signOut()} /> : null}

      <main
        className={
          isAuthenticated
            ? 'mx-auto w-full max-w-5xl px-4 py-8 md:px-8'
            : 'flex min-h-screen w-full items-center justify-center px-4 py-8'
        }
      >
        <Routes>
          <Route
            path="/"
            element={
              <RequireAuth>
                <WardrobePage />
              </RequireAuth>
            }
          />
          <Route
            path="/wishlist"
            element={
              <RequireAuth>
                <WishlistPage />
              </RequireAuth>
            }
          />
          <Route path="/login" element={auth.status === 'authenticated' ? <Navigate to="/" replace /> : <LoginPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  )
}

const bootstrapState: AuthBootstrapState = {
  sessionStorageKey: 'virtual-wardrobe/session-token',
  locale: 'pt-BR',
}

export default function App() {
  return (
    <AppProviders bootstrap={bootstrapState}>
      <AppFrame />
    </AppProviders>
  )
}
