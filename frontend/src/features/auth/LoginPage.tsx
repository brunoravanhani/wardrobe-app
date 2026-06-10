import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuthActions, useAuthSession } from '../../app/providers/auth-context'
import { createAuthApi } from '../../services/authApi'

type RedirectState = {
  from?: {
    pathname?: string
  }
}

type GoogleCredentialResponse = {
  credential?: string
}

type GoogleAccountsId = {
  initialize: (config: {
    client_id: string
    callback: (response: GoogleCredentialResponse) => void
  }) => void
  renderButton: (
    parent: HTMLElement,
    options: {
      theme?: 'outline' | 'filled_blue' | 'filled_black'
      size?: 'large' | 'medium' | 'small'
      text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin'
      shape?: 'rectangular' | 'pill' | 'circle' | 'square'
      locale?: string
      width?: number
    },
  ) => void
}

type GoogleIdentityServices = {
  accounts: {
    id: GoogleAccountsId
  }
}

declare global {
  interface Window {
    google?: GoogleIdentityServices
  }
}

const GOOGLE_SCRIPT_ID = 'google-identity-services'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const auth = useAuthSession()
  const { signIn } = useAuthActions()
  const googleButtonRef = useRef<HTMLDivElement | null>(null)
  const googleClientId = (import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '').trim()

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isGoogleLoading, setIsGoogleLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const authApi = useMemo(
    () =>
      createAuthApi({
        baseUrl: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
      }),
    [],
  )

  if (auth.status === 'authenticated') {
    return null
  }

  const completeLoginFromIdToken = useCallback(
    async (token: string) => {
      setIsSubmitting(true)
      setErrorMessage(null)

      try {
        const session = await authApi.exchangeGoogleToken(token)
        signIn({
          accessToken: session.accessToken,
          email: session.user.email,
        })

        const redirectState = location.state as RedirectState | null
        const targetPath = redirectState?.from?.pathname ?? '/'
        navigate(targetPath, { replace: true })
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Falha ao autenticar com a API.'
        setErrorMessage(message)
      } finally {
        setIsSubmitting(false)
      }
    },
    [authApi, location.state, navigate, signIn],
  )

  useEffect(() => {
    if (!googleClientId || !googleButtonRef.current) {
      return
    }

    let isMounted = true

    const initializeGoogleButton = async () => {
      setIsGoogleLoading(true)

      try {
        await loadGoogleIdentityScript()

        if (!isMounted || !googleButtonRef.current || !window.google?.accounts?.id) {
          return
        }

        googleButtonRef.current.innerHTML = ''
        window.google.accounts.id.initialize({
          client_id: googleClientId,
          callback: (response) => {
            const token = response.credential?.trim()

            if (!token) {
              setErrorMessage('Google nao retornou um token valido. Tente novamente.')
              return
            }

            void completeLoginFromIdToken(token)
          },
        })

        window.google.accounts.id.renderButton(googleButtonRef.current, {
          theme: 'outline',
          size: 'large',
          text: 'signin_with',
          shape: 'pill',
          locale: 'pt-BR',
          width: 320,
        })

      } catch {
        if (isMounted) {
          setErrorMessage('Nao foi possivel carregar o Google Sign-In. Tente novamente.')
        }
      } finally {
        if (isMounted) {
          setIsGoogleLoading(false)
        }
      }
    }

    void initializeGoogleButton()

    return () => {
      isMounted = false
    }
  }, [completeLoginFromIdToken, googleClientId])

  return (
    <section className="mx-auto w-full max-w-xl rounded-xl border border-amber-300 bg-white/90 p-6 shadow-sm">
      <h2 className="mb-2 text-2xl font-semibold text-slate-900">Entrar no Virtual Wardrobe</h2>
      <p className="mb-4 text-sm text-slate-700">
        Entre com Google para obter token de sessao e acessar os endpoints autenticados da API.
      </p>

      {googleClientId ? (
        <div className="rounded-md border border-slate-200 bg-slate-50 p-4">
          {isGoogleLoading ? <p className="mb-3 text-sm text-slate-700">Carregando botao do Google...</p> : null}
          <div ref={googleButtonRef} className="min-h-10" aria-live="polite" />
        </div>
      ) : (
        <p className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900">
          VITE_GOOGLE_CLIENT_ID nao configurado. Configure no frontend/.env para habilitar o login.
        </p>
      )}

      {isSubmitting ? <p className="mt-4 text-sm text-slate-700">Autenticando...</p> : null}
      {errorMessage ? <p className="mt-4 text-sm text-red-700">{errorMessage}</p> : null}
    </section>
  )
}

function loadGoogleIdentityScript(): Promise<void> {
  if (window.google?.accounts?.id) {
    return Promise.resolve()
  }

  const existingScript = document.getElementById(GOOGLE_SCRIPT_ID) as HTMLScriptElement | null

  if (existingScript) {
    return new Promise((resolve, reject) => {
      existingScript.addEventListener('load', () => resolve(), { once: true })
      existingScript.addEventListener('error', () => reject(new Error('google_script_load_failed')), { once: true })
    })
  }

  return new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.id = GOOGLE_SCRIPT_ID
    script.src = 'https://accounts.google.com/gsi/client'
    script.async = true
    script.defer = true
    script.onload = () => resolve()
    script.onerror = () => reject(new Error('google_script_load_failed'))
    document.head.appendChild(script)
  })
}
