import { useEffect, useState } from "react"
import { QrCode, X } from "lucide-react"
import QRCode from "qrcode"

import { Button } from "~/components/ui/button"
import { apiRequest } from "~/lib/api/client"

type PairingPayload = {
  version: number
  serverId: string
  name: string
  apiUrl: string
}

export function PairingQr() {
  const [open, setOpen] = useState(false)
  const [image, setImage] = useState<string | null>(null)
  const [payload, setPayload] = useState<PairingPayload | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return

    let active = true
    setError(null)
    apiRequest<PairingPayload>("/server/pairing")
      .then(async (server) => {
        const dataUrl = await QRCode.toDataURL(JSON.stringify(server), {
          width: 320,
          margin: 2,
          color: { dark: "#25231F", light: "#FFFFFF" },
          errorCorrectionLevel: "M",
        })
        if (active) {
          setPayload(server)
          setImage(dataUrl)
        }
      })
      .catch(() => {
        if (active) setError("Não foi possível gerar o código de conexão.")
      })

    return () => {
      active = false
    }
  }, [open])

  if (!open) {
    return (
      <Button
        type="button"
        variant="outline"
        className="mt-4 h-10 w-full"
        onClick={() => setOpen(true)}
      >
        <QrCode className="mr-2 size-4" />
        Conectar aplicativo
      </Button>
    )
  }

  return (
    <div className="mt-4 border border-border bg-card p-4">
      <div className="mb-3 flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-foreground">Conectar aplicativo</p>
          <p className="text-xs text-muted-foreground">Escaneie pelo app Hollo.</p>
        </div>
        <button
          type="button"
          className="p-1 text-muted-foreground hover:text-foreground"
          onClick={() => setOpen(false)}
          aria-label="Fechar"
        >
          <X className="size-4" />
        </button>
      </div>

      {image && payload ? (
        <div className="flex flex-col items-center gap-3">
          <img src={image} alt="QR Code de conexão do servidor Hollo" className="size-56" />
          <div className="w-full min-w-0 text-center">
            <p className="text-sm font-medium text-foreground">{payload.name}</p>
            <p className="truncate text-xs text-muted-foreground">{payload.apiUrl}</p>
          </div>
        </div>
      ) : error ? (
        <p className="text-sm text-destructive">{error}</p>
      ) : (
        <p className="text-sm text-muted-foreground">Gerando código...</p>
      )}
    </div>
  )
}
