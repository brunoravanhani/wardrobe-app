import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { ImageFileInput } from '../../../src/components/ImageFileInput'

function Harness({ error }: { error?: string | null }) {
  const [file, setFile] = useState<File | null>(null)
  return (
    <ImageFileInput
      id="test-image"
      name="test-image"
      label="Foto da peça (JPG, PNG ou WebP)"
      accept="image/jpeg,image/png,image/webp"
      value={file}
      onChange={setFile}
      error={error}
    />
  )
}

describe('ImageFileInput', () => {
  it('renders the styled button and the empty placeholder', () => {
    render(<Harness />)
    expect(screen.getByRole('button', { name: 'Escolher imagem' })).toBeInTheDocument()
    expect(screen.getByText('Nenhum arquivo selecionado')).toBeInTheDocument()
  })

  it('forwards accept and keeps the input associated with its label', () => {
    render(<Harness />)
    const input = screen.getByLabelText('Foto da peça (JPG, PNG ou WebP)') as HTMLInputElement
    expect(input).toHaveAttribute('type', 'file')
    expect(input).toHaveAttribute('accept', 'image/jpeg,image/png,image/webp')
  })

  it('shows the selected filename, replacing the placeholder', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const input = screen.getByLabelText('Foto da peça (JPG, PNG ou WebP)') as HTMLInputElement
    const file = new File(['x'], 'camiseta.png', { type: 'image/png' })
    await user.upload(input, file)

    expect(screen.getByText('camiseta.png')).toBeInTheDocument()
    expect(screen.queryByText('Nenhum arquivo selecionado')).not.toBeInTheDocument()
  })

  it('renders the error message when provided', () => {
    render(<Harness error="Arquivo acima do limite de 10 MB." />)
    expect(screen.getByText('Arquivo acima do limite de 10 MB.')).toBeInTheDocument()
  })
})
