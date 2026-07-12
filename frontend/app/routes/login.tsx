import { useEffect, useState } from "react"
import { useNavigate } from "react-router"
import { Eye, EyeOff, HardDrive, Loader2 } from "lucide-react"

import {
  useCurrentUserQuery,
  useLoginMutation,
  useRegisterMutation,
} from "~/features/auth/queries"
import { ApiError } from "~/lib/api/client"
import { BrandLogo } from "~/components/brand-logo"
import { PairingQr } from "~/components/pairing-qr"
import { Button } from "~/components/ui/button"
import { Input } from "~/components/ui/input"
import { Label } from "~/components/ui/label"
import { cn } from "~/lib/utils"

type AuthMode = "login" | "register"

export default function LoginPage() {
  const navigate = useNavigate()
  const currentUserQuery = useCurrentUserQuery()
  const loginMutation = useLoginMutation()
  const registerMutation = useRegisterMutation()
  const [mode, setMode] = useState<AuthMode>("login")
  const [showPassword, setShowPassword] = useState(false)
  const [name, setName] = useState("")
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [confirmPassword, setConfirmPassword] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const isRegisterMode = mode === "register"
  const isSubmitting = loginMutation.isPending || registerMutation.isPending

  useEffect(() => {
    if (currentUserQuery.data) {
      navigate("/", { replace: true })
    }
  }, [currentUserQuery.data, navigate])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setFormError(null)

    try {
      if (isRegisterMode) {
        if (password !== confirmPassword) {
          setFormError("As senhas não conferem.")
          return
        }

        await registerMutation.mutateAsync({ name, email, password })
      } else {
        await loginMutation.mutateAsync({ email, password })
      }

      navigate("/", { replace: true })
    } catch (error) {
      setFormError(getAuthErrorMessage(error))
    }
  }

  const handleModeChange = () => {
    setMode(isRegisterMode ? "login" : "register")
    setFormError(null)
    setConfirmPassword("")
  }

  return (
    <div className="flex min-h-screen bg-background">
      <div className="relative hidden flex-col justify-between overflow-hidden bg-foreground p-12 lg:flex lg:w-[52%]">
        <div className="absolute inset-0 opacity-5">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage:
                "repeating-linear-gradient(0deg, transparent, transparent 40px, oklch(1 0 0 / 0.5) 40px, oklch(1 0 0 / 0.5) 41px), repeating-linear-gradient(90deg, transparent, transparent 40px, oklch(1 0 0 / 0.5) 40px, oklch(1 0 0 / 0.5) 41px)",
            }}
          />
        </div>

        <BrandLogo
          inverted
          className="relative z-10 gap-3"
          markClassName="size-9"
          textClassName="text-lg"
        />

        <div className="relative z-10 flex flex-col gap-4">
          <div className="rounded-2xl border border-primary-foreground/15 bg-primary-foreground/8 p-6 backdrop-blur-sm">
            <div className="mb-5 flex items-center gap-3">
              <div className="flex size-10 items-center justify-center rounded-xl bg-primary-foreground/15">
                <HardDrive className="size-5 text-primary-foreground/70" />
              </div>
              <div>
                <div className="mb-1.5 h-3 w-28 rounded-full bg-primary-foreground/25" />
                <div className="h-2.5 w-20 rounded-full bg-primary-foreground/15" />
              </div>
              <div className="ml-auto h-7 w-14 rounded-lg bg-primary-foreground/15" />
            </div>
            <div className="grid grid-cols-3 gap-3">
              {Array.from({ length: 6 }).map((_, index) => (
                <div
                  key={index}
                  className="rounded-xl border border-primary-foreground/10 bg-primary-foreground/8 p-3"
                >
                  <div className="mb-2.5 size-8 rounded-lg bg-primary-foreground/15" />
                  <div className="mb-1.5 h-2 w-full rounded-full bg-primary-foreground/20" />
                  <div className="h-2 w-2/3 rounded-full bg-primary-foreground/12" />
                </div>
              ))}
            </div>
          </div>

          <div className="rounded-xl border border-primary-foreground/15 bg-primary-foreground/8 p-4 backdrop-blur-sm">
            <div className="mb-2.5 flex justify-between">
              <div className="h-2.5 w-24 rounded-full bg-primary-foreground/25" />
              <div className="h-2.5 w-16 rounded-full bg-primary-foreground/15" />
            </div>
            <div className="h-1.5 overflow-hidden rounded-full bg-primary-foreground/15">
              <div className="h-full w-[62%] rounded-full bg-primary-foreground/50" />
            </div>
          </div>
        </div>

        <div className="relative z-10">
          <blockquote className="max-w-xs text-sm leading-relaxed text-primary-foreground/60">
            &ldquo;Organize e acesse seus arquivos de qualquer lugar com total
            segurança.&rdquo;
          </blockquote>
        </div>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center px-8 py-12">
        <BrandLogo className="mb-10 lg:hidden" textClassName="text-base" />

        <div className="w-full max-w-sm">
          <div className="mb-8">
            <h1 className="mb-2 text-2xl font-semibold tracking-tight text-foreground">
              {isRegisterMode ? "Crie sua conta" : "Bem-vindo de volta"}
            </h1>
            <p className="text-sm leading-relaxed text-muted-foreground">
              {isRegisterMode
                ? "Cadastre-se para começar a organizar seus arquivos."
                : "Acesse sua conta para continuar gerenciando seus arquivos."}
            </p>
          </div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            {isRegisterMode && (
              <div className="flex flex-col gap-1.5">
                <Label
                  htmlFor="name"
                  className="text-sm font-medium text-foreground"
                >
                  Nome
                </Label>
                <Input
                  id="name"
                  type="text"
                  placeholder="Seu nome"
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  required
                  autoComplete="name"
                  className="h-10 border-border bg-background text-foreground placeholder:text-muted-foreground/60 focus-visible:ring-ring/50"
                />
              </div>
            )}

            <div className="flex flex-col gap-1.5">
              <Label
                htmlFor="email"
                className="text-sm font-medium text-foreground"
              >
                E-mail
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="seu@email.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
                autoComplete="email"
                className="h-10 border-border bg-background text-foreground placeholder:text-muted-foreground/60 focus-visible:ring-ring/50"
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <div className="flex items-center justify-between">
                <Label
                  htmlFor="password"
                  className="text-sm font-medium text-foreground"
                >
                  Senha
                </Label>
                {!isRegisterMode && (
                  <button
                    type="button"
                    className="text-xs text-muted-foreground transition-colors hover:text-foreground"
                  >
                    Esqueceu a senha?
                  </button>
                )}
              </div>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  placeholder="********"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  required
                  minLength={8}
                  autoComplete={
                    isRegisterMode ? "new-password" : "current-password"
                  }
                  className="h-10 border-border bg-background pr-10 text-foreground placeholder:text-muted-foreground/60 focus-visible:ring-ring/50"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute top-1/2 right-3 -translate-y-1/2 text-muted-foreground transition-colors hover:text-foreground"
                  aria-label={showPassword ? "Ocultar senha" : "Mostrar senha"}
                >
                  {showPassword ? (
                    <EyeOff className="size-4" />
                  ) : (
                    <Eye className="size-4" />
                  )}
                </button>
              </div>
            </div>

            {isRegisterMode && (
              <div className="flex flex-col gap-1.5">
                <Label
                  htmlFor="confirmPassword"
                  className="text-sm font-medium text-foreground"
                >
                  Confirmar senha
                </Label>
                <Input
                  id="confirmPassword"
                  type={showPassword ? "text" : "password"}
                  placeholder="********"
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.target.value)}
                  required
                  minLength={8}
                  autoComplete="new-password"
                  className="h-10 border-border bg-background text-foreground placeholder:text-muted-foreground/60 focus-visible:ring-ring/50"
                />
              </div>
            )}

            {formError && (
              <p className="rounded-lg border border-destructive/20 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {formError}
              </p>
            )}

            <Button
              type="submit"
              disabled={isSubmitting}
              className={cn(
                "mt-1 h-10 w-full font-medium",
                "bg-primary text-primary-foreground hover:bg-primary/90",
                "transition-all duration-200"
              )}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 size-4 animate-spin" />
                  {isRegisterMode ? "Criando..." : "Entrando..."}
                </>
              ) : isRegisterMode ? (
                "Criar conta"
              ) : (
                "Entrar"
              )}
            </Button>
          </form>

          <PairingQr />

          <p className="mt-8 text-center text-sm text-muted-foreground">
            {isRegisterMode ? "Já tem uma conta?" : "Não tem uma conta?"}{" "}
            <button
              type="button"
              onClick={handleModeChange}
              className="font-medium text-foreground underline-offset-4 transition-all hover:underline"
            >
              {isRegisterMode ? "Entrar" : "Criar conta"}
            </button>
          </p>
        </div>
      </div>
    </div>
  )
}

function getAuthErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    return error.message
  }

  return "Não foi possível concluir a autenticação."
}
