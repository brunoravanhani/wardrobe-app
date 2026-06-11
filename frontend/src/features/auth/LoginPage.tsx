import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuthActions, useAuthSession } from '../../app/providers/auth-context'
import { createAuthApi } from '../../services/authApi'
import { HangerIcon } from '../../components/BrandLogo'

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

  if (auth.status === 'authenticated') {
    return null
  }

  return (
    <div className="w-full max-w-md">
      <div className="rounded-2xl border border-stone-200 bg-white p-8 text-center shadow-md">
        <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-amber-50 text-amber-600">
          <HangerIcon className="h-8 w-8" />
        </div>
        <h1 className="text-2xl font-semibold text-slate-900">Guarda-Roupa &amp; Wishlist</h1>
        <p className="mt-2 text-sm text-slate-600">Organize seu guarda-roupa de forma simples e elegante.</p>

        <div className="mt-6">
          {googleClientId ? (
            <div className="flex flex-col items-center gap-3">
              {isGoogleLoading ? <p className="text-sm text-slate-600">Carregando botao do Google...</p> : null}
              <div ref={googleButtonRef} className="flex min-h-10 justify-center" aria-live="polite" />
            </div>
          ) : (
            <p className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900">
              VITE_GOOGLE_CLIENT_ID nao configurado. Configure no frontend/.env para habilitar o login.
            </p>
          )}

          {isSubmitting ? <p className="mt-4 text-sm text-slate-600">Autenticando...</p> : null}
          {errorMessage ? <p className="mt-4 text-sm text-red-700">{errorMessage}</p> : null}
        </div>
      </div>
    </div>
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
