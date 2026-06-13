import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { TemplateProgressBar } from '../../../src/features/wardrobe/components/TemplateProgressBar'

describe('TemplateProgressBar', () => {
  it('shows fulfilled and total count', () => {
    render(<TemplateProgressBar fulfilled={3} total={20} />)
    expect(screen.getByText('3 de 20 pecas adquiridas')).toBeInTheDocument()
  })

  it('shows zero when nothing fulfilled', () => {
    render(<TemplateProgressBar fulfilled={0} total={9} />)
    expect(screen.getByText('0 de 9 pecas adquiridas')).toBeInTheDocument()
  })

  it('has a progressbar role with correct aria attributes', () => {
    render(<TemplateProgressBar fulfilled={5} total={20} />)
    const bar = screen.getByRole('progressbar')
    expect(bar).toHaveAttribute('aria-valuenow', '5')
    expect(bar).toHaveAttribute('aria-valuemax', '20')
  })

  it('shows full progress when all slots fulfilled', () => {
    render(<TemplateProgressBar fulfilled={20} total={20} />)
    expect(screen.getByText('20 de 20 pecas adquiridas')).toBeInTheDocument()
    const bar = screen.getByRole('progressbar')
    expect(bar).toHaveAttribute('aria-valuenow', '20')
  })
})
