import { cn } from "~/lib/utils"

interface BrandLogoProps {
  className?: string
  markClassName?: string
  textClassName?: string
  inverted?: boolean
}

export function BrandLogo({
  className,
  markClassName,
  textClassName,
  inverted = false,
}: BrandLogoProps) {
  return (
    <div className={cn("flex items-center gap-2.5", className)}>
      <div
        className={cn(
          "relative flex size-8 shrink-0 items-center justify-center overflow-hidden rounded-lg",
          inverted
            ? "border border-primary-foreground/20 bg-primary-foreground/10"
            : "bg-foreground",
          markClassName
        )}
        aria-hidden="true"
      >
        <span
          className={cn(
            "absolute top-2 bottom-2 left-2 w-1 rounded-full",
            inverted ? "bg-primary-foreground" : "bg-background"
          )}
        />
        <span
          className={cn(
            "absolute top-2 bottom-2 right-2 w-1 rounded-full",
            inverted ? "bg-primary-foreground" : "bg-background"
          )}
        />
        <span
          className={cn(
            "absolute top-1/2 left-2 right-2 h-1 -translate-y-1/2 rounded-full",
            inverted ? "bg-primary-foreground" : "bg-background"
          )}
        />
        <span
          className={cn(
            "absolute right-1.5 bottom-1.5 size-1.5 rounded-full",
            inverted ? "bg-primary-foreground/60" : "bg-background/60"
          )}
        />
      </div>
      <span
        className={cn(
          "font-semibold tracking-tight",
          inverted ? "text-primary-foreground" : "text-foreground",
          textClassName
        )}
      >
        Hollo
      </span>
    </div>
  )
}
