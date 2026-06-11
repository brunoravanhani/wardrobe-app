type BrandLogoProps = {
  className?: string
}

/** Clothes-hanger mark used across the app shell and login screen. */
export function HangerIcon({ className }: BrandLogoProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 6.5a1.75 1.75 0 111.4 1.71c-.55.12-.9.62-.9 1.18v.61" />
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12.5 10.6l8.1 5.05c1.04.65.58 2.25-.64 2.25H4.04c-1.22 0-1.68-1.6-.64-2.25l8.1-5.05a1 1 0 011 0z"
      />
    </svg>
  )
}
