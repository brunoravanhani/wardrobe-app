import { useRef, type ChangeEvent } from 'react'

const NO_FILE_PLACEHOLDER = 'Nenhum arquivo selecionado'

type ImageFileInputProps = {
  id: string
  label: string
  accept: string
  value: File | null
  onChange: (file: File | null) => void
  name?: string
  error?: string | null
}

export function ImageFileInput({
  id,
  label,
  accept,
  value,
  onChange,
  name,
  error,
}: ImageFileInputProps) {
  const inputRef = useRef<HTMLInputElement>(null)

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    onChange(event.target.files?.[0] ?? null)
  }

  return (
    <div className="flex flex-col gap-1 text-sm font-medium text-slate-800">
      <label htmlFor={id}>{label}</label>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          className="rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-800 hover:bg-stone-50"
        >
          Escolher imagem
        </button>
        <span className="min-w-0 truncate text-sm font-normal text-slate-500">
          {value ? value.name : NO_FILE_PLACEHOLDER}
        </span>
      </div>
      <input
        ref={inputRef}
        id={id}
        name={name}
        type="file"
        accept={accept}
        className="sr-only"
        onChange={handleChange}
      />
      {error ? <span className="text-sm font-normal text-red-700">{error}</span> : null}
    </div>
  )
}
