import { NavLink, Route, Routes } from 'react-router-dom'
import { useMemo } from 'react'
import { AppProviders } from './providers/AppProviders'
import { useAuthSession, type AuthBootstrapState } from './providers/auth-context'
import { WardrobePage } from '../features/wardrobe/WardrobePage'
import { WishlistPage } from '../features/wishlist/WishlistPage'

function GuardedContent() {
  const auth = useAuthSession()

  if (auth.status === 'loading') {
    return <p className="text-center text-slate-700">Carregando sessao...</p>
  }

  if (auth.status === 'anonymous') {
    return (
      <section className="mx-auto max-w-xl rounded-xl border border-amber-300 bg-white/80 p-6 shadow-sm">
        <h2 className="mb-2 text-2xl font-semibold text-slate-900">Bem-vinda(o) ao Virtual Wardrobe</h2>
        <p className="text-slate-700">
          Faça login com Google para acessar guarda-roupa e wishlist com dados privados.
        </p>
      </section>
    )
  }

  return (
    <Routes>
      <Route path="/" element={<WardrobePage />} />
      <Route path="/wishlist" element={<WishlistPage />} />
    </Routes>
  )
}

function AppFrame() {
  const auth = useAuthSession()
  const authLabel = useMemo(() => {
    if (auth.status === 'loading') {
      return 'Inicializando autenticação...'
    }

    if (auth.status === 'authenticated') {
      return `Sessão ativa para ${auth.email}`
    }

    return 'Sessão não autenticada'
  }, [auth])

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-5xl flex-col gap-6 px-4 py-8 md:px-8">
      <header className="rounded-xl border border-amber-400 bg-amber-100/70 p-5 shadow-sm">
        <p className="mb-1 text-sm uppercase tracking-wide text-amber-900">Virtual Wardrobe</p>
        <h1 className="text-3xl font-semibold text-slate-950">Catálogo Pessoal e Wishlist</h1>
        <p className="mt-2 text-slate-700">{authLabel}</p>
      </header>

      <nav className="flex flex-wrap gap-2">
        <ShellLink to="/">Guarda-roupa</ShellLink>
        <ShellLink to="/wishlist">Wishlist</ShellLink>
      </nav>

      <GuardedContent />
    </main>
  )
}

function ShellLink({ to, children }: { to: string; children: string }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        [
          'rounded-md border px-4 py-2 text-sm font-medium transition-colors',
          isActive
            ? 'border-amber-700 bg-amber-700 text-white'
            : 'border-slate-300 bg-white text-slate-800 hover:border-amber-600 hover:text-amber-700',
        ].join(' ')
      }
      end={to === '/'}
    >
      {children}
    </NavLink>
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